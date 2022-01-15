namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a parser state.
    /// </summary>
    public class ParserState
    {
        public int Id { get; }

        /// <summary>
        /// Gets or sets the line break sensitivity modifier.
        /// </summary>
        public LineBreakModifier LineBreak { get; set; }

        /// <summary>
        /// Gets the set of core items.
        /// </summary>
        public ParserItem[] Core { get; }

        /// <summary>
        /// Gets the set of completed productions.
        /// </summary>
        public Production[] Completed { get; set; }

        /// <summary>
        /// Gets the state per symbol transition set.  
        /// </summary>
        public Dictionary<Symbol, ParserState> Shift { get; }

        /// <summary>
        /// Gets the production per symbol reduction set. The production is set from the <see cref="Completed"/> collection.
        /// </summary>
        public Dictionary<Symbol, Production> Reduce { get; }

        /// <summary>
        /// Get the final action table for the state.
        /// For each <c>Shift</c> action there is an entry in the <see cref="Shift"/> set.
        /// For <c>Reduce</c> actions there should be match in the <see cref="Reduce"/> set.
        /// </summary>
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
