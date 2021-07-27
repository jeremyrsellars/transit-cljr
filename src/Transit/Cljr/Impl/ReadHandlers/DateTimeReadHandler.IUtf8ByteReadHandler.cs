// Copyright (C) 2021 Jeremy Sellars.

using System;
using System.Buffers;
using Sellars.Transit.Alpha;
using Sellars.Transit.Util.Alpha;

namespace Beerendonk.Transit.Impl.ReadHandlers
{
    /// <summary>
    /// Represents a binary read handler.
    /// </summary>
    internal partial class DateTimeReadHandler : IUtf8ByteSpanReadHandler, IUtf8ByteSequenceReadHandler
    {
        public bool TryFromUtf8Representation(ReadOnlySequence<byte> utf8, out object value)
        {
            if (IntegerReadHandler.TryParseUtf8ByteSequence(utf8, out var n))
            {
                value = TimeUtils.FromTransitTime(n);
                return true;
            }
            value = default;
            return false;
        }

        public bool TryFromUtf8Representation(ReadOnlySpan<byte> utf8, out object value)
        {
            if (IntegerReadHandler.TryParseUtf8ByteSpan(utf8, out var n))
            {
                value = TimeUtils.FromTransitTime(n);
                return true;
            }
            value = default;
            return false;
        }
    }
}
