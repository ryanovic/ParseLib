namespace ParseLib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ParseLib.LALR;

    /// <summary>
    /// Represents a production.
    /// </summary>
    public sealed class Production
    {
        private readonly ProductionItem[] items;
        private Dictionary<Symbol, ParserAction> resolveOn;
        private HashSet<Symbol> lookaheads;

        public Symbol Head { get; }
        public string Name { get; }
        public int Size => items.Length - 1;
        public Symbol this[int index] => items[index].Symbol;

        /// <summary>
        /// Gets a set of lookaheads allowed for the production. If the set is not specified, then any valid lookahed is allowed.
        /// </summary>
        public ISet<Symbol> Lookaheads => lookaheads;

        /// <summary>
        /// Gets a set of rules defining how a shift-reduce conflict should be resolved.
        /// </summary>
        public IDictionary<Symbol, ParserAction> ResolveOn => resolveOn;

        public Production(Symbol head, string name, Symbol[] symbols)
        {
            this.Head = head ?? throw new ArgumentNullException(nameof(head));
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.items = GetItems(symbols ?? throw new ArgumentNullException(nameof(symbols)));
        }

        /// <summary>
        /// Gets a line-break modifier for a symbol at a specified index.
        /// </summary>
        public LineBreakModifier GetLineBreakModifier(int index) => items[index].LineBreak;

        /// <summary>
        /// Sets a set of lookaheads on which a target production can be reduced. The set may contain the [LB] and [NoLB] symbols.
        /// </summary>
        public void OverrideLookaheads(params Symbol[] symbols)
        {
            if (lookaheads == null)
            {
                lookaheads = new HashSet<Symbol>(symbols);
                return;
            }

            lookaheads.UnionWith(symbols);
        }

        /// <summary>
        /// Sets a set of lookaheads according to which a <c>reduce</c>-action should be preferred in case of a <c>shift-reduce</c> conflict.
        /// </summary>
        public void ReduceOn(Symbol symbol)
        {
            if (resolveOn == null)
            {
                resolveOn = new Dictionary<Symbol, ParserAction>();
            }

            resolveOn.Add(symbol, ParserAction.Reduce);
        }

        /// <summary>
        /// Sets a set of lookaheads according to which a <c>shift</c>-action should be preferred in case of a <c>shift-reduce</c> conflict.
        /// </summary>
        public void ShiftOn(Symbol symbol)
        {
            if (resolveOn == null)
            {
                resolveOn = new Dictionary<Symbol, ParserAction>();
            }

            resolveOn.Add(symbol, ParserAction.Shift);
        }

        public override string ToString() => Name;

        private ProductionItem[] GetItems(Symbol[] symbols)
        {
            var items = new List<ProductionItem>();
            var lineBreak = LineBreakModifier.None;

            for (int i = 0; i < symbols.Length; i++)
            {
                if (symbols[i].Type == SymbolType.LineBreak)
                {
                    lineBreak |= LineBreakModifier.LineBreak;
                }
                else if (symbols[i].Type == SymbolType.NoLineBreak)
                {
                    lineBreak |= LineBreakModifier.NoLineBreak;
                }
                else
                {
                    items.Add(new ProductionItem(symbols[i], lineBreak));
                    lineBreak = LineBreakModifier.None;
                }
            }

            items.Add(new ProductionItem(Symbol.EndOfProduction, lineBreak));
            return items.ToArray();
        }

        private readonly struct ProductionItem
        {
            public Symbol Symbol { get; }
            public LineBreakModifier LineBreak { get; }

            public ProductionItem(Symbol symbol, LineBreakModifier lineBreak)
            {
                CheckSymbol(symbol);

                this.Symbol = symbol;
                this.LineBreak = lineBreak;

                if (lineBreak == LineBreakModifier.Forbidden)
                {
                    throw new InvalidOperationException(Errors.LineBreakForbidden());
                }
            }

            private static void CheckSymbol(Symbol symbol)
            {
                if (symbol == null) throw new ArgumentNullException(nameof(symbol));

                switch (symbol.Type)
                {
                    case SymbolType.Terminal:
                    case SymbolType.NonTerminal:
                    case SymbolType.EndOfProduction:
                        break;
                    default:
                        throw new InvalidOperationException(Errors.SymbolNotAllowed(symbol.Name));
                }
            }
        }
    }
}
