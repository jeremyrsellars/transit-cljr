using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Sellars.Transit.Util
{
    internal static class DictionaryHelper
    {
        private static readonly clojure.lang.AtomicReference<ImmutableDictionary<Type, Func<object, KeyValuePair<object, object>>>> coercions =
            new clojure.lang.AtomicReference<ImmutableDictionary<Type, Func<object, KeyValuePair<object, object>>>>(
            ImmutableDictionary<Type, Func<object, KeyValuePair<object, object>>>.Empty);

        public static Dictionary<TKey, TVal> CoerceDictionary<TKey, TVal>(
            object keyValuePairEnumerable, Func<object, KeyValuePair<object, object>> coerceOrThrow = null)
        {
            if (keyValuePairEnumerable is null)
                return null;
            var dict = new Dictionary<TKey, TVal>();
            foreach (var kvp in CoerceKeyValuePairs(keyValuePairEnumerable, coerceOrThrow))
                dict.Add((TKey)kvp.Key, (TVal)kvp.Value);
            return dict;
        }

        public static IImmutableDictionary<TKey, TVal> CoerceIImmutableDictionary<TKey, TVal>(
            object keyValuePairEnumerable, Func<object, KeyValuePair<object, object>> coerceOrThrow = null) =>
            keyValuePairEnumerable is null
            ? null
            : ImmutableDictionary<TKey, TVal>.Empty.AddRange(
                CoerceKeyValuePairs(keyValuePairEnumerable, coerceOrThrow)
                .Select(kvp => new KeyValuePair<TKey, TVal>((TKey)kvp.Key, (TVal)kvp.Value)));

        public static IEnumerable<KeyValuePair<object, object>> CoerceKeyValuePairs(
            object keyValuePairEnumerable, Func<object, KeyValuePair<object, object>> coerceOrThrow = null) =>
            CoerceKeyValuePairs((IEnumerable)keyValuePairEnumerable, coerceOrThrow);

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
                else if (TryCoerceGenericKeyValuePair(item, out kvp))
                    yield return kvp;
                else if (coerceOrThrow != null)
                    yield return coerceOrThrow(item);
                else
                    throw new NotSupportedException($"Unknown coercion from {item?.GetType()?.FullName ?? "null"} to KeyValuePair.");
            }
        }

        private static bool TryCoerceGenericKeyValuePair(object item, out KeyValuePair<object, object> kvp)
        {
            if (item?.GetType() is Type t && t.Name == typeof(KeyValuePair<,>).Name
                && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var coercions = DictionaryHelper.coercions.Get();
                if (!coercions.TryGetValue(t, out var coercion))
                {
                    coercion = MakeCoercionFunc(t.GetGenericArguments());
                    DictionaryHelper.coercions.CompareAndSet(coercions, coercions.SetItem(t, coercion));
                }
                kvp = coercion(item);
                return true;

                //kvp = new KeyValuePair<object, object>(
                //    t.GetProperty("Key").GetValue(item),
                //    t.GetProperty("Value").GetValue(item));
                //return true;
            }
            kvp = default;
            return false;
        }

        private static Func<object, KeyValuePair<object, object>> MakeCoercionFunc(Type[] types) => 
            (Func<object, KeyValuePair<object, object>>)
            typeof(DictionaryHelper).GetMethod(nameof(CoerceGenericKeyValuePair)).MakeGenericMethod(types).CreateDelegate(
                typeof(Func<object, KeyValuePair<object, object>>));

        public static KeyValuePair<object, object> CoerceGenericKeyValuePair<TKey, TVal>(KeyValuePair<TKey, TVal> kvp) =>
            new KeyValuePair<object, object>(kvp.Key, kvp.Value);

        public static KeyValuePair<object, object> ThrowCannotCoerceKeyValuePair(object item) =>
            throw new NotSupportedException($"Unknown coercion from {item?.GetType()?.FullName ?? "null"} to KeyValuePair.");
    }
}
