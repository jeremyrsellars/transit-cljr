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
using Sellars.Transit.Alpha;

namespace Beerendonk.Transit.Impl.ReadHandlers
{
    internal class VerboseDateTimeReadHandler : IReadHandler
    {
        public object FromRepresentation(object representation)
        {
            var s = (string)representation;
            DateTime result;

            switch (s.Length)
            {
                case 29:
                    if (DateTime.TryParseExact(s, "yyyy-MM-dd'T'HH:mm:ss.fff-00:00", default, default, out result))
                        return result;
                    break;
                case 24:
                    if (DateTime.TryParseExact(s, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", default, default, out result))
                        return result;
                    break;
                case 20:
                    if (DateTime.TryParseExact(s, "yyyy-MM-dd'T'HH:mm:ss'Z'", default, default, out result))
                        return result;
                    break;
            }

            if (!DateTime.TryParse((string)representation, out result))
            {
                throw new TransitException("Cannot parse representation as a DateTime: " + representation);
            }

            return result;
        }
    }
}