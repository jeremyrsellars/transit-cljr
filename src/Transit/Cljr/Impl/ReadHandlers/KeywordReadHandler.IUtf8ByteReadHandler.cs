// Copyright (C) 2021 Jeremy Sellars.

using System;
using System.Buffers;
using Sellars.Transit.Alpha;

namespace Beerendonk.Transit.Impl.ReadHandlers
{
    /// <summary>
    /// Represents a binary read handler.
    /// </summary>
    internal partial class KeywordReadHandler
#if NETSTANDARD2_1
        : IUtf8ByteSpanReadHandler, IUtf8ByteSequenceReadHandler
#endif
    {
        public bool TryFromUtf8Representation(ReadOnlySequence<byte> utf8, out object value)
        {
            if (SymbolReadHandler.TryParseUtf8ByteSequence(utf8, out var symbol))
            {
                value = TransitFactory.Keyword(symbol);
                return true;
            }
            value = default;
            return false;
        }

        public bool TryFromUtf8Representation(ReadOnlySpan<byte> utf8, out object value)
        {
            if (SymbolReadHandler.TryParseUtf8ByteSpan(utf8, out var symbol))
            {
                value = TransitFactory.Keyword(symbol);
                return true;
            }
            value = default;
            return false;
        }
    }
}
