using System;
using System.Collections.Immutable;
using Sellars.Transit.Alpha;

namespace TimeExecution.In
{
    using Format = Sellars.Transit.Alpha.TransitFactory.Format;

    public interface IInputTest
    {
        int Iterations { get; set; }
        Format Type { get; set; }
        IImmutableDictionary<string, IReadHandler> CustomHandlers { get; set; }
        IDefaultReadHandler<object> DefaultHandler { get; set; }
        public object ReadAtOnce();
        public object ReadStreaming();
        string Describe();
    }
}
