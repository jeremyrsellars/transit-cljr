using System;
using System.Collections;
using clojure.lang;
using NUnit.Framework;

namespace Sellars.Transit.tests
{
    /// <summary>
    /// Generative tests.
    /// </summary>
    [TestFixture]
    public class TransitGenTests
    {
        static TransitGenTests()
        {
            clojure.lang.RT.LoadSpecCode();
        }

        public const string TestsNs = "Sellars.Transit.tests.transit-gen-tests";
        private string currentDirectory;

        [OneTimeSetUp]
        public void SetCurrentDirectory()
        {
            currentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(GetType().Assembly.Location);
        }

        [OneTimeTearDown]
        public void UnsetCurrentDirectory()
        {
            Environment.CurrentDirectory = currentDirectory;
        }

        [TestCase(TestsNs)]
        public void TestsAreDefined(string testsNs) => 
            NUnitClojureAdapter.TestSymbolsEnumerator(testsNs);

        [TestCaseSource(nameof(TestSymbols))]
        public void Sample(string testSym) => NUnitClojureAdapter.deftest(testSym);

        public static IEnumerable TestSymbols =>
            NUnitClojureAdapter.SymbolsFor(TestsNs);
    }
}
