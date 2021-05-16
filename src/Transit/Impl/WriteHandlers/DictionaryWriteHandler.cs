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
using System.Linq;
using Sellars.Transit.Alpha;
using Sellars.Transit.Util;

namespace Beerendonk.Transit.Impl.WriteHandlers
{
    internal class DictionaryWriteHandler : AbstractWriteHandler, IAbstractEmitterAware
    {
        private static readonly string[] Dictionary_2InterfaceTypeNames =
        {
            "System.Collections.Generic.IReadOnlyDictionary`2",
            "System.Collections.Generic.IDictionary`2",
            "System.Collections.Immutable.IImmutableDictionary`2",
        };

        private AbstractEmitter abstractEmitter;
        
        public void SetEmitter(AbstractEmitter abstractEmitter)
        {
            this.abstractEmitter = abstractEmitter;
        }

        private bool StringableKeys(object d)
        {
            if (d != null && TypeStaticallyUsesStringableKeys(d.GetType()))
                return true;

            System.Collections.IEnumerable keys;
            if (d is System.Collections.IDictionary dict)
                keys = dict.Keys;
            else if (d is IDictionary<object, object> gdict)
                keys = gdict.Keys;
            else if (d is IReadOnlyDictionary<object, object> rodict)
                keys = rodict.Keys;
            else
                return false; // unknown type.

            keys = keys ?? Enumerable.Empty<object>();

            foreach (var key in keys)
	        {
                string tag = abstractEmitter.GetTag(key);

                if (tag != null && tag.Length > 1)
                {
                    return false;
                }
                else if (tag == null && !(key is string)) 
                {
                    return false;
                }
	        }
            
            return true;
        }

        /// <summary>
        /// This is a "more-static" version of StringableKeys intent
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool TypeStaticallyUsesStringableKeys(Type type)
        {
            try
            {
                foreach (string name in Dictionary_2InterfaceTypeNames)
                {
                    if (type.GetInterface(name) is Type interfaceType
                        && interfaceType.GetGenericArguments().First() is Type keyType)
                    {
                        return KeyTypeIsAlwaysStringable(keyType);
                    }
                }
            }
            catch (System.Reflection.AmbiguousMatchException)
            {
            }
            return false;
        }

        /// <summary>
        /// This is a "more-static" version of StringableKeys intent
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool KeyTypeIsAlwaysStringable(Type keyType)
        {
            if (abstractEmitter.GetHandlerForType(keyType) is IKnownTag knownTag)
            {
                var tag = knownTag.KnownTag;
                return (tag != null && tag.Length == 1)
                    || (tag == null && typeof(string) == keyType);
            }

            return false;
        }

        public override string Tag(object obj)
        {
            if (StringableKeys(obj))
            {
                return "map";
            }
            else
            {
                return "cmap";
            }
        }

        public override object Representation(object obj)
        {
            if (StringableKeys(obj))
            {
                return obj;// Enumerable.ToList(DictionaryHelper.CoerceKeyValuePairs(obj));
            }
            else
            {
                var l = new List<object>();

                foreach (var item in DictionaryHelper.CoerceKeyValuePairs(obj))
                {
                    l.Add(item.Key);
                    l.Add(item.Value);
                }

                return TransitFactory.TaggedValue("array", l);
            }
        }
    }
}
