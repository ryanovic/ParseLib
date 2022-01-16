namespace ParseLib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Defines a kind of a symbol.
    /// </summary>
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


    /// <summary>
    /// Defines a basic unit of grammar.
    /// </summary>
    public class Symbol
    {
        /// <summary>
        /// Gets a special symbol that represents a root non-terminal. Can't be used in a production.
        /// </summary>
        public static Symbol Target { get; } = new Symbol("$Goal", SymbolType.Target);

        /// <summary>
        /// Gets a special symbol that represents a end of source. Can't be used in a production.
        /// </summary>
        public static Symbol EndOfSource { get; } = new Symbol("$EOS", SymbolType.EndOfSource);

        /// <summary>
        /// Gets a special symbol that represents a end of production. Can't be used in a production.
        /// </summary>
        public static Symbol EndOfProduction { get; } = new Symbol(String.Empty, SymbolType.EndOfProduction);

        /// <summary>
        /// Gets a special symbol that, when used in a production, requires a line-break between surrounding symbols.
        /// </summary>
        public static Symbol LineBreak { get; } = new Symbol("[LB]", SymbolType.LineBreak);

        /// <summary>
        /// Gets a special symbol that, when used in a production, denies a line-break between surrounding symbols.
        /// </summary>
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
