// Inspired by https://github.com/neuecc/MessagePack-CSharp/blob/ffc18319670d49246db1abbd05c404a820280776/src/MessagePack.UnityClient/Assets/Scripts/MessagePack/MessagePackStreamReader.cs

namespace Sellars.Transit.Impl.Alpha
{
    public enum StreamState
    {
        /// <summary>Start of stream, middle of stream, or end of stream that hasn't been detected yet.</summary>
        InStream,
        /// <summary>The stream has reported that it has no more bytes.</summary>
        EndOfStream,
    }
}
