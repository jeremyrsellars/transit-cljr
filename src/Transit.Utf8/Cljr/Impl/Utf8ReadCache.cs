using Beerendonk.Transit.Impl;
using System.Text.Json;

namespace Sellars.Transit.Impl
{
    using static Utf8ReadUtil;
    internal class Utf8ReadCache : ReadCache
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
                return cache[index++] = p.ParseString(ref rdr); // another implementation checked for null parser.
            }

            return p.ParseString(ref rdr); // another implementation checked for null parser.
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
    }
}
