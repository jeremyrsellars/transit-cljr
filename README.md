# Transit-cljr (a fork of transit-csharp)

This is an implementation of the [Transit Format](http://github.com/cognitect/transit-format) for conveying values between applications written in different programming languages.  This library is a fork of the C# implementation created and maintained by Rick Beerendonk (thanks to his good work!), but with emphasis on supporting types from [Clojure CLR](https://github.com/clojure/clojure-clr).

## Why fork instead of use transit-csharp as a library?

There were several reasons that led me to fork the repository instead of use transit-csharp nuget package as-is:
* There are many changes in the .Net ecosystem since the 2014 [transit-csharp](https://github.com/rickbeerendonk/transit-csharp), including .net 5.  Visual Studio tooling was not confident the library would work in this context.
* My past attempts to wrap the transit-csharp library for Clojure were unsuccessful due to type visibility/sealed reasons, the increased complexity of .Net generic types, lack of support for non-generic System.Collections types, for example.  Perhaps the issues were my own and not the limitation of the transit-csharp library (or perhaps only small changes would be necessary to the library).
* I wanted to implement the `application/transit+msgpack` format.

## About the Transit Format
Transit is a data format and a set of libraries for conveying values between applications written in different languages. This library provides support for marshalling Transit data to/from .Net.

* [Rationale](http://blog.cognitect.com/blog/2014/7/22/transit)
* [Specification](http://github.com/cognitect/transit-format)

This implementation's major.minor version number corresponds to the version of the Transit specification it supports.

JSON and JSON-Verbose are implemented, but more tests need to be written.  MessagePack is implemented, but is insufficiently tested.

> NOTE: Transit is intended primarily as a wire protocol for transferring data between applications. If storing Transit data durably, readers and writers are expected to use the same version of Transit and you are responsible for migrating/transforming/re-storing that data when and if the transit format changes.

– https://github.com/cognitect/transit-format#implementations

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
|time|System.DateTime|System.DateTime (kind=utc)|
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


## Dates

In the Transit specification, times are represented in UTC.  Transit 0.8.4 offers only UTC times, but in .Net, System.DateTime can have 3 kinds:

|System.DateTimeKind|Writer behavior|
|----|----|
|Utc|Date is written as-is (to the millisecond)|
|Unspecified|Same as UTC|
|Local|Converted to UTC|

Reading dates.  Since transit uses UTC, this is the default in transit-cljr.

If you wish the reader to convert the dates to local on deserialization,
consider using the provided custom readers: `DateTimeLocalReadHandler` and `VerboseDateTimeLocalReadHandler`.
These custom handlers may be supplied when creating the reader, 
for example, `TransitFactory.Writer<T>(Format type, Stream output, IDictionary<Type, IWriteHandler> customHandlers)`.

Note: **This is a departure from Beerendonk's Transit-csharp library which converted the times to local on reading.**  When migrating from transit-csharp, or if your serialization and deserialization libraries are different, please ensure the times are communicated correctly in this regard.

Custom handlers may also be useful for other more verbose time representations, such as `System.DateTimeOffset` or Noda Time formats.  However, since these would potentially reduce the cross-platform usefulness of the library, they are not provided out of the box.

## MIME Types

The MIME type for Transit format data depends on the encoding scheme:

|Encoding|MIME type
|----|----
JSON / JSON-Verbose|application/transit+json
MessagePack|application/transit+msgpack

## Copyright and License

Portions of this project are based on several projects licensed under the Apache License, Version 2.0.  No endorsement by the copyright holders is expressed or implied.

Copyright © 2021 Jeremy Sellars.

Parts of this library are a Fork of the C# version created and maintained by Rick Beerendonk, therefore

Copyright © 2014 Rick Beerendonk.

Parts of this library are a C# port of the Java version created and maintained by Cognitect, therefore

Copyright © 2014 Cognitect

Parts of this library are a ClojureClr port of the Clojure version created and maintained by Rich Hickey, therefore

Copyright © 2014 Rich Hickey.

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
