namespace Ry.ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class RexRepeat : RexNode
    {
        public override bool Nullable => true;
        public RexNode Node { get; }

        public RexRepeat(RexNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            this.Node = node;
        }

        internal override PositionGraph GeneratePositions(int tokenId)
        {
            var graph = Node.GeneratePositions(tokenId);
            Position.Connect(graph.Last, graph.First);
            return graph;
        }
    }
}
