namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;

    public interface ILexicalStates : IEnumerable<LexicalState>
    {
        int Count { get; }
        bool HasLookaheads { get; }
        LexicalState this[int id] { get; }

        LexicalState CreateStates(IEnumerable<Terminal> terminals);
        LexicalState CreateStates(int tokenId, RexNode expression, bool lazy = false);
    }
}
