﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Sellars.Transit.Util
{
    internal static class DictionaryHelper
    {
        public static IEnumerable<KeyValuePair<object, object>> CoerceKeyValuePairs(
            object keyValuePairEnumerable, Func<object, KeyValuePair<object, object>> coerceOrThrow = null) =>
            keyValuePairEnumerable is IEnumerable<KeyValuePair<object, object>> keyValuePairs
                ? keyValuePairs
                : CoerceKeyValuePairs((IEnumerable)keyValuePairEnumerable, coerceOrThrow);

        private static IEnumerable<KeyValuePair<object, object>> CoerceKeyValuePairs(
            IEnumerable keyValuePairEnumerable, Func<object, KeyValuePair<object, object>> coerceOrThrow = null)
        {
            var enumerator = keyValuePairEnumerable.GetEnumerator();
            try
            {
                foreach (var entry in enumerator is IDictionaryEnumerator de
                    ? DictionaryEnumeratorKeyValuePairs(de)
                    : DynamicEnumeratorKeyValuePairs(enumerator, coerceOrThrow))
                    yield return entry;
            }
            finally
            {
                if (enumerator is IDisposable disp)
                    disp.Dispose();
            }
        }

        private static IEnumerable<KeyValuePair<object, object>> DictionaryEnumeratorKeyValuePairs(IDictionaryEnumerator de)
        {
            while (de.MoveNext())
                yield return new KeyValuePair<object, object>(de.Key, de.Value);
        }

        private static IEnumerable<KeyValuePair<object, object>> DynamicEnumeratorKeyValuePairs(
            IEnumerator enumerator, Func<object, KeyValuePair<object, object>> coerceOrThrow = null)
        {
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if (item is KeyValuePair<object, object> kvp)
                    yield return kvp;
                else if (item is DictionaryEntry dentry)
                    yield return new KeyValuePair<object, object>(dentry.Key, dentry.Value);
                else if (item is clojure.lang.IMapEntry mentry)
                    yield return new KeyValuePair<object, object>(mentry.key(), mentry.val());
                else if (coerceOrThrow != null)
                    yield return coerceOrThrow(item);
                else
                    throw new NotSupportedException($"Unknown coercion from {item?.GetType()?.FullName ?? "null"} to KeyValuePair.");
            }
        }

        public static KeyValuePair<object, object> ThrowCannotCoerceKeyValuePair(object item) =>
            throw new NotSupportedException($"Unknown coercion from {item?.GetType()?.FullName ?? "null"} to KeyValuePair.");
    }
}