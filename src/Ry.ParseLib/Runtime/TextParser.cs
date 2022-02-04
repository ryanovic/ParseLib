namespace Ry.ParseLib.Runtime
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Threading.Tasks;

    public abstract class TextParser : ParserBase
    {
        private const int DefaultBufferSize = 4096;

        private readonly TextReader reader;
        private readonly LineCounter lines;
        private char[] buffer;
        private int bufferPosition;

        public TextParser(TextReader reader)
            : this(reader, DefaultBufferSize)
        {
        }

        public TextParser(TextReader reader, int bufferSize)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (bufferSize < 1) throw new ArgumentOutOfRangeException(nameof(bufferSize));

            this.reader = reader;
            this.buffer = new char[bufferSize];
            this.lines = new LineCounter();
        }

        public override void Parse()
        {
            try
            {
                int read = reader.Read(buffer, 0, buffer.Length), offset = 0;

                while (read > 0)
                {
                    offset = ProcessBuffer(buffer.AsSpan(0, offset + read));
                    read = reader.Read(buffer, offset, buffer.Length - offset);
                }

                Read(bufferPosition, buffer.AsSpan(0, offset), isFinal: true);
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
                int read = await reader.ReadAsync(buffer, 0, buffer.Length), offset = 0;

                while (read > 0)
                {
                    offset = ProcessBuffer(buffer.AsSpan(0, offset + read));
                    read = await reader.ReadAsync(buffer, offset, buffer.Length - offset);
                }

                Read(bufferPosition, buffer.AsSpan(0, offset), isFinal: true);
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

        protected string GetValue(int start, int count)
        {
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0 || count > Length - start) throw new ArgumentOutOfRangeException(nameof(count));

            return count == 0 ? null : new string(buffer, StartPosition - bufferPosition + start, count);
        }

        protected string GetValue(int start) => GetValue(start, Length - start);

        protected override string GetValue() => GetValue(0, Length);

        protected ReadOnlySpan<char> GetSpan(int start, int count)
        {
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0 || count > Length - start) throw new ArgumentOutOfRangeException(nameof(count));

            return count == 0 ? null : buffer.AsSpan(StartPosition - bufferPosition + start, count);
        }

        protected ReadOnlySpan<char> GetSpan(int start) => GetSpan(start, Length - start);

        protected ReadOnlySpan<char> GetSpan() => GetSpan(0, Length);

        private int ProcessBuffer(ReadOnlySpan<char> span)
        {
            lines.Accept(bufferPosition, span);
            Read(bufferPosition, span, isFinal: false);
            ShfitBuffer();
            lines.Discard(bufferPosition);
            return CurrentPosition - StartPosition;
        }

        private void ShfitBuffer()
        {
            var currentLength = CurrentPosition - StartPosition;

            if (2 * currentLength > buffer.Length)
            {
                var tmp = new char[buffer.Length * 2];
                Array.Copy(buffer, StartPosition - bufferPosition, tmp, 0, currentLength);
                buffer = tmp;
            }
            else if (currentLength > 0)
            {
                Array.Copy(buffer, StartPosition - bufferPosition, buffer, 0, currentLength);
            }

            bufferPosition = StartPosition;
        }
    }
}
