using Beerendonk.Transit.Impl;
using System.Text.Json;

namespace Sellars.Transit.Impl
{
    internal class Utf8ReadCache: ReadCache
    {
        public object CacheReadParseString(ref Utf8JsonReader rdr, bool asDictionaryKey, Utf8JsonParser p)
        {
            if (IsValueEmpty(ref rdr))
            {
                p.ReadToken(ref rdr);
                return string.Empty;
            }

            if (CacheCode(ref rdr))
            {
                int index = CodeToIndex(ref rdr);
                p.ReadToken(ref rdr);
                return cache[index];
            }
            else if (IsCacheable(ref rdr, asDictionaryKey))
            {
                if (index == WriteCache.MaxCacheEntries)
                {
                    Init();
                }
                return cache[index++] = p.ParseString(p.ParseString(ref rdr, this)); // another implementation checked for null parser.
            }

            return p.ParseString(p.ParseString(ref rdr, this)); // another implementation checked for null parser.
        }

        private bool CacheCode(ref Utf8JsonReader rdr)
        {
            return Nth(ref rdr, 0) == Constants.Sub && (!rdr.ValueTextEquals(Constants.DirectoryAsList));
        }

        private int CodeToIndex(ref Utf8JsonReader rdr)
        {
            if (ValueLengthAtLeast(ref rdr, 3))
                return ((byte)(Nth(ref rdr, 1) - WriteCache.BaseCharIdx) * WriteCache.CacheCodeDigits) +
                        ((byte)Nth(ref rdr, 2) - WriteCache.BaseCharIdx);

            return ((byte)Nth(ref rdr, 1) - WriteCache.BaseCharIdx);
        }

        /// <summary>
        /// Determines whether the specified s is cacheable.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="asDictionaryKey">if set to <c>true</c> [as dictionary key].</param>
        /// <returns></returns>
        public static bool IsCacheable(ref Utf8JsonReader rdr, bool asDictionaryKey)
        {
            byte b1;
            return (ValueLengthAtLeast(ref rdr, WriteCache.MinSizeCacheable)) &&
                (asDictionaryKey ||
                    (Nth(ref rdr, 0) == Constants.Esc &&
                    ((b1 = (byte)Nth(ref rdr, 1)) == ':' || b1 == '$' || b1 == '#')));
        }

        public static bool IsValueEmpty(ref Utf8JsonReader rdr) =>
            (!rdr.HasValueSequence && rdr.ValueSpan.IsEmpty)
            || (rdr.HasValueSequence && rdr.ValueSequence.IsEmpty);

        public static byte? Nth(ref Utf8JsonReader rdr, int valueIndex)
        {
            if (rdr.HasValueSequence)
            {
                var seq = rdr.ValueSequence;
                var p = seq.GetPosition(valueIndex);
                if (seq.TryGet(ref p, out var mem, false))
                    return mem.Span[0];
                return default;
            }

            var s = rdr.ValueSpan;
            return valueIndex < s.Length ? s[valueIndex] : default;
        }

        public static bool ValueLengthAtLeast(ref Utf8JsonReader rdr, int minLen)
        {
            if (rdr.HasValueSequence)
            {
                var seq = rdr.ValueSequence;
                return seq.First.Length >= minLen
                    || seq.Length >= minLen;
            }
            else
                return rdr.ValueSpan.Length >= minLen;
        }
    }
}
