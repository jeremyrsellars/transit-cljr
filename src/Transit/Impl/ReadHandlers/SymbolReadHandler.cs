﻿// Modifications Copyright (C) 2021 Jeremy Sellars.
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

using Sellars.Transit.Alpha;

namespace Beerendonk.Transit.Impl.ReadHandlers
{
    internal partial class SymbolReadHandler : IReadHandler
    {
        public object FromRepresentation(object representation) =>
            ParseString((string)representation);

        public static object ParseString(string representation) =>
            TransitFactory.Symbol(representation);
    }
}
