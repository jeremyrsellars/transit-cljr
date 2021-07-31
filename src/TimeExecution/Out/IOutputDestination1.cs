using System.IO;

namespace TimeExecution.Out
{
    public interface IOutputDestination<T> : IOutputTest
    {
        T Value { get; set; }
        void SetStream(Stream stream);
    }
}
