(defproject sellars.transit.roundtrip "0.1.0-SNAPSHOT"
  :source-paths [".." "src"]
  :dependencies [[org.clojure/clojure "1.10.3"]
                 [org.clojure/test.check "1.1.0"]
                 [org.clojure/tools.reader "1.3.5"]
                 [com.cognitect/transit-clj "1.0.324"]]
  :main TransitTool.roundtrip
  :clean-targets ^{:protect false} ["target"]
  :profiles {:uberjar {:aot :all}})
  
