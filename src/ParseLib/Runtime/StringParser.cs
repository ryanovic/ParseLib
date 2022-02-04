namespace ParseLib.Runtime
{
    using System;

    /// <summary>
    /// Represents the basis for a string parser.
    /// </summary>
    public abstract class StringParser : ParserBase
    {
        private readonly StringLineCounter lines;

        protected string Content { get; }

        public StringParser(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            this.lines = new StringLineCounter();
            this.Content = content;
        }

        public override void Parse()
        {
            try
            {
                lines.Accept(Content);
                Read(Content.AsSpan());
            }
            catch (SystemException ex)
            {
                throw CreateParserException(ex);
            }
        }

        /// <summary>
        /// Processes a defined string value.
        /// </summary>
        /// <remarks>The method is implemented by a string parser generator.</remarks>
        protected abstract void Read(ReadOnlySpan<char> buffer);

        protected override (int, int) GetLinePosition(int position)
        {
            return lines.GetLinePosition(position);
        }

        protected string GetLexeme(int trimLeft, int trimRight)
        {
            var start = StartPosition + trimLeft;
            var end = CurrentPosition - trimRight;

            return start < end
                ? Content.Substring(start, end - start)
                : null;
        }

        protected string GetLexeme(int trim) => GetLexeme(trim, trim);

        protected override string GetLexeme() => GetLexeme(0, 0);
    }
}
