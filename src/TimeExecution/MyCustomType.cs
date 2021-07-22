using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sellars.Transit.Alpha;

namespace TimeExecution
{
    public record MyNamedValue(string ValueName, double Value, MyVersion Version, string Metadata, byte[] Bytes);

    public class MyNamedValueWriteHandler : IWriteHandler
    {
        public IWriteHandler GetVerboseHandler() => null;

        public object Representation(object obj)
        {
            var v = (MyNamedValue)obj;
            return new Hashtable{
                {nameof(v.ValueName), v.ValueName },
                {nameof(v.Value), v.Value },
                {nameof(v.Version), v.Version },
                {nameof(v.Metadata), v.Metadata },
                {nameof(v.Bytes), v.Bytes },
            };
        }

        public string StringRepresentation(object obj) => obj.ToString();

        public string Tag(object obj) => nameof(MyNamedValue);
    }

    public class MyNamedValueReadHandler : IReadHandler
    {
        public object FromRepresentation(object representation)
        {
            var parts = (IDictionary)representation;
            return new MyNamedValue(
                (string)parts[nameof(MyNamedValue.ValueName)],
                Convert.ToDouble(parts[nameof(MyNamedValue.Value)]),
                (MyVersion)parts[nameof(MyNamedValue.Version)],
                (string)parts[nameof(MyNamedValue.Metadata)],
                (byte[])parts[nameof(MyNamedValue.Bytes)]);
        }

        public string Tag(object obj) => nameof(MyNamedValue);
    }

    public class MyNamedValueJsonConverter : JsonConverter<MyNamedValue>
    {
        public override MyNamedValue Read(ref Utf8JsonReader rdr, Type typeToConvert, JsonSerializerOptions options)
        {
            rdr.Read(); // StartObject
            Hashtable dict = new Hashtable();
            while (rdr.TokenType != JsonTokenType.EndObject)
            {
                var prop = rdr.GetString();
                rdr.Read();
                object val =
                (rdr.TokenType) switch {
                    JsonTokenType.String => 
                        prop switch
                        {
                            nameof(MyNamedValue.Bytes) => rdr.GetBytesFromBase64(),
                            _ => rdr.GetString(),
                        },
                    JsonTokenType.Number => rdr.GetDouble(),
                    JsonTokenType.StartArray when prop == nameof(MyNamedValue.Version) =>
                        JsonSerializer.Deserialize<MyVersion>(ref rdr, options),
                    _ => throw new NotSupportedException(prop+"@"+ rdr.TokenType),
                };
                dict[prop] = val;
                rdr.Read();
            }

            return new MyNamedValue(
                (string)dict[nameof(MyNamedValue.ValueName)], 
                (double)dict[nameof(MyNamedValue.Value)], 
                (MyVersion)dict[nameof(MyNamedValue.Version)], 
                (string)dict[nameof(MyNamedValue.Metadata)],
                (byte[])dict[nameof(MyNamedValue.Bytes)]);
        }

        public override void Write(Utf8JsonWriter writer, MyNamedValue v, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new Hashtable{
                {nameof(v.ValueName), v.ValueName },
                {nameof(v.Value), v.Value },
                {nameof(v.Version), v.Version },
                {nameof(v.Metadata), v.Metadata },
            }, options);
    }

    public class MyNamedValueJsonConverterAdvanced : JsonConverter<MyNamedValue>
    {
        public override MyNamedValue Read(ref Utf8JsonReader rdr, Type typeToConvert, JsonSerializerOptions options)
        {
            var (Name, Value, Version, Meta, Bytes) = new MyNamedValue(default, default, default, default, default);
            rdr.Read(); // StartObject
            while (rdr.TokenType != JsonTokenType.EndObject)
            {
                //var prop = rdr.GetString();
                if (rdr.ValueTextEquals(nameof(MyNamedValue.ValueName)))
                {
                    rdr.Read();
                    Name = rdr.GetString();
                }
                else if (rdr.ValueTextEquals(nameof(MyNamedValue.Value)))
                {
                    rdr.Read();
                    Value = rdr.GetDouble();
                }
                else if (rdr.ValueTextEquals(nameof(MyNamedValue.Version)))
                {
                    rdr.Read();
                    Version = JsonSerializer.Deserialize<MyVersion>(ref rdr, options);
                }
                else if (rdr.ValueTextEquals(nameof(MyNamedValue.Metadata)))
                {
                    rdr.Read();
                    Meta = rdr.GetString();
                }
                else if (rdr.ValueTextEquals(nameof(MyNamedValue.Bytes)))
                {
                    rdr.Read();
                    Bytes = rdr.GetBytesFromBase64();
                }
                rdr.Read();
            }

            return new MyNamedValue(Name, Value, Version, Meta, Bytes);
        }

        public override void Write(Utf8JsonWriter writer, MyNamedValue v, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new Hashtable{
                {nameof(v.ValueName), v.ValueName },
                {nameof(v.Value), v.Value },
                {nameof(v.Version), v.Version },
                {nameof(v.Metadata), v.Metadata },
            }, options);
    }
}
