(ns Sellars.Transit.tests.transit-tests
  (:require [clojure.test :as test :refer [deftest is]]
            Sellars.Transit
            [sellars.transit.alpha :as t]
            [clojure.stacktrace :as stack]
            Sellars.Transit.tests.nunit-clojure-test-adapter)
  (:import [NUnit.Framework Assert Is]
           [System.IO MemoryStream]))

(def formats [:json :json-verbose :msgpack])

(deftest Something
  (is true "true"))

(defn test-round-trip
 ([value]
  (doseq [fmt formats]
    (test-round-trip fmt value)))
 ([fmt value]
  (let [stream (MemoryStream. 2000)
        out      (doto stream (.set_Position 0))
        w        (t/writer out fmt)
        _        (t/write w value)
        in       (doto stream (.set_Position 0))
        s        (.ReadToEnd (System.IO.StreamReader. in))
        in       (doto stream (.set_Position 0))
        r        (t/reader in fmt)
        actual   (t/read r)]
    (is (Debuggable/Equals value actual)
        (str "Trouble with " fmt " for "
             "(= " (pr-str value) " " (pr-str actual) ") "
             s)))))

(deftest test-basic-json
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json)
        _   (t/write w [1 2 3])
        in  (doto out (.set_Position 0))
        r   (t/reader in :json)
        x   (t/read r)]
    (is (= [1 2 3] x))))

(deftest test-basic-keyword-json
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json)
        _   (t/write w :a-keyword)
        in  (doto out (.set_Position 0))
        r   (t/reader in :json)
        x   (t/read r)]
    (is (= :a-keyword x))))

(deftest test-basic-keyword-json-verbose
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json-verbose)
        _   (t/write w :a-keyword)
        in  (doto out (.set_Position 0))
        r   (t/reader in :json-verbose)
        x   (t/read r)]
    (is (= :a-keyword x))))

(deftest test-basic-map-json-verbose
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json-verbose)
        _   (t/write w {:a "a", :b "bb", :one "one"})
        in  (doto out (.set_Position 0))
        r   (t/reader in :json-verbose)
        x   (t/read r)]
    (is (= {:a "a", :b "bb", :one "one"} x))))

(deftest test-basic-map-json
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json)
        _   (t/write w {:a "a", :b "bb", :one "one"})
        in  (doto out (.set_Position 0))
        r   (t/reader in :json)
        x   (t/read r)]
    (is (= {:a "a", :b "bb", :one "one"} x))))

(deftest test-basic-vector-json-verbose
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json-verbose)
        _   (t/write w [1 2 3])
        in  (doto out (.set_Position 0))
        r   (t/reader in :json-verbose)
        x   (t/read r)]
    (is (= [1 2 3] x))))

(deftest test-basic-vector-json
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json)
        _   (t/write w [1 2 3])
        in  (doto out (.set_Position 0))
        r   (t/reader in :json)
        x   (t/read r)]
    (is (= [1 2 3] x))))


(deftest test-basic-msgpack
  (let [out (MemoryStream. 2000)
        w   (t/writer out :msgpack)
        _   (t/write w [1 2 3])
        in  (doto out (.set_Position 0))
        r   (t/reader in :msgpack)
        x   (t/read r)]
    (is (= [1 2 3] x))))

(deftest test-basic-keyword-msgpack
  (let [out (MemoryStream. 2000)
        w   (t/writer out :msgpack)
        _   (t/write w :a-keyword)
        in  (doto out (.set_Position 0))
        r   (t/reader in :msgpack)
        x   (t/read r)]
    (is (= :a-keyword x))))

(deftest test-basic-map-msgpack
  (let [out (MemoryStream. 2000)
        w   (t/writer out :msgpack)
        _   (t/write w {:a "a", :b "bb", :one "one"})
        in  (doto out (.set_Position 0))
        r   (t/reader in :msgpack)
        x   (t/read r)]
    (is (= {:a "a", :b "bb", :one "one"} x))))

(deftest test-basic-cmap-msgpack
  (let [out (MemoryStream. 2000)
        w   (t/writer out :msgpack)
        _   (t/write w {:a "a", {"bb" :bb} :b, :one "one"})
        in  (doto out (.set_Position 0))
        r   (t/reader in :msgpack)
        x   (t/read r)]
    (is (= {:a "a", {"bb" :bb} :b, :one "one"} x))))

(deftest test-basic-vector-msgpack
  (let [out (MemoryStream. 2000)
        w   (t/writer out :msgpack)
        _   (t/write w [1 2 3])
        in  (doto out (.set_Position 0))
        r   (t/reader in :msgpack)
        x   (t/read r)]
    (is (= [1 2 3] x))))

(deftest test-basic-big-int-msgpack
  (let [out (MemoryStream. 2000)
        w   (t/writer out :msgpack)
        _   (t/write w (biginteger 238749872348972342309874809234))
        in  (doto out (.set_Position 0))
        r   (t/reader in :msgpack)
        x   (t/read r)]
    (is (= (biginteger 238749872348972342309874809234) x))))

(deftest test-basic-int-msgpack
  (let [out (MemoryStream. 2000)
        w   (t/writer out :msgpack)
        _   (t/write w 987978)
        in  (doto out (.set_Position 0))
        r   (t/reader in :msgpack)
        x   (t/read r)]
    (is (= 987978 x))))


(deftest test-set
  (test-round-trip #{})
  (test-round-trip #{:a :b :c}))

(deftest test-vec
  (test-round-trip [])
  (test-round-trip [\q -8 nil -5 #uuid "431a4354-32f5-b1ca-27d0-18937de600bc" -12 #uuid "b3b14607-2553-6bd0-1acc-4051b091f293" '+.*]))

(deftest test-list
  (test-round-trip (list))
  (test-round-trip (list \q -8 nil -5 #uuid "431a4354-32f5-b1ca-27d0-18937de600bc" -12 #uuid "b3b14607-2553-6bd0-1acc-4051b091f293" '+.*)))

(deftest test-1.0
  (test-round-trip 1.0))

(deftest test-double
  (test-round-trip -1.58456325028529E+29)
  (test-round-trip -2147483648.0)
  (test-round-trip -32768.0))

(deftest test-list-builder
  (let [value [\q -8 nil -5 #uuid "431a4354-32f5-b1ca-27d0-18937de600bc" -12 #uuid "b3b14607-2553-6bd0-1acc-4051b091f293" '+.*]
        ^Sellars.Transit.Alpha.IListReader lr (t/list-builder)
        actual (.Complete lr (reduce (fn [t x](.Add lr t x)) (.Init lr) value))]
    (is (= value actual)
        (str "(= " (pr-str value) " " (pr-str actual) ") "))))
