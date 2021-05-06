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

using System.Collections.Generic;
using System.Linq;
using Sellars.Transit.Alpha;
using Sellars.Transit.Util;

namespace Beerendonk.Transit.Impl.WriteHandlers
{
    internal class DictionaryWriteHandler : AbstractWriteHandler, IAbstractEmitterAware
    {
        private AbstractEmitter abstractEmitter;
        
        public void SetEmitter(AbstractEmitter abstractEmitter)
        {
            this.abstractEmitter = abstractEmitter;
        }

        private bool StringableKeys(object d)
        {
            System.Collections.IEnumerable keys;
            if (d is System.Collections.IDictionary dict)
                keys = dict.Keys;
            else if (d is IDictionary<object, object> gdict)
                keys = gdict.Keys;
            else if (d is IReadOnlyDictionary<object, object> rodict)
                keys = rodict.Keys;
            else
                return false; // unknown type.

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
                return Enumerable.ToList(DictionaryHelper.CoerceKeyValuePairs(obj));
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
