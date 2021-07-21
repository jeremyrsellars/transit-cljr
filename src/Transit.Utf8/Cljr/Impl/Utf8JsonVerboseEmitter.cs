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
    /// Represents a JSON verbose emitter.
    /// </summary>
    internal class Utf8JsonVerboseEmitter : Utf8JsonEmitter
    {
        public Utf8JsonVerboseEmitter(Utf8JsonWriter jsonWriter, IImmutableDictionary<Type, IWriteHandler> handlers,
            IWriteHandler defaultHandler, Func<object, object> transform)
            : base(jsonWriter, handlers, defaultHandler, transform)
        {
        }

        public override void EmitString(string prefix, string tag, string s, bool asDictionaryKey, WriteCache cache)
        {
            string outString = cache.CacheWrite(Beerendonk.Transit.Impl.Util.MaybePrefix(prefix, tag, s), asDictionaryKey);
            if (asDictionaryKey)
                jsonWriter.WritePropertyName(outString);
            else
                jsonWriter.WriteStringValue(outString);
        }

        protected override void EmitTagged(string t, object obj, bool ignored, WriteCache cache)
        {
            EmitDictionaryStart(1L);
            EmitString(Constants.EscTag, t, "", true, cache);
            Marshal(obj, false, cache);
            EmitDictionaryEnd();
        }

        protected override void EmitDictionary(IEnumerable<KeyValuePair<object, object>> keyValuePairs,
            bool ignored, WriteCache cache)
        {
            EmitDictionaryStart(LazyCount(keyValuePairs));
            foreach (KeyValuePair<object, object> item in keyValuePairs)
            {
                Marshal(item.Key, true, cache);
                Marshal(item.Value, false, cache);
            }
            EmitDictionaryEnd();
        }
    }
}
