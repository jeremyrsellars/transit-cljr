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
using System.IO;
using Sellars.Transit.Alpha;

namespace Beerendonk.Transit.Tests
{
    public class FactoryImplementationAdapter
    {
        public string Name { get; set; }
        public Func<TransitFactory.Format, Stream, IReader> CreateReader;
        public Func<TransitFactory.Format, Stream, IWriter<object>> CreateWriter;
        public Func<TransitFactory.Format, Stream, IDictionary<Type, IWriteHandler>, IWriter<object>> CreateCustomWriter;
        public Type ListType { get; set; }
        public Type DictionaryType { get; set; }
        public Type SetType { get; set; }
        public override string ToString() => Name;

        public static IEnumerable<FactoryImplementationAdapter> Adapters =>
            new[]
            {
                new FactoryImplementationAdapter
                {
                    Name = typeof(Sellars.Transit.Alpha.TransitFactory).FullName,
                    CreateReader = Sellars.Transit.Alpha.TransitFactory.Reader,
                    CreateWriter = Sellars.Transit.Alpha.TransitFactory.Writer<object>,
                    CreateCustomWriter = Sellars.Transit.Alpha.TransitFactory.Writer<object>,
                    SetType = typeof(System.Collections.Immutable.IImmutableSet<object>),
                    DictionaryType = typeof(System.Collections.Immutable.IImmutableDictionary<object, object>),
                    ListType = typeof(System.Collections.Immutable.IImmutableList<object>),
                },
                new FactoryImplementationAdapter
                {
                    Name = typeof(Sellars.Transit.Cljr.Alpha.TransitFactory).FullName,
                    CreateReader = Sellars.Transit.Cljr.Alpha.TransitFactory.Reader,
                    CreateWriter = Sellars.Transit.Cljr.Alpha.TransitFactory.TypedWriter<object>,
                    CreateCustomWriter = Sellars.Transit.Cljr.Alpha.TransitFactory.TypedWriter<object>,
                    SetType = typeof(clojure.lang.IPersistentSet),
                    DictionaryType = typeof(clojure.lang.IPersistentMap),
                    ListType = typeof(clojure.lang.IPersistentVector),
                },
            };
    }
}
