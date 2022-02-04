namespace Ry.ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a position for conditional expressions.
    /// </summary>
    internal sealed class SentinelPosition : Position
    {
        /// <summary>
        /// Gets a value indicating whether conditional should match or fail to continue.
        /// </summary>
        public bool Positive { get; }

        /// <summary>
        /// Gets positions representing a condition to evaluate.
        /// </summary>
        public Position[] Lookahead { get; }

        public SentinelPosition(int tokenId, Position[] lookahead, bool positive = false)
            : base(tokenId)
        {
            this.Lookahead = lookahead;
            this.Positive = positive;
        }
    }
}
