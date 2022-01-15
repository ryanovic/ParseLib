namespace ParseLib.Emit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using ParseLib.LALR;
    using ParseLib.Text;

    /// <summary>
    /// Represents a collection of <see cref="StateMetadata"/> items extended with a grammar level metadata.
    /// </summary>
    public sealed class ParserMetadata
    {
        /// <summary>
        /// Generates a lexical state and aggregates a collection of valid symbols for each parser state.
        /// </summary>
        /// <param name="grammar">The source grammar.</param>
        /// <param name="lexicalStates">The collection for accumulating generated lexical states.</param>
        /// <param name="parserStates">The collection of parser states.</param>
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
                // Do not need to generate a lexical state for non-terminal issued states.
                // Consider an example:
                // S -> *A B
                // A -> *a
                //
                // A -> a* { reduce on 'b' }
                //
                // S -> A *B
                // B -> *b
                //
                // So once A -> a is reduced, we already would have 'b' terminal recognized as a lookahead,
                // so the following state will have no need to read it from the source once again.
                terminals = Utils.Concate(terminals, whitespaces);
                lexState = lexicalStates.CreateStates(terminals);
            }

            // Sort to ensure we can do binary search later.
            Array.Sort(terminals, (x, y) => x.Id - y.Id);
            Array.Sort(nonTerminals, (x, y) => x.Id - y.Id);

            return new StateMetadata(symbol, state, lexState, terminals, nonTerminals);
        }

        public Grammar Grammar { get; }
        public ILexicalStates LexicalStates { get; }

        /// <summary>
        /// Gets the set of grammar terminals.
        /// </summary>
        /// <remarks><see cref="Terminal.Id"/> is guaranteed to match the position.</remarks>
        public Terminal[] Terminals { get; }

        /// <summary>
        /// Gets the set of grammar non-terminals.
        /// </summary>
        /// <remarks><see cref="NonTerminal.Id"/> is guaranteed to match the position.</remarks>
        public NonTerminal[] NonTerminals { get; }

        /// <summary>
        /// Get the set of parser states.
        /// </summary>
        /// <remarks><see cref="StateMetadata.ParserState"/>'s ID is guaranteed to match the position.</remarks>
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
