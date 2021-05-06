using System;
using System.Collections;
using System.Collections.Generic;
using clojure.lang;

namespace Sellars.Transit.tests
{
    public class NsTestVarsEnumerable : IEnumerable<object[]>
    {
        public static readonly Keyword TestKw = RT.keyword(null, "test");

        private static Var _nsPublics;

        static NsTestVarsEnumerable()
        {
            RT.Init();
            _nsPublics = Var.find(Symbol.intern("clojure.core", "ns-publics"));
        }

        public NsTestVarsEnumerable(string ns)
        {
            DelayedClj.RequireNS(ns);
            NamespaceName = Symbol.intern(ns);
        }

        public Symbol NamespaceName { get; }

        public IEnumerator<object[]> GetEnumerator()
        {
            var publicVars = _nsPublics.invoke(NamespaceName);
            var iter = RT.iter(publicVars);
            while (iter.MoveNext())
            {
                if (iter.Current is MapEntry entry
                    && entry.key() is Symbol sym
                    && RT.meta(entry.val()) is IPersistentMap meta
                    && meta.valAt(TestKw) is object)
                    yield return new object[] { $"{NamespaceName}/{sym}" };
            }
            if (iter is IDisposable disp)
                disp.Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
