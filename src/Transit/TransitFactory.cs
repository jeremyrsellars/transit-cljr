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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Beerendonk.Transit.Impl;
using Sellars.Transit.Spi.Alpha;
using clojure.lang;
using Sellars.Transit.Util;

namespace Sellars.Transit.Alpha
{
    /// <summary>
    /// Main entry point for using transit-cljr library. Provides methods to construct
    /// readers and writers, as well as helpers to make various other values.
    /// </summary>
    public static class TransitFactory
    {
        /// <summary>
        /// Transit formats.
        /// </summary>
        public enum Format 
        { 
            /// <summary>
            /// JSON
            /// </summary>
            Json, 

            /// <summary>
            /// MessagePack
            /// </summary>
            MsgPack, 

            /// <summary>
            /// JSON Verbose
            /// </summary>
            JsonVerbose 
        }

        /// <summary>
        /// Creates a writer instance.
        /// </summary>
        /// <param name="type">The format to write in.</param>
        /// <param name="output">The output stream to write to.</param>
        /// <returns>A writer.</returns>
        public static IWriter<T> Writer<T>(Format type, Stream output)
        {
            return Writer<T>(type, output, null, null, null);
        }
        
        /// <summary>
        /// Creates a writer instance.
        /// </summary>
        /// <param name="type">The format to write in.</param>
        /// <param name="output">The output stream to write to.</param>
        /// <param name="customHandlers">Additional IWriteHandlers to use in addition 
        /// to or in place of the default IWriteHandlers.</param>
        /// <returns>A writer</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <exception cref="System.ArgumentException">Unknown Writer type:  + type.ToString()</exception>
        public static IWriter<T> Writer<T>(Format type, Stream output, IDictionary<Type, IWriteHandler> customHandlers,
            IWriteHandler defaultHandler, Func<object, object> transform)
        {
            switch (type) {
                case Format.MsgPack:
                    return WriterFactory.GetMsgPackInstance<T>(output, customHandlers, defaultHandler, transform);
                case Format.Json:
                    return WriterFactory.GetJsonInstance<T>(output, customHandlers, false, defaultHandler, transform);
                case Format.JsonVerbose:
                    return WriterFactory.GetJsonInstance<T>(output, customHandlers, true, defaultHandler, transform);
                default:
                    throw new ArgumentException("Unknown Writer type: " + type.ToString());
            }
        }

        /// <summary>
        /// Creates a reader instance.
        /// </summary>
        /// <param name="type">The format to read in.</param>
        /// <param name="input">The stream to read from.</param>
        /// <returns>A reader</returns>
        public static IReader Reader(Format type, Stream input) 
        {
            return Reader(type, input, DefaultDefaultReadHandler());
        }

        /// <summary>
        /// Creates a reader instance.
        /// </summary>
        /// <param name="type">The format to read in.</param>
        /// <param name="input">The input stream to read from.</param>
        /// <param name="customDefaultHandler">
        /// A DefaultReadHandler to use for processing encoded values for which there is no read handler
        /// </param>
        /// <returns>A reader</returns>
        public static IReader Reader(Format type, Stream input, IDefaultReadHandler<object> customDefaultHandler)
        {
            return Reader(type, input, null, customDefaultHandler);
        }

        private class DeferredJsonReader : IReader, IReaderSpi {
            private Stream input;
            private IImmutableDictionary<string, IReadHandler> customHandlers;
            private IDefaultReadHandler<object> customDefaultHandler;
            private IReader reader;
            private IDictionaryReader dictionaryBuilder;
            private IListReader listBuilder;

            public DeferredJsonReader(Stream input, IImmutableDictionary<string, IReadHandler> customHandlers, IDefaultReadHandler<object> customDefaultHandler)
            {
                this.input = input;
                this.customHandlers = customHandlers;
                this.customDefaultHandler = customDefaultHandler;
            }

            public T Read<T>() 
            {
                if (reader == null) 
                {
                    reader = ReaderFactory.GetJsonInstance(input, customHandlers, customDefaultHandler);
                    if ((dictionaryBuilder != null) || (listBuilder != null)) 
                    {
                        ((IReaderSpi)reader).SetBuilders(dictionaryBuilder, listBuilder);
                    }
                }
                return reader.Read<T>();
            }

            object IReader.Read() => Read<object>();

            public IReader SetBuilders(
                IDictionaryReader dictionaryBuilder,
                IListReader listBuilder) 
            {
                this.dictionaryBuilder = dictionaryBuilder;
                this.listBuilder = listBuilder;
                return this;
            }
        }

        /// <summary>
        /// Creates a reader instance.
        /// </summary>
        /// <param name="type">The format to read in.</param>
        /// <param name="input">The input stream to read from.</param>
        /// <param name="customHandlers">
        /// A dictionary of custom ReadHandlers to use in addition or in place of the default ReadHandlers.
        /// </param>
        /// <param name="customDefaultHandler">
        /// A DefaultReadHandler to use for processing encoded values for which there is no read handler.
        /// </param>
        /// <returns>A reader.</returns>
        public static IReader Reader(Format type, Stream input,
                                    IImmutableDictionary<string, IReadHandler> customHandlers,
                                    IDefaultReadHandler<object> customDefaultHandler) 
        {
            switch (type) {
                case Format.Json:
                case Format.JsonVerbose:
                    // TODO: Check if this is true in C# too.
                    // JSON parser creation blocks on input stream until 4 bytes
                    // are available to determine character encoding - this is
                    // unexpected, so defer creation until first read
                    return new DeferredJsonReader(input, customHandlers, customDefaultHandler);
                case Format.MsgPack:
                    return ReaderFactory.GetMsgPackInstance(input, customHandlers, customDefaultHandler);
                default:
                    throw new ArgumentException("Unknown Reader type: " + type.ToString());
            }
        }

        /// <summary>
        /// Converts a <see cref="string"/> or <see cref="Keyword"/> to an <see cref="Keyword"/>.
        /// </summary>
        /// <param name="obj">A string or a keyword.</param>
        /// <returns>A keyword.</returns>
        public static Keyword Keyword(object obj)
        {
            if (obj is Keyword kw)
                return kw;
            if (obj is Symbol sym)
                return clojure.lang.Keyword.intern(sym);
            if (obj is string)
            {
                string s = (string)obj;

                if (s[0] == ':')
                    return ParseKeyword(s.Substring(1));
                else
                    return ParseKeyword(s);
            }
            else
            {
                throw new TransitException("Cannot make keyword from " + obj.GetType().ToString());
            }
        }

        private static Keyword ParseKeyword(string v)
        {
            var split = v.Split(new[] { '/' }, 2);
            return split.Length == 1 ? RT.keyword(null, split[0]) : RT.keyword(split[0], split[1]);
        }

        /// <summary>
        /// Converts a <see cref="string"/> or <see cref="ISymbol"/> to an <see cref="ISymbol"/>.
        /// </summary>
        /// <param name="obj">A string or a symbol.</param>
        /// <returns>A symbol.</returns>
        public static Symbol Symbol(object obj)
        {
            if (obj is Symbol)
            {
                return (Symbol)obj;
            }
            else
            {
                if (obj is string)
                {
                    string s = (string)obj;

                    if (s[0] == ':')
                        return clojure.lang.Symbol.create(s.Substring(1));
                    else
                        return clojure.lang.Symbol.create(s);
                }
                else
                {
                    throw new TransitException("Cannot make symbol from " + obj.GetType().ToString());
                }
            }
        }

        /// <summary>
        /// Creates an <see cref="ITaggedValue"/>.
        /// </summary>
        /// <param name="tag">Tag string.</param>
        /// <param name="representation">Value representation.</param>
        /// <returns>A tagged value.</returns>
        public static ITaggedValue TaggedValue(string tag, object representation) {
            return new TaggedValue(tag, representation);
        }

        /// <summary>
        /// Creates a <see cref="ILink"/>.
        /// </summary>
        /// <param name="href">The href.</param>
        /// <param name="rel">The relative.</param>
        /// <returns>An <see cref="ILink"/> instance.</returns>
        public static ILink Link(string href, string rel)
        {
            return Link(href, rel, null, null, null);
        }

        /// <summary>
        /// Creates a <see cref="ILink"/>.
        /// </summary>
        /// <param name="href">The href.</param>
        /// <param name="rel">The relative.</param>
        /// <returns>An <see cref="ILink"/> instance.</returns>
        public static ILink Link(Uri href, string rel)
        {
            return Link(href, rel, null, null, null);
        }

        /// <summary>
        /// Creates a <see cref="ILink"/>.
        /// </summary>
        /// <param name="href">The href.</param>
        /// <param name="rel">The rel.</param>
        /// <param name="name">The optional name.</param>
        /// <param name="prompt">The optional prompt.</param>
        /// <param name="render">The optional render.</param>
        /// <returns>An <see cref="ILink"/> instance.</returns>
        public static ILink Link(string href, string rel, string name, string prompt, string render)
        {
            return Link(new Uri(href), rel, name, prompt, render);
        }

        /// <summary>
        /// Creates a <see cref="ILink"/>.
        /// </summary>
        /// <param name="href">The href.</param>
        /// <param name="rel">The rel.</param>
        /// <param name="name">The optional name.</param>
        /// <param name="prompt">The optional prompt.</param>
        /// <param name="render">The optional render.</param>
        /// <returns>An <see cref="ILink"/> instance.</returns>
        public static ILink Link(Uri href, string rel, string name, string prompt, string render) 
        {
            return new Link(href, rel, name, prompt, render);
        }

        /// <summary>
        /// Creates a <see cref="IRatio"/>.
        /// </summary>
        /// <param name="numerator">The numerator.</param>
        /// <param name="denominator">The denominator.</param>
        /// <returns>An <see cref="IRatio"/> instance.</returns>
        public static Ratio Ratio(clojure.lang.BigInteger numerator, clojure.lang.BigInteger denominator) 
        {
            return new Ratio(numerator, denominator);
        }

        /// <summary>
        /// Returns a directory of classes to read handlers that is used by default.
        /// </summary>
        /// <returns>Tag to read handler directory.</returns>
        public static IImmutableDictionary<string, IReadHandler> DefaultReadHandlers() 
        { 
            return ReaderFactory.DefaultHandlers(); 
        }

        /// <summary>
        /// Returns a directory of classes to write handlers that is used by default.
        /// </summary>
        /// <returns>Class to write handler directory.</returns>
        public static IImmutableDictionary<Type, IWriteHandler> DefaultWriteHandlers() 
        {
            return WriterFactory.DefaultHandlers(); 
        }

        /// <summary>
        /// Returns the <see cref="IDefaultReadHandler{T}"/> of <see cref="ITaggedValue"/> that is used by default.
        /// </summary>
        /// <returns><see cref="IDefaultReadHandler{T}"/> of <see cref="ITaggedValue"/> instance.</returns>
        public static IDefaultReadHandler<ITaggedValue> DefaultDefaultReadHandler()
        {
            return ReaderFactory.DefaultDefaultHandler();
        }

        public static Cljr.Impl.Alpha.WriteHandlerMap WriteHandlerMap(object handlerMap) =>
            handlerMap is Cljr.Impl.Alpha.WriteHandlerMap whm
            ? whm
            : Cljr.Impl.Alpha.WriteHandlerMap.Create(
                DictionaryHelper.CoerceDictionary<Type, IWriteHandler>(handlerMap), 
                WriterFactory.Handlers);

        public static Cljr.Impl.Alpha.ReadHandlerMap ReadHandlerMap(object handlerMap) =>
            handlerMap is Cljr.Impl.Alpha.ReadHandlerMap whm
            ? whm
            : Cljr.Impl.Alpha.ReadHandlerMap.Create(
                DictionaryHelper.CoerceIImmutableDictionary<string, IReadHandler>(handlerMap),
                ReaderFactory.Handlers);
    }
}
