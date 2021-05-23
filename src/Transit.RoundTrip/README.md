# Transit Round Trip

Designed for testing transit-clj and transit-cljr interop (between Clojure JVM and Clojure CLR).

## About (for developers)

Clojure endorses a directory structure where namespaces are mapped to files as follows (approximately):

1. A namespace `a.b.c` is expected to be a path relative in the form of `root/a/b/c.clj*` where `root` is one of the roots, and `.clj*` is a platform appropriate extension.
2. In order to make this multi-platform project support code sharing with the same directory structure, there are 2 sets of rules:
    * In Leiningen, the roots could be specified in `:source-paths ["../"]`, for example.
    * In ClojureClr (.Net Framework, .Net Core, and .Net 5), the roots could be assembly Embedded Resources resources named `a.b.c.cljc`, for example.  The easiest way to make msbuild embed resources in the appropriate directory structure, use a `.csproj` with relative paths `a/b/c.cljc` relative to the `.csproj`.


## Usage

    lein repl
