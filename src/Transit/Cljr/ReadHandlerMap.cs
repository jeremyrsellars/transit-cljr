using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sellars.Transit.Alpha;
using System.Collections;

namespace Sellars.Transit.Cljr.Impl.Alpha
{
    /// <summary>
    /// An immutable collection of handlers that have been prevented from overriding grounded types,
    /// and thus is reusable without additional validation.
    /// </summary>
    public class ReadHandlerMap : IDictionary<string, IReadHandler>, IImmutableDictionary<string, IReadHandler>
    {
        private IImmutableDictionary<string, IReadHandler> handlers;

        private ReadHandlerMap() { }

        internal static ReadHandlerMap Create<T>(T handlers, Func<T, IImmutableDictionary<string, IReadHandler>> addDefaultsAndVettHandlers) =>
            new ReadHandlerMap()
            {
                handlers = addDefaultsAndVettHandlers(handlers),
            };

        public IReadHandler this[string key] => handlers[key];
        IReadHandler IDictionary<string, IReadHandler>.this[string key]
        {
            get => handlers[key];
            set => throw MayNotMutate();
        }

        public IEnumerable<string> Keys => handlers.Keys;
        public IEnumerable<IReadHandler> Values => handlers.Values;
        public int Count => handlers.Count;
        public bool IsReadOnly => true;
        ICollection<string> IDictionary<string, IReadHandler>.Keys =>
            handlers.Keys.ToImmutableArray();
        ICollection<IReadHandler> IDictionary<string, IReadHandler>.Values =>
            handlers.Values.ToImmutableArray();

        public IImmutableDictionary<string, IReadHandler> Add(string key, IReadHandler value) => 
            throw MayNotMutate();

        public void Add(KeyValuePair<string, IReadHandler> item) => 
            throw MayNotMutate();

        public IImmutableDictionary<string, IReadHandler> AddRange(IEnumerable<KeyValuePair<string, IReadHandler>> pairs) => 
            throw MayNotMutate();

        public IImmutableDictionary<string, IReadHandler> Clear() => 
            throw MayNotMutate();

        public bool Contains(KeyValuePair<string, IReadHandler> pair) => handlers.Contains(pair);

        public bool ContainsKey(string key) => handlers.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, IReadHandler>[] array, int arrayIndex)
        {
            foreach (var kvp in handlers)
                array[arrayIndex++] = kvp;
        }

        public IEnumerator<KeyValuePair<string, IReadHandler>> GetEnumerator() => handlers.GetEnumerator();

        public IImmutableDictionary<string, IReadHandler> Remove(string key) => 
            throw MayNotMutate();

        public bool Remove(KeyValuePair<string, IReadHandler> item) => 
            throw MayNotMutate();

        public IImmutableDictionary<string, IReadHandler> RemoveRange(IEnumerable<string> keys) => 
            throw MayNotMutate();

        public IImmutableDictionary<string, IReadHandler> SetItem(string key, IReadHandler value) => 
            throw MayNotMutate();

        public IImmutableDictionary<string, IReadHandler> SetItems(IEnumerable<KeyValuePair<string, IReadHandler>> items) => 
            throw MayNotMutate();

        public bool TryGetKey(string equalKey, out string actualKey) => handlers.TryGetKey(equalKey, out actualKey);

        public bool TryGetValue(string key, out IReadHandler value) => handlers.TryGetValue(key, out value);

        void IDictionary<string, IReadHandler>.Add(string key, IReadHandler value) => 
            throw MayNotMutate();

        void ICollection<KeyValuePair<string, IReadHandler>>.Clear() => 
            throw MayNotMutate();

        IEnumerator IEnumerable.GetEnumerator() => handlers.GetEnumerator();

        bool IDictionary<string, IReadHandler>.Remove(string key) => 
            throw MayNotMutate();

        static Exception MayNotMutate() => 
            new NotSupportedException("May not mutate handler map.");
    }

}
