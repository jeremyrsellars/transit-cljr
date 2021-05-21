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
        (str "Testing " fmt " for "
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

(deftest test-basic-with-meta-json
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json {:transform t/write-meta})
        _   (t/write w (with-meta [1 2 3] {:foo 'bar}))
        in  (doto out (.set_Position 0))
        r   (t/reader in :json)
        x   (t/read r)]
    (is (= [1 2 3] x) (pr-str x))
    (is (= {:foo 'bar} (meta x)) (pr-str (meta x)))))

(deftest test-basic-with-meta-msgpack
  (let [out (MemoryStream. 2000)
        w   (t/writer out :msgpack {:transform t/write-meta})
        _   (t/write w (with-meta [1 2 3] {:foo 'bar}))
        in  (doto out (.set_Position 0))
        r   (t/reader in :msgpack)
        x   (t/read r)]
    (is (= [1 2 3] x) (pr-str x))
    (is (= {:foo 'bar} (meta x)) (pr-str (meta x)))))

(deftest test-symbol-with-meta
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json {:transform t/write-meta})
        _   (t/write w (with-meta 'foo {:bar "baz"}))
        in  (doto out (.set_Position 0))
        r   (t/reader in :json)
        x   (t/read r)]
    (is (= 'foo x) (pr-str x))
    (is (= {:bar "baz"} (meta x)) (pr-str (meta x)))))

(deftest test-nested-with-meta
  (let [out (MemoryStream. 2000)
        w   (t/writer out :json {:transform t/write-meta})
        _   (t/write w {:amap (with-meta [1 2 3] {:foo 'bar})})
        in  (doto out (.set_Position 0))
        r   (t/reader in :json)
        x   (t/read r)]
    (is (= [1 2 3] (:amap x)) (pr-str (:amap x)))
    (is (= {:foo 'bar} (-> x :amap meta)) (pr-str (-> x :amap meta)))))

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

(deftest test-transform
  ; add transform
  (doseq [fmt formats
          [input expected] [[1 2]
                            [{:kw 2} {:kw 4}]
                            [{3 :kw2} {6 :kw2}]]]
    (let [stream (MemoryStream.)
          writer (t/writer stream fmt
                    {:transform
                     (fn double [o] 
                       (if (int? o) 
                         (* 2 o)
                         o))})
          _ (t/write writer input)
          _ (.set_Position stream 0)
          reader (t/reader stream fmt)]
      (is (= expected (t/read reader))
          (str expected (pr-str expected))))))

(deftest test-default-handlers
  ; throw when writing an unknown type & no default handler
  (doseq [fmt formats]
    (is (thrown? Exception
          (let [stream (MemoryStream.)
                writer (t/writer stream fmt)]
            (t/write writer (Version. 1 2 3 4))))
      "Expected exception when writing an unknown type & no default handler"))

  ; add default write handler
  (doseq [fmt formats]
    (let [stream (MemoryStream.)
          writer (t/writer stream fmt
                    {:default-handler
                     (t/write-handler
                       (fn [_] "version")
                       (fn [v] (.ToString v)))})
          _ (t/write writer (Version. 1 2 3 4))
          _ (.set_Position stream 0)
          reader (t/reader stream fmt)]
      (is (= (t/tagged-value "version" "1.2.3.4") (t/read reader)))))

  ; add default write and read handlers
  (doseq [fmt formats]
    (let [stream (MemoryStream.)
          writer (t/writer stream fmt
                    {:default-handler
                     (t/write-handler
                       (fn [_] "version")
                       (fn [v] (.ToString v)))})
          _ (t/write writer (Version. 1 2 3 4))
          _ (.set_Position stream 0)
          reader (t/reader stream fmt
                  {:default-handler 
                   (fn default-read-handler
                     [tag object]
                     (is (= tag "version"))
                     (Version. object))})]
      (is (= (Version. 1 2 3 4) (t/read reader))))))

(deftest test-handler-maps
  (let [write-handlers (t/write-handler-map
                        {Version
                         (t/write-handler
                           (fn [_] "version")
                           (fn [v] (.ToString v)))})
        read-handlers  (t/read-handler-map
                        {"version"
                         (t/read-handler
                           (fn [rep] (Version. rep)))})]
    (doseq [fmt formats]
      (let [value (Version. 1 1 1 1)
            stream (MemoryStream. 2000)
            out      (doto stream (.set_Position 0))
            w        (t/writer out fmt {:handlers write-handlers})
            _        (t/write w value)
            in       (doto stream (.set_Position 0))
            s        (.ReadToEnd (System.IO.StreamReader. in))
            in       (doto stream (.set_Position 0))
            r        (t/reader in fmt {:handlers read-handlers})
            actual   (t/read r)]
        (is (Debuggable/Equals value actual)
            (str "Testing " fmt " for "
                 "(= " (pr-str value) " " (pr-str actual) ") "
                 s))))))
