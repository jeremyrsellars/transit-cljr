using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sellars.Transit.Alpha;
using NUnit.Framework;

namespace Sellars.Transit.Tests
{
    [TestFixtureSource(typeof(FactoryImplementationAdapter), nameof(FactoryImplementationAdapter.Adapters))]
    public class NumberTests
    {
        private FactoryImplementationAdapter adapter;

        public NumberTests(FactoryImplementationAdapter adapter)
        {
            this.adapter = adapter;
        }

        [TestCaseSource(nameof(Formats))]
        public void TestFloatingPointIntegers(TransitFactory.Format format) => Assert.Multiple(() =>
        {
            TestRoundTripOfFloats(format, new float[][] { new float[] { 1, 2 } });
            TestRoundTripOfDoubles(format, new double[][] { new double[] { 1, 3 } });
            TestRoundTripOfFloats(format, new float[][] { new float[] { -10000, 20000 } });
            TestRoundTripOfDoubles(format, new double[][] { new double[] { -100000, 30000 } });
        });

        [TestCaseSource(nameof(Formats))]
        public void TestFloatingPointNumbers(TransitFactory.Format format) => Assert.Multiple(() =>
        {
            TestRoundTripOfDoubles(format, new double[][] { new double[] { 1.654d, 2.8765d } });
            TestRoundTripOfDoubles(format, new double[][] { new double[] { -32.654d, -23487.8765d } });
            TestRoundTripOfFloats(format, new float[][] { new float[] { 1.654f, 2.8765f } });
            TestRoundTripOfFloats(format, new float[][] { new float[] { -32.654f, -23487.8765f } });
            TestRoundTripOfFloats(format, new float[][] { new float[] { 1.654e19f, 2.8765e19f } });
            TestRoundTripOfFloats(format, new float[][] { new float[] { -32.654e19f, -23487.8765e19f } });
        });

        private void TestRoundTripOfDoubles(TransitFactory.Format format, double[][] value)
        {
            var json = Write(value, format);
            Console.WriteLine(json);
            var deser = Reader(json, format).Read<System.Collections.IList>();
            Assert.That(deser.Count, Is.EqualTo(value.Length), "Top-level count");
            for (int a = 0; a < value.Length; a++)
            {
                Assert.That(((IList)deser[a]).Count, Is.EqualTo(value[a].Length), $"level-2 count");
                for (int i = 0; i < value[a].Length; i++)
                {
                    Assert.That(((IList)deser[a])[i], Is.EqualTo(value[a][i]).Within(0.00001), $"Index: [{a}][{i}]; Format: {format}");
                    Assert.That(((IList)deser[a])[i], Is.InstanceOf<double>(), $"Format: {format}");
                }
            }
        }

        private void TestRoundTripOfFloats(TransitFactory.Format format, float[][] value)
        {
            var encoded = Write(value, format);
            Console.WriteLine(encoded);
            var deser = Reader(encoded, format).Read<System.Collections.IList>();
            Assert.That(deser.Count, Is.EqualTo(value.Length), "Top-level count");
            for (int a = 0; a < value.Length; a++)
            {
                Assert.That(((IList)deser[a]).Count, Is.EqualTo(value[a].Length), "level-2 count");
                for (int i = 0; i < value[a].Length; i++)
                {
                    Assert.That(((IList)deser[a])[i], Is.EqualTo(value[a][i]).Within(Math.Max(0.0001, Math.Abs(value[a][i] / 10000))), $"Index: [{a}][{i}]; Format: {format}");
                    Assert.That(((IList)deser[a])[i], Is.InstanceOf<double>());
                }
            }
        }

        private IReader Reader(object s, TransitFactory.Format format)
        {
            Stream input = new MemoryStream(s as byte[] ?? Encoding.UTF8.GetBytes((string)s));
            return adapter.CreateReader(format, input);
        }

        private object Write(object obj, TransitFactory.Format format, IDictionary<Type, IWriteHandler> customHandlers = null,
            IWriteHandler defaultHandler = null, Func<object, object> transform = null)
        {
            using (var output = new MemoryStream())
            {
                IWriter<object> w = adapter.CreateCustomWriter(format, output, customHandlers, defaultHandler, transform);
                w.Write(obj);

                if (format == TransitFactory.Format.MsgPack)
                    return output.ToArray();

                output.Position = 0;
                var sr = new StreamReader(output);
                return sr.ReadToEnd();
            }
        }

        public static TransitFactory.Format[] Formats => Enum.GetValues(typeof(TransitFactory.Format)).Cast<TransitFactory.Format>().ToArray();
    }
}
