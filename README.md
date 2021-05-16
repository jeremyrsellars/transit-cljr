# Transit-cljr (a fork of transit-csharp)

This is an implementation of the [Transit Format](http://github.com/cognitect/transit-format) for conveying values between applications written in different programming languages.  This library is a fork of the C# implementation created and maintained by Rick Beerendonk (thanks to his good work!), but with emphasis on supporting types from [Clojure CLR](https://github.com/clojure/clojure-clr).

## Why fork instead of use transit-csharp as a library?

There were several reasons that led me to fork the repository instead of use transit-csharp nuget package as-is:
* There are many changes in the .Net ecosystem since the 2014 [transit-csharp](https://github.com/rickbeerendonk/transit-csharp), including .net 5.  Visual Studio tooling was not confident the library would work in this context.
* My past attempts to wrap the transit-csharp library for Clojure were unsuccessful due to type visibility/sealed reasons, the increased complexity of .Net generic types, lack of support for non-generic System.Collections types, for example.  Perhaps the issues were my own and not the limitation of the transit-csharp library (or perhaps only small changes would be necessary to the library).
* I hope to implement the MessagePack format at some point.

## About the Transit Format
Transit is a data format and a set of libraries for conveying values between applications written in different languages. This library provides support for marshalling Transit data to/from .Net.

* [Rationale](http://blog.cognitect.com/blog/2014/7/22/transit)
* [Specification](http://github.com/cognitect/transit-format)

This implementation's major.minor version number corresponds to the version of the Transit specification it supports.

JSON and JSON-Verbose are implemented, but more tests need to be written.
MessagePack is **not** implemented yet. 

_NOTE: Transit is a work in progress and may evolve based on feedback. As a result, while Transit is a great option for transferring data between applications, it should not yet be used for storing data durably over time. This recommendation will change when the specification is complete._

## Releases and Dependency Information

None at this time.

## Usage

## Default Type Mapping

Transit provides some built-in types and the ability to extend the format to support additional "extension" types.

|Transit type|Write accepts|Read returns (common csharp/cljr)|
|------------|-------------|---------------------------------|
|null|null|null|
|string|System.String|System.String|
|boolean|System.Boolean|System.Boolean|
|integer|System.Byte, System.Int16, System.Int32, System.Int64|System.Int64|
|decimal|System.Single, System.Double|System.Double|
|keyword|clojure.lang.Keyword|clojure.lang.Keyword|
|symbol|clojure.lang.Symbol|clojure.lang.Symbol|
|big decimal|_not implemented_|Sellars.TransitCljr.Numerics.Alpha|
|big integer|clojure.lang.BigInteger|clojure.lang.BigInteger|
|time|System.DateTime|System.DateTime|
|uri|System.Uri|System.Uri|
|uuid|System.Guid|System.Guid|
|char|System.Char|System.Char|
|array|T[], System.Collections.Generic.IList<>, clojure.lang.IPersistentVector|IList|
|list|System.Collections.Generic.IEnumerable<>, clojure.lang.IPersistentVector|IList|
|set|System.Collections.Generic.ISet<>,clojure.lang.IPersistentSet|IEnumerable|
|map|System.Collections.Generic.IDictionary<,>, clojure.lang.IPersistentMap|IDictionary
|link|Sellars.TransitCljr.Alpha.Link|Sellars.TransitCljr.Alpha.Link|
|ratio +|Sellars.TransitCljr.Alpha.Ratio|Sellars.TransitCljr.Alpha.Ratio|

\+ Extension type

Extension types are composed of types that transit understands (ground types, extension types, and your other extension types).  Since this library offers two TransitFactory implementations (friendly to C# and ClojureCLR), it may be helpful to know the specific guarantee of each implementation.  (Building extension types often requires casting `object` to a more useful interface type.)

In general, the scalar types are the same between implementations and collection types are different.  Sellars.Transit.Alpha.TransitFactory uses System.Collections.Generic, while Sellars.Transit.Cljr.Alpha.TransitFactory uses collections from clojure.lang.

### Collection Types

If you wish to provide a ReadHandler that will work for either implementation, use the common interface.  Remember, `IDictionary.GetEnumerator` yields an IDictionaryEnumerator that has `.MoveNext`, `.Key`, `.Value` that can be useful if you wish to enumerate the map.

|Transit type|Read returns (common)|Read (C#)|Read (cljr)|
|------------|---------------------|---------|-----------|
|array|`IList`|`IList<object>`|`clojure.lang.IPersistentVector`|
|list|`IList`|`IList<object>`|`clojure.lang.IPersistentVector`|
|set|`IEnumerable`,`ISet<object>`|`clojure.lang.IPersistentSet`|
|map|`IDictionary`|`IImmutableDictionary<object,object>`|`clojure.lang.IPersistentMap`|

## Layered Implementations

## Copyright and License
Copyright © 2021 Jeremy Sellars.

This library is a Fork of the C# version created and maintained by Rick Beerendonk, therefore

Copyright © 2014 Rick Beerendonk.

This library is a C# port of the Java version created and maintained by Cognitect, therefore

Copyright © 2014 Cognitect

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
