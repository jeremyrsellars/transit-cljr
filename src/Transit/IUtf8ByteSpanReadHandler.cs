using System;

namespace Sellars.Transit.Alpha
{
    public interface IUtf8ByteSpanReadHandler : IReadHandler
    {
        bool TryFromUtf8Representation(ReadOnlySpan<byte> utf8, out object value);
    }
}
