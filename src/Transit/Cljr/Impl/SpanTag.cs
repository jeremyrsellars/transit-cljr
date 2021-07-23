using System;
using Beerendonk.Transit.Impl;

namespace Sellars.Transit.Impl
{
    internal class SpanTag : Tag
    {
        public static SpanTag TryFromSpan(ReadOnlySpan<byte> utf8)
        {
            int i = 0;
            switch (utf8.Length)
            {
                case 1:
                    switch((char)utf8[0])
                    {
                        case '_':  return new SpanTag("_");
                        case 's':  return new SpanTag("s");
                        case '?':  return new SpanTag("?");
                        case 'i':  return new SpanTag("i");
                        case 'd':  return new SpanTag("d");
                        case 'b':  return new SpanTag("b");
                        case '\'': return new SpanTag("'");
                        default:   return null;
                    };
                case 3: // "map".Length:
                    return
                        unchecked((byte)'m' == utf8[i++]
                               && (byte)'a' == utf8[i++]
                               && (byte)'p' == utf8[i++])
                        ? new SpanTag("map")
                        : null;
                case 5: // "array".Length:
                    return
                        unchecked((byte)'a' == utf8[i++]
                               && (byte)'r' == utf8[i++]
                               && (byte)'r' == utf8[i++]
                               && (byte)'a' == utf8[i++]
                               && (byte)'y' == utf8[i++])
                        ? new SpanTag("array")
                        : null;
            }
            return null;
        }

        private SpanTag(string value) : base(value)
        {
        }
    }
}
