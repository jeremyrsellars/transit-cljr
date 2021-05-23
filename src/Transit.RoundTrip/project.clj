(defproject sellars.transit.roundtrip "0.1.0-SNAPSHOT"
  :source-paths [".." "src"]
  :dependencies [[org.clojure/clojure "1.10.1"]
                 [com.cognitect/transit-clj "0.8.300"]]
  :main TransitTool.roundtrip
  :clean-targets ^{:protect false} ["target"]
  :profiles {:uberjar {:aot :all}})
  
