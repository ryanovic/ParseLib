namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;

    public class ParserState
    {
        public int Id { get; }
        public LineBreakModifier LineBreak { get; set; }
        public ParserItem[] Core { get; }
        public Production[] Completed { get; set; }
        public Dictionary<Symbol, ParserState> Shift { get; }
        public Dictionary<Symbol, Production> Reduce { get; }
        public Dictionary<Symbol, ParserAction> Actions { get; }

        public ParserState(int id, ParserItem[] core)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));

            this.Id = id;
            this.Core = core;
            this.Completed = Array.Empty<Production>();
            this.Reduce = new Dictionary<Symbol, Production>();
            this.Shift = new Dictionary<Symbol, ParserState>();
            this.Actions = new Dictionary<Symbol, ParserAction>();
        }

        public ParserState(int id, ParserState original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));

            this.Id = id;
            this.Core = original.Core;
            this.Completed = original.Completed;
            this.Reduce = new Dictionary<Symbol, Production>(original.Reduce);
            this.Shift = new Dictionary<Symbol, ParserState>(original.Shift);
            this.Actions = new Dictionary<Symbol, ParserAction>(original.Actions);
        }

        public ParserState GetState(params Symbol[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var state = this;

            for (int i = 0; i < path.Length; i++)
            {
                if (!state.Shift.TryGetValue(path[i], out var next))
                {
                    return null;
                }

                state = next;
            }

            return state;
        }
    }
}
