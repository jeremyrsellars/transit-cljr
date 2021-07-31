using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using Sellars.Transit.Alpha;

namespace TimeExecution.Out
{
    using Format = Sellars.Transit.Alpha.TransitFactory.Format;

    public struct PipeWtrDestination<T> : IOutputTest, IOutputDestination<T, PipeWriter>
    {
        public PipeWriter Output { get; set; }
        public T Value { get; set; }
        public Func<Format, PipeWriter, IDictionary<Type, IWriteHandler>, IWriteHandler, Func<object, object>, IWriter<T>> CreateWriter { get; set; }
        public Func<Format, PipeWriter, IDictionary<Type, IWriteHandler>, IWriteHandler, Func<object, object>, IWriter<IEnumerable<T>>> CreateEnumerableWriter { get; set; }
        public Format Type { get; set; }
        public IDictionary<Type, IWriteHandler> CustomHandlers { get; set; }
        public IWriteHandler DefaultHandler { get; set; }
        public Func<object, object> Transform { get; set; }
        public int Iterations { get; set; }

        public void WriteAtOnce()
        {
            var writer = CreateEnumerableWriter(Type, Output, CustomHandlers, DefaultHandler, Transform);
            writer.Write(Enumerable.Repeat(Value, Iterations));
        }

        public void WriteStreaming()
        {
            var writer = CreateWriter(Type, Output, CustomHandlers, DefaultHandler, Transform);
            for (int i = 0; i < Iterations; i++)
                writer.Write(Value);
        }

        public void SetStream(Stream stream) => Output = stream.ToPipeWriter();

        public string Describe() =>
            CreateWriter.Method.DeclaringType.Name.Replace("TransitFactory", "TF") + "\tof " + GetType().Name.Replace("Destination`1", "");
    }
}
