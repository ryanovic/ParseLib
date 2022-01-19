namespace ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;

    internal abstract class LineCounter
    {
        /// <summary>
        /// Gets a list of positions representing the beginning of a line.
        /// </summary>
        protected List<int> Lines { get; }

        public LineCounter()
        {
            Lines = new List<int> { 0 };
        }

        /// <summary>
        /// Returns an index of a row corresponding to a specified position.
        /// </summary>
        protected int GetLineIndex(int position)
        {
            if (position < Lines[0])
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            int lo = 0, hi = Lines.Count - 1;

            while (lo <= hi)
            {
                var m = (lo + hi) / 2;

                if (position >= Lines[m])
                {
                    lo = m + 1;
                }
                else
                {
                    hi = m - 1;
                }
            }

            return hi;
        }
    }
}
