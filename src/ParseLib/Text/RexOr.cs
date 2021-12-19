namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class RexOr : RexNode
    {
        internal override bool Nullable => this.Left.Nullable || this.Right.Nullable;
        public RexNode Left { get; }
        public RexNode Right { get; }

        public RexOr(RexNode left, RexNode right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            this.Left = left;
            this.Right = right;
        }

        internal override PositionGraph GeneratePositions(int tokenId)
        {
            var graphLeft = Left.GeneratePositions(tokenId);
            var graphRight = Right.GeneratePositions(tokenId);

            return new PositionGraph(
                Position.Union(graphLeft.First, graphRight.First),
                Position.Union(graphLeft.Last, graphRight.Last));
        }
    }
}
