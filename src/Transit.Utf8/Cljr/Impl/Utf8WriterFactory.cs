using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using Beerendonk.Transit.Impl;
using Sellars.Transit.Alpha;
using Sellars.Transit.Cljr.Impl;
using static Sellars.Transit.Cljr.Impl.WriterFactory;

namespace Sellars.Transit.Cljr.Alpha
{
    internal class Utf8WriterFactory
    {
        public static IWriter<T> GetJsonInstance<T>(Stream utf8Json, IDictionary<Type, IWriteHandler> customHandlers, bool verboseMode,
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

            return new Writer<T>(utf8Json, emitter, wc, utf8Json.Flush);
        }

        public static IWriter<T> GetJsonInstance<T>(System.IO.Pipelines.PipeWriter utf8Json, IDictionary<Type, IWriteHandler> customHandlers, bool verboseMode,
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

            return new Writer<T>(null, emitter, wc, () =>
            {
                jsonWriter.Flush();
                var forceFlush = utf8Json.FlushAsync().Result;
            });
        }
    }
}