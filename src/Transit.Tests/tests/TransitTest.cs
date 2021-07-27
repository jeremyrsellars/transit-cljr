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

using Moq;
using Beerendonk.Transit.Impl;
using Beerendonk.Transit.Java;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using clojure.lang;
using Sellars.Transit.Alpha;
using NUnit.Framework;
using Sellars.Transit.Tests;
using Sellars.Transit.Util.Alpha;

namespace Beerendonk.Transit.Tests
{
    [TestFixtureSource(typeof(FactoryImplementationAdapter), nameof(FactoryImplementationAdapter.Adapters))]
    public class TransitTest
    {
        private FactoryImplementationAdapter adapter;

        public TransitTest(FactoryImplementationAdapter adapter)
        {
            this.adapter = adapter;
        }

        #region Reading

        public IReader Reader(string s)
        {
            Stream input = new MemoryStream(adapter.Encoding.GetBytes(s));
            return adapter.CreateReader(TransitFactory.Format.Json, input);
        }

        public IReader Reader(byte[] stringEncodedAsBytes)
        {
            Stream input = new MemoryStream(stringEncodedAsBytes);
            return adapter.CreateReader(TransitFactory.Format.Json, input);
        }

        public string Transcode(string s) =>
            adapter.Encoding.GetString(adapter.Encoding.GetBytes(s));

        public char Transcode(char s) => 
            Transcode(s.ToString())[0];

        [Test]
        public void TestReadString()
        {
            Assert.AreEqual("foo", Reader("\"foo\"").Read<string>());
            Assert.AreEqual(Transcode("אЌ"), Reader("\"אЌ\"").Read<string>());  // Don't penalize implementation about characters that are unrepresentable in the current Encoding/codeset.
            Assert.AreEqual("~foo", Reader("\"~~foo\"").Read<string>());
            Assert.AreEqual("`foo", Reader("\"~`foo\"").Read<string>());
            Assert.AreEqual("foo", Reader("\"~#foo\"").Read<Tag>().GetValue());
            Assert.AreEqual("^foo", Reader("\"~^foo\"").Read<string>());
        }

        [Test]
        public void TestReadBoolean()
        {
            Assert.IsTrue(Reader("\"~?t\"").Read<bool>());
            Assert.IsFalse(Reader("\"~?f\"").Read<bool>());

            IDictionary d = Reader("{\"~?t\":1,\"~?f\":2}").Read<IDictionary>();
            Assert.AreEqual(1L, d[true]);
            Assert.AreEqual(2L, d[false]);
        }

        [Test]
        public void TestReadNull()
        {
            IReader r = Reader("\"~_\"");
            object v = r.Read<object>();
            Assert.IsNull(v);
        }

        [Test]
        public void TestReadKeyword()
        {
            Keyword v = Reader("\"~:foo\"").Read<Keyword>();
            Assert.AreEqual(":foo", v.ToString());

            Keyword v1 = Reader("\"~:אЌ/foo\"").Read<Keyword>();
            Assert.AreEqual(Transcode(":אЌ/foo"), v1.ToString());  // Don't penalize implementation about characters that are unrepresentable in the current Encoding/codeset.

            IReader r = Reader("[\"~:foo\",\"^" + (char)WriteCache.BaseCharIdx + "\",\"^" + (char)WriteCache.BaseCharIdx + "\",\"~:אЌ/foo\",\"^" + ((char)(WriteCache.BaseCharIdx + 1)) + "\"]");
            var v2 = r.Read<IEnumerable>();
            Assert.AreEqual(":foo", RT.nth(v2, 0).ToString());
            Assert.AreEqual(":foo", RT.nth(v2, 1).ToString());
            Assert.AreEqual(":foo", RT.nth(v2, 2).ToString());
            Assert.AreEqual(Transcode(":אЌ/foo"), RT.nth(v2, 3).ToString());
            Assert.AreEqual(Transcode(":אЌ/foo"), RT.nth(v2, 4).ToString());
        }

        [Test]
        public void TestReadInteger()
        {
            IReader r = Reader("\"~i42\"");
            long v = r.Read<long>();
            AssertAreEqual<long>(42L, v);
        }

        [Test]
        public void TestReadBigInteger()
        {
            BigInteger expected = BigInteger.Parse("4256768765123454321897654321234567");
            IReader r = Reader("\"~n4256768765123454321897654321234567\"");
            BigInteger v = r.Read<BigInteger>();
            AssertAreEqual<BigInteger>(expected, v);
        }

        [Test]
        public void TestReadDouble()
        {
            AssertAreEqual<double>(42.5D, Reader("\"~d42.5\"").Read<double>());
        }

        [Test]
        public void TestReadSpecialNumbers()
        {
            AssertAreEqual<double>(double.NaN, Reader("\"~zNaN\"").Read<double>());
            AssertAreEqual<double>(double.PositiveInfinity, Reader("\"~zINF\"").Read<double>());
            AssertAreEqual<double>(double.NegativeInfinity, Reader("\"~z-INF\"").Read<double>());
        }

        [Test]
        public void TestReadDateTime()
        {
            var d = new DateTime(2014, 8, 9, 10, 6, 21, 497, DateTimeKind.Local);
            var expected = new DateTimeOffset(d).UtcDateTime;
            long javaTime = TimeUtils.ToTransitTime(d);

            string timeString = JsonParser.FormatUtcDateTime(d);
            Assert.AreEqual(expected, Reader("\"~t" + timeString + "\"").Read<DateTime>());

            Assert.AreEqual(expected, Reader("{\"~#m\": " + javaTime + "}").Read<DateTime>().ToUniversalTime());

            timeString = new DateTimeOffset(d).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            Assert.AreEqual(expected, Reader("\"~t" + timeString + "\"").Read<DateTime>());

            timeString = new DateTimeOffset(d).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            Assert.AreEqual(expected.AddMilliseconds(-497D), Reader("\"~t" + timeString + "\"").Read<DateTime>());

            timeString = new DateTimeOffset(d).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff-00:00");
            Assert.AreEqual(expected, Reader("\"~t" + timeString + "\"").Read<DateTime>());
        }

        [Test]
        public void TestReadGuid()
        {
            Guid guid = Guid.NewGuid();
            long hi64 = ((Uuid)guid).MostSignificantBits;
            long lo64 = ((Uuid)guid).LeastSignificantBits;

            Assert.AreEqual(guid, Reader("\"~u" + guid.ToString() + "\"").Read<Guid>());
            Assert.AreEqual(guid, Reader("{\"~#u\": [" + hi64 + ", " + lo64 + "]}").Read<Guid>());
        }

        [Test]
        public void TestReadUri()
        {
            Uri expected = new Uri("http://www.foo.com");
            IReader r = Reader("\"~rhttp://www.foo.com\"");
            Uri v = r.Read<Uri>();
            Assert.AreEqual(expected, v);
        }

        [Test]
        public void TestReadSymbol()
        {
            IReader r = Reader("\"~$foo\"");
            Symbol v = r.Read<Symbol>();
            Assert.AreEqual("foo", v.ToString());

            Symbol v1 = Reader("\"~$אЌ/foo\"").Read<Symbol>();
            Assert.AreEqual(Transcode("אЌ/foo"), v1.ToString());

            IReader r2 = Reader("[\"~$foo\",\"^" + (char)WriteCache.BaseCharIdx + "\",\"^" + (char)WriteCache.BaseCharIdx + "\"]");
            var v2 = r2.Read<IEnumerable>();
            Assert.AreEqual("foo", RT.nth(v2, 0).ToString());
            Assert.AreEqual("foo", RT.nth(v2, 1).ToString());
            Assert.AreEqual("foo", RT.nth(v2, 2).ToString());
        }

        [Test]
        public void TestReadCharacter()
        {
            IReader r = Reader("\"~cf\"");
            char v = r.Read<char>();
            Assert.AreEqual('f', v);

            Assert.AreEqual(Transcode('א'), Reader("\"~cא\"").Read<char>());  // Don't penalize implementation about characters that are unrepresentable in the current Encoding/codeset.
            Assert.AreEqual(Transcode('Ќ'), Reader("\"~cЌ\"").Read<char>());  // Don't penalize implementation about characters that are unrepresentable in the current Encoding/codeset.
        }

        [Test]
        public void TestReadBinary()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("foobarbaz");
            string encoded = System.Convert.ToBase64String(bytes);
            byte[] decoded = Reader("\"~b" + encoded + "\"").Read<byte[]>();

            Assert.AreEqual(bytes.Length, decoded.Length);

            bool same = true;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != decoded[i])
                    same = false;
            }

            Assert.IsTrue(same);
        }

        [Test]
        public void TestReadUnknown()
        {
            Assert.AreEqual(TransitFactory.TaggedValue("j", "foo"), Reader("\"~jfoo\"").Read<ITaggedValue>());
            ISeq l = RT.arrayToList(new object [] { 1L, 2L });

            ITaggedValue expected = TransitFactory.TaggedValue("point", l);
            ITaggedValue result = Reader("{\"~#point\":[1,2]}").Read<ITaggedValue>();
            Assert.AreEqual(expected.Tag, result.Tag);
            CollectionAssert.AreEqual(RT.toArray(expected.Representation), RT.toArray(result.Representation));
        }

        [Test]
        public void TestReadList()
        {
            IList l = Reader("[1, 2, 3]").Read<IList>();

            Assert.IsTrue(l is IList<object>);
            Assert.AreEqual(3, l.Count);

            Assert.AreEqual(1L, l[0]);
            Assert.AreEqual(2L, l[1]);
            Assert.AreEqual(3L, l[2]);
        }

        [Test]
        public void TestReadListWithNested()
        {
            var d = new DateTime(2014, 8, 10, 13, 34, 35, DateTimeKind.Utc);
            String t = JsonParser.FormatUtcDateTime(d);

            IList l = Reader("[\"~:foo\", \"~t" + t + "\", \"~?t\"]").Read<IList>();

            Assert.AreEqual(3, l.Count);

            Assert.AreEqual(":foo", l[0].ToString());
            Assert.AreEqual(d, (DateTime)l[1]);
            Assert.IsTrue((bool)l[2]);
        }

        [Test]
        public void TestReadDictionary()
        {
            var m = Reader("{\"a\": 2, \"b\": 4}").Read<object>();

            Assert.AreEqual(2, RT.count(m));

            Assert.AreEqual(2L, RT.get(m, "a"));
            Assert.AreEqual(4L, RT.get(m, "b"));
        }

        [Test]
        public void TestReadDictionaryWithNested()
        {
            Guid guid = Guid.NewGuid();

            IDictionary m = Reader("{\"a\": \"~:foo\", \"b\": \"~u" + (Uuid)guid + "\"}").Read<IDictionary>();

            Assert.AreEqual(2, m.Count);

            Assert.AreEqual(":foo", m["a"].ToString());
            Assert.AreEqual(guid, m["b"]);
        }

        [Test]
        public void TestReadSet()
        {
            var s = Reader("{\"~#set\": [1, 2, 3]}").Read<object>();

            Assert.AreEqual(3, RT.count(s));

            Assert.IsTrue(RT.IsTrue(RT.contains(s, 1L)));
            Assert.IsTrue(RT.IsTrue(RT.contains(s, 2L)));
            Assert.IsTrue(RT.IsTrue(RT.contains(s, 3L)));
        }

        [Test]
        public void TestReadEnumerable()
        {
            IEnumerable l = Reader("{\"~#list\": [1, 2, 3]}").Read<IEnumerable>();
            IEnumerable<object> lo = l.OfType<object>();

            Assert.IsTrue(l is IEnumerable);
            Assert.AreEqual(3, lo.Count());

            Assert.AreEqual(1L, lo.First());
            Assert.AreEqual(2L, lo.Skip(1).First());
            Assert.AreEqual(3L, lo.Skip(2).First());
        }

        [Test]
        public void TestReadRatio()
        {
            Ratio r = Reader("{\"~#ratio\": [\"~n1\",\"~n2\"]}").Read<Ratio>();

            Assert.AreEqual(BigInteger.One, r.numerator);
            Assert.AreEqual(BigInteger.One + 1, r.denominator);
            Assert.AreEqual(0.5d, r.ToDouble(null), 0.01d); // The null FormatProvider might be wrong.
        }

        [Test]
        public void TestReadCDictionary()
        {
            IDictionary m = Reader("{\"~#cmap\": [{\"~#ratio\":[\"~n1\",\"~n2\"]},1,{\"~#list\":[1,2,3]},2]}").Read<IDictionary>();

            Assert.AreEqual(2, m.Count);
            AssertIsInstanceOfThese(adapter.DictionaryTypeGuarantees, m);

            var e = (IDictionaryEnumerator)m.GetEnumerator();
            while(e.MoveNext())
            {
                if ((long)e.Value == 1L)
                {
                    Ratio r = (Ratio)e.Key;
                    Assert.AreEqual(new BigInteger(+1, 1), r.numerator);
                    Assert.AreEqual(new BigInteger(+1, 2), r.denominator);
                }
                else
                {
                    if ((long)e.Value == 2L)
                    {
                        IList l = (IList)e.Key;
                        Assert.AreEqual(1L, l[0]);
                        Assert.AreEqual(2L, l[1]);
                        Assert.AreEqual(3L, l[2]);
                    }
                }
            }
        }

        [Test]
        public void TestReadSetTagAsString()
        {
            object o = Reader("{\"~~#set\": [1, 2, 3]}").Read<object>();
            Assert.IsFalse(o is ISet<object>);
            Assert.IsTrue(o is IDictionary);
        }

        [Test]
        public void TestReadMany()
        {
            IReader r;

            // TODO Make sure JSON parser can parse number larger than Int64
            /*
            BigInteger expected = BigInteger.Parse("4256768765123454321897654321234567");
            r = Reader("4256768765123454321897654321234567");
            BigInteger v = r.Read<BigInteger>();
            AssertAreEqual<BigInteger>(expected, v);
            */

            // Since numbers are the only tokens that seem to "make sense" in partial form
            // (If 1234567 is truncated like 123 it still seems a valid token,
            // so don't return it before we know for sure, like when followed by whitespace).
            r = Reader("true false null \"foo\" 44.2 42 [1] 41");
            var a = r.Read<object>();//<bool>();
            var b = r.Read<object>();//<bool>();
            var c = r.Read<object>();//<object>();
            var d = r.Read<object>();//<string>();
            var e = r.Read<object>();//<double>();
            var f = r.Read<object>();//<long>();
            var g = r.Read<object>();//<IList>();
            var h = r.Read<object>();//<long>();
            Assert.IsTrue((bool)a, a is object ? a.GetType().Name : null);
            Assert.IsFalse((bool)b, b is object ? b.GetType().Name : null);
            Assert.IsNull(c, c is object ? c.GetType().Name : null);
            Assert.AreEqual("foo", d, d is object ? d.GetType().Name : null);
            Assert.AreEqual(44.2, e, e is object ? e.GetType().Name : null);
            Assert.AreEqual(42L, f, f is object ? f.GetType().Name : null);
            Assert.That(() => g is IList l && l.Count == 1 && (long)l[0] == 1L, g is object ? g.GetType().Name : null);
            Assert.AreEqual(41L, h, h is object ? f.GetType().Name : null);
        }

        [Test]
        public void TestReadHuge()
        {
            var s = new string('0', 1024 * 1024 + 3);
            IReader r = Reader($"\"1{s}2\"");
            string v = r.Read<string>();
            AssertAreEqual<string>(1 + s + 2, v);
        }

        [Test]
        public void TestReadCache()
        {
            ReadCache rc = new ReadCache();
            Assert.AreEqual("~:foo", rc.CacheRead("~:foo", false));
            Assert.AreEqual("~:foo", rc.CacheRead("^" + (char)WriteCache.BaseCharIdx, false));
            Assert.AreEqual("~$bar", rc.CacheRead("~$bar", false));
            Assert.AreEqual("~$bar", rc.CacheRead("^" + (char)(WriteCache.BaseCharIdx + 1), false));
            Assert.AreEqual("~#baz", rc.CacheRead("~#baz", false));
            Assert.AreEqual("~#baz", rc.CacheRead("^" + (char)(WriteCache.BaseCharIdx + 2), false));
            Assert.AreEqual("foobar", rc.CacheRead("foobar", false));
            Assert.AreEqual("foobar", rc.CacheRead("foobar", false));
            Assert.AreEqual("foobar", rc.CacheRead("foobar", true));
            Assert.AreEqual("foobar", rc.CacheRead("^" + (char)(WriteCache.BaseCharIdx + 3), true));
            Assert.AreEqual("abc", rc.CacheRead("abc", false));
            Assert.AreEqual("abc", rc.CacheRead("abc", false));
            Assert.AreEqual("abc", rc.CacheRead("abc", true));
            Assert.AreEqual("abc", rc.CacheRead("abc", true));
        }

        [Test]
        public void TestReadIdentity()
        {
            IReader r = Reader("\"~'42\"");
            string v = r.Read<string>();
            AssertAreEqual<string>("42", v);
        }

        [Test]
        public void TestReadLink()
        {
            IReader r = Reader("[\"~#link\" , {\"href\": \"~rhttp://www.Beerendonk.nl\", \"rel\": \"a-rel\", \"name\": \"a-name\", \"prompt\": \"a-prompt\", \"render\": \"link or image\"}]");
            ILink v = r.Read<ILink>();
            Assert.AreEqual(new Uri("http://www.Beerendonk.nl"), v.Href);
            Assert.AreEqual("a-rel", v.Rel);
            Assert.AreEqual("a-name", v.Name);
            Assert.AreEqual("a-prompt", v.Prompt);
            Assert.AreEqual("link or image", v.Render);
        }

        #endregion

        #region Writing

        public string Write(object obj, TransitFactory.Format format, IDictionary<Type, IWriteHandler> customHandlers = null,
            IWriteHandler defaultHandler = null, Func<object, object> transform = null)
        {
            using (Stream output = new MemoryStream())
            {
                IWriter<object> w = adapter.CreateCustomWriter(format, output, customHandlers, defaultHandler, transform);
                w.Write(obj);

                output.Position = 0;
                var sr = new StreamReader(output);
                return sr.ReadToEnd();
            }
        }

        public string WriteJsonVerbose(object obj)
        {
            return Write(obj, TransitFactory.Format.JsonVerbose, null);
        }

        public string WriteJsonVerbose(object obj, IDictionary<Type, IWriteHandler> customHandlers)
        {
            return Write(obj, TransitFactory.Format.JsonVerbose, customHandlers);
        }

        public string WriteJson(object obj)
        {
            return Write(obj, TransitFactory.Format.Json, null);
        }

        public string WriteJson(object obj, IDictionary<Type, IWriteHandler> customHandlers)
        {
            return Write(obj, TransitFactory.Format.Json, customHandlers);
        }

        public bool IsEqual(object o1, object o2)
        {
            if (o1 is bool && o2 is bool)
                return (bool)o1 == (bool)o2;
            else
                return false;
        }

        [Test]
        public void TestRoundTrip()
        {
            object inObject = true;
            object outObject;

            string s;

            using (Stream output = new MemoryStream())
            {
                IWriter<object> w = adapter.CreateWriter(TransitFactory.Format.JsonVerbose, output);
                w.Write(inObject);

                output.Position = 0;
                var sr = new StreamReader(output);
                s = sr.ReadToEnd();
            }

            byte[] buffer = Encoding.ASCII.GetBytes(s);
            using (Stream input = new MemoryStream(buffer))
            {
                IReader reader = adapter.CreateReader(TransitFactory.Format.Json, input);
                outObject = reader.Read<object>();
            }

            Assert.IsTrue(IsEqual(inObject, outObject));
        }

        public string Scalar(string value)
        {
            return "[\"~#'\"," + value + "]";
        }

        public string ScalarVerbose(string value)
        {
            return "{\"~#'\":" + value + "}";
        }

        [Test]
        public void TestWriteNull()
        {
            Assert.AreEqual(ScalarVerbose("null"), WriteJsonVerbose(null));
            Assert.AreEqual(Scalar("null"), WriteJson(null));
        }

        [Test]
        public void TestWriteKeyword()
        {
            Assert.AreEqual(ScalarVerbose("\"~:foo\""), WriteJsonVerbose(TransitFactory.Keyword("foo")));
            Assert.AreEqual(Scalar("\"~:foo\""), WriteJson(TransitFactory.Keyword("foo")));

            Assert.AreEqual(ScalarVerbose(Transcode("\"~:אЌ/foo\"")), WriteJsonVerbose(TransitFactory.Keyword("אЌ/foo")));
            Assert.AreEqual(Transcode(Scalar("\"~:אЌ/foo\"")), WriteJson(TransitFactory.Keyword("אЌ/foo")));

            IList l = new Keyword[] 
            {
                TransitFactory.Keyword("foo"),
                TransitFactory.Keyword("foo"),
                TransitFactory.Keyword("foo")
            };
            Assert.AreEqual("[\"~:foo\",\"~:foo\",\"~:foo\"]", WriteJsonVerbose(l));
            Assert.AreEqual("[\"~:foo\",\"^0\",\"^0\"]", WriteJson(l));
        }

        [Test]
        public void TestWriteObjectJson() => Assert.Throws<NotSupportedException>(() =>
        {
            WriteJson(new object());
        });

        [Test]
        public void TestWriteObjectJsonVerbose() => Assert.Throws<NotSupportedException>(() =>
        {
            WriteJsonVerbose(new object());
        });

        [Test]
        public void TestWriteString()
        {
            Assert.AreEqual(ScalarVerbose("\"foo\""), WriteJsonVerbose("foo"));
            Assert.AreEqual(Scalar("\"foo\""), WriteJson("foo"));
            Assert.AreEqual(ScalarVerbose(Transcode("\"אЌfoo\"")), WriteJsonVerbose("אЌfoo"));
            Assert.AreEqual(Scalar("\"~~foo\""), WriteJson("~foo"));
            Assert.AreEqual(ScalarVerbose(Transcode("\"א\"")), WriteJsonVerbose("א"));
            Assert.AreEqual(Scalar(Transcode("\"א\"")), WriteJson("א"));
            Assert.AreEqual(ScalarVerbose(Transcode("\"Ќ\"")), WriteJsonVerbose("Ќ"));
            Assert.AreEqual(Scalar(Transcode("\"Ќ\"")), WriteJson("Ќ"));
        }

        [Test]
        public void TestWriteBoolean()
        {
            Assert.AreEqual(ScalarVerbose("true"), WriteJsonVerbose(true));
            Assert.AreEqual(Scalar("true"), WriteJson(true));
            Assert.AreEqual(Scalar("false"), WriteJson(false));

            var d = new Dictionary<bool, int>();
            d[true] = 1;
            Assert.AreEqual("{\"~?t\":1}", WriteJsonVerbose(d));
            Assert.AreEqual("[\"^ \",\"~?t\",1]", WriteJson(d));

            var d2 = new Dictionary<bool, int>();
            d2[false] = 1;
            Assert.AreEqual("{\"~?f\":1}", WriteJsonVerbose(d2));
            Assert.AreEqual("[\"^ \",\"~?f\",1]", WriteJson(d2));
        }

        [Test]
        public void TestWriteInteger()
        {
            Assert.AreEqual(ScalarVerbose("42"), WriteJsonVerbose(42));
            Assert.AreEqual(ScalarVerbose("42"), WriteJsonVerbose(42L));
            Assert.AreEqual(ScalarVerbose("42"), WriteJsonVerbose((byte)42));
            Assert.AreEqual(ScalarVerbose("42"), WriteJsonVerbose((short)42));
            Assert.AreEqual(ScalarVerbose("42"), WriteJsonVerbose((int)42));
            Assert.AreEqual(ScalarVerbose("42"), WriteJsonVerbose(42L));
            Assert.AreEqual(ScalarVerbose("\"~n42\""), WriteJsonVerbose(BigInteger.Parse("42")));
            Assert.AreEqual(ScalarVerbose("\"~n4256768765123454321897654321234567\""), WriteJsonVerbose(BigInteger.Parse("4256768765123454321897654321234567")));
        }

        [Test]
        public void TestWriteFloatDouble()
        {
            Assert.AreEqual(ScalarVerbose("42.5"), WriteJsonVerbose(42.5));
            Assert.AreEqual(ScalarVerbose("42.5"), WriteJsonVerbose(42.5F));
            Assert.AreEqual(ScalarVerbose("42.5"), WriteJsonVerbose(42.5D));
        }

        [Test]
        public void TestSpecialNumbers()
        {
            Assert.AreEqual(Scalar("\"~zNaN\""), WriteJson(double.NaN));
            Assert.AreEqual(Scalar("\"~zINF\""), WriteJson(double.PositiveInfinity));
            Assert.AreEqual(Scalar("\"~z-INF\""), WriteJson(double.NegativeInfinity));

            Assert.AreEqual(Scalar("\"~zNaN\""), WriteJson(float.NaN));
            Assert.AreEqual(Scalar("\"~zINF\""), WriteJson(float.PositiveInfinity));
            Assert.AreEqual(Scalar("\"~z-INF\""), WriteJson(float.NegativeInfinity));

            Assert.AreEqual(ScalarVerbose("\"~zNaN\""), WriteJsonVerbose(double.NaN));
            Assert.AreEqual(ScalarVerbose("\"~zINF\""), WriteJsonVerbose(double.PositiveInfinity));
            Assert.AreEqual(ScalarVerbose("\"~z-INF\""), WriteJsonVerbose(double.NegativeInfinity));

            Assert.AreEqual(ScalarVerbose("\"~zNaN\""), WriteJsonVerbose(float.NaN));
            Assert.AreEqual(ScalarVerbose("\"~zINF\""), WriteJsonVerbose(float.PositiveInfinity));
            Assert.AreEqual(ScalarVerbose("\"~z-INF\""), WriteJsonVerbose(float.NegativeInfinity));
        }

        [Test]
        public void TestWriteBigDecimal()
        {
            //Assert.Inconclusive();

            // TODO
            Assert.AreEqual(ScalarVerbose("\"~f42.5\""), WriteJsonVerbose(new BigDecimal(new BigInteger(1, 425), -1)));
        }

        [Test]
        public void TestWriteDateTime()
        {
            var d = DateTime.Now;
            String dateString = AbstractParser.FormatUtcDateTime(d);
            long dateLong = TimeUtils.ToTransitTime(d);
            Assert.AreEqual(ScalarVerbose("\"~t" + dateString + "\""), WriteJsonVerbose(d));
            Assert.AreEqual(Scalar("\"~m" + dateLong + "\""), WriteJson(d));
        }

        [Test]
        public void TestWriteUUID()
        {
            Guid guid = Guid.NewGuid();
            Assert.AreEqual(ScalarVerbose("\"~u" + guid.ToString() + "\""), WriteJsonVerbose(guid));
        }

        [Test]
        public void TestWriteURI()
        {
            Uri uri = new Uri("http://www.foo.com/");

            Assert.AreEqual(ScalarVerbose("\"~rhttp://www.foo.com/\""), WriteJsonVerbose(uri));
        }

        [Test]
        public void TestWriteBinary()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("foobarbaz");
            string encoded = System.Convert.ToBase64String(bytes);

            Assert.AreEqual(ScalarVerbose("\"~b" + encoded + "\""), WriteJsonVerbose(bytes));
            Assert.AreEqual(Scalar("\"~b" + encoded + "\""), WriteJson(bytes));
        }

        [Test]
        public void TestWriteSymbol()
        {
            Assert.AreEqual(ScalarVerbose("\"~$foo\""), WriteJsonVerbose(TransitFactory.Symbol("foo")));
            Assert.AreEqual(Scalar("\"~$foo\""), WriteJson(TransitFactory.Symbol("foo")));
            Assert.AreEqual(ScalarVerbose(Transcode("\"~$אЌ/foo\"")), WriteJsonVerbose(TransitFactory.Symbol("אЌ/foo")));
            Assert.AreEqual(Transcode(Scalar("\"~$אЌ/foo\"")), WriteJson(TransitFactory.Symbol("אЌ/foo")));
        }

        [Test]
        public void TestWriteList()
        {
            IList<int> l = new List<int> { 1, 2, 3 };

            Assert.AreEqual("[1,2,3]", WriteJsonVerbose(l));
            Assert.AreEqual("[1,2,3]", WriteJson(l));
        }

        [Test]
        public void TestWritePrimitiveArrays()
        {
            int[] ints = { 1, 2 };
            Assert.AreEqual("[1,2]", WriteJsonVerbose(ints));

            long[] longs = { 1L, 2L };
            Assert.AreEqual("[1,2]", WriteJsonVerbose(longs));

            float[] floats = { 1.5f, 2.78f };
            Assert.AreEqual(// "[1.5,2.78]"
                adapter.SerializeJson(floats),
                WriteJsonVerbose(floats));

            bool[] bools = { true, false };
            Assert.AreEqual("[true,false]", WriteJsonVerbose(bools));

            double[] doubles = { 1.654d, 2.8765d };
            Assert.AreEqual(// "[1.654,2.8765]"
                adapter.SerializeJson(doubles),
                WriteJsonVerbose(doubles));

            short[] shorts = { 1, 2 };
            Assert.AreEqual("[1,2]", WriteJsonVerbose(shorts));

            char[] chars = { '5', '/' };
            Assert.AreEqual("[\"~c5\",\"~c/\"]", WriteJsonVerbose(chars));
        }

        [Test]
        public void TestWriteStringKeyedDictionary()
        {
            // A static StringableKeys map.
            IDictionary<string, int> d = new Dictionary<string, int> { {"foo", 1}, {"bar", 2} };

            Assert.AreEqual("{\"foo\":1,\"bar\":2}", WriteJsonVerbose(d));
            Assert.AreEqual("[\"^ \",\"foo\",1,\"bar\",2]", WriteJson(d));

            IDictionary<char, int> u = new Dictionary<char, int> { { 'א', 1 }, { 'Ќ', 2 } };
            Assert.AreEqual(Transcode("{\"~cא\":1,\"~cЌ\":2}"), WriteJsonVerbose(u));
            Assert.AreEqual(Transcode("[\"^ \",\"~cא\",1,\"~cЌ\",2]"), WriteJson(u));
        }

        [Test]
        public void TestWriteObjectKeyedDictionary()
        {
            // Another StringableKeys map, but that cannot be checked by key type.
            IDictionary<object, int> d = new Dictionary<object, int> { { "foo", 1 }, { "bar", 2 } };

            Assert.AreEqual("{\"foo\":1,\"bar\":2}", WriteJsonVerbose(d));
            Assert.AreEqual("[\"^ \",\"foo\",1,\"bar\",2]", WriteJson(d));

            // A composite-keyed map.
            IDictionary<object, int> d2 = new Dictionary<object, int> {
                { d, 1 },
                { RT.keyword(null, "bar"), 2 },
            };

            Assert.AreEqual("{\"~#cmap\":[{\"foo\":1,\"bar\":2},1,\"~:bar\",2]}", WriteJsonVerbose(d2));
            Assert.AreEqual("[\"~#cmap\",[[\"^ \",\"foo\",1,\"bar\",2],1,\"~:bar\",2]]", WriteJson(d2));

            // A composite-keyed map.  Are composite keys cached?.
            var dKey = new Dictionary<object, object> { { "foo", "bling" }, { "bar", "blorg" } };
            var d3 = new []
            {
                new Dictionary<object, int> {
                    { dKey, 1 },
                    { RT.keyword(null, "bar"), 2 },
                },
                new Dictionary<object, int> {
                    { dKey, 3 },
                    { RT.keyword(null, "bar"), 4 },
                },
            };

            Assert.AreEqual("[{\"~#cmap\":[{\"foo\":\"bling\",\"bar\":\"blorg\"},1,\"~:bar\",2]},{\"~#cmap\":[{\"foo\":\"bling\",\"bar\":\"blorg\"},3,\"~:bar\",4]}]", WriteJsonVerbose(d3));
            Assert.AreEqual("[[\"~#cmap\",[[\"^ \",\"foo\",\"bling\",\"bar\",\"blorg\"],1,\"~:bar\",2]],[\"^0\",[[\"^ \",\"foo\",\"bling\",\"bar\",\"blorg\"],3,\"^1\",4]]]", WriteJson(d3));
        }

        [Test]
        public void TestWriteKeywordDictionary()
        {
            // Another static StringableKeys map.
            IDictionary<Keyword, int> d = new Dictionary<Keyword, int> { 
                { RT.keyword("s", "foo"), 1 }, 
                { RT.keyword(null, "bar"), 2 },
            };

            Assert.AreEqual("{\"~:s/foo\":1,\"~:bar\":2}", WriteJsonVerbose(d));
            Assert.AreEqual("[\"^ \",\"~:s/foo\",1,\"~:bar\",2]", WriteJson(d));
        }

        [Test]
        public void TestWriteEmptyDictionary()
        {
            IDictionary<object, object> d = new Dictionary<object, object>();
            Assert.AreEqual("{}", WriteJsonVerbose(d));
            Assert.AreEqual("[\"^ \"]", WriteJson(d));
        }

        [Test]
        public void TestWriteSet()
        {
            ISet<string> s = new HashSet<string> { "foo", "bar" };

            Assert.AreEqual("{\"~#set\":[\"foo\",\"bar\"]}", WriteJsonVerbose(s));
            Assert.AreEqual("[\"~#set\",[\"foo\",\"bar\"]]", WriteJson(s));
        }

        [Test]
        public void TestWriteEmptySet()
        {
            ISet<object> s = new HashSet<object>();
            Assert.AreEqual("{\"~#set\":[]}", WriteJsonVerbose(s));
            Assert.AreEqual("[\"~#set\",[]]", WriteJson(s));
        }

        [Test]
        public void TestWriteEnumerable()
        {
            ICollection<string> c = new LinkedList<string>();
            c.Add("foo");
            c.Add("bar");
            IEnumerable<string> e = c;
            Assert.AreEqual("{\"~#list\":[\"foo\",\"bar\"]}", WriteJsonVerbose(e));
            Assert.AreEqual("[\"~#list\",[\"foo\",\"bar\"]]", WriteJson(e));
        }

        [Test]
        public void TestWriteEmptyEnumerable()
        {
            IEnumerable<string> c = new LinkedList<string>();
            Assert.AreEqual("{\"~#list\":[]}", WriteJsonVerbose(c));
            Assert.AreEqual("[\"~#list\",[]]", WriteJson(c));
        }

        [Test]
        public void TestWriteCharacter()
        {
            Assert.AreEqual(ScalarVerbose("\"~cf\""), WriteJsonVerbose('f'));
            Assert.AreEqual(ScalarVerbose(Transcode("\"~cא\"")), WriteJsonVerbose('א'));
            Assert.AreEqual(ScalarVerbose(Transcode("\"~cЌ\"")), WriteJsonVerbose('Ќ'));
        }

        [Test]
        public void TestWriteRatio()
        {
            Ratio r = new Ratio(BigInteger.One, new BigInteger(+1, 2));
            Assert.AreEqual("{\"~#ratio\":[\"~n1\",\"~n2\"]}", WriteJsonVerbose(r));
            Assert.AreEqual("[\"~#ratio\",[\"~n1\",\"~n2\"]]", WriteJson(r));
        }

        [Test]
        public void TestWriteCDictionary()
        {
            Ratio r = new Ratio(BigInteger.One, new BigInteger(+1, 2));
            IDictionary<object, object> d = new Dictionary<object, object>();
            d.Add(r, 1);
            Assert.AreEqual("{\"~#cmap\":[{\"~#ratio\":[\"~n1\",\"~n2\"]},1]}", WriteJsonVerbose(d));
            Assert.AreEqual("[\"~#cmap\",[[\"~#ratio\",[\"~n1\",\"~n2\"]],1]]", WriteJson(d));
        }

        [Test]
        public void TestWriteCache()
        {
            WriteCache wc = new WriteCache(true);
            Assert.AreEqual("~:foo", wc.CacheWrite("~:foo", false));
            Assert.AreEqual("^" + (char)WriteCache.BaseCharIdx, wc.CacheWrite("~:foo", false));
            Assert.AreEqual("~$bar", wc.CacheWrite("~$bar", false));
            Assert.AreEqual("^" + (char)(WriteCache.BaseCharIdx + 1), wc.CacheWrite("~$bar", false));
            Assert.AreEqual("~#baz", wc.CacheWrite("~#baz", false));
            Assert.AreEqual("^" + (char)(WriteCache.BaseCharIdx + 2), wc.CacheWrite("~#baz", false));
            Assert.AreEqual("foobar", wc.CacheWrite("foobar", false));
            Assert.AreEqual("foobar", wc.CacheWrite("foobar", false));
            Assert.AreEqual("foobar", wc.CacheWrite("foobar", true));
            Assert.AreEqual("^" + (char)(WriteCache.BaseCharIdx + 3), wc.CacheWrite("foobar", true));
            Assert.AreEqual("abc", wc.CacheWrite("abc", false));
            Assert.AreEqual("abc", wc.CacheWrite("abc", false));
            Assert.AreEqual("abc", wc.CacheWrite("abc", true));
            Assert.AreEqual("abc", wc.CacheWrite("abc", true));
            Assert.AreEqual("unicodeאЌ", wc.CacheWrite("unicodeאЌ", false));
            Assert.AreEqual("unicodeאЌ", wc.CacheWrite("unicodeאЌ", false));
            Assert.AreEqual("unicodeאЌ", wc.CacheWrite("unicodeאЌ", true));
            Assert.AreEqual("^" + (char)(WriteCache.BaseCharIdx + 4), wc.CacheWrite("unicodeאЌ", true));
        }

        [Test]
        public void TestWriteCacheDisabled()
        {
            WriteCache wc = new WriteCache(false);
            Assert.AreEqual("foobar", wc.CacheWrite("foobar", false));
            Assert.AreEqual("foobar", wc.CacheWrite("foobar", false));
            Assert.AreEqual("foobar", wc.CacheWrite("foobar", true));
            Assert.AreEqual("foobar", wc.CacheWrite("foobar", true));
        }

        [Test]
        public void TestWriteUnknown()
        {
            var l = new List<object>();
            l.Add("`jfoo");
            Assert.AreEqual("[\"~`jfoo\"]", WriteJsonVerbose(l));
            Assert.AreEqual(ScalarVerbose("\"~`jfoo\""), WriteJsonVerbose("`jfoo"));

            var l2 = new List<object>();
            l2.Add(1L);
            l2.Add(2L);
            Assert.AreEqual("{\"~#point\":[1,2]}", WriteJsonVerbose(TransitFactory.TaggedValue("point", l2)));

            var l3 = new List<object>();
            l3.Add('א');
            l3.Add('Ќ');
            Assert.AreEqual(Transcode("{\"~#אЌ\":[\"~cא\",\"~cЌ\"]}"), WriteJsonVerbose(TransitFactory.TaggedValue("אЌ", l3)));
        }

        [Test]
        public void TestWriteWithCustomHandler()
        {
            Mock<IWriteHandler> mock = new Mock<IWriteHandler>();
            mock.Setup(m => m.Tag(It.IsAny<object>())).Returns("s");
            mock.Setup(m => m.Representation(It.IsAny<object>())).Returns("NULL");
            mock.Setup(m => m.StringRepresentation(It.IsAny<object>())).Returns<string>(null);
            mock.Setup(m => m.GetVerboseHandler()).Returns<IWriteHandler>(null);

            IDictionary<Type, IWriteHandler> customHandlers = new Dictionary<Type, IWriteHandler>();
            customHandlers.Add(typeof(NullType), mock.Object);

            // JSON-Verbose
            Assert.AreEqual(ScalarVerbose("\"NULL\""), WriteJsonVerbose(null, customHandlers));
            mock.Verify(m => m.Representation(null));
            mock.Verify(m => m.GetVerboseHandler());

            // JSON
            mock.Invocations.Clear();
            Assert.AreEqual(Scalar("\"NULL\""), WriteJson(null, customHandlers));
            mock.Verify(m => m.Representation(null));
        }

        #endregion

        [Test]
        public void TestUseKeywordAsDictionaryKey()
        {
            IDictionary<object, object> d = new Dictionary<object, object>();
            d.Add(TransitFactory.Keyword("foo"), 1);
            d.Add("foo", 2);
            d.Add(TransitFactory.Keyword("bar"), 3);
            d.Add("bar", 4);

            Assert.AreEqual(1, d[TransitFactory.Keyword("foo")]);
            Assert.AreEqual(2, d["foo"]);
            Assert.AreEqual(3, d[TransitFactory.Keyword("bar")]);
            Assert.AreEqual(4, d["bar"]);
        }

        [Test]
        public void TestUseSymbolAsDictionaryKey()
        {
            IDictionary<object, object> d = new Dictionary<object, object>();
            d.Add(TransitFactory.Symbol("foo"), 1);
            d.Add("foo", 2);
            d.Add(TransitFactory.Symbol("bar"), 3);
            d.Add("bar", 4);

            Assert.AreEqual(1, d[TransitFactory.Symbol("foo")]);
            Assert.AreEqual(2, d["foo"]);
            Assert.AreEqual(3, d[TransitFactory.Symbol("bar")]);
            Assert.AreEqual(4, d["bar"]);
        }

        [Test]
        public void TestKeywordEquality()
        {
            string s = "foo";

            Keyword k1 = TransitFactory.Keyword("foo");
            Keyword k2 = TransitFactory.Keyword("!foo".Substring(1));
            Keyword k3 = TransitFactory.Keyword("bar");

            Assert.AreEqual(k1, k2);
            Assert.AreEqual(k2, k1);
            Assert.IsFalse(k1.Equals(k3));
            Assert.IsFalse(k3.Equals(k1));
            Assert.IsFalse(s.Equals(k1));
            Assert.IsFalse(k1.Equals(s));
        }

        [Test]
        public void TestKeywordHashCode()
        {
            string s = "foo";

            Keyword k1 = TransitFactory.Keyword("foo");
            Keyword k2 = TransitFactory.Keyword("!foo".Substring(1));
            Keyword k3 = TransitFactory.Keyword("bar");
            Symbol symbol = TransitFactory.Symbol("bar");

            Assert.AreEqual(k1.GetHashCode(), k2.GetHashCode());
            Assert.IsFalse(k3.GetHashCode() == k1.GetHashCode());
            Assert.IsFalse(symbol.GetHashCode() == k1.GetHashCode());
            Assert.IsFalse(s.GetHashCode() == k1.GetHashCode());
        }

        [Test]
        public void TestKeywordComparator()
        {

            List<Keyword> l = new List<Keyword> {
                { TransitFactory.Keyword("bbb") },
                { TransitFactory.Keyword("ccc") },
                { TransitFactory.Keyword("abc") },
                { TransitFactory.Keyword("dab") } };

            l.Sort();

            Assert.AreEqual(":abc", l[0].ToString());
            Assert.AreEqual(":bbb", l[1].ToString());
            Assert.AreEqual(":ccc", l[2].ToString());
            Assert.AreEqual(":dab", l[3].ToString());
        }

        [Test]
        public void TestSymbolEquality()
        {
            string s = "foo";

            Symbol sym1 = TransitFactory.Symbol("foo");
            Symbol sym2 = TransitFactory.Symbol("!foo".Substring(1));
            Symbol sym3 = TransitFactory.Symbol("bar");

            Assert.AreEqual(sym1, sym2);
            Assert.AreEqual(sym2, sym1);
            Assert.IsFalse(sym1.Equals(sym3));
            Assert.IsFalse(sym3.Equals(sym1));
            Assert.IsFalse(s.Equals(sym1));
            Assert.IsFalse(sym1.Equals(s));
        }

        [Test]
        public void TestSymbolHashCode()
        {
            string s = "foo";

            Symbol sym1 = TransitFactory.Symbol("foo");
            Symbol sym2 = TransitFactory.Symbol("!foo".Substring(1));
            Symbol sym3 = TransitFactory.Symbol("bar");
            Keyword keyword = TransitFactory.Keyword("bar");

            Assert.AreEqual(sym1.GetHashCode(), sym2.GetHashCode());
            Assert.IsFalse(sym3.GetHashCode() == sym1.GetHashCode());
            Assert.IsFalse(keyword.GetHashCode() == sym1.GetHashCode());
            Assert.IsFalse(s.GetHashCode() == sym1.GetHashCode());
        }

        [Test]
        public void TestSymbolComparator()
        {

            List<Symbol> l = new List<Symbol> {
                { TransitFactory.Symbol("bbb") },
                { TransitFactory.Symbol("ccc") },
                { TransitFactory.Symbol("abc") },
                { TransitFactory.Symbol("dab") } };

            l.Sort();

            Assert.AreEqual("abc", l[0].ToString());
            Assert.AreEqual("bbb", l[1].ToString());
            Assert.AreEqual("ccc", l[2].ToString());
            Assert.AreEqual("dab", l[3].ToString());
        }

        [Test]
        public void TestDictionaryWithEscapedKey()
        {
            var d1 = new Dictionary<object, object> { { "~Gfoo", 20L } };
            string str = WriteJson(d1);

            IDictionary d2 = Reader(str).Read<IDictionary>();
            Assert.IsTrue(d2.Contains("~Gfoo"));
            Assert.IsTrue(d2["~Gfoo"].Equals(20L));
        }

        [Test]
        public void TestLink()
        {
            ILink l1 = TransitFactory.Link("http://google.com/", "search", "name", "link", "prompt");
            String str = WriteJson(l1);
            ILink l2 = Reader(str).Read<ILink>();
            Assert.AreEqual("http://google.com/", l2.Href.AbsoluteUri);
            Assert.AreEqual("search", l2.Rel);
            Assert.AreEqual("name", l2.Name);
            Assert.AreEqual("link", l2.Render);
            Assert.AreEqual("prompt", l2.Prompt);
        }

        [Test]
        public void TestEmptySet()
        {
            string str = WriteJson(new HashSet<object>());
            AssertIsInstanceOfThese(adapter.SetTypeGuarantees, Reader(str).Read<object>());
        }

        // The point of this method is to force the input types.
        private static void AssertAreEqual<T>(T expected, T actual)
        {
            Assert.AreEqual(expected, actual);
        }

        private static void AssertIsInstanceOfThese(IEnumerable<Type> types, object actual)
        {
            foreach (var type in types)
                Assert.IsInstanceOf(type, actual);
        }
    }
}
