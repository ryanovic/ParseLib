namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class SentinelPosition : Position
    {
        public bool Positive { get; }
        public Position[] Lookahead { get; }

        public SentinelPosition(int tokenId, Position[] lookahead, bool positive = false)
            : base(tokenId)
        {
            this.Lookahead = lookahead;
            this.Positive = positive;
        }
    }
}
