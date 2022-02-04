namespace Ry.ParseLib.Runtime
{
    using System;

    /// <summary>
    /// Represents the basis for a string parser.
    /// </summary>
    public abstract class StringParser : ParserBase
    {
        private readonly LineCounter lines;
        protected string Content { get; }

        public StringParser(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            this.lines = new LineCounter();
            this.Content = content;
        }

        public override void Parse()
        {
            try
            {
                var span = Content.AsSpan();
                lines.Accept(0, span);
                Read(span);
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

        protected string GetValue(int start, int count)
        {
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0 || count > Length - start) throw new ArgumentOutOfRangeException(nameof(count));

            return count == 0 ? null : Content.Substring(StartPosition + start, count);
        }

        protected string GetValue(int start) => GetValue(start, Length - start);

        protected override string GetValue() => GetValue(0, Length);

        protected ReadOnlySpan<char> GetSpan(int start, int count)
        {
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0 || count > Length - start) throw new ArgumentOutOfRangeException(nameof(count));

            return count == 0 ? null : Content.AsSpan(StartPosition + start, count);
        }

        protected ReadOnlySpan<char> GetSpan(int start) => GetSpan(start, Length - start);

        protected ReadOnlySpan<char> GetSpan() => GetSpan(0, Length);
    }
}
