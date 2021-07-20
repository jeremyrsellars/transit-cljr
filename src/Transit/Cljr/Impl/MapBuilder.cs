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

using clojure.lang;
using Sellars.Transit.Alpha;

namespace Sellars.Transit.Cljr.Impl
{
    /// <summary>
    /// Represents a dictionary builder targetted at clojure.lang.IPersistentMap.
    /// </summary>
    internal class MapBuilder : IDictionaryReader
    {
        /// <summary>
        /// Initializes a new gestational dictionary.
        /// </summary>
        /// <returns>
        /// A new gestational dictionary.
        /// </returns>
        public object Init()
        {
            return PersistentArrayMap.EMPTY.asTransient();
        }

        /// <summary>
        /// Adds a key and value to the dictionary, returning a new dictionary;
        /// new dictionary must be used for any further invocations.
        /// </summary>
        /// <param name="dictionary">A gestational dictionary.</param>
        /// <param name="key">A key.</param>
        /// <param name="value">A value.</param>
        /// <returns>
        /// A new gestational dictionary.
        /// </returns>
        public object Add(object dictionary, object key, object value)
        {
            return ((ITransientCollection)dictionary).conj(new MapEntry(key, value));
        }

        /// <summary>
        /// Completes building of a dictionary from a gestational dictionary.
        /// </summary>
        /// <param name="dictionary">The gestational dictionary.</param>
        /// <returns>
        /// The completed dictionary.
        /// </returns>
        public object Complete(object dictionary)
        {
            return ((ITransientCollection)dictionary).persistent();
        }
    }
}
