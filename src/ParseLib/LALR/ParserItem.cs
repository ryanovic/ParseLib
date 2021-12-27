namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class ParserItem : IEquatable<ParserItem>
    {
        internal static int CompareBySymbol(ParserItem x, ParserItem y) => x.Symbol.Name.CompareTo(y.Symbol.Name);
        internal static int CompareByProduction(ParserItem x, ParserItem y) => x.Production.Name.CompareTo(y.Production.Name);

        public int Index { get; }
        public Production Production { get; }
        public Symbol Symbol => Production[Index];
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

        public Symbol[] GetPrefix()
        {
            var symbols = new Symbol[Index];

            for (int i = 0; i < symbols.Length; i++)
            {
                symbols[i] = Production[i];
            }

            return symbols;
        }

        public bool IsAllowed(ParserState state) => IsAllowed(state.LineBreak);

        public bool IsAllowed(LineBreakModifier lineBreak) => (LineBreak | lineBreak) != LineBreakModifier.Forbidden;

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
