namespace ParseLib.Emit
{
    using ParseLib.LALR;
    using ParseLib.Text;

    /// <summary>
    /// Represents computed state metadata.
    /// </summary>
    public sealed class StateMetadata
    {
        /// <summary>
        /// Gets the symbol leading to the state.
        /// </summary>
        public Symbol InputSymbol { get; }

        /// <summary>
        /// Gets the LALR parser state.
        /// </summary>
        public ParserState ParserState { get; }

        /// <summary>
        /// Gets the lexical analyzer state.
        /// </summary>
        public LexicalState LexicalState { get; }

        /// <summary>
        /// Gets the ordered set of terminals correspoded to the state.
        /// </summary>
        public Terminal[] Terminals { get; }

        /// <summary>
        /// Gets the ordered set of non-terminals correspoded to the state.
        /// </summary>
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
