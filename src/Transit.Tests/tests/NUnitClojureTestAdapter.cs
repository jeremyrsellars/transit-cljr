using System.Collections;
using clojure.lang;
using NUnit.Framework;

namespace Sellars.Transit.tests
{
    public static class NUnitClojureAdapter
    {
        public const string ClojureNamespace = "Sellars.Transit.tests.nunit-clojure-test-adapter";

        public static void TestSymbolsEnumerator(string testNs)
        {
            Assert.That(new NsTestVarsEnumerable(testNs), Is.Not.Empty);
        }

        public static void deftest(string testSym)
        {
            Var.find(Symbol.intern(ClojureNamespace, "test-var"))
                .invoke(Var.find(Symbol.intern(testSym)));
        }

        public static IEnumerable SymbolsFor(string ns) =>
            new NsTestVarsEnumerable(ns);
    }
}
