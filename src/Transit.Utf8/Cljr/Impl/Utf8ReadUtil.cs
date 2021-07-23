using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using Beerendonk.Transit.Impl;

namespace Sellars.Transit.Impl
{
    internal static class Utf8ReadUtil
    {
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

        internal static Tag TryReadTag(ref Utf8JsonReader rdr, int startIndex)
        {
            const int longestGroundType = 5; //  "array".Length (the size of the longest ground type tag)
            SpanTag st = null;

            if (rdr.HasValueSequence)
            {
                if (rdr.ValueSequence.Length <= longestGroundType + startIndex)
                {
                    Span<byte> tag = stackalloc byte[longestGroundType];
                    rdr.ValueSequence.Slice(startIndex).CopyTo(tag);
                    st = SpanTag.TryFromSpan(tag);
                }
            }
            else if (rdr.ValueSpan.Length <= longestGroundType + startIndex)
                st = SpanTag.TryFromSpan(rdr.ValueSpan.Slice(startIndex));
            return st ?? new Tag(ReadSubstring(ref rdr, startIndex));
        }

        internal static string ReadSubstring(ref Utf8JsonReader rdr, int startIndex)
        {
            if (rdr.HasValueSequence)
            {
                var quoted = Quote(rdr.ValueSequence.Slice(startIndex));
                var subrdr = new Utf8JsonReader(quoted);
                if (!subrdr.Read())
                    throw new InvalidOperationException("Invalid string");
                return subrdr.GetString();
            }
            else
            {
                var span = rdr.ValueSpan.Slice(startIndex);
                if (span.IndexOf((byte)'\\') < 0)
                    return
#if NETSTANDARD2_1
                    Encoding.UTF8.GetString(span);
#else
                    Encoding.UTF8.GetString(span.ToArray());
#endif
                return rdr.GetString().Substring(startIndex);
            }
        }

        private static string DecodeJsonString(ReadOnlySpan<byte> valueSpan, StringBuilder s)
        {
            if (valueSpan.IndexOf((byte)'\\') < 0)
                return
#if NETSTANDARD2_1
                    Encoding.UTF8.GetString(valueSpan);
#else
                    Encoding.UTF8.GetString(valueSpan.ToArray());
#endif
            s.Clear();
            s.EnsureCapacity(valueSpan.Length);
            for (var i = 0; i < valueSpan.Length; i++)
            {
                char c = (char)valueSpan[i];
                if (c != '\\')
                {
                    s.Append(c);
                    continue;
                }
                char e = (char)valueSpan[++i];
                switch (e)
                {
                    case '"':
                        s.Append('\"');
                        break;
                    case '\\':
                        s.Append('\\');
                        break;
                    case '/':
                        s.Append('/');
                        break;
                    case 'b':
                        s.Append('\b');
                        break;
                    case 'f':
                        s.Append('\f');
                        break;
                    case 'n':
                        s.Append('\n');
                        break;
                    case 'r':
                        s.Append('\r');
                        break;
                    case 't':
                        s.Append('\t');
                        break;
                    case 'u': //followed by four-hex-digits
                        if (valueSpan.Length < ++i + 4)
                            throw new Alpha.TransitException($"Unexpected JSON string escape sequence at {i} near end of string length {valueSpan.Length}");
#if NETSTANDARD2_1
                        char uchar;
                        Span<char> hexParse = stackalloc char[4];
                        hexParse[0] = (char)valueSpan[i++];
                        hexParse[1] = (char)valueSpan[i++];
                        hexParse[2] = (char)valueSpan[i++];
                        hexParse[3] = (char)valueSpan[i]; // the `for` statement handles the last increment.
                        uchar = (char)int.Parse((ReadOnlySpan<char>)hexParse, System.Globalization.NumberStyles.HexNumber);
#else
                        string uchar = char.ConvertFromUtf32(unchecked(valueSpan[i++] << 24 | valueSpan[i++] << 16 | valueSpan[i++] << 8 | valueSpan[i++]));
#endif
                        s.Append(uchar);
                        break;
                    default:
                        throw new Alpha.TransitException($"Unexpected JSON string escape sequence: \\{e}");
                }
            }
            return s.ToString();
        }

        internal static ReadOnlySequence<byte> Quote(ReadOnlySequence<byte> str)
        {
            var quote = new byte[] { (byte)'\"' };
            var first = new MemorySegment<byte>(quote);
            var last = first;
            if (str.IsSingleSegment)
                last = last.Append(str.First);
            else
            {
                foreach (var x in str)
                    last = last.Append(x);
            }
            last = last.Append(quote);

            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        internal static ReadOnlySequence<byte> Quote(ReadOnlySpan<byte> str)
        {
            var quote = new byte[] { (byte)'\"' };
            var first = new MemorySegment<byte>(quote);
            var last = first
                .Append(str.ToArray())  // to-do: fix performance;
                .Append(quote);

            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        internal class MemorySegment<T> : ReadOnlySequenceSegment<T>
        {
            public MemorySegment(ReadOnlyMemory<T> memory)
            {
                Memory = memory;
            }

            public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
            {
                var segment = new MemorySegment<T>(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };

                Next = segment;

                return segment;
            }
        }
    }
}
