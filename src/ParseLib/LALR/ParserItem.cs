namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a grammar production at a specific position.
    /// </summary>
    public sealed class ParserItem : IEquatable<ParserItem>
    {
        internal static int CompareBySymbol(ParserItem x, ParserItem y) => x.Symbol.Name.CompareTo(y.Symbol.Name);
        internal static int CompareByProduction(ParserItem x, ParserItem y) => x.Production.Name.CompareTo(y.Production.Name);

        /// <summary>
        /// Gets the current index in the production.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Get the produciton.
        /// </summary>
        public Production Production { get; }

        /// <summary>
        /// Get the current symbol.
        /// </summary>
        public Symbol Symbol => Production[Index];

        /// <summary>
        /// Get the line break modifier for the current position.
        /// </summary>
        public LineBreakModifier LineBreak => Production.GetLineBreakModifier(Index);

        public ParserItem(Production production)
            : this(production, 0)
        {
        }

        public ParserItem(Production production, int index)
        {
            if (production == null) throw new ArgumentNullException(nameof(production));

            if (index > production.Size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            this.Index = index;
            this.Production = production;
        }

        /// <summary>
        /// Gets a sybmols sub-array before the current position.
        /// </summary>
        public Symbol[] GetPrefix()
        {
            var symbols = new Symbol[Index];

            for (int i = 0; i < symbols.Length; i++)
            {
                symbols[i] = Production[i];
            }

            return symbols;
        }

        /// <summary>
        /// Gets a value indicating wether the production can be applied to the specified state at the current position according to the state line-break modifier.
        /// </summary>
        public bool IsAllowed(ParserState state) => IsAllowed(state.LineBreak);

        /// <summary>
        /// Gets a value indicating whether the production can be applied at the current position according to the specified line-break modifier.
        /// </summary>
        public bool IsAllowed(LineBreakModifier lineBreak) => (LineBreak | lineBreak) != LineBreakModifier.Forbidden;

        /// <summary>
        /// Gets an item for the production at the next position.
        /// </summary>
        public ParserItem CreateNextItem()
        {
            return new ParserItem(Production, Index + 1);
        }

        public bool Equals(ParserItem other)
        {
            return Index == other.Index && Production == other.Production;
        }

        public override bool Equals(object obj)
        {
            return obj is ParserItem item && Equals(item);
        }

        public override int GetHashCode()
        {
            var hashCode = 176955453;
            hashCode = hashCode * -1521134295 + Index;
            hashCode = hashCode * -1521134295 + Production.GetHashCode();
            return hashCode;
        }
    }
}
