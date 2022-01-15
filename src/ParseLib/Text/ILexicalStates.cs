namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an extendable set of lexical states. 
    /// </summary>
    public interface ILexicalStates : IEnumerable<LexicalState>
    {
        int Count { get; }
        bool HasLookaheads { get; }
        LexicalState this[int id] { get; }

        /// <summary>
        /// Creates lexical states for the specified terminals and returns a root state for the generated graph.
        /// </summary>
        LexicalState CreateStates(IEnumerable<Terminal> terminals);

        /// <summary>
        /// Creates lexical states for the specified regular expression and returns a root state for the generated graph.
        /// </summary>
        LexicalState CreateStates(int tokenId, RexNode expression, bool lazy = false);
    }
}
