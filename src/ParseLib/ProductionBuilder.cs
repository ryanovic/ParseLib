namespace ParseLib
{
    using System;
    using System.Collections.Generic;

    public sealed class ProductionBuilder
    {
        public Grammar Grammar { get; }
        public Production Production { get; }

        public ProductionBuilder(Grammar grammar, Production production)
        {
            this.Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            this.Production = production ?? throw new ArgumentNullException(nameof(production));
        }

        public ProductionBuilder ShiftOn(params object[] lookaheads)
        {
            if (lookaheads == null) throw new ArgumentNullException(nameof(lookaheads));

            foreach (var symbol in Grammar.GetSymbols(lookaheads))
            {
                Production.SetResolveOn(symbol, preferReduce: false);
            }

            return this;
        }

        public ProductionBuilder ReduceOn(params object[] lookaheads)
        {
            if (lookaheads == null) throw new ArgumentNullException(nameof(lookaheads));

            foreach (var symbol in Grammar.GetSymbols(lookaheads))
            {
                Production.SetResolveOn(symbol, preferReduce: true);
            }

            return this;
        }

        public ProductionBuilder OverrideLookaheads(params object[] lookaheads)
        {
            if (lookaheads == null) throw new ArgumentNullException(nameof(lookaheads));
            Production.SetReduceOn(Grammar.GetSymbols(lookaheads));
            return this;
        }
    }
}
