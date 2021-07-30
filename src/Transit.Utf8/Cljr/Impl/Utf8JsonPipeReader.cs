using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;

namespace Sellars.Transit.Impl.Alpha
{
    /// <summary>
    /// Reads one or more Utf8Json tokens from a <see cref="ReadOnlySequence{byte}"/>.
    /// </summary>
    /// <remarks>
    /// This class is *not* thread-safe. Do not call more than one member at once and be sure any call completes (including asynchronous tasks)
    /// before calling the next one.
    /// </remarks>
    public partial class Utf8JsonPipeReader : IUtf8JsonTokenReader
    {
        private readonly PipeReader pipeReader;
        private SequencePool.Rental sequenceRental = SequencePool.Shared.Rent();
        private SequencePosition? endOfLastToken;
        private bool forceNewReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8JsonPipeReader"/> class.
        /// </summary>
        /// <param name="pipeReader">The stream to read from.</param>
        public Utf8JsonPipeReader(PipeReader pipeReader)
        {
            this.pipeReader = pipeReader;
        }

        /// <summary>
        /// Gets any bytes that have been read since the last complete token returned from <see cref="ReadAsync(CancellationToken)"/>.
        /// </summary>
        public ReadOnlySequence<byte> RemainingBytes =>
            this.endOfLastToken.HasValue
            ? this.ReadData.AsReadOnlySequence.Slice(this.endOfLastToken.Value)
            : this.ReadData.AsReadOnlySequence;

        /// <summary>
        /// Gets the sequence that we read data from the <see cref="stream"/> into.
        /// </summary>
        private Sequence<byte> ReadData => this.sequenceRental.Value;

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
            this.sequenceRental.Value.Reset();
            endOfLastToken = default;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.sequenceRental.Dispose();
            this.sequenceRental = default;
        }

        /// <summary>
        /// Recycle memory from a previously returned token.
        /// </summary>
        private void RecycleLastToken()
        {
            if (endOfLastToken.HasValue)
            {
                // A previously returned token can now be safely recycled since the caller wants more.
                ReadData.AdvanceTo(this.endOfLastToken.Value);
                endOfLastToken = null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TryReadMoreDataAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int bytesRead = 0;
            try
            {
                var result = await pipeReader.ReadAsync().ConfigureAwait(false);
                bytesRead = checked((int)result.Buffer.Length);
                Memory<byte> buffer = this.ReadData.GetMemory(sizeHint: bytesRead);
                result.Buffer.CopyTo(buffer.Span);
                pipeReader.AdvanceTo(result.Buffer.End);

                this.forceNewReader |= bytesRead > 0;
                return bytesRead > 0;
            }
            finally
            {
                // Keep our state clean in case the caller wants to call us again.
                this.ReadData.Advance(bytesRead);
            }
        }
    }
}
