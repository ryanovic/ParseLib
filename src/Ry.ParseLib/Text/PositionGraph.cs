namespace Ry.ParseLib.Text
{
    using System;

    internal readonly struct PositionGraph
    {
        public Position[] First { get; }
        public Position[] Last { get; }

        public PositionGraph(Position[] first, Position[] last)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (last == null) throw new ArgumentNullException(nameof(last));

            this.First = first;
            this.Last = last;
        }
    }
}
