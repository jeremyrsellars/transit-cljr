(try
  (System.Diagnostics.Debug/WriteLine "loading clojure.test.check")
  (Console/WriteLine "loading clojure.test.check")
  (System.Diagnostics.Debug/WriteLine (assembly-load "clojure.test.check"))
  (Console/WriteLine (assembly-load "clojure.test.check"))
  (System.Diagnostics.Debug/WriteLine "Did it work?")
  (Console/WriteLine "Did it work?")
  (catch Exception e
      (throw (Exception. (str "Pre-load " (.ToString e)) e))))  

(ns Sellars.Transit.tests.transit-gen-tests
  (:require [clojure.test :as test :refer [deftest is]]
            Sellars.Transit
            [sellars.transit.alpha :as t]
            [clojure.spec.alpha :as s]
            [clojure.spec.gen.alpha :as gen]
            [clojure.stacktrace :as stack]
            [clojure.test.check.generators]
            [clojure.spec.test.alpha :as stest]
            [clojure.walk :refer [postwalk]]
            Sellars.Transit.tests.nunit-clojure-test-adapter)
  (:import [NUnit.Framework Assert Is]
           [System.IO MemoryStream]))

(alias 'stc 'clojure.spec.test.check)

(def formats #{:json :json-verbose :msgpack})

(s/def ::t/format formats)

(s/def ::example (s/with-gen any? #(s/gen some?)))        ; nil can be tested elsewhere.  `any?` put too many nils in the sample.

(defn- to-equatable-type
  [x]
  (condp instance? x
    Double           (if (Double/IsNaN x) "##NaN") ; ##NaN never equals ##NaN
    Char             (int x) ; https://clojure.atlassian.net/browse/CLJCLR-112  Chars and ints collide in maps

    x))

(defn walk-to-equatable-types
  [x]
  (postwalk to-equatable-type x))

(defn gen-sample
 [kw]
 (try
   (map walk-to-equatable-types (gen/sample (s/gen kw) 10000))
   (catch Exception e
     (throw (Exception. (str "gen-sample " (.ToString e)) e)))))  

(defn- to-transit-type
  [x]
  (condp instance? x
    clojure.lang.BigInteger           (clojure.lang.BigInt/fromBigInteger ^clojure.lang.BigInteger x)

    x))

(defn walk-to-transit-types
  [x]
  (postwalk to-transit-type x))

(defn test-round-trip
  [fmt value]
  (let [value    (walk-to-equatable-types value)
        stream   (MemoryStream. 2000)
        out      (doto stream (.set_Position 0))
        w        (t/writer out fmt)
        _        (t/write w value)
        in       (doto stream (.set_Position 0))
        s        (.ReadToEnd (System.IO.StreamReader. in))
        in       (doto stream (.set_Position 0))
        r        (t/reader in fmt)
        actual   (t/read r)]
    (is (= (walk-to-transit-types value) actual)
        (let [ok (= (walk-to-transit-types value) actual)]
          (str (if ok "Correctly parsed " "Trouble with ") fmt " for\n"
               "(= " (pr-str value) " " (pr-str actual) ")\n"
               (when-not ok s))))))

(deftest json
  (doseq [example (gen-sample ::example)]
    (test-round-trip :json example)))

(deftest json-verbose
  (doseq [example (gen-sample ::example)]
    (test-round-trip :json-verbose example)))

(deftest msgpack
  (doseq [example (gen-sample ::example)]
    (test-round-trip :msgpack example)))


(defn- something
  [x]
  (if (nil? x)
    ::something
    x))

(defn round-trip
  [fmt value]
  (let [eq-value (walk-to-equatable-types value)
        stream   (MemoryStream. 2000)
        out      (doto stream (.set_Position 0))
        w        (t/writer out fmt)
        _        (t/write w eq-value)
        in       (doto stream (.set_Position 0))
        r        (t/reader in fmt)
        actual   (t/read r)]
    actual))
(s/fdef round-trip
  :args (s/cat :fmt ::t/format :value ::example)
  :ret ::example
  :fn #(Debuggable/Equals
          (-> % :args :value walk-to-transit-types walk-to-equatable-types)
          (-> % :ret walk-to-transit-types)))

(deftest check-round-trip1
  (let [results (stest/check `round-trip {::stc/opts {:num-tests 1}})
        {:keys [total check-passed]} (stest/summarize-results results)]
    (is (= total check-passed)
      (when-not (= total check-passed)
        (pr-str (map stest/abbrev-result (take-last 5 results)))))))
(deftest check-round-trip10000
  (let [results (stest/check `round-trip {::stc/opts {:num-tests 10000}})
        {:keys [total check-passed]} (stest/summarize-results results)]
    (is (= total check-passed)
      (when-not (= total check-passed)
        (pr-str (map stest/abbrev-result (take-last 5 results)))))))

(deftest check-round-trip-verbose1
  (let [results (stest/check `round-trip {::stc/opts {:num-tests 1}})
        {:keys [total check-passed]} (stest/summarize-results results)]
    (is (= total check-passed)
      (when-not (= total check-passed)
        (pr-str (take-last 5 results))))))
(deftest check-round-trip-verbose10000
  (let [results (stest/check `round-trip {::stc/opts {:num-tests 10000}})
        {:keys [total check-passed]} (stest/summarize-results results)]
    (is (= total check-passed)
      (when-not (= total check-passed)
        (pr-str (take-last 5 results))))))
