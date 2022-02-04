namespace Ry.ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;

    public sealed class LineCounter
    {
        /// <summary>
        /// Gets a list of positions representing the beginning of a line.
        /// </summary>
        private readonly List<int> lines;
        private int offset = 0;

        public LineCounter()
        {
            lines = new List<int> { 0 };
        }

        public int GetLine(int position)
        {
            return FindLineIndex(position) + offset;
        }

        public (int, int) GetLinePosition(int position)
        {
            var line = FindLineIndex(position);
            return (line + offset, position - lines[line]);
        }

        /// <summary>
        /// Lookups for and stores all line breaks in a buffer.
        /// </summary>
        public void Accept(int bufferPosition, ReadOnlySpan<char> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == '\n')
                {
                    lines.Add(bufferPosition + i + 1);
                }
            }
        }

        /// <summary>
        /// Removes all lines above a specified position.
        /// </summary>
        public void Discard(int startPosition)
        {
            var line = FindLineIndex(startPosition);
            offset += line;
            lines.RemoveRange(0, line);
        }

        /// <summary>
        /// Returns an index of a row corresponding to a specified position.
        /// </summary>
        private int FindLineIndex(int position)
        {
            if (position < lines[0])
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            int lo = 0, hi = lines.Count - 1;

            while (lo <= hi)
            {
                var m = (lo + hi) / 2;

                if (position >= lines[m])
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
