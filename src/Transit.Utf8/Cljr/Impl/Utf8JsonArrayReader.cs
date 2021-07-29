using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sellars.Transit.Impl.Alpha
{
    /// <summary>
    /// Reads one or more Utf8Json tokens from a <see cref="byte[]"/>.
    /// </summary>
    /// <remarks>
    /// This class is *not* thread-safe. Do not call more than one member at once and be sure any call completes (including asynchronous tasks)
    /// before calling the next one.
    /// </remarks>
    public partial class Utf8JsonArrayReader : IUtf8JsonTokenReader
    {
        private readonly byte[] availableData;
        private int? endOfLastToken;
        private bool forceNewReader;
        bool hasRead = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8JsonArrayReader"/> class.
        /// </summary>
        /// <param name="bytes">The stream to read from.</param>
        public Utf8JsonArrayReader(byte[] bytes, int? startIndex = default)
        {
            availableData = bytes;
            endOfLastToken = startIndex;
        }

        /// <summary>
        /// Gets any bytes that have been read since the last complete token returned from <see cref="ReadAsync(CancellationToken)"/>.
        /// </summary>
        public ReadOnlySpan<byte> RemainingBytes =>
            endOfLastToken.HasValue
            ? availableData.AsSpan().Slice(endOfLastToken.Value)
            : availableData;

        /// <inheritdoc/>
        public void Init(JsonReaderOptions options, out Utf8JsonReader reader, out StreamState streamState)
        {
            reader = new Utf8JsonReader(ReadOnlySpan<byte>.Empty, options);
            reader = new Utf8JsonReader(RemainingBytes, false, reader.CurrentState);
            streamState = StreamState.InStream;
        }

        /// <inheritdoc/>
        public void Continue(JsonReaderState state, out Utf8JsonReader reader, out StreamState streamState)
        {
            reader = new Utf8JsonReader(RemainingBytes, false, state);
            streamState = StreamState.InStream;
        }

        /// <inheritdoc/>
        public bool TryRead(ref Utf8JsonReader reader, StreamState streamState, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isFinalBlock = streamState == StreamState.EndOfStream;
            if (forceNewReader || isFinalBlock)
            {
                forceNewReader = false;
                reader = new Utf8JsonReader(RemainingBytes, isFinalBlock, reader.CurrentState);
            }

            try
            {
                var prevBC = reader.BytesConsumed;
                if (reader.Read())
                {
                    var bytesConsumed = checked((int)(reader.BytesConsumed - prevBC));
                    endOfLastToken = bytesConsumed + (endOfLastToken ?? 0);
                    return true;
                }
            }
            catch (JsonException)
            {
                throw;
            }

            return false;
        }

        /// <inheritdoc/>
        public void DiscardBufferedData()
        {
            endOfLastToken = default;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public Task<bool> TryReadMoreDataAsync(CancellationToken cancellationToken)
        {
            if (hasRead)
                return Task.FromResult(false);
            hasRead = true;
            return Task.FromResult(true);
        }
    }
}
