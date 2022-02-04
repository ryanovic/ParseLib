namespace Ry.ParseLib
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a builder for a production.
    /// </summary>
    public sealed class ProductionBuilder
    {
        public Grammar Grammar { get; }
        public Production Production { get; }

        public ProductionBuilder(Grammar grammar, Production production)
        {
            this.Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            this.Production = production ?? throw new ArgumentNullException(nameof(production));
        }

        /// <summary>
        /// Sets a set of lookaheads according to which a <c>shift</c>-action should be preferred in case of a <c>shift-reduce</c> conflict.
        /// </summary>
        public ProductionBuilder ShiftOn(params object[] lookaheads)
        {
            if (lookaheads == null) throw new ArgumentNullException(nameof(lookaheads));

            foreach (var symbol in Grammar.GetSymbols(lookaheads))
            {
                Production.ShiftOn(symbol);
            }

            return this;
        }

        /// <summary>
        /// Sets a set of lookaheads according to which a <c>reduce</c>-action should be preferred in case of a <c>shift-reduce</c> conflict.
        /// </summary>
        public ProductionBuilder ReduceOn(params object[] lookaheads)
        {
            if (lookaheads == null) throw new ArgumentNullException(nameof(lookaheads));

            foreach (var symbol in Grammar.GetSymbols(lookaheads))
            {
                Production.ReduceOn(symbol);
            }

            return this;
        }

        /// <summary>
        /// Sets a set of lookaheads on which a target production can be reduced. The set may contain the [LB] and [NoLB] symbols.
        /// </summary>
        public ProductionBuilder OverrideLookaheads(params object[] lookaheads)
        {
            if (lookaheads == null) throw new ArgumentNullException(nameof(lookaheads));

            Production.OverrideLookaheads(Grammar.GetSymbols(lookaheads));
            return this;
        }
    }
}
