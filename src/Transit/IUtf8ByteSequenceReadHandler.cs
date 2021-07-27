using System.Buffers;

namespace Sellars.Transit.Alpha
{
    public interface IUtf8ByteSequenceReadHandler : IReadHandler
    {
        bool TryFromUtf8Representation(ReadOnlySequence<byte> utf8, out object value);
    }
}
