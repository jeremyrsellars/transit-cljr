;; Copyright 2014 Rich Hickey. All Rights Reserved.
;;
;; Licensed under the Apache License, Version 2.0 (the "License");
;; you may not use this file except in compliance with the License.
;; You may obtain a copy of the License at
;;
;;      http://www.apache.org/licenses/LICENSE-2.0
;;
;; Unless required by applicable law or agreed to in writing, software
;; distributed under the License is distributed on an "AS-IS" BASIS,
;; WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
;; See the License for the specific language governing permissions and
;; limitations under the License.

(ns sellars.transit.alpha
  "An implementation of the transit-format for Clojure built
   on top of the transit-cljr library."
  (:refer-clojure :exclude [read])
  (:require [clojure.string :as str])
  (:import [Sellars.Transit.Alpha IWriteHandler IReadHandler IListReadHandler IDictionaryReadHandler IDefaultReadHandler
            IReader IWriter
            IListReader TransitFactory+Format IDictionaryReader]
           [Sellars.Transit.Cljr.Alpha TransitFactory]
           [Sellars.Transit.Spi.Alpha IReaderSpi]
           [System.IO Stream]))

(defprotocol HandlerMapProvider
  (handler-map [this]))

(deftype HandlerMapContainer [m]
  HandlerMapProvider
  (handler-map [this] m))

;; writing

(set! *warn-on-reflection* true)

(defn- transit-format
  "Converts a keyword to a TransitFactory+Format value."
  [kw]
  (Enum/Parse
    TransitFactory+Format
    (str/replace (name kw) #"^.|-."
      #(-> %
           (str/upper-case)
           (str/replace #"-" "")))
    true))

(defn tagged-value
  "Creates a TaggedValue object."
  [tag rep] (TransitFactory/TaggedValue tag rep))

(defn nsed-name
  "Convert a keyword or symbol to a string in
   namespace/name format."
  [^clojure.lang.Named kw-or-sym]
  (if-let [ns (.getNamespace kw-or-sym)]
    (str ns "/" (.getName kw-or-sym))
    (.getName kw-or-sym)))

(defn- fn-or-val
  [f]
  (if (fn? f) f (constantly f)))

(defn write-handler
  "Creates a transit IWriteHandler whose tag, rep,
   stringRep, and verboseWriteHandler methods
   invoke the provided fns.

   If a non-fn is passed as an argument, implemented
   handler method returns the value unaltered."
  ([tag-fn rep-fn]
   (write-handler tag-fn rep-fn nil nil))
  ([tag-fn rep-fn str-rep-fn]
   (write-handler tag-fn rep-fn str-rep-fn nil))
  ([tag-fn rep-fn str-rep-fn verbose-handler-fn]
   (let [tag-fn (fn-or-val tag-fn)
         rep-fn (fn-or-val rep-fn)
         str-rep-fn (fn-or-val str-rep-fn)
         verbose-handler-fn (fn-or-val verbose-handler-fn)]
    (reify IWriteHandler
        (Tag [_ o] (tag-fn o))
        (Representation [_ o] (rep-fn o))
        (StringRepresentation [_ o] (when str-rep-fn (str-rep-fn o)))
        (GetVerboseHandler [_] (when verbose-handler-fn (verbose-handler-fn)))))))

(deftype WithMeta [value meta])

(def default-write-handlers
  "Returns a map of default WriteHandlers for
   Clojure types. Java types are handled
   by the default WriteHandlers provided by the
   transit-java library."
  {sellars.transit.alpha.WithMeta
   (reify IWriteHandler
     (Tag [_ _] "with-meta")
     (Representation [_ o]
       (TransitFactory/taggedValue "array"
         [(.-value ^sellars.transit.alpha.WithMeta o)
          (.-meta ^sellars.transit.alpha.WithMeta o)]))
     (StringRepresentation [_ _] nil)
     (GetVerboseHandler [_] nil))})

(deftype Writer [w])

(defn writer
  "Creates a writer over the provided destination `out` using
   the specified format, one of: :msgpack, :json or :json-verbose.

   An optional opts map may be passed. Supported options are:

   :handlers - a map of types to IWriteHandler instances, they are merged
   with the default-handlers and then with the default handlers
   provided by transit-java.

   :default-handler - a default IWriteHandler to use if NO handler is
   found for a type. If no default is specified, an error will be
   thrown for an unknown type.

   :transform - a function of one argument that will transform values before
   they are written."
  ([out type] (writer out type {}))
  ([^Stream out type {:keys [handlers default-handler transform]}]
   (if (#{:json :json-verbose :msgpack} type)
       (let [handler-map (if (instance? HandlerMapContainer handlers)
                           (handler-map handlers)
                           (merge default-write-handlers handlers))]
         (Writer. (TransitFactory/Writer (transit-format type) out handler-map default-handler
                    (when transform
                      (condp instance? transform
                        |System.Func`2[System.Object,System.Object]|
                        transform

                        clojure.lang.IFn
                        (sys-func [Object Object] [x] (transform x))
                        
                        (throw (ex-info (str "Invalid transform. Must be Func<object,object> or fn. " (class transform))
                                        {:transform transform})))))))
       (throw (ex-info "Type must be :json, :json-verbose, or :msgpack" {:type type})))))

(defn write
  "Writes a value to a transit writer."
  [^IWriter writer o]
  (.Write (.w writer) o)) ; is there a way to type-hint a generic interface?


;; reading

(defn read-handler
  "Creates a transit ReadHandler whose FromRepresentation
   method invokes the provided fn."
  [from-rep]
  (reify IReadHandler
    (FromRepresentation [_ o] (from-rep o))))

(defn- custom-default-read-handler
  "Returns an IDefaultReadHandler.
  fn-or-IDefaultReadHandler may be a fn like (fn default-read-handler [tag rep]rep)
  If fn-or-IDefaultReadHandler is already an IDefaultReadHandler, returns fn-or-IDefaultReadHandler.
  "
  [fn-or-IDefaultReadHandler]
  (if (instance? IDefaultReadHandler fn-or-IDefaultReadHandler)
    fn-or-IDefaultReadHandler
    (reify IDefaultReadHandler
      (FromRepresentation [_ tag representation]
        (fn-or-IDefaultReadHandler tag representation)))))

(defn read-map-handler
  "Creates a Transit MapReadHandler whose FromRepresentation
   and DictionaryReader methods invoke the provided fns."
  [from-rep map-reader]
  (reify IDictionaryReadHandler
    (FromRepresentation [_ o] (from-rep o))
    (DictionaryReader [_] (map-reader))))

(defn read-array-handler
  "Creates a Transit IListReadHandler whose FromRepresentation
   and ListReader methods invoke the provided fns."
  [from-rep array-reader]
  (reify IListReadHandler
    (FromRepresentation [_ o] (from-rep o))
    (ListReader [_] (array-reader))))


(def default-read-handlers
  "Returns a map of default ReadHandlers for
   Clojure types. Java types are handled
   by the default ReadHandlers provided by the
   transit-java library."
  {"with-meta"
   (reify IReadHandler
     (FromRepresentation [_ o]
       (with-meta (nth o 0) (nth o 1))))})

(defn map-builder
  "Creates a MapBuilder that makes Clojure-
   compatible maps."
  []
  (reify IDictionaryReader
    (Init [_] (transient {}))
    #_(Init [_ ^int size] (transient {}))
    (Add [_ m k v] (assoc! m k v))
    (Complete [_ m] (persistent! m))))

(defn list-builder
  "Creates an ArrayBuilder that makes Clojure-
   compatible lists."
  []
  (reify IListReader
    (Init [_] (transient []))
    #_(Init [_ ^int size] (transient []))
    (Add [_ v item] (conj! v item))
    (Complete [_ v] (persistent! v))))

(deftype Reader [r])

(defn reader
  "Creates a reader over the provided source `in` using
   the specified format, one of: :msgpack, :json or :json-verbose.

   An optional opts map may be passed. Supported options are:

   :handlers - a map of tags to ReadHandler instances, they are merged
   with the Clojure default-read-handlers and then with the default ReadHandlers
   provided by transit-java.

   :default-handler - an instance of DefaultReadHandler, used to process
   transit encoded values for which there is no other ReadHandler; if
   :default-handler is not specified, non-readable values are returned
   as TaggedValues."
  ([in type] (reader in type {}))
  ([^Stream in type {:keys [handlers default-handler]}]
   (if (#{:json :json-verbose :msgpack} type)
       (let [handler-map (if (instance? HandlerMapContainer handlers)
                           (handler-map handlers)
                           (merge default-read-handlers handlers))
             reader (TransitFactory/Reader (transit-format type)
                                           in
                                           handler-map
                                           (when default-handler
                                             (custom-default-read-handler default-handler)))]
         (Reader. (.SetBuilders ^IReaderSpi reader
                                (map-builder)
                                (list-builder))))
       (throw (ex-info "Type must be :json, :json-verbose or :msgpack" {:type type})))))

(defn read
  "Reads a value from a reader. Throws a RuntimeException when
   the reader's Stream is empty."
  [^Reader reader]
  (.Read ^IReader (.r reader)))

(defn record-write-handler
  "Creates a IWriteHandler for a record type"
  [^Type type]
  (reify IWriteHandler
    (Tag [_ _] (.GetFullName type))
    (Representation [_ rec] (tagged-value "map" rec))
    (StringRepresentation [_ _] nil)
    (GetVerboseHandler [_] nil)))

(defn record-write-handlers
  "Creates a map of record types to IWriteHandlers"
  [& types]
  (reduce (fn [h t] (assoc h t (record-write-handler t)))
          {}
          types))

(defn record-read-handler
  "Creates a ReadHandler for a record type"
  [^Type type]
  (let [type-name (map #(str/replace % "_" "-") (str/split (.GetFullName type) #"\."))
        map-ctor (-> (str (str/join "." (butlast type-name)) "/map->" (last type-name))
                     symbol
                     resolve)]
    (reify IReadHandler
      (FromRepresentation [_ m] (map-ctor m)))))

(defn record-read-handlers
  "Creates a map of record type tags to ReadHandlers"
  [& types]
  (reduce (fn [d ^Type t] (assoc d (.GetFullName t) (record-read-handler t)))
          {}
          types))

(defn read-handler-map
  "Returns a HandlerMapContainer containing a ReadHandlerMap
  containing all the default handlers for Clojure and Java and any
  custom handlers that you supply, letting you store the return value
  and pass it to multiple invocations of reader.  This can be more
  efficient than repeatedly handing the same raw map of tags -> custom
  handlers to reader."
  [custom-handlers]
  (HandlerMapContainer.
    (TransitFactory/ReadHandlerMap (merge default-read-handlers custom-handlers))))

(defn write-handler-map
  "Returns a HandlerMapContainer containing a WriteHandlerMap
  containing all the default handlers for Clojure and Java and any
  custom handlers that you supply, letting you store the return value
  and pass it to multiple invocations of writer.  This can be more
  efficient than repeatedly handing the same raw map of types -> custom
  handlers to writer."
  [custom-handlers]
  (HandlerMapContainer.
    (TransitFactory/WriteHandlerMap (merge default-write-handlers custom-handlers))))

(defn write-meta
  "For :transform. Will write any metadata present on the value."
  [x]
  (if (instance? clojure.lang.IObj x)
    (if-let [m (meta x)]
      (WithMeta. (with-meta x nil) m)
      x)
    x))

(comment
  (require 'sellars.transit)
  (in-ns 'sellars.transit)

  (import [java.io File ByteArrayStream ByteArrayStream StreamWriter])

  (def out (ByteArrayStream. 2000))

  (def w (writer out :json))
  (def w (writer out :json-verbose))
  (def w (writer out :msgpack))
  (def w (writer out :msgpack {:transform write-meta}))
  (def w (writer out :json {:transform write-meta}))

  (write w "foo")
  (write w 10)
  (write w [1 2 3])
  (write w (with-meta [1 2 3] {:foo 'bar}))
  (String. (.toByteArray out))

  (write w {:a-key 1 :b-key 2})
  (write w {"a" "1" "b" "2"})
  (write w {:a-key [1 2]})
  (write w #{1 2})
  (write w [{:a-key 1} {:a-key 2}])
  (write w [#{1 2} #{1 2}])
  (write w (int-array (range 10)))
  (write w {[:a :b] 2})
  (write w [123N])
  (write w 1/3)
  (write w {false 10 [] 20})

  (def in (ByteArrayStream. (.toByteArray out)))

  (def r (reader in :json))

  (def r (reader in :msgpack))

  (def x (read r))
  (meta x)

  (type (read r))

  ;; extensibility

  (defrecord Point [x y])

  (defrecord Circle [c r])

  (def ext-write-handlers
    {Point
     (write-handler "point" (fn [p] [(.x p) (.y p)]))
     Circle
     (write-handler "circle" (fn [c] [(.c c) (.r c)]))})

  (def ext-read-handlers
    {"point"
     (read-handler (fn [[x y]] (prn "making a point") (Point. x y)))
     "circle"
     (read-handler (fn [[c r]] (prn "making a circle") (Circle. c r)))})

  (def ext-write-handlers
    (record-write-handlers Point Circle))

  (def ext-read-handlers
    (record-read-handlers Point Circle))

  (def out (ByteArrayStream. 2000))
  (def w (writer out :json {:handlers ext-write-handlers}))
  (write w (Point. 10 20))
  (write w (Circle. (Point. 10 20) 30))
  (write w [(Point. 10 20) (Point. 20 40) (Point. 0 0)])

  (def in (ByteArrayStream. (.toByteArray out)))
  (def r (reader in :json {:handlers ext-read-handlers}))
  (read r)

  ;; write and read handler maps

  (def custom-write-handler-map (write-handler-map ext-write-handlers))
  (def custom-read-handler-map (read-handler-map ext-read-handlers))

  (def out (ByteArrayStream. 2000))
  (def w (writer out :json {:handlers custom-write-handler-map}))

  (write w (Point. 10 20))

  (def in (ByteArrayStream. (.toByteArray out)))
  (def r (reader in :json {:handlers custom-read-handler-map}))
  (read r))
  