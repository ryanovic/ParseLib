namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

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
