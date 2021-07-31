using System;
using System.Collections.Generic;
using Sellars.Transit.Alpha;

namespace TimeExecution.Out
{
    using Format = Sellars.Transit.Alpha.TransitFactory.Format;

    public interface IOutputTest
    {
        int Iterations { get; set; }
        Format Type { get; set; }
        IDictionary<Type, IWriteHandler> CustomHandlers { get; set; }
        IWriteHandler DefaultHandler { get; set; }
        Func<object, object> Transform { get; set; }
        public void WriteAtOnce();
        public void WriteStreaming();
        string Describe();
    }
}
