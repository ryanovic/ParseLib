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

        protected override string GetLexeme()
        {
            return CurrentPosition > StartPosition
                ? Content.Substring(StartPosition, CurrentPosition - StartPosition)
                : null;
        }
    }
}
