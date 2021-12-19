namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class RexAnd : RexNode
    {
        internal override bool Nullable => this.Left.Nullable && this.Right.Nullable;
        public RexNode Left { get; }
        public RexNode Right { get; }

        public RexAnd(RexNode left, RexNode right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            this.Left = left;
            this.Right = right;
        }

        internal override PositionGraph GeneratePositions(int tokenId)
        {
            var graphL = Left.GeneratePositions(tokenId);
            var graphR = Right.GeneratePositions(tokenId);

            Position.Connect(graphL.Last, graphR.First);

            var first_updated = Left.Nullable
                ? Position.Union(graphL.First, graphR.First)
                : graphL.First;

            var last_updated = Right.Nullable
                ? Position.Union(graphL.Last, graphR.Last)
                : graphR.Last;

            return new PositionGraph(first_updated, last_updated);
        }
    }
}
