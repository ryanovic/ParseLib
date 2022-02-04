namespace ParseLib.Runtime
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public abstract class TextParser : ParserBase
    {
        private const int DefaultBufferSize = 1024;

        private readonly TextLineCounter lines;

        protected int BufferPosition { get; private set; }
        protected char[] Buffer { get; private set; }
        protected TextReader Reader { get; }

        public TextParser(TextReader reader)
            : this(reader, DefaultBufferSize)
        {
        }

        public TextParser(TextReader reader, int bufferSize)
        {
            this.Buffer = new char[bufferSize];
            this.Reader = reader;
            this.lines = new TextLineCounter();
        }

        public override void Parse()
        {
            try
            {
                int read = Reader.Read(Buffer, 0, Buffer.Length), offset = 0;
                lines.Accept(0, Buffer, 0, read);

                while (read > 0)
                {
                    Read(BufferPosition, Buffer.AsSpan(0, offset + read), isFinal: false);
                    offset = ShfitBuffer();
                    read = Reader.Read(Buffer, offset, Buffer.Length - offset);
                    CollectLinePositions(offset, read);
                }

                Read(BufferPosition, Buffer.AsSpan(0, offset), isFinal: true);
            }
            catch (SystemException ex)
            {
                throw CreateParserException(ex);
            }
        }

        public override async Task ParseAsync()
        {
            try
            {
                int read = await Reader.ReadAsync(Buffer, 0, Buffer.Length), offset = 0;
                lines.Accept(0, Buffer, 0, read);

                while (read > 0)
                {
                    Read(BufferPosition, Buffer.AsSpan(0, offset + read), isFinal: false);
                    offset = ShfitBuffer();
                    read = await Reader.ReadAsync(Buffer, offset, Buffer.Length - offset);
                    CollectLinePositions(offset, read);
                }

                Read(BufferPosition, Buffer.AsSpan(0, offset), isFinal: true);
            }
            catch (SystemException ex)
            {
                throw CreateParserException(ex);
            }
        }

        /// <summary>
        /// Processes text from a specified buffer.
        /// </summary>
        /// <param name="bufferPosition">The source position corresponding to the beginning of the buffer.</param>
        /// <param name="buffer">The data buffer containing a sequence of pending character codes.</param>
        /// <param name="offset">The buffer start index.</param>
        /// <param name="length">The buffer length.</param>
        /// <param name="isFinal">The value indicating whether the source is completed indicating the buffer is a last data chunk in a row.</param>
        /// <returns><c>true</c> if the buffer is entirelly read or <c>false</c> if a current position is restored before the buffer start.</returns>
        /// <remarks>The method is implemented by a sequential parser generator.</remarks>
        protected abstract bool Read(int bufferPosition, ReadOnlySpan<char> buffer, bool isFinal);

        protected override (int, int) GetLinePosition(int position)
        {
            return lines.GetLinePosition(position);
        }

        protected string GetLexeme(int trimLeft, int trimRight)
        {
            var start = StartPosition + trimLeft;
            var end = CurrentPosition - trimRight;

            return start < end
                ? new string(Buffer, start - BufferPosition, end - start)
                : null;
        }

        protected string GetLexeme(int trim) => GetLexeme(trim, trim);

        protected override string GetLexeme() => GetLexeme(0, 0);

        private void CollectLinePositions(int offset, int read)
        {
            if (read > 0)
            {
                lines.Discard(StartPosition);
                lines.Accept(BufferPosition + offset, Buffer, offset, read);
            }
        }

        private int ShfitBuffer()
        {
            var currentLength = CurrentPosition - StartPosition;

            if (2 * currentLength > Buffer.Length)
            {
                var tmp = new char[Buffer.Length * 2];
                Array.Copy(Buffer, StartPosition - BufferPosition, tmp, 0, currentLength);
                Buffer = tmp;
            }
            else if (currentLength > 0)
            {
                Array.Copy(Buffer, StartPosition - BufferPosition, Buffer, 0, currentLength);
            }

            BufferPosition = StartPosition;
            return currentLength;
        }
    }
}
