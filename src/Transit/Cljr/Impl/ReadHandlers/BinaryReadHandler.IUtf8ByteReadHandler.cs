// Copyright (C) 2021 Jeremy Sellars.

using System;
using System.Buffers;
using System.Buffers.Text;
using Sellars.Transit.Alpha;

namespace Beerendonk.Transit.Impl.ReadHandlers
{
    /// <summary>
    /// Represents a binary read handler.
    /// </summary>
    internal partial class BinaryReadHandler : IUtf8ByteReadHandler
    {
        public object FromUtf8Representation(ReadOnlySequence<byte> utf8)
        {
            var inputLength = checked((int)utf8.Length);
            // Calculating the true size of the destination array requires knowing how many padding chars ('=')
            // there are at the end of the sequence.  I bet it's better performance to skip to the last mem chunk
            // rather than there being a 2/3 chance of creating the wrong size array and having to trim it to size.
            var bytes = new byte[CalculateDecodedSize(inputLength, Padding(utf8))];
            var outSpan = bytes.AsSpan();
            int overallBytesWritten = 0;
            int overallBytesConsumed = 0;
            OperationStatus result = OperationStatus.Done;
            const int MinChunkSize = 4;
            Span<byte> partialChunk = stackalloc byte[MinChunkSize];
            var partialBytes = 0;

            foreach (var unchunkedmem in utf8)
            {
                var availableBytes = partialBytes + unchunkedmem.Length;
                var isFinalBlock = availableBytes == inputLength - overallBytesConsumed;
                var mem = unchunkedmem;
                if (availableBytes < MinChunkSize)
                {
                    // Tiny chunk... still not enough to parse unless it's the final block.
                    mem.Span.CopyTo(partialChunk.Slice(partialBytes));
                    partialBytes += mem.Length;
                    mem = ReadOnlyMemory<byte>.Empty;
                    if (!isFinalBlock)
                    {
                        result = OperationStatus.NeedMoreData;
                        continue;
                    }
                }
                else if (partialBytes > 0)
                {
                    // Fill the partial chunk
                    var countToFill = MinChunkSize - partialBytes;
                    var fill = mem.Slice(0, countToFill);
                    mem = mem.Slice(countToFill);
                    fill.Span.CopyTo(partialChunk.Slice(partialBytes));
                    partialBytes += fill.Length;
                    // Read the partial chunk
                    result = Base64.DecodeFromUtf8(partialChunk,
                        outSpan,
                        out var pbConsumed, out var pbWritten,
                        isFinalBlock);
                    outSpan = outSpan.Slice(pbWritten);
                    partialBytes = 0;
                    overallBytesConsumed += pbConsumed;
                    overallBytesWritten += pbWritten;
                    if (isFinalBlock && overallBytesConsumed == inputLength && result == OperationStatus.Done)
                        break;
                    if (result != OperationStatus.Done)
                        throw new FormatException($"Could not decode binary data.  Status: {result}. Consumed {overallBytesConsumed}. Wrote {overallBytesWritten}.");
                }

                var completeChunksAvailable = mem.Length / MinChunkSize;
                var partialBytesAtEndOfChunk = mem.Length % MinChunkSize;
                if (!isFinalBlock && partialBytesAtEndOfChunk >= 1)
                {
                    mem.Slice(completeChunksAvailable * MinChunkSize).Span.CopyTo(partialChunk);
                    partialBytes = partialBytesAtEndOfChunk;
                    mem = mem.Slice(0, completeChunksAvailable * MinChunkSize);
                }
                result = Base64.DecodeFromUtf8(mem.Span,
                    outSpan,
                    out var bytesConsumed, out var bytesWritten,
                    isFinalBlock);
                outSpan = outSpan.Slice(bytesWritten);
                overallBytesConsumed += bytesConsumed;
                overallBytesWritten += bytesWritten;
                if (isFinalBlock && overallBytesConsumed == inputLength && result == OperationStatus.Done)
                    break;
                if (result == OperationStatus.Done)
                    continue;

                if (result == OperationStatus.NeedMoreData) // to-do: Handle this case.
                {
                }
                throw new FormatException($"Could not decode binary data.  Status: {result}. Consumed {bytesConsumed}. Wrote {bytesWritten}.");
            }

            if (result != OperationStatus.Done || overallBytesConsumed != utf8.Length)
                throw new FormatException($"Could not decode binary data.  Status: {result}. Consumed {overallBytesConsumed}. Wrote {overallBytesWritten}.");

            return TrimToSize(bytes, overallBytesWritten);
        }

        public object FromUtf8Representation(ReadOnlySpan<byte> utf8)
        {
            var bytes = new byte[CalculateDecodedSize(utf8.Length, Padding(utf8))];

            var result = Base64.DecodeFromUtf8(utf8, bytes, out var bytesConsumed, out var bytesWritten);
            if (result != OperationStatus.Done || bytesConsumed != utf8.Length)
                throw new FormatException($"Could not decode binary data.  Status: {result}. Consumed {bytesConsumed}. Wrote {bytesWritten}.");

            return TrimToSize(bytes, bytesWritten);
        }

        private int Padding(ReadOnlySpan<byte> utf8)
        {
            var len = utf8.Length;
            var checkCount = Math.Min(len, 3);
            var check = utf8.Slice(len - checkCount, checkCount);
            var relIdx = check.IndexOf((byte)'=');
            return relIdx < 0 ? 0 : checkCount - relIdx;
        }

        private int Padding(ReadOnlySequence<byte> utf8)
        {
            const int MaxPadding = 3;
            Span<byte> check = stackalloc byte[MaxPadding];
            var len = checked((int)utf8.Length);
            var checkCount = Math.Min(len, 3);
            utf8.Slice(len - checkCount, checkCount).CopyTo(check);
            var relIdx = check.IndexOf((byte)'=');
            return relIdx < 0 ? 0 : checkCount - relIdx;
        }

        private static int CalculateDecodedSize(int inputLength, int padding = 0)
        {
            var max = Base64.GetMaxDecodedFromUtf8Length(inputLength);
            //var size = (int)Math.Ceiling(inputLength / 4.0 * 3.0);
            return max - padding;
        }

        private static object TrimToSize(byte[] bytes, int bytesWritten)
        {
            if (bytesWritten == bytes.Length)
                return bytes;
            return bytes.AsMemory(0, bytesWritten).ToArray();
        }
    }
}