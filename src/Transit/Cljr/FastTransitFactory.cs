using System;
using System.Collections.Generic;
using System.IO;
using Sellars.Transit.Alpha;
using Sellars.Transit.Cljr.Impl;
using Format = Sellars.Transit.Alpha.TransitFactory.Format;
using WriterFactory = Sellars.Transit.Cljr.Impl.WriterFactory;

namespace Sellars.Transit.Cljr.Alpha
{
    public class FastTransitFactory
    {
        public static IWriter Writer(Format type, Stream output) =>
            Writer(type, output, null);

        /// <summary>
        /// Creates a writer instance.
        /// </summary>
        /// <param name="type">The format to write in.</param>
        /// <param name="output">The output stream to write to.</param>
        /// <returns>A writer.</returns>
        public static IWriter<T> TypedWriter<T>(Format type, Stream output)
        {
            return TypedWriter<T>(type, output, null);
        }

        public static IWriter Writer(Format type, Stream output, object customHandlers,
            IWriteHandler defaultHandler = null, Func<object, object> transform = null) =>
            new TypedWriterWrapper<object>(TypedWriter<object>(type, output,
                WriteHandlers(customHandlers),
                defaultHandler,
                transform));

        internal static IDictionary<Type, IWriteHandler> WriteHandlers(object customHandlers) =>
            TransitFactory.WriteHandlers(customHandlers);

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
        public static IWriter<T> TypedWriter<T>(Format type, Stream output, IDictionary<Type, IWriteHandler> customHandlers,
            IWriteHandler defaultHandler = null, Func<object, object> transform = null)
        {
            switch (type)
            {
                case Format.MsgPack:
                    return WriterFactory.GetMsgPackInstance<T>(output, customHandlers,
                        defaultHandler, transform);
                case Format.Json:
                    return WriterFactory.GetUtf8JsonInstance<T>(output, customHandlers, false,
                        defaultHandler, transform);
                case Format.JsonVerbose:
                    return WriterFactory.GetUtf8JsonInstance<T>(output, customHandlers, true,
                        defaultHandler, transform);
                default:
                    throw new ArgumentException("Unknown Writer type: " + type.ToString());
            }
        }

        public static IReader Reader(Format type, Stream input) =>
            Reader(type, input, default, default);

        public static IReader Reader(Format type, Stream input, 
            object customHandlers,
            IDefaultReadHandler customDefaultHandler)
        {
            switch (type)
            {
                case Format.MsgPack:
                    return ReaderFactory.GetMsgPackInstance(input, TransitFactory.ReadHandlerMap(customHandlers), customDefaultHandler);
                case Format.Json:
                case Format.JsonVerbose:
                    return ReaderFactory.GetFastJsonInstance(input, TransitFactory.ReadHandlerMap(customHandlers), customDefaultHandler);
                default:
                    throw new ArgumentException("Unknown Writer type: " + type.ToString());
            }
        }
    }
}
