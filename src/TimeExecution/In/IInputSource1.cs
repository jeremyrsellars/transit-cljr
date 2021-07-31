using System.IO;

namespace TimeExecution.In
{
    public interface IInputSource<T> : IInputTest
    {
        T Value { get; set; }
        void SetStream(Stream stream);
    }
}
