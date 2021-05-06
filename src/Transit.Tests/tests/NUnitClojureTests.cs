using System.Collections;
using NUnit.Framework;

namespace Sellars.Transit.tests
{
    public class NUnitClojureTests
    {
        public const string TestsNs = "Sellars.Transit.tests.nunit-clojure-test-adapter";

        [TestCase(TestsNs)]
        public void TestsAreDefined(string testsNs) => NUnitClojureAdapter.TestSymbolsEnumerator(testsNs);

        [TestCaseSource(nameof(TestSymbols))]
        public void Test(string testSym) => NUnitClojureAdapter.deftest(testSym);

        public static IEnumerable TestSymbols =>
            NUnitClojureAdapter.SymbolsFor(TestsNs);
    }
}
