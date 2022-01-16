namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a position that matches a single character in a source.
    /// </summary>
    internal sealed class TextPosition : Position
    {
        public CharSet CharSet { get; }

        public TextPosition(int tokenId, CharSet charSet)
            : base(tokenId)
        {
            if (charSet == null) throw new ArgumentNullException(nameof(charSet));

            this.CharSet = charSet;
        }
    }
}
