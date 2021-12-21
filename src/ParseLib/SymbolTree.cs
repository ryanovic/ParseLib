namespace ParseLib
{
    using System;
    using System.Collections.Generic;

    internal sealed class SymbolTree<T>
    {
        private readonly Dictionary<Symbol, SymbolTree<T>> next = new Dictionary<Symbol, SymbolTree<T>>();

        public T Data { get; set; }

        public SymbolTree<T> GetPrefix(Symbol[] prefix)
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

        public SymbolTree<T> EnsurePrefix(Symbol[] prefix)
        {
            var node = this;

            for (int i = 0; i < prefix.Length; i++)
            {
                if (!node.next.TryGetValue(prefix[i], out var next))
                {
                    next = new SymbolTree<T>();
                    node.next.Add(prefix[i], next);
                }

                node = next;
            }

            return node;
        }
    }
}
