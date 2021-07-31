using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipelines;
using Sellars.Transit.Alpha;

namespace TimeExecution.In
{
    using Format = Sellars.Transit.Alpha.TransitFactory.Format;

    public struct PipeRdrSource<T> : IInputTest, IInputSource<T, PipeReader>
    {
        public PipeReader Input { get; set; }
        public T Value { get; set; }
        public Func<Format, PipeReader, IImmutableDictionary<string, IReadHandler>, IDefaultReadHandler<object>, IReader> CreateReader { get; set; }
        public Format Type { get; set; }
        public IImmutableDictionary<string, IReadHandler> CustomHandlers { get; set; }
        public IDefaultReadHandler<object> DefaultHandler { get; set; }
        public int Iterations { get; set; }

        public object ReadAtOnce()
        {
            var Reader = CreateReader(Type, Input, CustomHandlers, DefaultHandler);
            return Reader.Read<T>();
        }

        public object ReadStreaming()
        {
            var Reader = CreateReader(Type, Input, CustomHandlers, DefaultHandler);
            object o = null;
            for (int i = 0; i < Iterations; i++)
                o = Reader.Read();
            return (T)o;
        }

        public void SetStream(Stream stream) => Input = stream.ToPipeReader();

        public string Describe() =>
            CreateReader.Method.DeclaringType.Name.Replace("TransitFactory", "TF") + "\tof " + GetType().Name.Replace("Source`1", "");
    }
}
