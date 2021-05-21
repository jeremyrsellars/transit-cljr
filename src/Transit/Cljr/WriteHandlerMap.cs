using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sellars.Transit.Alpha;
using System.Collections;

namespace Sellars.Transit.Cljr.Impl.Alpha
{
    public class WriteHandlerMap : IDictionary<Type, IWriteHandler>, IImmutableDictionary<Type, IWriteHandler>
    {
        private IImmutableDictionary<Type, IWriteHandler> handlers;

        private WriteHandlerMap() { }

        internal static WriteHandlerMap Create<T>(T handlers, Func<T, IImmutableDictionary<Type, IWriteHandler>> addDefaultsAndVettHandlers) =>
            new WriteHandlerMap()
            {
                handlers = addDefaultsAndVettHandlers(handlers),
            };

        public IWriteHandler this[Type key] => handlers[key];
        IWriteHandler IDictionary<Type, IWriteHandler>.this[Type key]
        {
            get => handlers[key];
            set => throw MayNotMutate();
        }

        public IEnumerable<Type> Keys => handlers.Keys;
        public IEnumerable<IWriteHandler> Values => handlers.Values;
        public int Count => handlers.Count;
        public bool IsReadOnly => true;
        ICollection<Type> IDictionary<Type, IWriteHandler>.Keys =>
            handlers.Keys.ToImmutableArray();
        ICollection<IWriteHandler> IDictionary<Type, IWriteHandler>.Values =>
            handlers.Values.ToImmutableArray();

        public IImmutableDictionary<Type, IWriteHandler> Add(Type key, IWriteHandler value) => 
            throw MayNotMutate();

        public void Add(KeyValuePair<Type, IWriteHandler> item) => 
            throw MayNotMutate();

        public IImmutableDictionary<Type, IWriteHandler> AddRange(IEnumerable<KeyValuePair<Type, IWriteHandler>> pairs) => 
            throw MayNotMutate();

        public IImmutableDictionary<Type, IWriteHandler> Clear() => 
            throw MayNotMutate();

        public bool Contains(KeyValuePair<Type, IWriteHandler> pair) => handlers.Contains(pair);

        public bool ContainsKey(Type key) => handlers.ContainsKey(key);

        public void CopyTo(KeyValuePair<Type, IWriteHandler>[] array, int arrayIndex)
        {
            foreach (var kvp in handlers)
                array[arrayIndex++] = kvp;
        }

        public IEnumerator<KeyValuePair<Type, IWriteHandler>> GetEnumerator() => handlers.GetEnumerator();

        public IImmutableDictionary<Type, IWriteHandler> Remove(Type key) => 
            throw MayNotMutate();

        public bool Remove(KeyValuePair<Type, IWriteHandler> item) => 
            throw MayNotMutate();

        public IImmutableDictionary<Type, IWriteHandler> RemoveRange(IEnumerable<Type> keys) => 
            throw MayNotMutate();

        public IImmutableDictionary<Type, IWriteHandler> SetItem(Type key, IWriteHandler value) => 
            throw MayNotMutate();

        public IImmutableDictionary<Type, IWriteHandler> SetItems(IEnumerable<KeyValuePair<Type, IWriteHandler>> items) => 
            throw MayNotMutate();

        public bool TryGetKey(Type equalKey, out Type actualKey) => handlers.TryGetKey(equalKey, out actualKey);

        public bool TryGetValue(Type key, out IWriteHandler value) => handlers.TryGetValue(key, out value);

        void IDictionary<Type, IWriteHandler>.Add(Type key, IWriteHandler value) => 
            throw MayNotMutate();

        void ICollection<KeyValuePair<Type, IWriteHandler>>.Clear() => 
            throw MayNotMutate();

        IEnumerator IEnumerable.GetEnumerator() => handlers.GetEnumerator();

        bool IDictionary<Type, IWriteHandler>.Remove(Type key) => 
            throw MayNotMutate();

        static Exception MayNotMutate() => 
            new NotSupportedException("May not mutate handler map.");
    }

}
