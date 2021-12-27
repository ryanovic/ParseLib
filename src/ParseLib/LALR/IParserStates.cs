namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;

    public interface IParserStates : IEnumerable<ParserState>
    {
        int Count { get; }
        ParserState this[int id] { get; }
    }
}
