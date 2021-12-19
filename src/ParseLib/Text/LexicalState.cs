namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
        /// In the case of Lookahead state determines the next state to go when node match has failed.
        /// </summary>
        public LexicalState OnFalse { get; }

        /// <summary>
        /// In the case of Lookahead state determines the next state to go when node has matched.
        /// </summary>
        public LexicalState OnTrue { get; }

        public RangeTransition[] Ranges { get; internal set; }
        public CategoryTransition[] Categories { get; internal set; }
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
