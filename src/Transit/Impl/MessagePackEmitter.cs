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
using System.Linq;
using Beerendonk.Transit.Impl;
using Sellars.Transit.Alpha;
using System.IO.Pipelines;
using MessagePack;
using Sellars.Transit.Util;

namespace Sellars.Transit.Impl
{
    /// <summary>
    /// Represents a MessagePack emitter.
    /// </summary>
    internal class MessagePackEmitter : AbstractEmitter
    {
        private static readonly System.Text.Encoding Encoding = System.Text.Encoding.UTF8;
        private static readonly long JsonIntMax = (long)Math.Pow(2, 53);
        private static readonly long JsonIntMin = -JsonIntMax;

        private PipeWriter pipeWriter;

        public MessagePackEmitter(Stream stream, IImmutableDictionary<Type, IWriteHandler> handlers)
            : base(handlers)
        {
            pipeWriter = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        }

        public override void Emit(object obj, bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            MarshalTop(ref writer, obj, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }
        
        protected void EmitTagged(ref MessagePackWriter writer, string t, object obj, bool ignored, WriteCache cache)
        {
            EmitListStart(ref writer, 2L);
            EmitString(ref writer, Constants.EscTag, t, "", false, cache);
            Marshal(ref writer, obj, false, cache);
        }

        protected void EmitEncoded(ref MessagePackWriter writer, string t, IWriteHandler handler, object obj, bool asDictionaryKey, WriteCache cache)
        {
            if (t.Length == 1)
            {
                object r = handler.Representation(obj);
                if (r is string)
                {
                    EmitString(ref writer, Constants.EscStr, t, (string)r, asDictionaryKey, cache);
                }
                else if (PrefersStrings() || asDictionaryKey)
                {
                    string sr = handler.StringRepresentation(obj);
                    if (sr != null)
                    {
                        EmitString(ref writer, Constants.EscStr, t, sr, asDictionaryKey, cache);
                    }
                    else
                    {
                        throw new TransitException("Cannot be encoded as a string " + obj);
                    }
                }
                else
                {
                    EmitTagged(ref writer, t, r, asDictionaryKey, cache);
                }
            }
            else
            {
                if (asDictionaryKey)
                {
                    throw new TransitException("Cannot be used as a map key " + obj);
                }
                else
                {
                    EmitTagged(ref writer, t, handler.Representation(obj), asDictionaryKey, cache);
                }
            }
        }

        private void EmitDictionary(ref MessagePackWriter writer, object keyValuePairEnumerable, bool ignored, WriteCache cache) =>
            EmitDictionary(ref writer,
                DictionaryHelper.CoerceKeyValuePairs(keyValuePairEnumerable, CoerceKeyValuePair),
                ignored, cache);


        protected void EmitList(ref MessagePackWriter writer, object o, bool ignored, WriteCache cache)
        {
            var enumerable = o as System.Collections.IEnumerable;
            var length = enumerable.Cast<object>().Count();

            EmitListStart(ref writer, length);

            if (o is IEnumerable<int>)
            {
                foreach (var n in (IEnumerable<int>)o)
                {
                    EmitInteger(ref writer, n, false, cache);
                }
            }
            else if (o is IEnumerable<short>)
            {
                foreach (var n in (IEnumerable<short>)o)
                {
                    EmitInteger(ref writer, n, false, cache);
                }
            }
            else if (o is IEnumerable<long>)
            {
                foreach (var n in (IEnumerable<long>)o)
                {
                    EmitInteger(ref writer, n, false, cache);
                }
            }
            else if (o is IEnumerable<float>)
            {
                foreach (var n in (IEnumerable<float>)o)
                {
                    EmitDouble(ref writer, n, false, cache);
                }
            }
            else if (o is IEnumerable<double>)
            {
                foreach (var n in (IEnumerable<double>)o)
                {
                    EmitDouble(ref writer, n, false, cache);
                }
            }
            else if (o is IEnumerable<bool>)
            {
                foreach (var n in (IEnumerable<bool>)o)
                {
                    EmitBoolean(ref writer, n, false, cache);
                }
            }
            else if (o is IEnumerable<char>)
            {
                foreach (var n in (IEnumerable<char>)o)
                {
                    Marshal(ref writer, n, false, cache);
                }
            }
            else
            {
                foreach (var n in enumerable)
                {
                    Marshal(ref writer, n, false, cache);
                }
            }
        }

        protected void Marshal(ref MessagePackWriter writer, object o, bool asDictionaryKey, WriteCache cache)
        {
            bool supported = false;

            IWriteHandler h = GetHandler(o);
            if (h != null)
            {
                string t = h.Tag(o);
                if (t != null)
                {
                    supported = true;
                    if (t.Length == 1)
                    {
                        switch (t[0])
                        {
                            case '_': EmitNull(ref writer, asDictionaryKey, cache); break;
                            case 's': EmitString(ref writer, null, null, Escape((string)h.Representation(o)), asDictionaryKey, cache); break;
                            case '?': EmitBoolean(ref writer, (bool)h.Representation(o), asDictionaryKey, cache); break;
                            case 'i': EmitInteger(ref writer, h.Representation(o), asDictionaryKey, cache); break;
                            case 'd': EmitDouble(ref writer, h.Representation(o), asDictionaryKey, cache); break;
                            case 'b': EmitBinary(ref writer, h.Representation(o), asDictionaryKey, cache); break;
                            case '\'': EmitTagged(ref writer, t, h.Representation(o), false, cache); break;
                            default: EmitEncoded(ref writer, t, h, o, asDictionaryKey, cache); break;
                        }
                    }
                    else
                    {
                        if (t.Equals("array"))
                        {
                            EmitList(ref writer, h.Representation(o), asDictionaryKey, cache);
                        }
                        else if (t.Equals("map"))
                        {
                            EmitDictionary(ref writer, h.Representation(o), asDictionaryKey, cache);
                        }
                        else
                        {
                            EmitEncoded(ref writer, t, h, o, asDictionaryKey, cache);
                        }
                    }
                    FlushWriter();
                }
            }

            if (!supported)
            {
                throw new NotSupportedException("Not supported: " + o.GetType());
            }
        }

        protected void MarshalTop(ref MessagePackWriter writer, object obj, WriteCache cache)
        {
            IWriteHandler handler = GetHandler(obj);
            if (handler == null)
            {
                throw new NotSupportedException(
                    string.Format("Cannot marshal type {0} ({1})", obj != null ? obj.GetType() : null, obj));
            }

            string tag = handler.Tag(obj);
            if (tag == null)
            {
                throw new NotSupportedException(
                    string.Format("Cannot marshal type {0} ({1})", obj != null ? obj.GetType() : null, obj));
            }

            if (tag.Length == 1)
                obj = new Quote(obj);

            Marshal(ref writer, obj, false, cache);
        }

        public void EmitNull(ref MessagePackWriter writer, bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey)
            {
                EmitString(ref writer, Constants.EscStr, "_", "", asDictionaryKey, cache);
            }
            else
            {
                writer.WriteNil();
            }
        }

        public void EmitString(ref MessagePackWriter writer, string prefix, string tag, string s, bool asDictionaryKey, WriteCache cache)
        {
            string outString = cache.CacheWrite(Beerendonk.Transit.Impl.Util.MaybePrefix(prefix, tag, s), asDictionaryKey);
            writer.WriteString(Encoding.GetBytes(outString));
        }

        public void EmitBoolean(ref MessagePackWriter writer, bool b, bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey)
            {
                EmitString(ref writer, Constants.EscStr, "?", b ? "t" : "f", asDictionaryKey, cache);
            }
            else
            {
                writer.Write(b);
            }
        }

        public void EmitInteger(ref MessagePackWriter writer, object i, bool asDictionaryKey, WriteCache cache)
        {
            EmitInteger(ref writer, Beerendonk.Transit.Impl.Util.NumberToPrimitiveLong(i), asDictionaryKey, cache);
        }

        public void EmitInteger(ref MessagePackWriter writer, long i, bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey || i > JsonIntMax || i < JsonIntMin)
                EmitString(ref writer, Constants.EscStr, "i", i.ToString(), asDictionaryKey, cache);
            else
                writer.Write(i);
        }

        public void EmitDouble(ref MessagePackWriter writer, object d, bool asDictionaryKey, WriteCache cache)
        {
            if (d is double)
                EmitDouble(ref writer, (double)d, asDictionaryKey, cache);
            else if (d is float)
                EmitDouble(ref writer, (float)d, asDictionaryKey, cache);
            else
                throw new TransitException("Unknown double type: " + d.GetType());
        }

        public void EmitDouble(ref MessagePackWriter writer, float d, bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey)
                EmitString(ref writer, Constants.EscStr, "d", d.ToString(), asDictionaryKey, cache);
            else
                writer.Write(d);
        }

        public void EmitDouble(ref MessagePackWriter writer, double d, bool asDictionaryKey, WriteCache cache)
        {
            if (asDictionaryKey)
                EmitString(ref writer, Constants.EscStr, "d", d.ToString(), asDictionaryKey, cache);
            else
                writer.Write(d);
        }

        public void EmitBinary(ref MessagePackWriter writer, object b, bool asDictionaryKey, WriteCache cache)
        {
            EmitString(ref writer, Constants.EscStr, "b", Convert.ToBase64String((byte[])b), asDictionaryKey, cache);
        }

        public void EmitListStart(ref MessagePackWriter writer, long size)
        {
            writer.WriteArrayHeader(checked((uint)size));
        }

        public void EmitDictionaryStart(ref MessagePackWriter writer, long size)
        {
            writer.WriteMapHeader(checked((uint)size));
        }

        public override void FlushWriter()
        {
            //jsonWriter.Flush();
            var _ = pipeWriter.FlushAsync();
        }

        public override bool PrefersStrings()
        {
            return true;
        }

        protected void EmitDictionary(ref MessagePackWriter writer, IEnumerable<KeyValuePair<object, object>> keyValuePairs, 
            bool ignored, WriteCache cache)
        {
            long sz = Enumerable.Count(keyValuePairs);

            EmitDictionaryStart(ref writer, sz);

            foreach (var kvp in keyValuePairs)
        	{
                Marshal(ref writer, kvp.Key, true, cache);
                Marshal(ref writer, kvp.Value, false, cache);
            }
        }

        #region Implement AbstractEmitter methods that probably shouldn't be used

        protected override void EmitDictionary(IEnumerable<KeyValuePair<object, object>> keyValuePairs, bool ignored, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitDictionary(ref writer, keyValuePairs, ignored, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitNull(bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitNull(ref writer, asDictionaryKey, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitString(string prefix, string tag, string s, bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitString(ref writer, prefix, tag, s, asDictionaryKey, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitBoolean(bool b, bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitBoolean(ref writer, b, asDictionaryKey, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitInteger(object o, bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitInteger(ref writer, o, asDictionaryKey, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitInteger(long i, bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitInteger(ref writer, i, asDictionaryKey, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitDouble(object d, bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitDouble(ref writer, d, asDictionaryKey, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitDouble(float d, bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitDouble(ref writer, d, asDictionaryKey, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitDouble(double d, bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitDouble(ref writer, d, asDictionaryKey, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitBinary(object b, bool asDictionaryKey, WriteCache cache)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitBinary(ref writer, b, asDictionaryKey, cache);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitListStart(long size)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitListStart(ref writer, size);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitListEnd()
        {
        }

        public override void EmitDictionaryStart(long size)
        {
            var writer = new MessagePackWriter(pipeWriter);
            EmitDictionaryStart(ref writer, size);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        public override void EmitDictionaryEnd()
        {
            var writer = new MessagePackWriter(pipeWriter);
            writer.Flush();
            pipeWriter.FlushAsync();
        }

        #endregion
    }
}
