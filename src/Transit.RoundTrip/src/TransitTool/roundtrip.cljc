#?(:cljr 
    (try
      (assembly-load "clojure.test.check")
      (assembly-load "clojure.tools.reader")
      (catch Exception e
          (throw (Exception. (str "Pre-load " (.ToString e)) e)))))

(ns TransitTool.roundtrip
  (:require #?@(:cljr [[clojure.clr.io :as io]
                       Sellars.Transit
                       [sellars.transit.alpha :as transit]]
                :clj [[clojure.java.io :as io]
                      [cognitect.transit :as transit]])
            [clojure.test.check :as tc]
            [clojure.test.check.properties :as prop]
            [clojure.test.check.generators :as gen]
            [clojure.tools.reader.edn :as edn]
;            [clojure.tools.reader.reader-types :as trt]
            [clojure.pprint :as pprint]
            [clojure.string :as str])
  (:import #?@(:cljr [[System.IO MemoryStream]] 
               :clj  [[java.io ByteArrayOutputStream ByteArrayInputStream
                               FileOutputStream FileInputStream]]))
  #?(:clj (:gen-class)))

(def all-encodings #{:json :json-verbose :msgpack})

(def enc-extension
  {:json         ".json"
   :json-verbose ".json"
   :msgpack      ".msgpack"
   :edn          ".edn"})

(def extension-enc
  (reduce-kv
    (fn inv [m enc ext](assoc m ext enc))
    {}
    enc-extension))

#?(:clj (defn path-combine
          ([p & ps]
           (apply io/file (io/as-file p) (map io/as-file ps))))
   :cljr (defn path-combine
            ([p & ps]
             (io/as-file (reduce #(System.IO.Path/Combine %1 (str %2)) (str p) ps)))))

(defn now-ms
  []
  (inst-ms #?(:clj (java.util.Date.)
              :cljr (DateTime/UtcNow))))

(defmulti describe-command
  (fn describe-command-dispatch [command]
    (keyword command))
  :default :usage)

(defmulti run-command
  (fn run-command-dispatch [[command]]
    (keyword command))
  :default :usage)

(defmethod describe-command :usage describe-command_usage
  [command]
  "Usage information")
(defmethod run-command :usage run-command_usage
  [[command dir & encodings]]
  (println "Usage: command args")
  (println)
  (println "Commands:")
  (doseq [[command f] (methods run-command)]
    (println "  " (name command) " - " (describe-command command)))
  (println "<command> dir [json json-verbose msgpack]"))

(defn parse-example-file
  [f]
  (when-let [[_ dir ex-id encoding extension] (re-find #"(.*[/\\])(\d+)\.(\w+)(\.\w+)" (str f))]
    (when (contains? enc-extension (keyword encoding)) 
      [dir (#?(:cljr Convert/ToInt64 :clj Long.) ex-id) (keyword encoding)])))

(defn example-file
  [dir ex-id encoding]
  (path-combine dir (str ex-id "." (name encoding) (enc-extension encoding))))

#_
(let [ex-file-args ["../" 1 :msgpack]
      _ (prn :ex-file1 (apply example-file ex-file-args))
      ex-file (apply example-file ex-file-args)
      parsed-ex-file (parse-example-file ex-file)]
  (prn :ex-file ex-file)
  (prn 'parse-example-file (parse-example-file ex-file))
  (prn 'apply 'example-file (apply example-file parsed-ex-file))
  (prn '(= ex-file-args parsed-ex-file) (= ex-file-args parsed-ex-file))
  (as-> (io/as-file "./1.msgpack.msgpack") f
    (= (str f) (str (apply example-file (parse-example-file f))))))

#?(:clj
   (defn output-stream [f](FileOutputStream. f))
   :cljr
   (defn output-stream [f](io/output-stream f)))

#?(:clj
   (defn input-stream [f](FileInputStream. f))
   :cljr
   (defn input-stream [f](io/input-stream f)))


(defn write-examples
  [f encoding & values]
  (with-open [out (output-stream f)]
    (let [writer (transit/writer out encoding)]
      (doseq [value values]
        (transit/write writer value))))
  f)

#_ (write-examples (example-file "./examples" 1 :json) :json "example uno"  "example dos")

(defn write-example
  [dir ex-id encoding value]
  (write-examples (example-file dir ex-id encoding) encoding value))

#_ (write-example "./examples" 1 :json "example uno")

(defn read-example
 ([dir ex-id encoding](read-example (example-file dir ex-id encoding) encoding))
 ([f encoding]
  (with-open [in (input-stream f)]
    (let [reader (transit/reader in encoding)]
      (transit/read reader)))))

#_ (read-example "./examples" 1 :json)

(defn read-examples
  [f encoding]
  (with-open [in (input-stream f)]
    (let [reader (transit/reader in encoding)]
      (loop [examples []]
        (if-let [ex (try
                      (transit/read reader)
                      (catch Exception e
                        #?(:clj (when-not (instance? java.io.EOFException (.getCause e))
                                  (throw e))
                           :default (println e))))]
          (recur (conj examples ex))
          examples)))))
#_ (read-examples (example-file "./examples" 1 :json) :json)

(defn roundtrip-example
  [dir ex-id encoding value]
  (-> (write-example  dir ex-id encoding value)
      (read-example encoding)))

#_ (roundtrip-example "./examples" 1 :json "example uno")

(defmethod describe-command :gen-dir describe-command_gen-dir
  [command]
  "Generate directory of sample data")
(defmethod run-command :gen-dir run-command_gen-dir
  [[command dir & encodings]]
  (when (io/as-file dir)
    (let [encodings (or (seq (map keyword encodings)) (seq all-encodings))
          serial-num (atom (now-ms))]
      (tc/quick-check 10
        (prop/for-all [value gen/any-printable]
          (let [ex-id (swap! serial-num inc)]
            (spit (example-file dir ex-id :edn) (pr-str value))
            (apply = value (map #(roundtrip-example dir ex-id % value) encodings))))
        :max-size 50))))

#_ (run-command ["gen-dir" "examples"])

(defmethod describe-command :test-dir describe-command_test-dir
  [command]
  "Test directory of sample data created with gen-dir")
(defmethod run-command :test-dir run-command_test-dir
  [[command dir & encodings]]
  (let [encodings (or (seq (map keyword encodings)) (seq all-encodings))
        dir  #?(:cljr (System.IO.DirectoryInfo. dir)
                :clj  (io/as-file dir)
                :default dir)
        examples (->> (file-seq dir)
                   (keep parse-example-file)
                   (filter (fn [[dir ex-id encoding]](= :edn encoding))))
        stats (atom {:= 0, :not= 0 :problems {}})]
    (doseq [[dir ex-id enc :as ex-edn] examples
            encoding encodings]
      (let [ex-args [dir ex-id encoding]
            ex-file (apply example-file ex-args)
            ; _ (println ex-id enc ex-file)
            edn (slurp (apply example-file ex-edn)
                 #?@(:cljr [:encoding System.Text.Encoding/UTF8]))
            expected (edn/read-string edn)
            actual (apply read-example ex-args)
            equal? (= expected actual)]
        (swap! stats update (if equal? := :not=) inc)
        (when-not equal?
          (swap! stats update-in [:problems encoding] conj (str ex-file)))))
    (if (pos? (:not= @stats))
      (binding [*out* *err*]
        (println "Mismatches occurred")))
    @stats))

#_ (run-command ["test-dir" "examples"])

(defmethod describe-command :test-cache describe-command_test-cache
  [command]
  "Test multiple read/write a hard-coded example designed to test the write/read cache.")
(defmethod run-command :test-cache run-command_test-cache
  [[command dir & encodings]]
  (let [encodings (or (seq (map keyword encodings)) (seq all-encodings))
        examples [{:alpha "a", :beta "b", :echo {:alpha "a", :beta "b"}}
                  {:king "fisher" :artichoke {:beta "b"}, :echo {:king "fisher" :artichoke {:beta "b"}}}
                  {:kale      {:alpha "a"}, :echo {:kale      {:alpha "a"}}}]
        stats (atom {:= 0, :not= 0 :problems {}})]
    (doseq [encoding encodings]
      (let [example-values examples
            temp-f (doto (apply write-examples (example-file dir "cache" encoding) encoding example-values)
                     #?(:clj (.deleteOnExit)))
            actual-values (read-examples temp-f encoding)]
        ;(println :example-values example-values)
        ;(println :actual-values actual-values)
        (doall
          (map
            (fn upd-stats [expected actual ex-args]
              (let [equal? (= expected actual)]
                (swap! stats update (if equal? := :not=) inc)
                (when-not equal?
                  (swap! stats update-in [:problems encoding] conj (str (apply example-file ex-args))))))
            example-values
            (concat actual-values (repeat nil))
            examples))
        #?(;:clj (io/delete-file temp-f)
           :cljr (.Delete temp-f))))
    (if (pos? (:not= @stats))
      (binding [*out* *err*]
        (println "Mismatches occurred")))
    @stats))

#_ (run-command ["test-cache" "examples"])

(defmethod describe-command :test-multi describe-command_test-multi
  [command]
  "Test multiple read/write from a file generated from a directory of sample data.")
(defmethod run-command :test-multi run-command_test-multi
  [[command dir & encodings]]
  (let [encodings (or (seq (map keyword encodings)) (seq all-encodings))
        dir  #?(:cljr (System.IO.DirectoryInfo. dir)
                :clj  (io/as-file dir)
                :default dir)
        examples (->> (file-seq dir)
                   (keep parse-example-file)
                   (filter (fn [[dir ex-id encoding]](= :edn encoding))))
        stats (atom {:= 0, :not= 0 :problems {}})]
    (doseq [encoding encodings]
      (let [example-values (for [[dir ex-id enc :as ex-edn] examples]
                             (edn/read-string (slurp (apply example-file ex-edn)
                                                #?@(:cljr [:encoding System.Text.Encoding/UTF8]))))
            temp-f (doto (apply write-examples (example-file dir "many" encoding) encoding example-values)
                     #?(:clj (.deleteOnExit)))
            actual-values (read-examples temp-f encoding)]
        ;(println :example-values example-values)
        ;(println :actual-values actual-values)
        (doall
          (map
            (fn upd-stats [expected actual ex-args]
              (let [equal? (= expected actual)]
                (swap! stats update (if equal? := :not=) inc)
                (when-not equal?
                  (swap! stats update-in [:problems encoding] conj (str (apply example-file ex-args))))))
            example-values
            (concat actual-values (repeat nil))
            examples))
        #?(:clj (io/delete-file temp-f)
           :cljr (.Delete temp-f))))
    (if (pos? (:not= @stats))
      (binding [*out* *err*]
        (println "Mismatches occurred")))
    @stats))

#_ (run-command ["test-multi" "examples"])

(defn -main [& args]
  (clojure.pprint/pprint (run-command args)))
