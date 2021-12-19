namespace ParseLib.Emit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using ParseLib.LALR;
    using ParseLib.Text;

    /// <summary>
    /// Represents computed parser's states and metadata. 
    /// </summary>
    public sealed class ParserMetadata
    {
        public static ParserMetadata Create(Grammar grammar, ILexicalStates lexicalStates, IParserStates parserStates)
        {
            if (grammar == null) throw new ArgumentNullException(nameof(grammar));
            if (lexicalStates == null) throw new ArgumentNullException(nameof(lexicalStates));
            if (parserStates == null) throw new ArgumentNullException(nameof(parserStates));

            var states = new StateMetadata[parserStates.Count];
            var whitespaces = grammar.Whitespaces.Keys.ToArray();

            for (int i = 0; i < states.Length; i++)
            {
                states[i] = CreateState(lexicalStates, parserStates[i], whitespaces);
            }

            return new ParserMetadata(grammar, lexicalStates, states);
        }

        internal static StateMetadata CreateState(ILexicalStates lexicalStates, ParserState state, Terminal[] whitespaces)
        {
            var terminals = state.Actions.Keys.OfType<Terminal>().ToArray();
            var nonTerminals = state.Shift.Keys.OfType<NonTerminal>().ToArray();
            var symbol = state.Id == 0 ? null : state.Core[0].Production[state.Core[0].Index - 1];
            var lexState = (LexicalState)null;

            if (symbol == null || symbol is Terminal)
            {
                terminals = Utils.Concate(terminals, whitespaces);
                lexState = lexicalStates.CreateStates(terminals);
            }

            Array.Sort(terminals, (x, y) => x.Id - y.Id);
            Array.Sort(nonTerminals, (x, y) => x.Id - y.Id);

            return new StateMetadata(symbol, state, lexState, terminals, nonTerminals);
        }

        public Grammar Grammar { get; }
        public ILexicalStates LexicalStates { get; }
        public Terminal[] Terminals { get; }
        public NonTerminal[] NonTerminals { get; }
        public StateMetadata[] States { get; }

        internal ParserMetadata(Grammar grammar, ILexicalStates lexicalStates, StateMetadata[] states)
        {
            this.Grammar = grammar;
            this.LexicalStates = lexicalStates;
            this.Terminals = grammar.GetTerminals();
            this.NonTerminals = grammar.GetNonTerminals();
            this.States = states;
        }
    }
}
