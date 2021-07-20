using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sellars.Transit.Alpha;

namespace TimeExecution
{
    public record MyVersion(int Major, int Minor, int Build, int Revision);

    public class MyVersionWriteHandler : IWriteHandler
    {
        public IWriteHandler GetVerboseHandler() => null;

        public object Representation(object obj)
        {
            var v = (MyVersion)obj;
            return new[] { v.Major, v.Minor, v.Build, v.Revision };
        }

        public string StringRepresentation(object obj) => obj.ToString();

        public string Tag(object obj) => nameof(MyVersion);
    }

    public class MyVersionReadHandler : IReadHandler
    {
        public object FromRepresentation(object representation)
        {
            var parts = (IImmutableList<object>)representation;
            return new MyVersion(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[2]), Convert.ToInt32(parts[3]));
        }

        public string Tag(object obj) => nameof(MyVersion);
    }

    public class MyVersionJsonConverter : JsonConverter<MyVersion>
    {
        public override MyVersion Read(ref Utf8JsonReader rdr, Type typeToConvert, JsonSerializerOptions options)
        {
            static int Read(ref Utf8JsonReader reader)
            {
                var i = reader.GetInt32();
                reader.Read();
                return i;
            }
            rdr.Read(); // StartArray
            var v = new MyVersion(Read(ref rdr), Read(ref rdr), Read(ref rdr), Read(ref rdr));
            return v;
        }

        public override void Write(Utf8JsonWriter writer, MyVersion v, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new[] { v.Major, v.Minor, v.Build, v.Revision }, options);
    }
}
