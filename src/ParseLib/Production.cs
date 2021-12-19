namespace ParseLib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ParseLib.LALR;

    public sealed class Production
    {
        private readonly ProductionItem[] items;
        private Dictionary<Symbol, ParserAction> resolveOn;
        private HashSet<Symbol> lookaheads;

        public Symbol Head { get; }
        public string Name { get; }
        public int Size => items.Length - 1;
        public Symbol this[int index] => items[index].Symbol;
        public ISet<Symbol> Lookaheads => lookaheads;
        public IDictionary<Symbol, ParserAction> ResolveOn => resolveOn;

        public Production(Symbol head, string name, Symbol[] symbols)
        {
            this.Head = head ?? throw new ArgumentNullException(nameof(head));
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.items = GetItems(symbols ?? throw new ArgumentNullException(nameof(symbols)));
        }

        public LineBreakModifier GetLineBreakModifier(int index) => items[index].LineBreak;

        public void OverrideLookaheads(params Symbol[] symbols)
        {
            if (lookaheads == null)
            {
                lookaheads = new HashSet<Symbol>(symbols);
                return;
            }

            lookaheads.UnionWith(symbols);
        }

        public void ReduceOn(Symbol symbol)
        {
            if (resolveOn == null)
            {
                resolveOn = new Dictionary<Symbol, ParserAction>();
            }

            resolveOn.Add(symbol, ParserAction.Reduce);
        }

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
                    throw new InvalidOperationException("Can't use [LB] and [NoLB] simultaneously.");
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
                        throw new InvalidOperationException($"{symbol.Name} is not allowed in the production body.");
                }
            }
        }
    }
}
