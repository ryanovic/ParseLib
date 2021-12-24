namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class RexChar : RexNode
    {
        public override bool Nullable => false;
        public CharSet Set { get; }

        public RexChar(CharSet set)
        {
            if (set == null) new ArgumentNullException(nameof(set));

            this.Set = set;
        }

        internal override PositionGraph GeneratePositions(int tokenId)
        {
            var positions = new Position[] { new TextPosition(tokenId, Set) };

            return new PositionGraph(positions, positions);
        }
    }
}
