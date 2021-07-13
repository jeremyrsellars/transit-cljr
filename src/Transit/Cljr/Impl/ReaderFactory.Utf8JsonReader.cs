namespace Sellars.Transit.Cljr.Impl
{
    using System.Collections.Immutable;
    using System.IO;
    using Sellars.Transit.Alpha;
    using Beerendonk.Transit.Impl;
    using Sellars.Transit.Impl;
    using System.Text.Json;
    /// <summary>
    /// Represents a reader factory.
    /// </summary>
    partial class ReaderFactory
    {
        private class Utf8JsonReader : Reader
        {
            public Utf8JsonReader(Stream input, IImmutableDictionary<string, IReadHandler> handlers, IDefaultReadHandler<object> defaultHandler
                , JsonReaderOptions options)
                : base(input, handlers, defaultHandler)
            {
                Options = options;
            }

            public JsonReaderOptions Options { get; }

            protected override IParser CreateParser()
            {
                return new Utf8JsonParser(input, Options,
                    handlers, defaultHandler,
                    dictionaryBuilder, listBuilder);
            }
        }
    }
}

namespace Beerendonk.Transit.Impl
{
    using System.Collections.Immutable;
    using System.IO;
    using Sellars.Transit.Alpha;
    using Beerendonk.Transit.Impl;
    using System.Text.Json;

    /// <summary>
    /// Represents a reader factory.
    /// </summary>
    partial class ReaderFactory
    {
        private class Utf8JsonReader : Reader
        {
            public Utf8JsonReader(Stream input, IImmutableDictionary<string, IReadHandler> handlers, IDefaultReadHandler<object> defaultHandler
                , JsonReaderOptions options)
                : base(input, handlers, defaultHandler)
            {
                Options = options;
            }

            public JsonReaderOptions Options { get; }

            protected override IParser CreateParser()
            {
                return new Sellars.Transit.Impl.Utf8JsonParser(input, Options,
                    handlers, defaultHandler,
                    dictionaryBuilder, listBuilder);
            }
        }
    }
}
