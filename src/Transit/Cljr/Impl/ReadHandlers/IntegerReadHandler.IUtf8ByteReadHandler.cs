// Copyright (C) 2021 Jeremy Sellars.

using System;
using System.Buffers;
using Sellars.Transit.Alpha;

namespace Beerendonk.Transit.Impl.ReadHandlers
{
    /// <summary>
    /// Represents a binary read handler.
    /// </summary>
    internal partial class IntegerReadHandler : IUtf8ByteSpanReadHandler, IUtf8ByteSequenceReadHandler
    {
        public bool TryFromUtf8Representation(ReadOnlySequence<byte> utf8, out object value)
        {
            if (TryParseUtf8ByteSequence(utf8, out var val))
            {
                value = val;
                return true;
            }
            value = default;
            return false;
        }

        public bool TryFromUtf8Representation(ReadOnlySpan<byte> utf8, out object value)
        {
            if (TryParseUtf8ByteSpan(utf8, out var val))
            {
                value = val;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryParseUtf8ByteSequence(ReadOnlySequence<byte> utf8, out long value)
        {
            var length = utf8.Length;
            if (length > 256)
            {
                value = default;
                return false; 
            }

            Span<byte> bytes = stackalloc byte[checked((int)length)];
            utf8.CopyTo(bytes);
            return TryParseUtf8ByteSpan(bytes, out value);
        }

        public static bool TryParseUtf8ByteSpan(ReadOnlySpan<byte> utf8, out long value) =>
            System.Buffers.Text.Utf8Parser.TryParse(utf8, out value, out var bytesConsumed);
    }
}
