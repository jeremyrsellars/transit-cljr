using System;
using System.Buffers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sellars.Transit.Impl.Alpha
{
    /// <summary>
    /// Reads one or more Utf8Json tokens from a <see cref="ReadOnlySequence{byte}"/>.
    /// </summary>
    /// <remarks>
    /// This class is *not* thread-safe. Do not call more than one member at once and be sure any call completes (including asynchronous tasks)
    /// before calling the next one.
    /// </remarks>
    public partial class Utf8JsonSequenceReader : IUtf8JsonTokenReader
    {
        private readonly ReadOnlySequence<byte> availableData;
        private SequencePosition? endOfLastToken;
        private bool forceNewReader;
        bool hasRead = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8JsonSequenceReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public Utf8JsonSequenceReader(ReadOnlySequence<byte> stream, SequencePosition? startPosition = default)
        {
            availableData = stream;
            endOfLastToken = startPosition;
        }

        /// <summary>
        /// Gets any bytes that have been read since the last complete token returned from <see cref="ReadAsync(CancellationToken)"/>.
        /// </summary>
        public ReadOnlySequence<byte> RemainingBytes =>
            endOfLastToken.HasValue
            ? availableData.Slice(endOfLastToken.Value)
            : availableData;

        /// <inheritdoc/>
        public void Init(JsonReaderOptions options, out Utf8JsonReader reader, out StreamState streamState)
        {
            reader = new Utf8JsonReader(ReadOnlySequence<byte>.Empty, options);
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
            RecycleLastToken();

            // Check if we have a complete token and return it if we have it.
            // We do this before reading anything since a previous read may have brought in several tokens.
            cancellationToken.ThrowIfCancellationRequested();

            var isFinalBlock = streamState == StreamState.EndOfStream;
            if (forceNewReader || isFinalBlock)
            {
                forceNewReader = false;
                reader = new Utf8JsonReader(RemainingBytes, isFinalBlock, reader.CurrentState);
            }

            try
            {
                if (reader.Read())
                {
                    endOfLastToken = reader.Position;
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

        /// <summary>
        /// Recycle memory from a previously returned token.
        /// </summary>
        private void RecycleLastToken()
        {
            if (endOfLastToken.HasValue)
            {
                // A previously returned token can now be safely recycled since the caller wants more.
                //this.ReadData.AdvanceTo(this.endOfLastToken.Value);
                endOfLastToken = null;
            }
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
