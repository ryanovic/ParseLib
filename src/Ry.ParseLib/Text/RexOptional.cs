namespace Ry.ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class RexOptional : RexNode
    {
        public override bool Nullable => true;
        public RexNode Node { get; }

        public RexOptional(RexNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            this.Node = node;
        }

        internal override PositionGraph GeneratePositions(int tokenId)
        {
            return Node.GeneratePositions(tokenId);
        }
    }
}
