﻿namespace ParseLib.Text
{
    using System;

    internal sealed class RexSentinel : RexNode
    {
        internal override bool Nullable => false;
        public RexNode Lookahead { get; }
        public bool Positive { get; }

        public RexSentinel(RexNode lookahead, bool positive)
        {
            if (lookahead == null) throw new ArgumentNullException(nameof(lookahead));

            this.Lookahead = lookahead;
            this.Positive = positive;
        }

        internal override PositionGraph GeneratePositions(int tokenId)
        {
            var positions = new[] { new SentinelPosition(tokenId, Lookahead.Complete(lazy: true, lookaead: true), Positive) };
            return new PositionGraph(positions, positions);
        }
    }
}