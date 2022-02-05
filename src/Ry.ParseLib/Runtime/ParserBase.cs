namespace Ry.ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents basic operations for a target parser.
    /// </summary>
    public abstract class ParserBase
    {
        /// <summary>
        /// Gets the position in the source where a pending token is started.
        /// </summary>
        /// <remarks>The property is implemented by a target parser generator.</remarks>
        protected abstract int StartPosition { get; }

        /// <summary>
        /// Gets the current position in the source.
        /// </summary>
        /// <remarks>The property is implemented by a target parser generator.</remarks>
        protected abstract int CurrentPosition { get; }

        /// <summary>
        /// Gets length of the current lexeme.
        /// </summary>
        protected int Length => CurrentPosition - StartPosition;

        /// <summary>
        /// Reads the source and generates output according to the parser's configuration.
        /// </summary>
        public abstract void Parse();

        /// <summary>
        /// Reads the source asynchronously and generates output according to the parser's configuration.
        /// </summary>
        public virtual Task ParseAsync()
        {
            Parse();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns a top value from the data stack. The parser must be in the completed state.
        /// </summary>
        public abstract object GetResult();

        /// <summary>
        /// Gets a row and column correspoinding to a specififed position in a source.
        /// </summary>
        protected virtual (int, int) GetLinePosition(int position)
        {
            return (0, position);
        }

        protected virtual void PopulateExceptionDetails(ParserException exception, int position)
        {
            (var row, var col) = GetLinePosition(position);
            exception.Line = row;
            exception.Position = col;
            exception.Lexeme = GetValue();
            exception.ParserState = GetParserState();
        }

        protected virtual ParserException CreateParserException(string message)
        {
            return CreateParserException(message, CurrentPosition);
        }

        protected virtual ParserException CreateParserException(string message, int position)
        {
            var error = new ParserException(message);
            PopulateExceptionDetails(error, position);
            return error;
        }

        protected virtual ParserException CreateParserException(Exception innerException)
        {
            return CreateParserException(innerException, CurrentPosition);
        }

        protected virtual ParserException CreateParserException(Exception innerException, int position)
        {
            var error = new ParserException(Errors.ExceptionOccurred(), innerException);
            PopulateExceptionDetails(error, position);
            return error;
        }

        /// <summary>
        /// Gets a string represeting all grammar symbols are recognized by the parser. 
        /// </summary>
        /// <remarks>The method is implemented by a target parser generator.</remarks>
        protected abstract string GetParserState();

        /// <summary>
        /// Gets a part of the source which represents a pending terminal value and corresponds to the <see cref="StartPosition"/> and <see cref="CurrentPosition"/> properties.
        /// </summary>
        protected abstract string GetValue();
    }
}
