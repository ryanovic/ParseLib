namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;

    public abstract class RexNode
    {
        internal abstract bool Nullable { get; }

        public RexNode Optional() => new RexOptional(this);

        public RexNode NoneOrMore() => new RexRepeat(this);

        public RexNode OneOrMore() => this.Then(new RexRepeat(this));

        public RexNode Or(RexNode node) => new RexOr(this, node);

        public RexNode Or(char ch) => new RexOr(this, Rex.Char(ch));

        public RexNode Or(CharSet cs) => new RexOr(this, Rex.Char(cs));

        public RexNode Or(string text) => new RexOr(this, Rex.Text(text));

        public RexNode Then(RexNode node) => new RexAnd(this, node);

        public RexNode Then(char ch) => new RexAnd(this, Rex.Char(ch));

        public RexNode Then(CharSet cs) => new RexAnd(this, Rex.Char(cs));

        public RexNode Then(string text) => new RexAnd(this, Rex.Text(text));

        public RexNode Repeat(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));

            var node = this;

            for (int i = 1; i < count; i++)
            {
                node = node.Then(this);
            }

            return node;
        }

        public RexNode Repeat(int min, int max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException(nameof(min));
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max));

            return min == 0
                ? this.Optional().Repeat(max)
                : this.Repeat(min).Then(this.Optional().Repeat(max - min));
        }

        public RexNode FollowedBy(char ch) => FollowedBy(Rex.Char(ch));

        public RexNode FollowedBy(CharSet cs) => FollowedBy(Rex.Char(cs));

        public RexNode FollowedBy(string text) => FollowedBy(Rex.Text(text));

        public RexNode FollowedBy(RexNode node)
        {
            return this.Then(new RexSentinel(node, positive: true));
        }

        public RexNode NotFollowedBy(char ch) => NotFollowedBy(Rex.Char(ch));

        public RexNode NotFollowedBy(CharSet cs) => NotFollowedBy(Rex.Char(cs));

        public RexNode NotFollowedBy(string text) => NotFollowedBy(Rex.Text(text));

        public RexNode NotFollowedBy(RexNode node)
        {
            return this.Then(new RexSentinel(node, positive: false));
        }

        internal Position[] Complete(int tokenId = 0, bool lazy = false, bool lookaead = false)
        {
            return this.Then(new RexAccept(lazy, lookaead)).GeneratePositions(tokenId).First;
        }

        internal abstract PositionGraph GeneratePositions(int tokenId);
    }
}
