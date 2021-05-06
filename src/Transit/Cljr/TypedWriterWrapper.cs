using Sellars.Transit.Alpha;

namespace Sellars.Transit.Cljr.Alpha
{
    internal class TypedWriterWrapper<T> : IWriter
    {
        public TypedWriterWrapper(IWriter<T> writer)
        {
            Writer = writer;
        }

        public IWriter<T> Writer { get; }

        public void Write(object value) =>
            Writer.Write((T)value);
    }
}
