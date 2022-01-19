namespace ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class TextLineCounter : LineCounter
    {
        private int lineOffset = 0;

        public int GetLine(int position)
        {
            return GetLineIndex(position) + lineOffset;
        }

        public (int, int) GetLinePosition(int position)
        {
            var line = GetLineIndex(position);
            return (line + lineOffset, position - Lines[line]);
        }

        /// <summary>
        /// Lookups for and stores all line breaks in a buffer.
        /// </summary>
        public void Accept(int bufferPosition, char[] buffer, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (buffer[offset + i] == '\n')
                {
                    // Marks a new line position as one followed by the '\n'.
                    Lines.Add(bufferPosition + i + 1);
                }
            }
        }

        /// <summary>
        /// Removes all lines above a specified position.
        /// </summary>
        public void Discard(int startPosition)
        {
            var line = GetLineIndex(startPosition);
            lineOffset += line;
            Lines.RemoveRange(0, line);
        }
    }
}
