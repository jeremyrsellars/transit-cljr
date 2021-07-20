﻿// Modifications Copyright (C) 2021 Jeremy Sellars.
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using Beerendonk.Transit.Impl;
using Beerendonk.Transit.Impl.WriteHandlers;
using clojure.lang;
using Newtonsoft.Json;
using Sellars.Transit.Alpha;

namespace Sellars.Transit.Cljr.Impl
{
    /// <summary>
    /// Implements a writer factory.
    /// </summary>
    internal partial class WriterFactory
    {
        /// <summary>
        /// Get the default handlers.
        /// </summary>
        /// <returns>The default handlers.</returns>
        public static IImmutableDictionary<Type, IWriteHandler> DefaultHandlers()
        {
            var builder = ImmutableDictionary.Create<Type, IWriteHandler>().ToBuilder();

            var integerHandler = new IntegerWriteHandler("i");

            builder.Add(typeof(bool), new BooleanWriteHandler());
            builder.Add(typeof(NullType), new NullWriteHandler());
            builder.Add(typeof(string), new ToStringWriteHandler("s"));
            builder.Add(typeof(int), integerHandler);
            builder.Add(typeof(long), integerHandler);
            builder.Add(typeof(short), integerHandler);
            builder.Add(typeof(byte), integerHandler);
            builder.Add(typeof(BigInt), new ToStringWriteHandler("n"));
            builder.Add(typeof(BigInteger), new ToStringWriteHandler("n"));
            builder.Add(typeof(BigDecimal), new ToStringWriteHandler("f"));
            builder.Add(typeof(float), new FloatWriteHandler());
            builder.Add(typeof(double), new DoubleWriteHandler());
            builder.Add(typeof(char), new ToStringWriteHandler("c"));
            builder.Add(typeof(Keyword), new KeywordWriteHandler(":"));
            builder.Add(typeof(Symbol), new ToStringWriteHandler("$"));
            builder.Add(typeof(byte[]), new BinaryWriteHandler());
            builder.Add(typeof(Guid), new GuidWriteHandler());
            builder.Add(typeof(Uri), new ToStringWriteHandler("r"));
            builder.Add(typeof(DateTime), new DateTimeWriteHandler());
            builder.Add(typeof(Ratio), new RatioWriteHandler());
            builder.Add(typeof(ILink), new LinkWriteHandler());
            builder.Add(typeof(Quote), new QuoteWriteHandler());
            builder.Add(typeof(ITaggedValue), new TaggedValueWriteHandler());

            builder.Add(typeof(IPersistentSet), new SetWriteHandler());
            builder.Add(typeof(ISet<>), new SetWriteHandler());
            builder.Add(typeof(IEnumerable), new EnumerableWriteHandler());
            builder.Add(typeof(IList<>), new ListWriteHandler());
            builder.Add(typeof(IDictionary<,>), new DictionaryWriteHandler());
            builder.Add(typeof(IDictionary), new DictionaryWriteHandler());
            builder.Add(typeof(IPersistentMap), new DictionaryWriteHandler());

            return builder.ToImmutable();
        }

        public static IImmutableDictionary<Type, IWriteHandler> Handlers(IDictionary<Type, IWriteHandler> customHandlers) 
        {
            if (customHandlers is Alpha.WriteHandlerMap whm)
                return whm;

            IImmutableDictionary<Type, IWriteHandler> handlers = DefaultHandlers();

            if (customHandlers != null)
            {
                handlers = handlers.RemoveRange(customHandlers.Keys).AddRange(customHandlers);
            }

            return handlers;
        }

        private static void SetSubHandler(IImmutableDictionary<Type, IWriteHandler> handlers, AbstractEmitter abstractEmitter) 
        {
            foreach (var handler in handlers)
        	{
		        if (handler.Value is IAbstractEmitterAware)
                {
                    ((IAbstractEmitterAware)handler.Value).SetEmitter(abstractEmitter);
                }
        	}
        }

        private static IImmutableDictionary<Type, IWriteHandler> GetVerboseHandlers(IImmutableDictionary<Type, IWriteHandler> handlers) 
        {
            var verboseHandlersBuilder = ImmutableDictionary.Create<Type, IWriteHandler>().ToBuilder();

            foreach (var item in handlers)
	        {
                verboseHandlersBuilder.Add(item.Key, item.Value.GetVerboseHandler() ?? item.Value);
	        }

            return verboseHandlersBuilder.ToImmutable();
        }

        public static IWriter<T> GetJsonInstance<T>(Stream output, IDictionary<Type, IWriteHandler> customHandlers, bool verboseMode,
            IWriteHandler defaultHandler, Func<object, object> transform)
        {
            TextWriter textWriter = new StreamWriter(output);
            JsonWriter jsonWriter = new JsonTextWriter(textWriter);
            IImmutableDictionary<Type, IWriteHandler> handlers = Handlers(customHandlers);
            JsonEmitter emitter;
            if (verboseMode)
            {
                emitter = new JsonVerboseEmitter(jsonWriter, GetVerboseHandlers(handlers), defaultHandler, transform);
            }
            else
            {
                emitter = new JsonEmitter(jsonWriter, handlers, defaultHandler, transform);
            }

            SetSubHandler(handlers, emitter);
            WriteCache wc = new WriteCache(!verboseMode);

            return new Writer<T>(output, emitter, wc);
        }

        public static IWriter<T> GetUtf8JsonInstance<T>(Stream utf8Json, IDictionary<Type, IWriteHandler> customHandlers, bool verboseMode,
            IWriteHandler defaultHandler, Func<object, object> transform)
        {
            var jsonWriter = new Utf8JsonWriter(utf8Json, 
                new JsonWriterOptions
                {
                    // Why UnsafeRelaxedJsonEscaping? The default encoder encodes some transit type declaration chars like `"`.
                    // "It can be used if the output data is within a response whose content-type is known with a charset set to UTF-8." - https://docs.microsoft.com/en-us/dotnet/api/system.text.encodings.web.javascriptencoder.unsaferelaxedjsonescaping?view=net-5.0
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                });
            IImmutableDictionary<Type, IWriteHandler> handlers = Handlers(customHandlers);
            AbstractEmitter emitter;
            if (verboseMode)
            {
                emitter = new Utf8JsonVerboseEmitter(jsonWriter, GetVerboseHandlers(handlers), defaultHandler, transform);
            }
            else
            {
                emitter = new Utf8JsonEmitter(jsonWriter, handlers, defaultHandler, transform);
            }

            SetSubHandler(handlers, emitter);
            WriteCache wc = new WriteCache(!verboseMode);

            return new Writer<T>(utf8Json, emitter, wc);
        }

        private class Writer<T> : IWriter<T>
        {
            private Stream output; 
            private IEmitter emitter;
            private WriteCache wc;

            public Writer (Stream output, IEmitter emitter, WriteCache wc)
	        {
                this.output = output;
                this.emitter = emitter;
                this.wc = wc;
	        }

            public void Write(T value)
            {
                emitter.Emit(value, false, wc.Init());
                output.Flush();
            }
        }
    }
}
