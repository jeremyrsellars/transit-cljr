using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Beerendonk.Transit.Impl;
using Sellars.Transit.Alpha;

namespace Sellars.Transit.Cljr.Impl
{
    /// <summary>
    /// Represents a JSON emitter.
    /// </summary>
    internal class Utf8JsonEmitter : AbstractEmitter
    {
        private static readonly long JsonIntMax = (long)Math.Pow(2, 53);
        private static readonly long JsonIntMin = -JsonIntMax;

        protected readonly Utf8JsonWriter jsonWriter;

        public Utf8JsonEmitter(Utf8JsonWriter jsonWriter, IImmutableDictionary<Type, IWriteHandler> handlers,
            IWriteHandler defaultHandler, Func<object, object> transform)
            : base(handlers, defaultHandler, transform)
        {
            this.jsonWriter = jsonWriter;
        }

        public override void Emit(object obj, bool asDictionaryKey, WriteCache cache)
        {
            MarshalTop(obj, cache);
        }

        public override void EmitNull(bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey)
            {
                EmitString(Constants.EscStr, "_", "", asDictionaryKey, cache);
            }
            else
            {
                jsonWriter.WriteNullValue();
            }
        }

        public override void EmitString(string prefix, string tag, string s, bool asDictionaryKey, WriteCache cache)
        {
            string outString = cache.CacheWrite(Beerendonk.Transit.Impl.Util.MaybePrefix(prefix, tag, s), asDictionaryKey);
            jsonWriter.WriteStringValue(outString);
        }

        public override void EmitBoolean(bool b, bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey)
            {
                EmitString(Constants.EscStr, "?", b ? "t" : "f", asDictionaryKey, cache);
            }
            else
            {
                jsonWriter.WriteBooleanValue(b);
            }
        }

        public override void EmitInteger(object i, bool asDictionaryKey, WriteCache cache)
        {
            EmitInteger(Beerendonk.Transit.Impl.Util.NumberToPrimitiveLong(i), asDictionaryKey, cache);
        }

        public override void EmitInteger(long i, bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey || i > JsonIntMax || i < JsonIntMin)
                EmitString(Constants.EscStr, "i", i.ToString(), asDictionaryKey, cache);
            else
                jsonWriter.WriteNumberValue(i);
        }

        public override void EmitDouble(object d, bool asDictionaryKey, WriteCache cache)
        {
            if (d is double)
                EmitDouble((double)d, asDictionaryKey, cache);
            else if (d is float)
                EmitDouble((float)d, asDictionaryKey, cache);
            else
                throw new TransitException("Unknown double type: " + d.GetType());
        }

        public override void EmitDouble(float d, bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey)
                EmitString(Constants.EscStr, "d", d.ToString(), asDictionaryKey, cache);
            else
                WriteRoundTripableFloatValue(d);
        }

        public override void EmitDouble(double d, bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey)
                EmitString(Constants.EscStr, "d", d.ToString(), asDictionaryKey, cache);
            else
                WriteRoundTripableDoubleValue(d);
        }

        private void WriteRoundTripableDoubleValue(double value)
        {
            // Seek to preserve floating point in serialization format.
            // Note: By default, System.Text.Json will encode 1.0 as `1`
            // (and all other conceptual integers that are lossless in the floating point type),
            // so the receiving end will lose the fact that a floating point number was intended.
            if (value % 1 == 0.0 && value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture).IndexOf('.') < 0)
                jsonWriter.WriteStringValue(value.ToString(@"\~\d0\.\0;\~\d-0\.\0;\~\d0\.\0", System.Globalization.CultureInfo.InvariantCulture));
            else
                jsonWriter.WriteNumberValue(value);
        }

        private void WriteRoundTripableFloatValue(float value)
        {
            // Seek to preserve floating point in serialization format.
            // Note: By default, System.Text.Json will encode 1.0 as `1`
            // (and all other conceptual integers that are lossless in the floating point type),
            // so the receiving end will lose the fact that a floating point number was intended.
            if (value % 1 == 0.0 && value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture).IndexOf('.') < 0)
                jsonWriter.WriteStringValue(value.ToString(@"\~\d0\.\0;\~\d-0\.\0;\~\d0\.\0", System.Globalization.CultureInfo.InvariantCulture));
            else
                jsonWriter.WriteNumberValue(value);
        }

        public override void EmitBinary(object b, bool asDictionaryKey, WriteCache cache)
        {
            EmitString(Constants.EscStr, "b", Convert.ToBase64String((byte[])b), asDictionaryKey, cache);
        }

        public override void EmitListStart(long size)
        {
            jsonWriter.WriteStartArray();
        }

        public override void EmitListEnd()
        {
            jsonWriter.WriteEndArray();
        }

        public override void EmitDictionaryStart(long size)
        {
            jsonWriter.WriteStartObject();
        }

        public override void EmitDictionaryEnd()
        {
            jsonWriter.WriteEndObject();
        }

        public override void FlushWriter()
        {
            jsonWriter.Flush();
        }

        public override bool PrefersStrings()
        {
            return true;
        }

        protected override void EmitDictionary(IEnumerable<KeyValuePair<object, object>> keyValuePairs,
            bool ignored, WriteCache cache)
        {
            long sz = Enumerable.Count(keyValuePairs);

            EmitListStart(sz);
            EmitString(null, null, Constants.DirectoryAsList, false, cache);

            foreach (var kvp in keyValuePairs)
            {
                Marshal(kvp.Key, true, cache);
                Marshal(kvp.Value, false, cache);
            }

            EmitListEnd();
        }
    }
}
