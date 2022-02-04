namespace Ry.ParseLib.Text
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a position that matches the expression at some point.
    /// </summary>
    internal abstract class Position
    {
        /// <summary>
        /// Gets or sets a token ID, determined by the expression, to which the position belongs.
        /// </summary>
        public int TokenId { get; }

        /// <summary>
        /// Gets or sets a collection of the following positions.
        /// </summary>
        public Position[] Next { get; set; }

        public Position(int tokenId)
        {
            if (tokenId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenId), Errors.NegativeTokenId());
            }

            this.TokenId = tokenId;
        }

        /// <summary>
        /// Connects each position in the target set to each position in the source set as the next.
        /// </summary>
        public static void Connect(Position[] source, Position[] target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            foreach (var position in source)
            {
                if (position.Next == null)
                {
                    position.Next = target;
                    continue;
                }

                var existing = new HashSet<Position>(position.Next);
                existing.UnionWith(target);
                position.Next = existing.ToArray();
            }
        }

        /// <summary>
        /// Generates a combined set of unique positions.
        /// </summary>
        public static Position[] Union(Position[] setA, Position[] setB)
        {
            if (setA == null) throw new ArgumentNullException(nameof(setA));
            if (setB == null) throw new ArgumentNullException(nameof(setB));

            var updated = new HashSet<Position>();

            updated.UnionWith(setA);
            updated.UnionWith(setB);

            return updated.ToArray();
        }
    }
}
