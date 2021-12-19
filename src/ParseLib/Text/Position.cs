namespace ParseLib.Text
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    internal abstract class Position
    {
        public int TokenId { get; }
        public Position[] Next { get; set; }

        public Position(int tokenId)
        {
            if (tokenId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenId), "Token Id must be non-negative.");
            }

            this.TokenId = tokenId;
        }

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
