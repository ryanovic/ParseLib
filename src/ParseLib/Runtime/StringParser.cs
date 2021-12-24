namespace ParseLib.Runtime
{
    using System;

    public abstract class StringParser : ParserBase
    {
        protected string Content { get; }

        public StringParser(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            this.Content = content;
        }

        public override void Parse()
        {
            try
            {
                Read(Content, 0, Content.Length);
            }
            catch (SystemException ex)
            {
                throw CreateParserException(ex);
            }
        }

        protected abstract void Read(string content, int offset, int length);

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
