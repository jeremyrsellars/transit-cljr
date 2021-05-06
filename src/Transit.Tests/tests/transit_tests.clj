(ns Sellars.Transit.tests.transit-tests
  (:require [clojure.test :as test :refer [deftest is]]
            [sellars.transit.alpha :as t]
            [clojure.stacktrace :as stack]
            Sellars.Transit.tests.nunit-clojure-test-adapter)
  (:import [NUnit.Framework Assert Is]))

(deftest Something
  (is true "true"))

#_
(defn get-default-write-handlers
  []
  t/default-write-handlers)
#_
(def tf-type  Sellars.Transit.Cljr.Alpha.TransitFactory)
