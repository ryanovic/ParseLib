namespace ParseLib.Emit
{
    using ParseLib.LALR;
    using ParseLib.Text;

    public sealed class StateMetadata
    {
        public Symbol InputSymbol { get; }

        public ParserState ParserState { get; }
        public LexicalState LexicalState { get; }
        public Terminal[] Terminals { get; }
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
