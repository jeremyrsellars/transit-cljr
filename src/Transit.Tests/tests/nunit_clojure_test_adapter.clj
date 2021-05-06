(ns Sellars.Transit.tests.nunit-clojure-test-adapter
  (:require [clojure.test :as test 
              :refer [deftest is report with-test-out
                      *testing-contexts* *stack-trace-depth* *report-counters* *initial-report-counters*
                      testing-vars-str testing-contexts-str inc-report-counter]]
            [sellars.transit.alpha :as t]
            [clojure.stacktrace :as stack])
  (:import [NUnit.Framework Assert Is]))

(defmethod report :pass [m]
  (Console/WriteLine (:message m))
  (with-test-out (inc-report-counter :pass)))

(defmethod report :fail [m]
  (try
    (Console/WriteLine (:message m))
    (Assert/Fail (:message m))
    (finally
      (with-test-out
        (inc-report-counter :fail)
        (println "\nFAIL in" (testing-vars-str m))
        (when (seq *testing-contexts*) (println (testing-contexts-str)))
        (when-let [message (:message m)] (println message))
        (println "expected:" (pr-str (:expected m)))
        (println "  actual:" (pr-str (:actual m)))))))

(defmethod report :error [m]
  (try
    (Console/WriteLine (:message m))
    (Assert/Fail (:message m))
    (finally  
      (with-test-out
       (inc-report-counter :error)
       (println "\nERROR in" (testing-vars-str m))
       (when (seq *testing-contexts*) (println (testing-contexts-str)))
       (when-let [message (:message m)] (println message))
       (println "expected:" (pr-str (:expected m)))
       (print "  actual: ")
       (let [actual (:actual m)]
         (if (instance? Exception actual)                                    ;;; Throwable
           (stack/print-cause-trace actual *stack-trace-depth*)
           (prn actual)))))))

(deftest SomeTests
  (is (= 1 1) "number equality")
  ;(is false "is false never passes.  :-(")
  (is true "is true always passes"))

(defn test-var
  [v]
  (binding [*report-counters* (ref *initial-report-counters*)]
    (clojure.test/test-var v)
    @*report-counters*))
