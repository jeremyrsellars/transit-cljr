using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
        const int bytesSize = 1024;
#if DEBUG
        const int iterations = 1000000;
#else
        const int iterations = 1000000;
#endif
        private const int InitCapacity = iterations * (200 + bytesSize * 3 / 2); // bytes per iteration
        static System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        static int ctr;
        static TimeSpan prevReport = stopwatch.Elapsed;
        static string nextMessage;

        static void Inc(string message = null, int? per = default)
        {
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

        static void Main(string[] args)
        {
            Inc("Init: " + typeof(SUTTransitFactory));
            using var stream = new MemoryStream(InitCapacity);
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
                TestWrite1(format, stream, val);
                per = iterations;

                Inc(nameof(TestRead1) + ":" + format + ":" + stream.Position, per: per);
                stream.SetLength(stream.Position);
                stream.Position = 0;
                TestRead1(format, stream);
                per = iterations;

                stream.Position = 0;
                stream.SetLength(stream.Position);
                Inc(nameof(TestWriteMany) + ":" + format, per: per);
                TestWriteMany(format, stream, new[] { val });
                per = iterations;

                Inc(nameof(TestReadMany) + ":" + format + ":" + stream.Position, per: per);
                stream.SetLength(stream.Position);
                stream.Position = 0;
                TestReadMany(format, stream);
            }

            Inc("Done:" + stream.Position, per: per);


            stream.Position = 0;
            Inc(nameof(TestJSWrite1) + ":" + "S.T.Json");
            TestJSWrite1(stream, val);

            Inc(nameof(TestJSRead1) + ":" + "S.T.Json" + ":" + stream.Position, per: per);
            stream.SetLength(stream.Position);
            stream.Position = 0;
            TestJSRead1<MyNamedValue[]>(stream);

            stream.Position = 0;
            Inc(nameof(TestJSWriteMany) + ":" + "S.T.Json", per: per);
            TestJSWriteMany(stream, val);

            Inc(nameof(TestJSReadMany) + ":" + "S.T.Json" + ":" + stream.Position, per: per);
            stream.SetLength(stream.Position);
            stream.Position = 0;
            TestJSReadMany<MyNamedValue>(stream);

            Inc("Done:" + stream.Position, per: per);

            if (stream.Capacity > InitCapacity)
                Console.Error.WriteLine($"The stream grew from {InitCapacity} to {stream.Capacity}");
        }

        static void TestWrite1<T>(TransitFactory.Format format, Stream stream, T val)
        {
            var writer = SUTTransitFactory.Writer<IEnumerable<T>>(format, stream, TransitWriters, null, null);
            writer.Write(Enumerable.Repeat(val, iterations));
        }

        static void TestRead1(TransitFactory.Format format, Stream stream)
        {
            var writer = SUTTransitFactory.Reader(format, stream, TransitReaders, null);
            var x = writer.Read();
        }

        static void TestWriteMany(TransitFactory.Format format, Stream stream, IList val)
        {
            var writer = SUTTransitFactory.Writer<IList>(format, stream, TransitWriters, null, null);
            for (int ct = iterations; ct > 0; ct--)
                writer.Write(val);
        }

        static void TestReadMany(TransitFactory.Format format, Stream stream)
        {
            var writer = SUTTransitFactory.Reader(format, stream, TransitReaders, null);
            for (int ct = iterations; ct > 0; ct--)
                writer.Read();
        }

        static void TestJSWrite1<T>(MemoryStream stream, T val)
        {
            JsonSerializerOptions options = GetJsonSerializerOptions();
            JsonSerializer.Serialize(new Utf8JsonWriter(stream), Enumerable.Repeat(val, iterations), options);
        }

        static void TestJSRead1<T>(MemoryStream stream)
        {
            //var x = JsonSerializer.Deserialize<T>(stream.ToArray().AsSpan(), GetJsonSerializerOptions());
            var x = JsonSerializer.DeserializeAsync<T>(stream, GetJsonSerializerOptions()).Result;
        }

        static void TestJSWriteMany<T>(MemoryStream stream, T val)
        {
            JsonSerializerOptions options = GetJsonSerializerOptions();
            for (int i = 0; i < iterations; i++)
            {
                stream.Position = 0;
                JsonSerializer.Serialize(new Utf8JsonWriter(stream), val, options);
            }
        }

        static void TestJSReadMany<T>(MemoryStream stream)
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
