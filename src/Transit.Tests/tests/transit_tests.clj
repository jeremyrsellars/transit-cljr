(ns Sellars.Transit.tests.transit-tests
  (:require [clojure.test :as test :refer [deftest is]]
            Sellars.Transit
            [sellars.transit.alpha :as t]
            [clojure.stacktrace :as stack]
            Sellars.Transit.tests.nunit-clojure-test-adapter)
  (:import [NUnit.Framework Assert Is]
           [System.IO MemoryStream]))

(deftest Something
  (is true "true"))

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
