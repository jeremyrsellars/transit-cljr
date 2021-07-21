using System.Collections.Immutable;
using System.IO;
using Sellars.Transit.Alpha;
using Beerendonk.Transit.Impl;
using Sellars.Transit.Impl;
using MessagePack;

namespace Beerendonk.Transit.Impl
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
    }
}
