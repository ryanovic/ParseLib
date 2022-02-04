namespace Ry.ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a lexical state.
    /// </summary>
    public sealed class LexicalState
    {
        internal const int InvalidAcceptId = -1;
        private readonly AcceptPosition position;

        public int Id { get; internal set; }
        public int AcceptId => position?.TokenId ?? InvalidAcceptId;
        public bool IsFinal => position != null;
        public bool IsLookaheadStart => OnTrue != null || OnFalse != null;
        public bool IsLookaheadFinal => position != null && position.IsLookahead;
        public bool IsLowSurrogate { get; }

        /// <summary>
        /// Gets a state to continue execution if a lookahead condition failed. 
        /// </summary>
        public LexicalState OnFalse { get; }

        /// <summary>
        /// Gets a state to continue execution if a lookahead condition succeeded. 
        /// </summary>
        public LexicalState OnTrue { get; }

        /// <summary>
        /// Gets a collection of transitions defined by Unicode ranges.
        /// </summary>
        public RangeTransition[] Ranges { get; internal set; }

        /// <summary>
        /// Gets a collection of transitions defined by Unicode categories. Applicable when no Unicode ranges matched.
        /// </summary>
        public CategoryTransition[] Categories { get; internal set; }

        /// <summary>
        /// Gets a default transitions. Applicable when neither Unicode ranges nor categories matched.
        /// </summary>
        public LexicalState Default { get; internal set; }

        internal LexicalState(AcceptPosition position, LexicalState onFalse, LexicalState onTrue, bool isSurrogate = false)
        {
            this.position = position;
            this.OnFalse = onFalse;
            this.OnTrue = onTrue;
            this.IsLowSurrogate = isSurrogate;
        }

        internal LexicalState CreateSurrogate()
        {
            return new LexicalState(null, null, null, isSurrogate: true);
        }

        public override string ToString()
        {
            return $"State #{Id:D5}";
        }
    }
}
