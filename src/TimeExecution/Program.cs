using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using clojure.lang;
using Sellars.Transit.Alpha;

namespace TimeExecution
{
    using SUTTransitFactory = Utf8TransitFactory;
    //using SUTTransitFactory = TransitFactory;
    class Program
    {
        const bool skipBrotli = true;

        const int bytesSize = 1024;
#if DEBUG
        const int defIterations = 1000000;
#else
        const int defIterations = 1000000;
#endif
        private const int InitCapacity = defIterations * (200 + bytesSize * 3 / 2); // bytes per iteration
        static System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        static int ctr;
        static TimeSpan prevReport = stopwatch.Elapsed;
        static string nextMessage;

        static void Inc(string message = null, int? per = default, int forceGC = 2)
        {
            GC.Collect(forceGC);
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine(string.Join("\t", new string[] {
                ctr++.ToString(),
                FormatTimeSpan(elapsed),
                FormatTimeSpan(elapsed - prevReport),
                per is int p ? FormatTimeSpan((elapsed - prevReport) / p) : "          ",
                nextMessage,
                "-" + message
                }));
            prevReport = elapsed;
            nextMessage = message;
        }

        private static string FormatTimeSpan(TimeSpan ts, int minLength = 10)
        {
            //if (ts.TotalMinutes >= 1)
            //    return ts.ToString("c");
            if (ts.TotalSeconds >= 1)
                return PadLeft(ts.TotalSeconds.ToString(@"0.000\ \s\ \ "));
            if (ts.TotalMilliseconds >= 1)
                return PadLeft(ts.TotalMilliseconds.ToString(@"0.000\m\s\ "));
            return PadLeft((ts.TotalMilliseconds*1000.0).ToString(@"0.000\μ\s"));

            string PadLeft(string s) =>
                s.Length >= minLength ? s : new string(' ', minLength - s.Length) + s;
        }

        private static (long Size, int Iterations, object Val, bool alsoJson) DefaultValEtc()
        {
            var bytes = Enumerable.Range(0, bytesSize).Select(i => unchecked((byte)i)).ToArray();
            var val =
                //new ArrayList
                //{
                //    1,
                //    "2",
                //    3.1f,
                //    4.2d,
                //    new int[]{1,2,3,4},
                //    TransitFactory.Keyword("myKeyword"),
                //    TransitFactory.Symbol("mySymbol"),
                //    DateTime.Now,
                //    new MyVersion(1,2,3,4),
                //    //new MyNamedValue("An example", Math.PI, new MyVersion(1,2,3,4)),
                //}
                // new MyVersion(1, 2, 3, 4)
                new MyNamedValue("An example", Math.PI, new MyVersion(1, 2, 3, 4), "I'm some meta", bytes)
                ///new[] { new MyVersion(1, 2, 3, 4), new MyVersion(10, 2, 3, 4), new MyVersion(100, 2, 3, 4), new MyVersion(1000, 2, 3, 4) }
                ;
            return (bytesSize + 50, defIterations, val, true);
        }

        static void Main(string[] args)
        {
            Inc("Init: " + typeof(SUTTransitFactory));
            var (size, iterations, val, alsoJson) = ReadValFromArgsOrDefault(args);
            using var stream = new MemoryStream(InitCapacity);

            Inc();
            int? per = default;
            foreach (var format in new[] {
//#if !DEBUG
                TransitFactory.Format.JsonVerbose, 
//#endif
                TransitFactory.Format.Json,
#if !DEBUG
                //TransitFactory.Format.MsgPack,
#endif
            })
            {
                stream.Position = 0;
                stream.SetLength(stream.Position);
                Inc(nameof(TestWrite1) + ":" + format, per: per); // wait to initialize `per` until we've done that many.
                TestWrite1(format, stream, val, iterations);
                per = iterations;

                MaybeBrotliEncodeDecode(stream, per);

                Inc(nameof(TestRead1) + ":" + format + ":" + stream.Position, per: per);
                stream.SetLength(stream.Position);
                stream.Position = 0;
                TestRead1(format, stream);
                per = iterations;

                stream.Position = 0;
                stream.SetLength(stream.Position);
                Inc(nameof(TestWriteMany) + ":" + format, per: per);
                TestWriteMany(format, stream, new[] { val }, iterations);
                per = iterations;

                MaybeBrotliEncodeDecode(stream, per);

                Inc(nameof(TestReadMany) + ":" + format + ":" + stream.Position, per: per);
                stream.SetLength(stream.Position);
                stream.Position = 0;
                TestReadMany(format, stream, iterations);
            }

            Inc("Done:" + stream.Position, per: per);

            if(alsoJson)
            {
                stream.Position = 0;
                Inc(nameof(TestJSWrite1) + ":" + "S.T.Json");
                TestJSWrite1(stream, val, iterations);

                MaybeBrotliEncodeDecode(stream, per);

                Inc(nameof(TestJSRead1) + ":" + "S.T.Json" + ":" + stream.Position, per: per);
                stream.SetLength(stream.Position);
                stream.Position = 0;
                TestJSRead1<MyNamedValue[]>(stream);

                stream.Position = 0;
                Inc(nameof(TestJSWriteMany) + ":" + "S.T.Json", per: per);
                TestJSWriteMany(stream, val, iterations);

                MaybeBrotliEncodeDecode(stream, per);

                Inc(nameof(TestJSReadMany) + ":" + "S.T.Json" + ":" + stream.Position, per: per);
                stream.SetLength(stream.Position);
                stream.Position = 0;
                TestJSReadMany<MyNamedValue>(stream, iterations);

                Inc("Done:" + stream.Position, per: per);
            }

            if (stream.Capacity > InitCapacity)
                Console.Error.WriteLine($"The stream grew from {InitCapacity} to {stream.Capacity}");
        }

        private static (long Size, int Iterations, object Val, bool alsoJson) ReadValFromArgsOrDefault(string[] args)
        {
            if (args.Length == 0)
                return DefaultValEtc();

            if (args.Length == 1)
                return ReadValFromFile(args[0]);

            return args.Select(ReadValFromFile).Aggregate(
                (Size: 0L, Iterations:1, List: ImmutableList<object>.Empty, false),
                (a, t) => (a.Size + t.Size, CalcIterations(a.Size + t.Size), a.List.Add(t.Val), false));
        }

        private static (long Size, int Iterations, object Val, bool alsoJson) ReadValFromFile(string filename)
        {
            var size = new FileInfo(filename).Length;
            using var stream = File.OpenRead(filename);
            return (
                size,
                CalcIterations(size),
                TransitFactory.Reader(TransitFactory.Format.Json, stream).Read(),
                false);
        }

        private static int CalcIterations(long size) =>
            (int)Math.Ceiling(600000000.0 / size);

        private static void MaybeBrotliEncodeDecode(MemoryStream stream, int? per)
        {
            if (skipBrotli) return;
            const int HundredBaseTBytesSecond = 100 * 1024 * 1024 / 8;
            var position = stream.Position;
            var size = position;
            stream.Position = 0;
            using var intermediateEnc = new MemoryStream();
            using var encStream = new BrotliStream(intermediateEnc, CompressionLevel.NoCompression, true);
            stream.CopyTo(encStream);
            encStream.Flush();
            var compressedSize = intermediateEnc.Position;
            System.Threading.Thread.Sleep((int)(intermediateEnc.Position / (HundredBaseTBytesSecond/1000L)));
            intermediateEnc.Position = 0;
            //using var intermediate = new MemoryStream();
            using var decStream = new BrotliStream(intermediateEnc, CompressionMode.Decompress, true);
            //stream.CopyTo(encStream);
            var rdr = new StreamReader(decStream);
            var content = rdr.ReadToEnd();
            Inc($"Brotli encode({compressedSize}b)/decode({size})", per, forceGC: 0);
            stream.Position = position;
        }

        static string PeekStream(MemoryStream stream)
        {
            var p = stream.Position;
            var s = new StreamReader(stream).ReadToEnd();
            stream.Position = p;
            return s;
        }

        static void TestWrite1<T>(TransitFactory.Format format, Stream stream, T val, int iterations)
        {
            var writer = SUTTransitFactory.Writer<IEnumerable<T>>(format, stream, TransitWriters, null, null);
            writer.Write(Enumerable.Repeat(val, iterations));
        }

        static void TestRead1(TransitFactory.Format format, Stream stream)
        {
            var writer = SUTTransitFactory.Reader(format, stream, TransitReaders, null);
            var x = writer.Read();
        }

        static void TestWriteMany(TransitFactory.Format format, Stream stream, IList val, int iterations)
        {
            var writer = SUTTransitFactory.Writer<IList>(format, stream, TransitWriters, null, null);
            for (int ct = iterations; ct > 0; ct--)
                writer.Write(val);
        }

        static void TestReadMany(TransitFactory.Format format, Stream stream, int iterations)
        {
            var writer = SUTTransitFactory.Reader(format, stream, TransitReaders, null);
            for (int ct = iterations; ct > 0; ct--)
                writer.Read();
        }

        static void TestJSWrite1<T>(MemoryStream stream, T val, int iterations)
        {
            JsonSerializerOptions options = GetJsonSerializerOptions();
            JsonSerializer.Serialize(new Utf8JsonWriter(stream), Enumerable.Repeat(val, iterations), options);
        }

        static void TestJSRead1<T>(MemoryStream stream)
        {
            //var x = JsonSerializer.Deserialize<T>(stream.ToArray().AsSpan(), GetJsonSerializerOptions());
            var x = JsonSerializer.DeserializeAsync<T>(stream, GetJsonSerializerOptions()).Result;
        }

        static void TestJSWriteMany<T>(MemoryStream stream, T val, int iterations)
        {
            JsonSerializerOptions options = GetJsonSerializerOptions();
            for (int i = 0; i < iterations; i++)
            {
                stream.Position = 0;
                JsonSerializer.Serialize(new Utf8JsonWriter(stream), val, options);
            }
        }

        static void TestJSReadMany<T>(MemoryStream stream, int iterations)
        {
            JsonSerializerOptions options = GetJsonSerializerOptions();
            for (int i = 0; i < iterations; i++)
            {
                stream.Position = 0;
                var x = JsonSerializer.DeserializeAsync<T>(stream, options).Result;
            }
        }

        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions();
            //options.Converters.Add(new KeywordJsonConverter());
            options.Converters.Add(new MyVersionJsonConverter());
            options.Converters.Add(new MyNamedValueJsonConverter());
            return options;
        }

        public static ImmutableDictionary<string, IReadHandler> TransitReaders = ImmutableDictionary<string, IReadHandler>.Empty
            .Add(nameof(MyVersion), new MyVersionReadHandler())
            .Add(nameof(MyNamedValue), new MyNamedValueReadHandler());

        public static Dictionary<Type, IWriteHandler> TransitWriters =
                new Dictionary<Type, IWriteHandler> {
                    { typeof(MyVersion), new MyVersionWriteHandler() },
                    { typeof(MyNamedValue), new MyNamedValueWriteHandler() },
                };
    }

    public class KeywordJsonConverter : JsonConverter<Keyword>
    {
        public override Keyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Keyword.intern(reader.GetString());

        public override void Write(Utf8JsonWriter writer, Keyword value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }

}
