namespace Sellars.Transit.Cljr.Impl
{
    using System.Collections.Immutable;
    using Sellars.Transit.Alpha;
    using Beerendonk.Transit.Impl;
    using Sellars.Transit.Impl;
    using Sellars.Transit.Impl.Alpha;
    using System.Text.Json;
    /// <summary>
    /// Represents a reader factory.
    /// </summary>
    partial class Utf8ReaderFactory
    {
        private class Utf8JsonReader : Reader
        {
            public Utf8JsonReader(IUtf8JsonTokenReader input, IImmutableDictionary<string, IReadHandler> handlers, IDefaultReadHandler<object> defaultHandler
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

namespace Sellars.Transit.Impl
{
    using System.Collections.Immutable;
    using Sellars.Transit.Alpha;
    using Sellars.Transit.Impl.Alpha;
    using System.Text.Json;
    using Beerendonk.Transit.Impl;

    /// <summary>
    /// Represents a reader factory.
    /// </summary>
    partial class Utf8ReaderFactory
    {
        /// <summary>
        /// Gets the JSON instance.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="customHandlers">The custom handlers.</param>
        /// <param name="customDefaultHandler">The custom default handler.</param>
        /// <returns>A reader.</returns>
        public static IReader GetJsonInstance(IUtf8JsonTokenReader input,
            IImmutableDictionary<string, IReadHandler> customHandlers,
            IDefaultReadHandler customDefaultHandler)
        {
            return new Utf8JsonReader(input, ReaderFactory.Handlers(customHandlers),
                customDefaultHandler == null 
                ? ReaderFactory.DefaultDefaultHandler()
                : Cljr.Impl.DefaultReadHandlerAdapter.Adapt(customDefaultHandler), default);
        }

        private abstract class Reader : IReader, Sellars.Transit.Spi.Alpha.IReaderSpi
        {
            protected IUtf8JsonTokenReader input;
            protected IImmutableDictionary<string, IReadHandler> handlers;
            protected IDefaultReadHandler<object> defaultHandler;
            protected IDictionaryReader dictionaryBuilder;
            protected IListReader listBuilder;
            private ReadCache cache;
            private IParser p;
            private bool initialized;

            public Reader(IUtf8JsonTokenReader input, IImmutableDictionary<string, IReadHandler> handlers, IDefaultReadHandler<object> defaultHandler)
            {
                this.initialized = false;
                this.input = input;
                this.handlers = handlers;
                this.defaultHandler = defaultHandler;
                this.cache = false ? new ReadCache() : new Utf8ReadCache();
            }

            public T Read<T>()
            {
                if (!initialized)
                {
                    Initialize();
                }

                return (T)p.Parse(cache.Init());
            }

            object IReader.Read() => Read<object>();

            public IReader SetBuilders(IDictionaryReader dictionaryBuilder, IListReader listBuilder)
            {
                if (initialized)
                {
                    throw new TransitException("Cannot set builders after read has been called.");
                }

                this.dictionaryBuilder = dictionaryBuilder;
                this.listBuilder = listBuilder;
                return this;
            }

            private void EnsureBuilders()
            {
                if (dictionaryBuilder == null)
                {
                    dictionaryBuilder = new DictionaryBuilder();
                }

                if (listBuilder == null)
                {
                    listBuilder = new ListBuilder();
                }
            }

            protected void Initialize()
            {
                EnsureBuilders();
                p = CreateParser();
                initialized = true;
            }

            protected abstract IParser CreateParser();
        }


        private class Utf8JsonReader : Reader
        {
            public Utf8JsonReader(IUtf8JsonTokenReader input, IImmutableDictionary<string, IReadHandler> handlers, IDefaultReadHandler<object> defaultHandler
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
