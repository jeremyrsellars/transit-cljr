// Copyright (C) 2021 Jeremy Sellars.

using System;
using System.Buffers;
using Sellars.Transit.Alpha;

namespace Beerendonk.Transit.Impl.ReadHandlers
{
    /// <summary>
    /// Represents a binary read handler.
    /// </summary>
    internal partial class CharacterReadHandler : IUtf8ByteSpanReadHandler, IUtf8ByteSequenceReadHandler
    {
        public bool TryFromUtf8Representation(ReadOnlySequence<byte> utf8, out object value)
        {
            if (utf8.Length == 1) // Many unicode characters take more than one byte.  Otherwise fallback.
            {
                var p = utf8.Start;
                if (utf8.TryGet(ref p, out var mem, false))
                {
                    value = (char)mem.Span[0];
                    return true;
                }
            }
            value = default;
            return false;
        }

        public bool TryFromUtf8Representation(ReadOnlySpan<byte> utf8, out object value)
        {
            if (utf8.Length == 1) // Many unicode characters take more than one byte.  Otherwise fallback.
            {
                value = (char)utf8[0];
                return true;
            }
            value = default;
            return false;
        }
    }
}
