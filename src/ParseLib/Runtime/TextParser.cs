namespace ParseLib.Runtime
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public abstract class TextParser : ParserBase
    {
        private const int DefaultBufferSize = 1024;

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
        }

        public override void Parse()
        {
            try
            {
                int read = Reader.Read(Buffer, 0, Buffer.Length), offset = 0;

                while (read > 0)
                {
                    Read(BufferPosition, Buffer, 0, offset + read, endOfSource: false);
                    offset = ShfitBuffer();
                    read = Reader.Read(Buffer, offset, Buffer.Length - offset);
                }

                Read(BufferPosition, Buffer, 0, offset, endOfSource: true);
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

                while (read > 0)
                {
                    Read(BufferPosition, Buffer, 0, offset + read, endOfSource: false);
                    offset = ShfitBuffer();
                    read = await Reader.ReadAsync(Buffer, offset, Buffer.Length - offset);
                }

                Read(BufferPosition, Buffer, 0, offset, endOfSource: true);
            }
            catch (SystemException ex)
            {
                throw CreateParserException(ex);
            }
        }

        protected abstract bool Read(int bufferPosition, char[] buffer, int offset, int length, bool endOfSource);

        protected override string GetLexeme()
        {
            return CurrentPosition > StartPosition
                ? new String(Buffer, StartPosition - BufferPosition, CurrentPosition - StartPosition)
                : null;
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
