namespace Ry.ParseLib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ry.ParseLib.LALR;

    /// <summary>
    /// Represents a production.
    /// </summary>
    public sealed class Production : IComparable<Production>
    {
        private readonly ProductionItem[] items;
        private Dictionary<Symbol, ParserAction> reduceConflictActions;
        private HashSet<Symbol> allowList;
        private HashSet<Symbol> denyList;

        public Symbol Head { get; }
        public string Name { get; }
        public int Size => items.Length - 1;
        public Symbol this[int index] => items[index].Symbol;

        /// <summary>
        /// Gets a set of lookaheads that are only allowed for the production. If the set is not specified, then any valid lookahead, except ones from the denied list, is allowed.
        /// </summary>
        public ISet<Symbol> AllowList => allowList;

        /// <summary>
        /// Gets a set of symbols that can't be selected as a lookahead for the production.
        /// </summary>
        public ISet<Symbol> DenyList => denyList;

        /// <summary>
        /// Gets a set of inline rules defining how a shift-reduce conflict should be resolved for the production.
        /// </summary>
        public IDictionary<Symbol, ParserAction> ReduceConflictActions => reduceConflictActions;

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

        public void AllowOn(params Symbol[] symbols)
        {
            SymbolGuard.Verify(symbols, SymbolType.LineBreak, SymbolType.NoLineBreak, SymbolType.EndOfSource, SymbolType.Terminal);

            if (allowList == null)
            {
                allowList = new HashSet<Symbol>(symbols);
                return;
            }

            allowList.UnionWith(symbols);
        }

        /// <summary>
        /// Sets a set of lookaheads on which a target production can NOT be reduced. May contain terminals and $EOS lookaheads.
        /// </summary>
        public void DenyOn(params Symbol[] symbols)
        {
            SymbolGuard.Verify(symbols, SymbolType.EndOfSource, SymbolType.Terminal);

            if (denyList == null)
            {
                denyList = new HashSet<Symbol>(symbols);
                return;
            }

            denyList.UnionWith(symbols);
        }

        /// <summary>
        /// Sets a set of lookaheads according to which a <c>reduce</c>-action should be preferred in case of a <c>shift-reduce</c> conflict.
        /// </summary>
        public void ReduceOn(Symbol symbol)
        {
            SymbolGuard.Verify(symbol, SymbolType.Terminal);

            if (reduceConflictActions == null)
            {
                reduceConflictActions = new Dictionary<Symbol, ParserAction>();
            }

            reduceConflictActions.Add(symbol, ParserAction.Reduce);
        }

        /// <summary>
        /// Sets a set of lookaheads according to which a <c>shift</c>-action should be preferred in case of a <c>shift-reduce</c> conflict.
        /// </summary>
        public void ShiftOn(Symbol symbol)
        {
            SymbolGuard.Verify(symbol, SymbolType.Terminal);

            if (reduceConflictActions == null)
            {
                reduceConflictActions = new Dictionary<Symbol, ParserAction>();
            }

            reduceConflictActions.Add(symbol, ParserAction.Shift);
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

        public int CompareTo(Production other) => Head == other.Head ? Name.CompareTo(other.Name) : Head.Name.CompareTo(other.Name);

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

            public override string ToString()
            {
                switch (LineBreak)
                {
                    case LineBreakModifier.LineBreak:
                        return "[LB] " + Symbol.Name;
                    case LineBreakModifier.NoLineBreak:
                        return "[NoLB] " + Symbol.Name;
                    default:
                        return Symbol.Name;
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
                        throw new InvalidOperationException(Errors.SymbolNotAllowedInProduction(symbol.Name));
                }
            }
        }
    }
}
