﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Beerendonk.Transit.Impl;
using Sellars.Transit.Cljr.Alpha;
using Sellars.Transit.Impl;
using Sellars.Transit.Impl.Alpha;
using Format = Sellars.Transit.Alpha.TransitFactory.Format;

namespace Sellars.Transit.Alpha
{
    public class Utf8TransitFactory
    {
        /// <summary>
        /// This implementation can only use UTF8.
        /// </summary>
        internal static System.Text.Encoding Encoding { get; } = System.Text.Encoding.UTF8;

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
        /// <returns>A writer.</returns>
        public static IWriter<T> Writer<T>(Format type, System.IO.Pipelines.PipeWriter output)
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
            switch (type)
            {
                case Format.MsgPack:
                    return WriterFactory.GetMsgPackInstance<T>(output, customHandlers, defaultHandler, transform);
                case Format.Json:
                    return Utf8WriterFactory.GetJsonInstance<T>(output, customHandlers, false, defaultHandler, transform);
                case Format.JsonVerbose:
                    return Utf8WriterFactory.GetJsonInstance<T>(output, customHandlers, true, defaultHandler, transform);
                default:
                    throw new ArgumentException("Unknown Writer type: " + type.ToString());
            }
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
        public static IWriter<T> Writer<T>(Format type, System.IO.Pipelines.PipeWriter output, IDictionary<Type, IWriteHandler> customHandlers,
            IWriteHandler defaultHandler, Func<object, object> transform)
        {
            switch (type)
            {
                case Format.MsgPack:
                    return WriterFactory.GetMsgPackInstance<T>(output, customHandlers, defaultHandler, transform);
                case Format.Json:
                    return Utf8WriterFactory.GetJsonInstance<T>(output, customHandlers, false, defaultHandler, transform);
                case Format.JsonVerbose:
                    return Utf8WriterFactory.GetJsonInstance<T>(output, customHandlers, true, defaultHandler, transform);
                default:
                    throw new ArgumentException("Unknown Writer type: " + type.ToString());
            }
        }

        public static IReader Reader(Format type, Stream input)
        {
            switch (type)
            {
                case Format.MsgPack:
                    return ReaderFactory.GetMsgPackInstance(input, default, default);
                case Format.Json:
                case Format.JsonVerbose:
                    return Utf8ReaderFactory.GetJsonInstance(new Utf8JsonStreamReader(input), default, default);
                default:
                    throw new ArgumentException("Unknown Writer type: " + type.ToString());
            }
        }

        public static IReader Reader(Format type, Stream input,
            System.Collections.Immutable.IImmutableDictionary<string, IReadHandler> customHandlers,
            IDefaultReadHandler<object> customDefaultHandler)
        {
            switch (type)
            {
                case Format.MsgPack:
                    return ReaderFactory.GetMsgPackInstance(input, customHandlers, customDefaultHandler);
                case Format.Json:
                case Format.JsonVerbose:
                    return Utf8ReaderFactory.GetJsonInstance(new Utf8JsonStreamReader(input), customHandlers, Cljr.Impl.DefaultReadHandlerAdapter.Adapt(customDefaultHandler));
                default:
                    throw new ArgumentException("Unknown Writer type: " + type.ToString());
            }
        }

        public static IReader Reader(Format type, byte[] input,
            System.Collections.Immutable.IImmutableDictionary<string, IReadHandler> customHandlers,
            IDefaultReadHandler<object> customDefaultHandler)
        {
            switch (type)
            {
                case Format.MsgPack:
                    return ReaderFactory.GetMsgPackInstance(input, customHandlers, customDefaultHandler);
                case Format.Json:
                case Format.JsonVerbose:
                    return Utf8ReaderFactory.GetJsonInstance(new Utf8JsonArrayReader(input), customHandlers, Cljr.Impl.DefaultReadHandlerAdapter.Adapt(customDefaultHandler));
                default:
                    throw new ArgumentException("Unknown Writer type: " + type.ToString());
            }
        }

        public static IReader Reader(Format type, System.IO.Pipelines.PipeReader input,
            System.Collections.Immutable.IImmutableDictionary<string, IReadHandler> customHandlers,
            IDefaultReadHandler<object> customDefaultHandler)
        {
            switch (type)
            {
                case Format.MsgPack:
                    return ReaderFactory.GetMsgPackInstance(input.AsStream(), customHandlers, customDefaultHandler);
                case Format.Json:
                case Format.JsonVerbose:
                    return Utf8ReaderFactory.GetJsonInstance(new Utf8JsonPipeReader(input), customHandlers, Cljr.Impl.DefaultReadHandlerAdapter.Adapt(customDefaultHandler));
                default:
                    throw new ArgumentException("Unknown Writer type: " + type.ToString());
            }
        }
    }
}
