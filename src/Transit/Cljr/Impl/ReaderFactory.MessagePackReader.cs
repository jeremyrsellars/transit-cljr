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
        private class MessagePackReader : Reader
        {
            public MessagePackReader(Stream input, IImmutableDictionary<string, IReadHandler> handlers, IDefaultReadHandler<object> defaultHandler)
                : base(input, handlers, defaultHandler)
            {
            }

            protected override IParser CreateParser()
            {
                var msgpackStreamReader = new MessagePackStreamReader(input);
                return new MessagePackParser(msgpackStreamReader, handlers, defaultHandler, 
                    dictionaryBuilder, listBuilder);
            }
        }
    }
}
