namespace ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class StringLineCounter : LineCounter
    {
        public int GetLine(int position)
        {
            return GetLineIndex(position);
        }

        public (int, int) GetLinePosition(int position)
        {
            var line = GetLineIndex(position);
            return (line, position - Lines[line]);
        }

        /// <summary>
        /// Lookups for and stores all line breaks in a string.
        /// </summary>
        public void Accept(string content)
        {
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    // Marks a new line position as one followed by the '\n'.
                    Lines.Add(i + 1);
                }
            }
        }
    }
}
