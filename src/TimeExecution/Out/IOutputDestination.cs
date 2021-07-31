using System;
using System.Collections.Generic;
using Sellars.Transit.Alpha;

namespace TimeExecution.Out
{
    using Format = Sellars.Transit.Alpha.TransitFactory.Format;

    public interface IOutputDestination<T, TOut> : IOutputDestination<T>
    {
        TOut Output { get; set; }
        Func<Format, TOut, IDictionary<Type, IWriteHandler>, IWriteHandler, Func<object, object>, IWriter<T>> CreateWriter { get; set; }
        Func<Format, TOut, IDictionary<Type, IWriteHandler>, IWriteHandler, Func<object, object>, IWriter<IEnumerable<T>>> CreateEnumerableWriter { get; set; }
    }
}
