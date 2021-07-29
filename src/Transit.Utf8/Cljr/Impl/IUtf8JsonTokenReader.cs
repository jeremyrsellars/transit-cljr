using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sellars.Transit.Impl.Alpha
{
    /// <summary>
    /// Interface for adapting various input sources to something readable by the Utf8JsonReader.
    /// </summary>
    public interface IUtf8JsonTokenReader : IDisposable
    {
        /// <summary>
        /// Initializes a <see cref="Utf8JsonReader"/> with the specified <paramref name="options"/>
        /// that is ready to start reading at the start of a message.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reader"></param>
        /// <param name="streamState"></param>
        void Init(JsonReaderOptions options, out Utf8JsonReader reader, out StreamState streamState);

        /// <summary>
        /// Initializes a <see cref="Utf8JsonReader"/> with the specified <paramref name="options"/>
        /// that is ready to continue reading within a message.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reader"></param>
        /// <param name="streamState"></param>
        void Continue(JsonReaderState state, out Utf8JsonReader reader, out StreamState streamState);

        /// <summary>
        /// Reads the next Utf8Json token from the available data buffer.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// <c>true</c> if the <paramref name="reader"/> read a token;
        /// <c>false</c> if the end of the available had already been reached or there was insufficient data.
        /// When <c>false</c>, consider awaiting <see cref="TryReadMoreDataAsync(CancellationToken)"/>.
        /// </returns>
        bool TryRead(ref Utf8JsonReader reader, StreamState streamState, CancellationToken cancellationToken);

        /// <summary>
        /// Read more data from the source
        /// making it available to <see cref="TryRead(ref Utf8JsonReader, StreamState, CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task whose result is <c>true</c> if more data was read; 
        /// <c>false</c> if the end of the stream had already been reached.
        /// </returns>
        Task<bool> TryReadMoreDataAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Arranges for the next read operation to start by reading from the underlying source
        /// instead of any data buffered from a previous read.
        /// </summary>
        /// <remarks>
        /// This is appropriate if the underlying <see cref="ReadOnlySpan<byte>"/> has been repositioned such that
        /// any previously buffered data is no longer applicable to what the caller wants to read.
        /// </remarks>
        void DiscardBufferedData();
    }
}
