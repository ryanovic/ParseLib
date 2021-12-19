namespace ParseLib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum SymbolType
    {
        Target,
        Terminal,
        NonTerminal,
        LineBreak,
        NoLineBreak,
        EndOfSource,
        EndOfProduction,
    }

    public class Symbol
    {
        public static Symbol Target { get; } = new Symbol("$Goal", SymbolType.Target);
        public static Symbol EndOfSource { get; } = new Symbol("$EOS", SymbolType.EndOfSource);
        public static Symbol EndOfProduction { get; } = new Symbol(String.Empty, SymbolType.EndOfProduction);
        public static Symbol LineBreak { get; } = new Symbol("[LB]", SymbolType.LineBreak);
        public static Symbol NoLineBreak { get; } = new Symbol("[NoLB]", SymbolType.NoLineBreak);

        public virtual string Name { get; }
        public virtual SymbolType Type { get; }

        internal Symbol(string name, SymbolType type)
        {
            this.Name = name;
            this.Type = type;
        }

        public override string ToString()
        {
            return Name;
        }

        public static string ToString(IEnumerable<Symbol> symbols)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            return String.Join(" ", symbols.Select(x => x.Name));
        }
    }
}
