using System;
using System.Collections.Immutable;
using Sellars.Transit.Alpha;

namespace TimeExecution.In
{
    using Format = Sellars.Transit.Alpha.TransitFactory.Format;

    public interface IInputSource<T, TIn> : IInputSource<T>
    {
        TIn Input { get; set; }
        Func<Format, TIn, IImmutableDictionary<string, IReadHandler>, IDefaultReadHandler<object>, IReader> CreateReader { get; set; }
    }
}
