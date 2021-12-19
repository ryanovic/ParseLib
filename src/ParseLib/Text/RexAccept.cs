using System;
using System.Collections.Generic;
using System.Text;

namespace ParseLib.Text
{
    internal sealed class RexAccept : RexNode
    {
        internal override bool Nullable => false;
        public bool IsLazy { get; }
        public bool IsLookahead { get; }

        public RexAccept(bool lazy, bool lookahead)
        {
            this.IsLazy = lazy;
            this.IsLookahead = lookahead;
        }

        internal override PositionGraph GeneratePositions(int tokenId)
        {
            var positions = new[] { new AcceptPosition(tokenId, IsLazy, IsLookahead) };

            return new PositionGraph(
                positions,
                positions);
        }
    }
}
