﻿// Copyright © 2014 Rick Beerendonk. All Rights Reserved.
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
using System.IO;
using Sellars.Transit.Alpha;

namespace Sellars.Transit.Tests
{
    public class FactoryImplementationAdapter
    {
        public string Name { get; set; }
        public Func<TransitFactory.Format, Stream, IReader> CreateReader;
        public Func<TransitFactory.Format, Stream, IWriter<object>> CreateWriter;
        public Func<TransitFactory.Format, Stream, System.Collections.Generic.IDictionary<Type, IWriteHandler>, IWriteHandler, Func<object, object>, IWriter<object>> CreateCustomWriter;
        public Type[] ListTypeGuarantees { get; set; }
        public Type[] DictionaryTypeGuarantees { get; set; }
        public Type[] SetTypeGuarantees { get; set; }
        public Func<object, string> SerializeJson { get; set; }

        public override string ToString() => Name;

        public static System.Collections.Generic.IEnumerable<FactoryImplementationAdapter> Adapters =>
            new[]
            {
                // TransitFactory (Newtonsoft)
                new FactoryImplementationAdapter
                {
                    Name = typeof(Sellars.Transit.Alpha.TransitFactory).FullName,
                    CreateReader = Sellars.Transit.Alpha.TransitFactory.Reader,
                    CreateWriter = Sellars.Transit.Alpha.TransitFactory.Writer<object>,
                    CreateCustomWriter = Sellars.Transit.Alpha.TransitFactory.Writer<object>,
                    SerializeJson = SerializeNewtonsoft,
                    SetTypeGuarantees = new []{
                        typeof(System.Collections.IEnumerable),
                        typeof(System.Collections.Generic.IEnumerable<object>),
                        typeof(System.Collections.Generic.ISet<object>),
                    },
                    DictionaryTypeGuarantees = new []{
                        typeof(System.Collections.IDictionary),
                        typeof(System.Collections.Immutable.IImmutableDictionary<object, object>),
                    },
                    ListTypeGuarantees = new []{
                        typeof(System.Collections.IList),
                        typeof(System.Collections.Generic.IList<object>),
                    },
                },
                new FactoryImplementationAdapter
                {
                    Name = typeof(Sellars.Transit.Cljr.Alpha.TransitFactory).FullName,
                    CreateReader = Sellars.Transit.Cljr.Alpha.TransitFactory.Reader,
                    CreateWriter = Sellars.Transit.Cljr.Alpha.TransitFactory.TypedWriter<object>,
                    CreateCustomWriter = Sellars.Transit.Cljr.Alpha.TransitFactory.TypedWriter<object>,
                    SerializeJson = SerializeNewtonsoft,
                    SetTypeGuarantees = new []{
                        typeof(System.Collections.IEnumerable),
                        typeof(clojure.lang.IPersistentSet),
                    },
                    DictionaryTypeGuarantees = new []{
                        typeof(System.Collections.IDictionary),
                        typeof(clojure.lang.IPersistentMap),
                    },
                    ListTypeGuarantees = new []{
                        typeof(System.Collections.IList),
                        typeof(clojure.lang.IPersistentVector),
                    },
                },

                // FastTransitFactory (System.Text.Json)
                new FactoryImplementationAdapter
                {
                    Name = typeof(Sellars.Transit.Alpha.FastTransitFactory).FullName,
                    CreateReader = Sellars.Transit.Alpha.FastTransitFactory.Reader,
                    CreateWriter = Sellars.Transit.Alpha.FastTransitFactory.Writer<object>,
                    CreateCustomWriter = Sellars.Transit.Alpha.FastTransitFactory.Writer<object>,
                    SerializeJson = SerializeSystemTextJson,
                    SetTypeGuarantees = new []{
                        typeof(System.Collections.IEnumerable),
                        typeof(System.Collections.Generic.IEnumerable<object>),
                        typeof(System.Collections.Generic.ISet<object>),
                    },
                    DictionaryTypeGuarantees = new []{
                        typeof(System.Collections.IDictionary),
                        typeof(System.Collections.Immutable.IImmutableDictionary<object, object>),
                    },
                    ListTypeGuarantees = new []{
                        typeof(System.Collections.IList),
                        typeof(System.Collections.Generic.IList<object>),
                    },
                },
                new FactoryImplementationAdapter
                {
                    Name = typeof(Sellars.Transit.Cljr.Alpha.FastTransitFactory).FullName,
                    CreateReader = Sellars.Transit.Cljr.Alpha.FastTransitFactory.Reader,
                    CreateWriter = Sellars.Transit.Cljr.Alpha.FastTransitFactory.TypedWriter<object>,
                    CreateCustomWriter = Sellars.Transit.Cljr.Alpha.FastTransitFactory.TypedWriter<object>,
                    SerializeJson = SerializeSystemTextJson,
                    SetTypeGuarantees = new []{
                        typeof(System.Collections.IEnumerable),
                        typeof(clojure.lang.IPersistentSet),
                    },
                    DictionaryTypeGuarantees = new []{
                        typeof(System.Collections.IDictionary),
                        typeof(clojure.lang.IPersistentMap),
                    },
                    ListTypeGuarantees = new []{
                        typeof(System.Collections.IList),
                        typeof(clojure.lang.IPersistentVector),
                    },
                },
            };

        private static string SerializeNewtonsoft(object value)
        {
            var writer = new StringWriter();
            new Newtonsoft.Json.JsonSerializer().Serialize(writer, value);
            return writer.GetStringBuilder().ToString();
        }

        private static string SerializeSystemTextJson(object value) =>
            System.Text.Json.JsonSerializer.Serialize(value);
    }
}
