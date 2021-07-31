using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using clojure.lang;
using Sellars.Transit.Alpha;
using TimeExecution.In;
using TimeExecution.Out;

namespace TimeExecution
{
    using Format = Sellars.Transit.Alpha.TransitFactory.Format;
    using DefaultTransitFactory = TransitFactory;
    static class Program
    {
        const bool skipBrotli = true;

        const int bytesSize = 1024;
#if DEBUG
        const int defIterations = 100;//0000;
#else
        const int defIterations = 1000000;
#endif
        private const int InitCapacity = defIterations * (200 + bytesSize * 3 / 2); // bytes per iteration
        static System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        static int ctr;
        static TimeSpan prevReport = stopwatch.Elapsed;
        static string nextMessage;

        static void Inc(string message = null, int? per = default, int forceGC = 2, bool silent = false)
        {
            GC.Collect(forceGC);
            var elapsed = stopwatch.Elapsed;
            if(!silent)
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

        private static string FormatTimeSpan(TimeSpan ts, int minLength = 11)
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
            //return (16000, defIterations / 100, Enumerable.Range(1000, 1000).ToDictionary(i => (double)i * 10000, i => (double)i * 10000), false);
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

        static void DoRune()
        {
            // As of this writing, there aren't currently any 4-UTF8-byte unicode characters
            // that contain the UTF-8 '\\' character used to initiate a JSON string escape sequence.
            // This seems like a good thing.

            for (char c = (char)0; c < char.MaxValue; c++)
            {
                if (c == '\\')
                    continue;
                var bytes = System.Text.Encoding.UTF8.GetBytes(c.ToString());
                for (int b = 0; b < bytes.Length; b++)
                    if (bytes[b] == (byte)'\\')
                        Console.WriteLine(c);
            }
            Span<byte> buff = stackalloc byte[4];
            for (uint i = 0; i < uint.MaxValue; i++)
            {
                if (i == (uint)(byte)'\\')
                    continue;
                if (!System.Text.Rune.TryCreate(i, out var rune))
                    continue;
                if (!rune.TryEncodeToUtf8(buff, out var bytesWritten))
                    continue;
                for (int b = 0; b < 4; b++)
                    if (buff[b] == (byte)'\\')
                        Console.WriteLine(rune);
            }
        }

        static void Main(string[] args)
        {
            ReadValFromArgsOrDefault(args); // warmup.
            Inc("Init");
            var (size, iterations, val, alsoJson) = ReadValFromArgsOrDefault(args);
            Inc($"Initializing MemoryStream {InitCapacity}");
            using var stream = new MemoryStream(InitCapacity);

            // Warmup without output.
            var @out = Console.Out;
            Console.SetOut(new StringWriter());
            Run(size, 10, val, alsoJson, stream); // warmup
            Console.SetOut(@out);

            Run(size, iterations, val, alsoJson, stream);
        }

        static void Run(long size, int iterations, object val, bool alsoJson, MemoryStream stream)
        {
            //ReadValFromArgsOrDefault(args); // warmup.
            //Inc("Init");
            //var (size, iterations, val, alsoJson) = ReadValFromArgsOrDefault(args);
            //Inc($"Initializing MemoryStream {InitCapacity}");
            //using var stream = new MemoryStream(InitCapacity);

            Inc();
            int? per = default;
            var formats = new[] {
                Format.JsonVerbose,
                Format.Json,
#if !DEBUG
                //TransitFactory.Format.MsgPack,
#endif
            };
            var singleMessage = Enumerable.Repeat(val, iterations);
            var OutputDestinations = new IOutputDestination<object>[] {
                new StreamDestination<object>
                {
                    CreateEnumerableWriter = TransitFactory.Writer<IEnumerable<object>>,
                    CreateWriter = TransitFactory.Writer<object>,
                    CustomHandlers = TransitWriters,
                },
                //new PipeWriterOutputDestination<object>
                //{
                //    CreateEnumerableWriter = TransitFactory.Writer<IEnumerable<object>>,
                //    CreateWriter = TransitFactory.Writer<object>,
                //    CustomHandlers = TransitWriters,
                //},
                new StreamDestination<object>
                {
                    CreateEnumerableWriter = Utf8TransitFactory.Writer<IEnumerable<object>>,
                    CreateWriter = Utf8TransitFactory.Writer<object>,
                    CustomHandlers = TransitWriters,
                },
                new PipeWtrDestination<object>
                {
                    CreateEnumerableWriter = Utf8TransitFactory.Writer<IEnumerable<object>>,
                    CreateWriter = Utf8TransitFactory.Writer<object>,
                    CustomHandlers = TransitWriters,
                },
            };
            var InputSources = new IInputSource<object>[] {
                new StreamSource<object>
                {
                    CreateReader = TransitFactory.Reader,
                    CustomHandlers = TransitReaders,
                },
                //new PipeReaderInputSource<object>
                //{
                //    CreateReader = TransitFactory.Reader,
                //    CustomHandlers = TransitReaders,
                //},
                new StreamSource<object>
                {
                    CreateReader = Utf8TransitFactory.Reader,
                    CustomHandlers = TransitReaders,
                },
                new PipeRdrSource<object>
                {
                    CreateReader = Utf8TransitFactory.Reader,
                    CustomHandlers = TransitReaders,
                },
                new ByteArraySource<object>
                {
                    CreateReader = Utf8TransitFactory.Reader,
                    CustomHandlers = TransitReaders,
                },
            };
            foreach (var format in formats)
            {
                foreach (var destination in OutputDestinations)
                {
                    stream.Position = 0;
                    stream.SetLength(stream.Position);
                    var dst = InitDestination(destination);
                    dst.Type = format;
                    Inc(dst.Describe() + ":" + nameof(dst.WriteAtOnce) + ":" + format, silent: true); // wait to initialize `per` until we've done that many.
                    dst.WriteAtOnce();
                    Inc("Done: @" + stream.Position, per: iterations);
                    per = iterations;
                }

                MaybeBrotliEncodeDecode(stream, per);

                foreach (var source in InputSources)
                {
                    stream.Position = 0;
                    var dst = InitSource(source);
                    dst.Type = format;
                    Inc(dst.Describe() + ":" + nameof(dst.ReadAtOnce) + ":" + format, silent: true); // wait to initialize `per` until we've done that many.
                    var list = (IList)dst.ReadAtOnce();
                    Inc("Done: @" + stream.Position, per: iterations);
                    per = iterations;
                    AssertAreEqual(val, list[0]);
                    AssertAreEqual(val, list[iterations - 1]);
                }

                foreach (var destination in OutputDestinations)
                {
                    stream.Position = 0;
                    stream.SetLength(stream.Position);
                    var dst = InitDestination(destination);
                    dst.Type = format;
                    Inc(dst.Describe() + ":" + nameof(dst.WriteStreaming) + ":" + format, silent: true); // wait to initialize `per` until we've done that many.
                    dst.WriteStreaming();
                    Inc("Done: @" + stream.Position, per: iterations);
                    per = iterations;
                }

                MaybeBrotliEncodeDecode(stream, per);

                foreach (var source in InputSources)
                {
                    stream.Position = 0;
                    var dst = InitSource(source);
                    dst.Type = format;
                    Inc(dst.Describe() + ":" + nameof(dst.ReadStreaming) + ":" + format, silent: true); // wait to initialize `per` until we've done that many.
                    per = iterations;
                    var last = dst.ReadStreaming();
                    Inc("Done: @" + stream.Position, per: iterations);
                    AssertAreEqual(val, last);
                }

                Inc("Done: @" + stream.Position);
            }

            if (alsoJson)
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

                Inc("Done: @" + stream.Position, per: per);
            }

            if (stream.Capacity > InitCapacity)
                Console.Error.WriteLine($"The stream grew from {InitCapacity} to {stream.Capacity}");

            IOutputDestination<object> InitDestination(IOutputDestination<object> destination)
            {
                destination.SetStream(stream);
                destination.DefaultHandler = null;
                destination.Iterations = iterations;
                destination.Transform = null;
                destination.Value = val;
                return destination;
            }
            IInputSource<object> InitSource(IInputSource<object> source)
            {
                source.SetStream(stream);
                source.DefaultHandler = null;
                source.Iterations = iterations;
                source.Value = val;
                return source;
            }
        }

        [Conditional("DEBUG")]
        private static void AssertAreEqual<T>(T expected, object actual)
        {
            if (actual == null)
                throw new Exception("Actual is null");

            //if (expected.Equals((T)actual))
            //    return;
            //var message = $"Not equal. Expected {expected}. Actual: {actual}";
            //Console.Error.WriteLine(message);
        }

        private static (long Size, int Iterations, object Val, bool alsoJson) ReadValFromArgsOrDefault(string[] args)
        {
            if (args.Length == 0)
                return DefaultValEtc();

            if (args.Length == 1)
                return ReadValFromFile(args[0]);

            return args.AsParallel().Select(ReadValFromFile).Aggregate(
                (Size: 0L, Iterations:1, List: ImmutableList<object>.Empty, false),
                (a, t) => (a.Size + t.Size, CalcIterations(a.Size + t.Size), a.List.Add(t.Val), false));
        }

        private static (long Size, int Iterations, object Val, bool alsoJson) ReadValFromFile(string filename)
        {
            if (Directory.Exists(filename))
                return ReadValFromArgsOrDefault(Directory.GetFiles(filename, "*.json"));

            var fileInfo = new FileInfo(filename);
            var size = fileInfo.Length;
            using var stream = File.OpenRead(filename);
            return (
                size,
                CalcIterations(size),
                DefaultTransitFactory.Reader(TransitFactory.Format.Json, stream).Read(),
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

        static void TestWriteMany<T, TOut>(
            TransitFactory.Format format,
            Stream stream, Func<T, Stream, IOutputDestination<T, TOut>> createOutput,
            T val, int iterations)
            where T : IList
        {
            var x = createOutput(val, stream);
            var writer = x.CreateWriter(format, x.Output, TransitWriters, null, null);
            for (int ct = iterations; ct > 0; ct--)
                writer.Write(val);
        }

        static object TestReadMany<T, TIn>(
            TransitFactory.Format format,
            Stream stream, Func<T, Stream, IInputSource<T, TIn>> createInput,
            T val, int iterations)
            where T : IList
        {
            var x = createInput(val, stream);
            var reader = x.CreateReader(format, x.Input, TransitReaders, null);
            object o = null;
            for (int ct = iterations; ct > 0; ct--)
                o = reader.Read();
            return o;
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

        internal static PipeWriter ToPipeWriter(this Stream stream) =>
            PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

        internal static PipeReader ToPipeReader(this Stream stream) =>
            PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
    }

    public class KeywordJsonConverter : JsonConverter<Keyword>
    {
        public override Keyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Keyword.intern(reader.GetString());

        public override void Write(Utf8JsonWriter writer, Keyword value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
