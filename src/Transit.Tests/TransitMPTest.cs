using System;
using System.Collections;
using System.IO;
using System.Linq;
using Beerendonk.Transit.Impl;
using Beerendonk.Transit.Java;
using clojure.lang;
using MessagePack;
using NUnit.Framework;
using Sellars.Transit.Alpha;
using Sellars.Transit.Cljr.Alpha;
using Sellars.Transit.Numerics.Alpha;
using TransitFactory = Sellars.Transit.Cljr.Alpha.TransitFactory;
using TransitFormat = Sellars.Transit.Alpha.TransitFactory.Format;

namespace Sellars.Transit.Tests
{
    [TestFixtureSource(typeof(FactoryImplementationAdapter), nameof(FactoryImplementationAdapter.Adapters))]
    public class TransitMPTest
    {
        // Reading
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private FactoryImplementationAdapter adapter;

        public TransitMPTest(FactoryImplementationAdapter adapter)
        {
            this.adapter = adapter;
        }

        public IReader readerOf(params object[] things) 
        {
            var stream = new MemoryStream();

            foreach (var thing in things) {
                MessagePackSerializer.Serialize(stream, thing);
            }

            stream.Position = 0;

            return adapter.CreateReader(Sellars.Transit.Alpha.TransitFactory.Format.MsgPack, stream);
        }

        [Test]
        public void testReadString() {

            Assert.AreEqual("foo", readerOf("foo").Read());
            Assert.AreEqual("~foo", readerOf("~~foo").Read());
            Assert.AreEqual("`foo", readerOf("~`foo").Read());
            Assert.AreEqual("foo", ((Tag)readerOf("~#foo").Read()).GetValue());
            Assert.AreEqual("^foo", readerOf("~^foo").Read());
        }

        [Test]
        public void testReadBoolean() {

            Assert.That((Boolean)readerOf("~?t").Read());
            Assert.False((Boolean) readerOf("~?f").Read());

            var thing = new Hashtable() {
                { "~?t", 1 },
                {"~?f", 2 },
            };

            var m = (IDictionary)readerOf(thing).Read();
            Assert.AreEqual(1L, m[true]);
            Assert.AreEqual(2L, m[false]);
        }

        [Test]
        public void testReadNull() {
            Assert.Null(readerOf("~_").Read());
        }

        [Test]
        public void testReadKeyword() {

            Object v = readerOf("~:foo").Read();
            Assert.AreEqual(":foo", v.ToString());

            var thing = new ArrayList() {
                "~:foo",
                "^" + (char)WriteCache.BaseCharIdx,
                "^" + (char)WriteCache.BaseCharIdx,
            };

            var v2 = (IList)readerOf(thing).Read();
            Assert.AreEqual(":foo", v2[0].ToString());
            Assert.AreEqual(":foo", v2[1].ToString());
            Assert.AreEqual(":foo", v2[2].ToString());

        }

        [Test]
        public void testReadInteger() {

            IReader r = readerOf("~i42");
            Assert.AreEqual(42L, (long) r.Read());

            r = readerOf("~n4256768765123454321897654321234567");
            Assert.AreEqual(0, (BigInteger.Parse("4256768765123454321897654321234567")).CompareTo((BigInteger)r.Read()));
        }

        [Test]
        public void testReadDouble() {

            Assert.AreEqual(Double.Parse("42.5"), readerOf("~d42.5").Read());
        }

        [Test]
        public void testReadBigDecimal() {

            Assert.AreEqual(BigDecimal.Parse("42.5"), (BigDecimal)(BigRational)readerOf("~f42.5").Read());
        }

        private long readTimeString(String timeString) {
            return (long)(((DateTime)readerOf("~t" + timeString).Read()) - Epoch).TotalMilliseconds;
        }

        private void assertReadsFormat(String formatString){

            DateTime d = new DateTime();
            //SimpleDateFormat df = formatter(formatString);
            var ds = d.ToString(formatString);
            Assert.AreEqual(
                (long)(DateTime.ParseExact(ds, formatString, null) - Epoch).TotalMilliseconds,
                readTimeString(ds));
        }

        [Test]
        public void testReadTime()
        {
            var d = UtcNowToMillisecondGranularity();

            String timeString = AbstractParser.FormatDateTime(d);
            long t = (long)(d.ToUniversalTime() - Epoch).TotalMilliseconds;

            Assert.AreEqual(t, readTimeString(timeString));

            assertReadsFormat("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            assertReadsFormat("yyyy-MM-dd'T'HH:mm:ss'Z'");
            assertReadsFormat("yyyy-MM-dd'T'HH:mm:ss.fff-00:00");

            var thing = new Hashtable() {
                {"~#m", t },
            };

            Assert.AreEqual(d.ToLocalTime(), ((DateTime)readerOf(thing).Read()).ToLocalTime());
        }

        private static DateTime UtcNowToMillisecondGranularity()
        {
            DateTime d = DateTime.UtcNow;
            d = default(DateTime).AddMilliseconds((long)(d - default(DateTime)).TotalMilliseconds);
            d = new DateTime(d.Ticks, DateTimeKind.Utc);
            return d;
        }

        [Test]
        public void testReadGuid() {

            var guid = Guid.NewGuid();
            var uuid = (Uuid)guid;
            long hi64 = uuid.MostSignificantBits;
            long lo64 = uuid.LeastSignificantBits;

            Assert.AreEqual(guid, (Guid)readerOf("~u" + uuid.ToString()).Read());

            var thing = new ArrayList() {
                "~#u",
                new ArrayList() {
                    hi64,
                    lo64,
                },
            };

            Assert.AreEqual(guid, (Guid)readerOf(thing).Read());
        }

        [Test]
        public void testReadURI()
        {
            Uri uri = new Uri("http://www.foo.com");

            Assert.AreEqual(uri, (Uri)readerOf("~rhttp://www.foo.com").Read());
        }

        [Test]
        public void testReadSymbol() {

            IReader r = readerOf("~$foo");
            Object v = r.Read();
            Assert.AreEqual("foo", v.ToString());
        }

        [Test]
        public void testReadCharacter() {

            Assert.AreEqual('f', (char) readerOf("~cf").Read());
        }

        // Binary data tests

        private static byte[] GetBytes(string s) =>
            System.Text.Encoding.UTF8.GetBytes(s);

        private static string GetString(byte[] bs) =>
            System.Text.Encoding.UTF8.GetString(bs);

        [Test]
        public void testReadBinary() {
            byte[] bytes = GetBytes("foobarbaz");
            byte[] encodedBytes = GetBytes(System.Convert.ToBase64String(bytes));
            byte[] decoded = readerOf("~b" + GetString(encodedBytes)).Read() as byte[];

            Assert.AreEqual(bytes.Length, decoded.Length);

            var same = true;
            for(int i=0;i<bytes.Length;i++) {
                if(bytes[i]!=decoded[i])
                    same = false;
            }

            Assert.That(same);
        }

        [Test]
        public void testReadUnknown() {

            Assert.AreEqual(TransitFactory.TaggedValue("j", "foo"), readerOf("~jfoo").Read());

            var l = new ArrayList { 1L, 2L };

            var thing = new Hashtable() {
                {"~#point", l }
            };

            var expected = TransitFactory.TaggedValue("point", l);
            ITaggedValue actual = (ITaggedValue)readerOf(thing).Read();
            Assert.AreEqual(expected.Tag, actual.Tag, "tag");
            CollectionAssert.AreEqual((IList)expected.Representation, (IList)actual.Representation, "rep");
        }

        [Test]
        public void testReadArray() {
            long[] thing = {1L, 2L, 3L};

            var l = readerOf(thing).Read() as IList;

            AssertIsInstanceOfThese(adapter.ListTypeGuarantees, l);
            Assert.AreEqual(3, l.Count);

            Assert.AreEqual(1L, l[0]);
            Assert.AreEqual(2L, l[1]);
            Assert.AreEqual(3L, l[2]);
        }

        [Test]
        public void testReadArrayWithNestedDoubles() {
            var thing = new ArrayList() {
                -3.14159,
                3.14159,
                4.0E11,
                2.998E8,
                6.626E-34,
            };

            var l = readerOf(thing).Read() as IList;

            for(int i = 0; i < l.Count; i++) {
                Assert.AreEqual(l[i], thing[i]);
            }

            AssertIsInstanceOfThese(adapter.ListTypeGuarantees, l);
        }

        [Test]
        public void testReadArrayWithNested() {

            DateTime d = UtcNowToMillisecondGranularity();
            String t = AbstractParser.FormatDateTime(d);

            var thing = new ArrayList() {
                "~:foo",
                "~t" + t,
                "~?t",
            };

            var l = readerOf(thing).Read() as IList;

            Assert.AreEqual(3, l.Count);
            AssertIsInstanceOfThese(adapter.ListTypeGuarantees, l);

            Assert.AreEqual(":foo", l[0].ToString());
            Assert.AreEqual(d.Ticks, ((DateTime)l[1]).Ticks);
            Assert.That((Boolean) l[2]);

            DateTime[] da = {Epoch.AddMilliseconds(-6106017600000l),
                               Epoch.AddMilliseconds(0),
                               Epoch.AddMilliseconds(946728000000l),
                               Epoch.AddMilliseconds(1396909037000l)};

            var dates = new ArrayList() {
                "~t" + AbstractParser.FormatDateTime(da[0]),
                "~t" + AbstractParser.FormatDateTime(da[1]),
                "~t" + AbstractParser.FormatDateTime(da[2]),
                "~t" + AbstractParser.FormatDateTime(da[3]),
            };

            l = readerOf(dates).Read() as IList;

            for (int i = 0; i < l.Count; i++) {
                DateTime DateTime = (DateTime)l[i];
                Assert.AreEqual(DateTime, da[i]);
            }
            AssertIsInstanceOfThese(adapter.ListTypeGuarantees, l);
        }

        [Test]
        public void testReadMap() {

            var thing = new Hashtable() {
                {"a", 2 },
                {"b", 4},
            };

            var m = readerOf(thing).Read() as IDictionary;

            Assert.AreEqual(2, m.Count);
            AssertIsInstanceOfThese(adapter.DictionaryTypeGuarantees, m);

            Assert.AreEqual(2L, m["a"]);
            Assert.AreEqual(4L, m["b"]);
        }

        [Test]
        public void testReadMapWithNested() {

            String uuid = Guid.NewGuid().ToString();

            var thing = new Hashtable() {
                {"a", "~:foo" },
                {"b", "~u" + uuid},
            };

            var m = readerOf(thing).Read() as IDictionary;

            Assert.AreEqual(2, m.Count);

            Assert.AreEqual(":foo", m["a"].ToString());
            Assert.AreEqual(uuid, m["b"].ToString());
        }

        [Test]
        public void testReadSet() {

            int[] ints = {1,2,3};

            var thing = new Hashtable() {
                {"~#set", ints },
            };

            var s = readerOf(thing).Read() as ICollection;

            Assert.AreEqual(3, s.Count);
            AssertIsInstanceOfThese(adapter.SetTypeGuarantees, s);

            Assert.That((bool)RT.contains(s, 1L));
            Assert.That((bool)RT.contains(s, 2L));
            Assert.That((bool)RT.contains(s, 3L));
        }

        [Test]
        public void testReadList() {
            int[] ints = {1,2,3};

            var thing = new Hashtable() {
                {"~#list", ints },
            };

            var l = readerOf(thing).Read() as IList;

            AssertIsInstanceOfThese(adapter.ListTypeGuarantees, l);
            Assert.AreEqual(3, l.Count);

            Assert.AreEqual(1L, l[0]);
            Assert.AreEqual(2L, l[1]);
            Assert.AreEqual(3L, l[2]);
        }

        [Test]
        public void testReadRatio() {
            String[] ratioRep = {"~n1", "~n2"};

            var thing = new Hashtable() {
                {"~#ratio", ratioRep },
            };

            Ratio r = readerOf(thing).Read() as Ratio;

            Assert.AreEqual(BigInteger.Create(1), r.numerator);
            Assert.AreEqual(BigInteger.Create(2), r.denominator);
            Assert.AreEqual(0.5d, r.ToDouble(null), 0.01d);
        }

        [Test]
        public void testReadCmap() {
            String[] ratioRep = {"~n1", "~n2"};
            int[] mints = {1,2,3};

            var ratio = new Hashtable() {
                {"~#ratio", ratioRep },
            };

            var list = new Hashtable() {
                {"~#list", mints },
            };

            var things = new ArrayList() {
                ratio,
                1,
                list,
                2,
            };

            var thing = new Hashtable() {
                {"~#cmap", things },
            };

            var m = readerOf(thing).Read() as IDictionary;

            Assert.AreEqual(2, m.Count);
            AssertIsInstanceOfThese(adapter.DictionaryTypeGuarantees, m);

            var iter = m.GetEnumerator();
            while (iter.MoveNext())
            {
                if ((long)iter.Value == 1L)
                {
                    Ratio r = (Ratio)iter.Key;
                    Assert.AreEqual(BigInteger.Create(1), r.numerator);
                    Assert.AreEqual(BigInteger.Create(2), r.denominator);
                }
                else if ((long)iter.Value == 2L)
                {
                    var l = (IList)iter.Key;
                    Assert.AreEqual(1L, l[0]);
                    Assert.AreEqual(2L, l[1]);
                    Assert.AreEqual(3L, l[2]);
                }
            }
        }

        [Test]
        public void testReadMany() {

            var r = readerOf(true, null, false, "foo", 42.2, 42);
            Assert.That((Boolean)r.Read());
            Assert.Null(r.Read());
            Assert.False((Boolean) r.Read());
            Assert.AreEqual("foo", r.Read());
            Assert.AreEqual(42.2, r.Read());
            Assert.AreEqual(42L, (long) r.Read());
        }

        [Test]
        public void testWriteReadTime(){

            DateTime[] da = {//new DateTime(-6106017600000l),
                    new DateTime(0),
                    new DateTime(946728000000l),
                    new DateTime(1396909037000l)};

            var l = da.ToList();

            MemoryStream stream = new MemoryStream();
            var w = TransitFactory.Writer(TransitFormat.MsgPack, stream);
            w.Write(l);
            stream.Position = 0;
            var r = TransitFactory.Reader(TransitFormat.MsgPack, stream);
            Object o = r.Read();
            CollectionAssert.AreEqual(l, o as IEnumerable);
        }

        [Test]
        public void testWriteReadSpecialNumbers(){
            var stream = new MemoryStream();
            var w = TransitFactory.Writer(TransitFormat.MsgPack, stream);
            w.Write(Double.NaN);
            w.Write(Double.NaN);
            w.Write(Double.PositiveInfinity);
            w.Write(Double.PositiveInfinity);
            w.Write(Double.NegativeInfinity);
            w.Write(Double.NegativeInfinity);
            stream.Position = 0;
            var r = TransitFactory.Reader(TransitFormat.MsgPack, stream);
            Assert.IsNaN((Double)r.Read());
            Assert.IsNaN((Double)r.Read());
            Assert.AreEqual(Double.PositiveInfinity, (Double)r.Read());
            Assert.AreEqual(Double.PositiveInfinity, (Double)r.Read());
            Assert.AreEqual(Double.NegativeInfinity, (Double)r.Read());
            Assert.AreEqual(Double.NegativeInfinity, (Double)r.Read());
        }

        private static void AssertIsInstanceOfThese(System.Collections.Generic.IEnumerable<Type> types, object actual)
        {
            foreach (var type in types)
                Assert.IsInstanceOf(type, actual);
        }
    }
}
