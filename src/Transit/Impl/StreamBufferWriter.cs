using System;
using System.Buffers;
using System.IO;

namespace Sellars.Transit.Impl
{
    internal class StreamBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private readonly Stream output;
        private readonly MemoryStream buffer;

        public StreamBufferWriter(Stream output = null)
        {
            this.output = output;
            buffer = new MemoryStream();
        }

        public int DefaultBufferSize { get; set; } = 1024;

        public void Dispose()
        {
            buffer.Dispose();
        }

        public void Flush()
        {
        }

        public void Advance(int count)
        {
            if (count == 0)
                return;
            var pos = buffer.Position;
            buffer.Position += count;
            if(output != null)
                output.Write(buffer.GetBuffer(), (int)pos, (int)(buffer.Position - pos));
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            ResizeBufferForHint(sizeHint);

            return new Memory<byte>(buffer.GetBuffer(), (int)buffer.Position, (int)(buffer.Length - buffer.Position));
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            ResizeBufferForHint(sizeHint);

            return new Span<byte>(buffer.GetBuffer(), (int)buffer.Position, (int)(buffer.Length - buffer.Position));
        }

        private void ResizeBufferForHint(int sizeHint)
        {
            if (sizeHint == 0)
                sizeHint = DefaultBufferSize;

            var futurePosition = buffer.Position + sizeHint;
            if (futurePosition > buffer.Length)
                buffer.SetLength(futurePosition);
        }
   }
}
