namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the set of parser states.
    /// </summary>
    public interface IParserStates : IEnumerable<ParserState>
    {
        int Count { get; }
        ParserState this[int id] { get; }
    }
}
