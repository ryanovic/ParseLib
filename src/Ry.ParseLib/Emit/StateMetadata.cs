namespace Ry.ParseLib.Emit
{
    using Ry.ParseLib.LALR;
    using Ry.ParseLib.Text;

    /// <summary>
    /// Represents a parser state extended with the computed <see cref="LexicalState"/> and a list of valid symbols.
    /// </summary>
    public sealed class StateMetadata
    {
        /// <summary>
        /// Gets the grammar symbol which leads to the state.
        /// </summary>
        /// <remarks>Will always be <c>null</c> for a root state and specified otherwise.</remarks>
        /// <example>
        /// Let's say we have the following transition: <c> 'expr' * '+' 'expr' --> 'expr' '+' * 'expr'</c>.
        /// The symbol <c>+</c> is considered as an input symbol for the second state then.
        /// </example>
        public Symbol InputSymbol { get; }

        /// <summary>
        /// Gets the original LALR parser state.
        /// </summary>
        public ParserState ParserState { get; }

        /// <summary>
        /// Gets the associated lexical state.
        /// </summary>
        public LexicalState LexicalState { get; }

        /// <summary>
        /// Get the list of valid terminals.
        /// </summary>
        /// <remarks>Ordered by the <see cref="Terminal.Id"/> value.</remarks>
        public Terminal[] Terminals { get; }

        /// <summary>
        /// Get the list of valid non-terminals.
        /// </summary>
        /// <remarks>Ordered by the <see cref="NonTerminal.Id"/> value.</remarks>
        public NonTerminal[] NonTerminals { get; }

        internal StateMetadata(Symbol inputSymbol, ParserState parserState, LexicalState lexicalState, Terminal[] terminals, NonTerminal[] nonTerminals)
        {
            this.InputSymbol = inputSymbol;
            this.ParserState = parserState;
            this.LexicalState = lexicalState;
            this.Terminals = terminals;
            this.NonTerminals = nonTerminals;
        }
    }
}
