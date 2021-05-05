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

using System.Collections.Immutable;
using Sellars.Transit.Alpha;

namespace Beerendonk.Transit.Impl.ReadHandlers
{
    internal class LinkReadHandler : IReadHandler
    {
        public object FromRepresentation(object representation)
        {
            var dic = ImmutableDictionary<string, object>.Empty;

            foreach (var item in AbstractEmitter.CoerceKeyValuePairs(representation))
            {
                dic = dic.Add((string)item.Key, item.Value);
            }

            return new Link(dic);
        }
    }
}