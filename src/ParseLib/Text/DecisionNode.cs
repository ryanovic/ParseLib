namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Helper to build decision tree to handle lookahead expressions.
    /// </summary>
    internal sealed class DecisionNode
    {
        public bool IsLeaf => Left == null && Right == null;

        public Position[] Positions { get; }
        public DecisionNode Left { get; }
        public DecisionNode Right { get; }

        public DecisionNode(Position[] positions)
            : this(positions, null, null)
        {
        }

        public DecisionNode(Position[] positions, DecisionNode left, DecisionNode right)
        {
            this.Positions = positions;
            this.Left = left;
            this.Right = right;
        }

        public static DecisionNode Create(Position[] positions)
        {
            DecisionNode node = null;
            var items = new List<Position>();

            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i] is SentinelPosition sentinel)
                {
                    var condition = Create(sentinel.Lookahead);
                    var target = Create(sentinel.Next);

                    node = sentinel.Positive
                        ? Merge(node, OnTrue(condition, target))
                        : Merge(node, OnFalse(condition, target));
                }
                else
                {
                    items.Add(positions[i]);
                }
            }

            return Merge(node, items.ToArray());
        }

        public static DecisionNode OnFalse(DecisionNode condition, DecisionNode target)
        {
            if (condition == null)
            {
                return target;
            }

            if (condition.IsLeaf)
            {
                return new DecisionNode(condition.Positions, target, null);
            }

            return new DecisionNode(condition.Positions, OnFalse(condition.Left, target), OnFalse(condition.Right, target));
        }

        public static DecisionNode OnTrue(DecisionNode condition, DecisionNode target)
        {
            if (condition == null)
            {
                return null;
            }

            if (condition.IsLeaf)
            {
                return new DecisionNode(condition.Positions, null, target);
            }

            return new DecisionNode(condition.Positions, OnTrue(condition.Left, target), OnTrue(condition.Right, target));
        }

        public static DecisionNode Merge(DecisionNode nodeA, DecisionNode nodeB)
        {
            if (nodeA == null)
            {
                return nodeB;
            }

            if (nodeA.IsLeaf)
            {
                return Merge(nodeB, nodeA.Positions);
            }

            return new DecisionNode(nodeA.Positions, Merge(nodeA.Left, nodeB), Merge(nodeA.Right, nodeB));
        }

        public static DecisionNode Merge(DecisionNode node, Position[] positions)
        {
            if (node == null)
            {
                return new DecisionNode(positions);
            }

            if (node.IsLeaf)
            {
                return new DecisionNode(Utils.Concate(node.Positions, positions));
            }

            return new DecisionNode(node.Positions, Merge(node.Left, positions), Merge(node.Right, positions));
        }
    }
}
