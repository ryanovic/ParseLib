namespace ParseLib.LALR
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class ParserStates : IParserStates
    {
        private readonly List<ParserState> states;

        public ParserState this[int id] => states[id];
        public int Count => states.Count;

        public ParserStates(List<ParserState> states)
        {
            this.states = states;
        }

        public IEnumerator<ParserState> GetEnumerator()
        {
            return states.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return states.GetEnumerator();
        }
    }
}
