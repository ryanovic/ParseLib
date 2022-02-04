namespace Ry.ParseLib.Text
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a regular expression.
    /// </summary>
    public abstract class RexNode
    {
        /// <summary>
        /// Gets a value indicating if the expression is optional.
        /// </summary>
        public abstract bool Nullable { get; }

        /// <summary>
        /// Creates a copy of the current expression and makes it optional. 
        /// </summary>
        public RexNode Optional() => new RexOptional(this);

        /// <summary>
        /// Creates a new expression that matches an optional sequence of instances of the current expression.
        /// </summary>
        public RexNode NoneOrMore() => new RexRepeat(this);

        /// <summary>
        /// Creates a new expression that matches a sequence of instances of the current expression that contains at least one element.
        /// </summary>
        public RexNode OneOrMore() => this.Then(new RexRepeat(this));

        /// <summary>
        /// Creates a new expression that matches either the current expression or a specified one.
        /// </summary>
        public RexNode Or(RexNode node) => new RexOr(this, node);

        /// <summary>
        /// Creates a new expression that matches either the current expression or an expression defined by a specified character.
        /// </summary>
        public RexNode Or(char ch) => new RexOr(this, Rex.Char(ch));

        /// <summary>
        /// Creates a new expression that matches either the current expression or an expression defined by a specified charset.
        /// </summary>
        public RexNode Or(CharSet cs) => new RexOr(this, Rex.Char(cs));

        /// <summary>
        /// Creates a new expression that matches either the current expression or an expression defined by a specified text.
        /// </summary>
        public RexNode Or(string text) => new RexOr(this, Rex.Text(text));

        /// <summary>
        /// Creates a new expression that matches the current expression followed by a specified expression.
        /// </summary>
        public RexNode Then(RexNode node) => new RexAnd(this, node);

        /// <summary>
        /// Creates a new expression that matches the current expression followed by a expression defined by a specified character.
        /// </summary>
        public RexNode Then(char ch) => new RexAnd(this, Rex.Char(ch));

        /// <summary>
        /// Creates a new expression that matches the current expression followed by a expression defined by a specified charset.
        /// </summary>
        public RexNode Then(CharSet cs) => new RexAnd(this, Rex.Char(cs));

        /// <summary>
        /// Creates a new expression that matches the current expression followed by a expression defined by a specified text.
        /// </summary>
        public RexNode Then(string text) => new RexAnd(this, Rex.Text(text));

        /// <summary>
        /// Creates a new expression that matches a sequence of a specified number of instances of the current expression.
        /// </summary>
        /// <param name="count">A strictly positive number of elements in the sequence.</param>
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

        /// <summary>
        /// Creates a new expression that matches a sequence of instances of the current expression with a specified minimum and maximum occurrences.
        /// </summary>
        public RexNode Repeat(int min, int max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException(nameof(min));
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max));

            return min == 0
                ? this.Optional().Repeat(max)
                : this.Repeat(min).Then(this.Optional().Repeat(max - min));
        }

        /// <summary>
        /// Creates a conditional expression that allows execution of the current expression only when it's followed by an expression defined by a specified character.
        /// </summary>
        public RexNode FollowedBy(char ch) => FollowedBy(Rex.Char(ch));

        /// <summary>
        /// Creates a conditional expression that allows execution of the current expression only when it's followed by an expression defined by a specified charset.
        /// </summary>
        public RexNode FollowedBy(CharSet cs) => FollowedBy(Rex.Char(cs));

        /// <summary>
        /// Creates a conditional expression that allows execution of the current expression only when it's followed by an expression defined by a specified text.
        /// </summary>
        public RexNode FollowedBy(string text) => FollowedBy(Rex.Text(text));

        /// <summary>
        /// Creates a conditional expression that allows execution of the current expression only when it's followed by an expression defined by a specified expression.
        /// </summary>
        public RexNode FollowedBy(RexNode node)
        {
            return this.Then(new RexSentinel(node, positive: true));
        }

        /// <summary>
        /// Creates a conditional expression that allows execution of the current expression only when it's NOT followed by an expression defined by a specified character.
        /// </summary>
        public RexNode NotFollowedBy(char ch) => NotFollowedBy(Rex.Char(ch));

        /// <summary>
        /// Creates a conditional expression that allows execution of the current expression only when it's NOT followed by an expression defined by a specified charset.
        /// </summary>
        public RexNode NotFollowedBy(CharSet cs) => NotFollowedBy(Rex.Char(cs));

        /// <summary>
        /// Creates a conditional expression that allows execution of the current expression only when it's NOT followed by an expression defined by a specified text.
        /// </summary>
        public RexNode NotFollowedBy(string text) => NotFollowedBy(Rex.Text(text));

        /// <summary>
        /// Creates a conditional expression that allows execution of the current expression only when it's NOT followed by an expression defined by a specified expression.
        /// </summary>
        public RexNode NotFollowedBy(RexNode node)
        {
            return this.Then(new RexSentinel(node, positive: false));
        }

        /// <summary>
        /// Generates a position graph for the expression and returns positions that represent the beginning of the graph.
        /// </summary>
        internal Position[] Complete(int tokenId = 0, bool lazy = false, bool lookaead = false)
        {
            return this.Then(new RexAccept(lazy, lookaead)).GeneratePositions(tokenId).First;
        }

        /// <summary>
        /// Generates a position graph for the expression.
        /// </summary>
        internal abstract PositionGraph GeneratePositions(int tokenId);
    }
}
