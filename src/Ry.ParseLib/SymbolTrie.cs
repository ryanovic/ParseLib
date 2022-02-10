namespace Ry.ParseLib
{
    using System;
    using System.Collections.Generic;

    internal sealed class SymbolTrie<T>
    {
        private readonly Dictionary<Symbol, SymbolTrie<T>> next = new Dictionary<Symbol, SymbolTrie<T>>();

        public T Data { get; set; }

        public bool TryGetData(Symbol[] prefix, out T data)
        {
            var node = GetPrefix(prefix);

            if (node == null)
            {
                data = default(T);
                return false;
            }

            data = node.Data;
            return true;
        }

        public SymbolTrie<T> GetPrefix(Symbol[] prefix)
        {
            var node = this;

            for (int i = 0; i < prefix.Length; i++)
            {
                if (!node.next.TryGetValue(prefix[i], out var next))
                {
                    return null;
                }

                node = next;
            }

            return node;
        }

        public SymbolTrie<T> EnsurePrefix(Symbol[] prefix)
        {
            var node = this;

            for (int i = 0; i < prefix.Length; i++)
            {
                if (!node.next.TryGetValue(prefix[i], out var next))
                {
                    next = new SymbolTrie<T>();
                    node.next.Add(prefix[i], next);
                }

                node = next;
            }

            return node;
        }
    }
}
