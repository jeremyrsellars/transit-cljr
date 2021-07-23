using System;
using System.Buffers;

namespace Sellars.Transit.Alpha
{
    public interface IUtf8ByteReadHandler : IReadHandler
    {
        object FromUtf8Representation(ReadOnlySequence<byte> utf8);
        object FromUtf8Representation(ReadOnlySpan<byte> utf8);
    }
}
