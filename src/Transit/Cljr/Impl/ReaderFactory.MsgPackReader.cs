using System.Collections.Immutable;
using System.IO;
using Sellars.Transit.Alpha;
using Beerendonk.Transit.Impl;
using Sellars.Transit.Impl;
using MessagePack;

namespace Sellars.Transit.Cljr.Impl
{
    /// <summary>
    /// Represents a reader factory.
    /// </summary>
    partial class ReaderFactory
    {
        internal class MsgPackReader : Reader
        {
            public MsgPackReader(Stream input, IImmutableDictionary<string, IReadHandler> handlers, IDefaultReadHandler<object> defaultHandler,
                MessagePackSerializerOptions options)
                : base(input, handlers, defaultHandler)
            {
                Options = options;
            }

            public MessagePackSerializerOptions Options { get; }

            protected override IParser CreateParser()
            {
                var msgpackStreamReader = new MessagePackStreamReader(input);
                return new MessagePackParser(msgpackStreamReader, Options, handlers, defaultHandler, 
                    dictionaryBuilder, listBuilder);
            }
        }

        internal static IReader GetMsgPackInstance(Stream input, IImmutableDictionary<string, IReadHandler> customHandlers, IDefaultReadHandler<object> defaultHandler) =>
            new MsgPackReader(input, customHandlers, defaultHandler ?? DefaultDefaultHandler(), default);
    }
}
