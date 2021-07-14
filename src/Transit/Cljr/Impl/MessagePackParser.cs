using System.Collections.Immutable;
using MessagePack;
using Sellars.Transit.Alpha;
using Beerendonk.Transit.Impl;
using System.Threading;
using System;

namespace Sellars.Transit.Impl
{
    /// <summary>
    /// Represents a JSON parser.
    /// </summary>
    internal class MessagePackParser : AbstractParser
    {
        private readonly MessagePackStreamReader streamReader;
        private readonly MessagePackSerializerOptions options;

        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackParser"/> class.
        /// </summary>
        /// <param name="reader">The json text reader.</param>
        /// <param name="handlers">The handlers.</param>
        /// <param name="defaultHandler">The default handler.</param>
        /// <param name="dictionaryBuilder">The dictionary builder.</param>
        /// <param name="listBuilder">The list builder.</param>
        public MessagePackParser(
            MessagePackStreamReader reader,
            MessagePackSerializerOptions options,
            IImmutableDictionary<string, IReadHandler> handlers,
            IDefaultReadHandler<object> defaultHandler,
            IDictionaryReader dictionaryBuilder,
            IListReader listBuilder)
            : base(handlers, defaultHandler, dictionaryBuilder, listBuilder)
        {
            streamReader = reader;
            this.options = options ?? MessagePackSerializerOptions.Standard;
        }

        /// <summary>
        /// Parses the specified cache.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        public override object Parse(ReadCache cache)
        {
            var bytes = streamReader.ReadAsync(CancellationToken).Result;
            if (!bytes.HasValue)
                return null;

            var rdr = new MessagePackReader(bytes.Value);
            switch(rdr.NextMessagePackType)
            {
                //MessagePackType.Unknown: return ParseUnknown(ref rdr, cache);
                case MessagePackType.Integer:
                    return ParseInteger(ref rdr, cache);
                case MessagePackType.Nil:
                    rdr.ReadNil();
                    return null;
                case MessagePackType.Boolean:
                    return ParseBoolean(ref rdr, cache);
                case MessagePackType.Float:
                    return ParseFloat(ref rdr, cache);
                case MessagePackType.String:
                    return cache.CacheRead(ParseString(ref rdr, cache), false, this);
                case MessagePackType.Binary:
                    return ParseBinary(ref rdr, options, cache);
                case MessagePackType.Array:
                    return ParseArray(ref rdr, options, false, cache, null);
                case MessagePackType.Map:
                    return ParseMap(ref rdr, options, false, cache, null);
                case MessagePackType.Extension:
                    return ParseExtension(ref rdr, options, cache);
                default:
                    return ParseUnknown(ref rdr, options, cache);
            }
        }

        internal static object ParseUnknown(ref MessagePackReader rdr, MessagePackSerializerOptions options, ReadCache cache) =>
            throw new NotSupportedException($"Not supported/implemented.  Code: {rdr.NextCode}. Type: {rdr.NextMessagePackType}.");

        internal static long ParseInteger(ref MessagePackReader rdr, ReadCache cache) =>
            rdr.ReadInt64();

        internal static bool ParseBoolean(ref MessagePackReader rdr, ReadCache cache) =>
            rdr.ReadBoolean();

        internal static double ParseFloat(ref MessagePackReader rdr, ReadCache cache) =>
            rdr.ReadDouble();

        internal static string ParseString(ref MessagePackReader rdr, ReadCache cache) =>
            rdr.ReadString();

        internal object ParseBinary(ref MessagePackReader rdr, MessagePackSerializerOptions options, ReadCache cache) =>
            throw new NotSupportedException($"Not supported/implemented.  Code: {rdr.NextCode}. Type: {rdr.NextMessagePackType}.");

        internal object ParseArray(ref MessagePackReader rdr, MessagePackSerializerOptions options, bool asDictionaryKey, ReadCache cache, IListReadHandler handler)
        {
            var count = rdr.ReadArrayHeader();
            options.Security.DepthStep(ref rdr);
            try
            {
                if (count >= 1)
                {
                    object firstVal = ParseVal(ref rdr, options, false, cache);
                    if (firstVal != null)
                    {
                        if (firstVal is string fvs && fvs == Constants.DirectoryAsList)
                        {
                            // if the same, build a map w/ rest of array contents
                            return ParseArrayAsDictionary(ref rdr, options, false, cache, dictionaryBuilder, count - 1);
                        }
                        else if (firstVal is Tag)
                        {
                            if (firstVal is Tag)
                            {
                                object val;
                                //jp.Read(); // advance to value
                                string tag = ((Tag)firstVal).GetValue();
                                IReadHandler val_handler;
                                if (TryGetHandler(tag, out val_handler))
                                {
                                    if (rdr.NextMessagePackType == MessagePackType.Map && val_handler is IDictionaryReadHandler dictHandler)
                                    //if (this.jp.TokenType == JsonToken.StartObject && val_handler is IDictionaryReadHandler)
                                    {
                                        // use map reader to decode value
                                        val = ParseArrayAsDictionary(ref rdr, options, false, cache, dictHandler.DictionaryReader(), count - 1);
                                    }
                                    //else if (this.jp.TokenType == JsonToken.StartArray && val_handler is IListReadHandler)
                                    else if (rdr.NextMessagePackType == MessagePackType.Array && val_handler is IListReadHandler listHandler)
                                    {
                                        // use array reader to decode value
                                        val = ParseArray(ref rdr, options, false, cache, listHandler);
                                    }
                                    else
                                    {
                                        // read value and decode normally
                                        val = val_handler.FromRepresentation(ParseVal(ref rdr, options, false, cache));
                                    }
                                }
                                else
                                {
                                    // default decode
                                    val = this.Decode(tag, ParseVal(ref rdr, options, false, cache));
                                }
                                //jp.Read(); // advance past end of object or array
                                return val;
                            }
                        }
                    }

                    // Process list w/o special decoding or interpretation
                    IListReader lr = (handler != null) ? handler.ListReader() : listBuilder;
                    object l = lr.Init();
                    l = lr.Add(l, firstVal);
                    for(int i = 1; i < count; i++)
                    {
                        object item = ParseVal(ref rdr, options, false, cache);
                        l = lr.Add(l, item);
                    }
                    return lr.Complete(l);
                }

                // Make an empty collection, honoring handler's ListReader, if present
                IListReader lr2 = (handler != null) ? handler.ListReader() : listBuilder;
                return lr2.Complete(lr2.Init());
            }
            finally
            {
                rdr.Depth--;
            }
        }

        internal object ParseMap(ref MessagePackReader rdr, MessagePackSerializerOptions options, bool asDictionaryKey, ReadCache cache, IDictionaryReadHandler handler)
        {
            IDictionaryReader dr = (handler != null) ? handler.DictionaryReader() : dictionaryBuilder;

            object d = dr.Init();

            var count = rdr.ReadMapHeader();

            options.Security.DepthStep(ref rdr);
            try
            {
                //while (jp.NextToken() != endToken)
                for (int i = 0; i < count; i++)
                {
                    object key = ParseVal(ref rdr, options, true, cache);
                    if (key is Tag)
                    {
                        object val;
                        //jp.Read(); // advance to read value
                        string tag = ((Tag)key).GetValue();
                        IReadHandler val_handler;
                        if (TryGetHandler(tag, out val_handler))
                        {
                            //if (this.jp.TokenType == JsonToken.StartObject && val_handler is IDictionaryReadHandler dictHandler)
                            if (rdr.NextMessagePackType == MessagePackType.Map && val_handler is IDictionaryReadHandler dictHandler)
                            {
                                // use map reader to decode value
                                val = ParseMap(ref rdr, options, false, cache, dictHandler);
                            }
                            //else if (this.jp.TokenType == JsonToken.StartArray && val_handler is IListReadHandler listHandler)
                            else if (rdr.NextMessagePackType == MessagePackType.Array && val_handler is IListReadHandler listHandler)
                            {
                                // use array reader to decode value
                                val = ParseArray(ref rdr, options, false, cache, listHandler);
                            }
                            else
                            {
                                // read value and decode normally
                                val = val_handler.FromRepresentation(ParseVal(ref rdr, options, false, cache));
                            }
                        }
                        else
                        {
                            // default decode
                            val = this.Decode(tag, ParseVal(ref rdr, options, false, cache));
                        }
                        //jp.Read(); // advance to read end of object or array
                        return val;
                    }
                    else
                    {
                        //jp.Read(); // advance to read value
                        d = dr.Add(d, key, ParseVal(ref rdr, options, false, cache));
                    }
                }
            }
            finally
            {
                rdr.Depth--;
            }

            return dr.Complete(d);
        }

        internal object ParseExtension(ref MessagePackReader rdr, MessagePackSerializerOptions options, ReadCache cache) =>
            throw new NotSupportedException($"Not supported/implemented.  Code: {rdr.NextCode}. Type: {rdr.NextMessagePackType}.");

        /// <summary>
        /// Parses the value.
        /// </summary>
        /// <param name="asDictionaryKey">If set to <c>true</c> [as dictionary key].</param>
        /// <param name="cache">The cache.</param>
        /// <returns>
        /// The parsed value.
        /// </returns>
        public override object ParseVal(bool asDictionaryKey, ReadCache cache)
        {
            var bytes = streamReader.ReadAsync(CancellationToken).Result;
            if (!bytes.HasValue)
                return null;

            var rdr = new MessagePackReader(bytes.Value);
            return ParseVal(ref rdr, options, asDictionaryKey, cache);
        }

        internal object ParseVal(ref MessagePackReader rdr, MessagePackSerializerOptions options, bool asDictionaryKey, ReadCache cache)
        {
            switch (rdr.NextMessagePackType)
            {
                //MessagePackType.Unknown: return ParseUnknown(ref rdr, options, cache);
                case MessagePackType.Integer:
                    return ParseInteger(ref rdr, cache);
                case MessagePackType.Nil:
                    rdr.ReadNil();
                    return null;
                case MessagePackType.Boolean:
                    return ParseBoolean(ref rdr, cache);
                case MessagePackType.Float:
                    return ParseFloat(ref rdr, cache);
                case MessagePackType.String:
                    return cache.CacheRead(ParseString(ref rdr, cache), asDictionaryKey, this);
                case MessagePackType.Binary:
                    return ParseBinary(ref rdr, options, cache);
                case MessagePackType.Array:
                    return ParseArray(ref rdr, options, asDictionaryKey, cache, null);
                case MessagePackType.Map:
                    return ParseMap(ref rdr, options, asDictionaryKey, cache, null);
                case MessagePackType.Extension:
                    return ParseExtension(ref rdr, options, cache);
                default:
                    return ParseUnknown(ref rdr, options, cache);
            }
        }

        /// <summary>
        /// Parses the dictionary.
        /// </summary>
        /// <param name="ignored">if set to <c>true</c> [ignored].</param>
        /// <param name="cache">The cache.</param>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        public override object ParseDictionary(bool ignored, ReadCache cache, IDictionaryReadHandler handler)
        {
            throw new NotImplementedException($"Not implemented.");
        }

        private object ParseArrayAsDictionary(ref MessagePackReader rdr, MessagePackSerializerOptions options, bool ignored, ReadCache cache, IDictionaryReader dictReader, int arrayCountLeftToParse)
        {
            if (dictReader == null)
                throw new ArgumentNullException(nameof(dictReader));

            // Will it enter here? Maybe for cmap?
            var dictionary = dictReader.Init();
            //if (arrayCountLeftToParse % 2 == 1)
            //    throw new TransitException($"Unexpected Dictionary count in array: {arrayCountLeftToParse}");

            options.Security.DepthStep(ref rdr);
            try
            {
                for (; arrayCountLeftToParse >= 0; arrayCountLeftToParse -= 1)
                {
                    var key = ParseVal(ref rdr, options, true, cache);
                    var value = ParseVal(ref rdr, options, false, cache);
                    dictReader.Add(dictionary, key, value);
                }
            }
            finally
            {
                rdr.Depth--;
            }
            return dictReader.Complete(dictionary);
        }

        /// <summary>
        /// Parses the list.
        /// </summary>
        /// <param name="asDictionaryKey">If set to <c>true</c> [as dictionary key].</param>
        /// <param name="cache">The cache.</param>
        /// <param name="handler">The handler.</param>
        /// <returns>
        /// The parsed list.
        /// </returns>
        public override object ParseList(bool asDictionaryKey, ReadCache cache, IListReadHandler handler)
        {
            throw new NotImplementedException($"Not implemented.");
        }
    }
}
