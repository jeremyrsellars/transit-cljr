// Inspired by https://github.com/neuecc/MessagePack-CSharp/blob/ffc18319670d49246db1abbd05c404a820280776/src/MessagePack.UnityClient/Assets/Scripts/MessagePack/MessagePackStreamReader.cs
using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;

namespace Sellars.Transit.Impl
{
    /// <summary>
    /// Reads one or more Utf8Json data structures from a <see cref="Stream"/>.
    /// </summary>
    /// <remarks>
    /// This class is *not* thread-safe. Do not call more than one member at once and be sure any call completes (including asynchronous tasks)
    /// before calling the next one.
    /// </remarks>
    public partial class Utf8JsonStreamReader : IDisposable
    {
        private readonly Stream stream;
        private readonly bool leaveOpen;
        private SequencePool.Rental sequenceRental = SequencePool.Shared.Rent();
        private SequencePosition? endOfLastToken;
        private bool forceNewReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8JsonStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read from. This stream will be disposed of when this <see cref="Utf8JsonStreamReader"/> is disposed.</param>
        public Utf8JsonStreamReader(Stream stream)
            : this(stream, leaveOpen: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8JsonStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="leaveOpen">If true, leaves the stream open after this <see cref="Utf8JsonStreamReader"/> is disposed; otherwise, false.</param>
        public Utf8JsonStreamReader(Stream stream, bool leaveOpen)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.leaveOpen = leaveOpen;
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


        public void Init(JsonReaderOptions options, out Utf8JsonReader reader, out StreamState streamState)
        {
            reader = new Utf8JsonReader(ReadOnlySpan<byte>.Empty, options);
            reader = new Utf8JsonReader(RemainingBytes, false, reader.CurrentState);
            streamState = StreamState.InStream;
        }

        public void Continue(JsonReaderState state, out Utf8JsonReader reader, out StreamState streamState)
        {
            reader = new Utf8JsonReader(RemainingBytes, false, state);
            streamState = StreamState.InStream;
        }

        /// <summary>
        /// Reads the next Utf8Json token.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task whose result is the next token from the stream, or <c>null</c> if the stream ends.
        /// The returned sequence is valid until this <see cref="Utf8JsonStreamReader"/> is disposed or
        /// until this method is called again, whichever comes first.
        /// </returns>
        /// <remarks>
        /// When <c>null</c> is the result of the returned task,
        /// any extra bytes read (between the last complete token and the end of the stream) will be available via the <see cref="RemainingBytes"/> property.
        /// </remarks>
        public bool TryRead(ref Utf8JsonReader reader, StreamState streamState, CancellationToken cancellationToken)
        {
            this.RecycleLastToken();

            // Check if we have a complete token and return it if we have it.
            // We do this before reading anything since a previous read may have brought in several tokens.
            cancellationToken.ThrowIfCancellationRequested();

            var rollback = reader;
            if (TryReadNextToken(ref reader, streamState))
                return true;

            reader = rollback;
            return false;
        }

        /// <summary>
        /// Arranges for the next read operation to start by reading from the underlying <see cref="Stream"/>
        /// instead of any data buffered from a previous read.
        /// </summary>
        /// <remarks>
        /// This is appropriate if the underlying <see cref="Stream"/> has been repositioned such that
        /// any previously buffered data is no longer applicable to what the caller wants to read.
        /// </remarks>
        public void DiscardBufferedData()
        {
            this.sequenceRental.Value.Reset();
            this.endOfLastToken = default;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!this.leaveOpen)
            {
                this.stream.Dispose();
            }

            this.sequenceRental.Dispose();
            this.sequenceRental = default;
        }

        /// <summary>
        /// Recycle memory from a previously returned token.
        /// </summary>
        private void RecycleLastToken()
        {
            if (this.endOfLastToken.HasValue)
            {
                // A previously returned token can now be safely recycled since the caller wants more.
                this.ReadData.AdvanceTo(this.endOfLastToken.Value);
                this.endOfLastToken = null;
            }
        }

#if NETFRAMEWORK
        private readonly byte[] bytes = new byte[1024];
#endif

        /// <summary>
        /// Read more data from the stream into the <see cref="ReadData"/> buffer.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns><c>true</c> if more data was read; <c>false</c> if the end of the stream had already been reached.</returns>
        public async Task<bool> TryReadMoreDataAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Memory<byte> buffer = this.ReadData.GetMemory(sizeHint: 0);
            int bytesRead = 0;
            try
            {
#if !NETFRAMEWORK
                bytesRead = await this.stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false); // Doesn't work on net461
#else
                bytesRead = await this.stream.ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                bytes.AsSpan(0, bytesRead).CopyTo(buffer.Span);
#endif
                this.forceNewReader |= bytesRead > 0;
                return bytesRead > 0;
            }
            finally
            {
                // Keep our state clean in case the caller wants to call us again.
                this.ReadData.Advance(bytesRead);
            }
        }

        /// <summary>
        /// Checks whether the content in <see cref="ReadData"/> include a complete Utf8Json structure.
        /// </summary>
        /// <param name="completeToken">Receives the sequence of the first complete data structure found, if any.</param>
        /// <returns><c>true</c> if a complete data structure was found; <c>false</c> otherwise.</returns>
        private bool TryReadNextToken(ref Utf8JsonReader reader, StreamState streamState)
        {
            if (this.ReadData.Length > 0)
            {
                var isFinalBlock = streamState == StreamState.EndOfStream;
                var rdr = this.forceNewReader || isFinalBlock
                    ? new Utf8JsonReader(RemainingBytes, isFinalBlock, reader.CurrentState)
                    : reader;
                this.forceNewReader = false;

                try
                {
                    if (rdr.Read())
                    {
                        this.endOfLastToken = rdr.Position;
                        reader = rdr;
                        return true;
                    }
                }
                catch(JsonException e)
                {
                    Console.Error.WriteLine(e);
                }
            }

            return false;
        }

        public enum StreamState
        {
            /// <summary>Start of stream, middle of stream, or end of stream that hasn't been detected yet.</summary>
            InStream,
            /// <summary>The stream has reported that it has no more bytes.</summary>
            EndOfStream,
        }
    }
}
