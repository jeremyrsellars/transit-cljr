// Modifications Copyright (C) 2021 Jeremy Sellars.
// Copyright © 2014 Rick Beerendonk. All Rights Reserved.
//
// This code is a C# port of the Java version created and maintained by Cognitect, therefore
//
// Copyright © 2014 Cognitect. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System.Collections.Immutable;
using System.IO;
using Sellars.Transit.Alpha;
using Sellars.Transit.Spi.Alpha;
using Beerendonk.Transit.Impl;
using Beerendonk.Transit.Impl.ReadHandlers;

namespace Sellars.Transit.Cljr.Impl
{
    /// <summary>
    /// Represents a reader factory.
    /// </summary>
    internal partial class ReaderFactory
    {
        /// <summary>
        /// Get the default handlers.
        /// </summary>
        /// <returns>The default handlers.</returns>
        public static IImmutableDictionary<string, IReadHandler> DefaultHandlers()
        {
            var builder = ImmutableDictionary.Create<string, IReadHandler>().ToBuilder();

            builder.Add(":", new KeywordReadHandler());
            builder.Add("$", new SymbolReadHandler());
            builder.Add("i", new IntegerReadHandler());
            builder.Add("?", new BooleanReadHandler());
            builder.Add("_", new NullReadHandler());
            builder.Add("f", new ReadHandlers.BigDecimalReadHandler());
            builder.Add("n", new BigIntegerReadHandler());
            builder.Add("d", new DoubleReadHandler());
            builder.Add("z", new SpecialNumberReadHandler());
            builder.Add("c", new CharacterReadHandler());
            builder.Add("t", new VerboseDateTimeReadHandler());
            builder.Add("m", new DateTimeReadHandler());
            builder.Add("r", new UriReadHandler());
            builder.Add("u", new GuidReadHandler());
            builder.Add("b", new BinaryReadHandler());
            builder.Add("\'", new IdentityReadHandler());
            builder.Add("set", new ReadHandlers.SetReadHandler());
            builder.Add("list", new ReadHandlers.ListReadHandler());
            builder.Add("ratio", new ReadHandlers.RatioReadHandler());
            builder.Add("cmap", new ReadHandlers.CMapReadHandler());
            builder.Add("link", new LinkReadHandler());

            return builder.ToImmutable();
        }

        /// <summary>
        /// THe defaults default handler.
        /// </summary>
        /// <returns>The default read handler.</returns>
        public static IDefaultReadHandler<ITaggedValue> DefaultDefaultHandler()
        {
            return new DefaultReadHandler();
        }

        private static void DisallowOverridingGroundTypes(IImmutableDictionary<string, IReadHandler> handlers)
        {
            if (handlers != null)
            {
                string[] groundTypeTags = { "_", "s", "?", "i", "d", "b", "'", "map", "array" };
                foreach (string tag in groundTypeTags)
                {
                    if (handlers.ContainsKey(tag))
                    {
                        throw new TransitException("Cannot override decoding for transit ground type, tag " + tag);
                    }
                }
            }
        }

        public static IImmutableDictionary<string, IReadHandler> Handlers(IImmutableDictionary<string, IReadHandler> customHandlers) 
        {
            if (customHandlers is Alpha.ReadHandlerMap rhm)
                return rhm;

            DisallowOverridingGroundTypes(customHandlers);
            IImmutableDictionary<string, IReadHandler> handlers = DefaultHandlers();
            if (customHandlers != null) 
            {
                foreach (var customHandler in customHandlers)
	            {
                    handlers = handlers.Add(customHandler.Key, customHandler.Value);		 
	            }
            }

            return handlers;
        }

        public static IDefaultReadHandler DefaultHandler(IDefaultReadHandler customDefaultHandler = null)
        {
            if (customDefaultHandler == null)
                return DefaultReadHandlerAdapter.Adapt(DefaultDefaultHandler());
            return customDefaultHandler;
        }

        public static IDefaultReadHandler<object> TypedDefaultHandler(IDefaultReadHandler customDefaultHandler = null)
        {
            if (customDefaultHandler == null)
                return DefaultDefaultHandler();
            return customDefaultHandler as IDefaultReadHandler<object>
                ?? DefaultReadHandlerAdapter.Adapt(customDefaultHandler);
        }

        /// <summary>
        /// Gets the JSON instance.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="customHandlers">The custom handlers.</param>
        /// <param name="customDefaultHandler">The custom default handler.</param>
        /// <returns>A reader.</returns>
        public static IReader GetJsonInstance(Stream input,
            IImmutableDictionary<string, IReadHandler> customHandlers,
            IDefaultReadHandler<object> customDefaultHandler)
        {
            return new JsonReader(input, Handlers(customHandlers), customDefaultHandler ?? DefaultDefaultHandler());
        }

        /// <summary>
        /// Gets the MessagePack instance.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="customHandlers">The custom handlers.</param>
        /// <param name="customDefaultHandler">The custom default handler.</param>
        /// <returns>A reader.</returns>
        public static IReader GetMsgPackInstance(Stream input,
            IImmutableDictionary<string, IReadHandler> customHandlers,
            IDefaultReadHandler customDefaultHandler)
        {
            return new MsgPackReader(input, Handlers(customHandlers), TypedDefaultHandler(customDefaultHandler) ?? DefaultDefaultHandler(),
                MessagePack.MessagePackSerializerOptions.Standard);
        }

        private class DefaultReadHandler : IDefaultReadHandler<ITaggedValue>, IDefaultReadHandler
        {
            public ITaggedValue FromRepresentation(string tag, object representation)
            {
                return TransitFactory.TaggedValue(tag, representation);
            }

            object IDefaultReadHandler.FromRepresentation(string tag, object representation) =>
                FromRepresentation(tag, representation);
        }

        internal abstract class Reader : IReader, IReaderSpi
        {
            protected Stream input;
            protected IImmutableDictionary<string, IReadHandler> handlers;
            protected IDefaultReadHandler<object> defaultHandler;
            protected IDictionaryReader dictionaryBuilder;
            protected IListReader listBuilder;
            private ReadCache cache;
            private IParser p;
            private bool initialized;

            public Reader(Stream input, IImmutableDictionary<string, IReadHandler> handlers, IDefaultReadHandler<object> defaultHandler)
            {
                this.initialized = false;
                this.input = input;
                this.handlers = handlers;
                this.defaultHandler = defaultHandler;
                this.cache = new ReadCache();
            }

            public T Read<T>()
            {
                if (!initialized)
                {
                    Initialize();
                }

                var v = (T)p.Parse(cache.Init());
                if (v == null)
                    return default;
                return v;
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
                    dictionaryBuilder = new MapBuilder();
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

        private class JsonReader : Reader
        {
            public JsonReader(Stream input, IImmutableDictionary<string, IReadHandler> handlers, IDefaultReadHandler<object> defaultHandler)
                : base(input, handlers, defaultHandler)
            {
            }

            protected override IParser CreateParser()
            {
                var streamReader = new StreamReader(input, TransitFactory.Encoding);
                var jsonTextReader = new JsonTextReader(streamReader)
                {
                    SupportMultipleContent = true,
                };
                return new JsonParser(jsonTextReader, handlers, defaultHandler, 
                    dictionaryBuilder, listBuilder);
            }
        }
    }
}
