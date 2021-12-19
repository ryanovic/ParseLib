namespace ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class ParserBase
    {
        public abstract int StartPosition { get; }
        public abstract int CurrentPosition { get; }
        public abstract bool IsCompleted { get; }

        public abstract void Parse();

        public virtual Task ParseAsync()
        {
            Parse();
            return Task.CompletedTask;
        }

        public virtual object GetResult()
        {
            if (!IsCompleted)
            {
                throw new InvalidOperationException("Can't get the result until source is not entirely processed.");
            }

            return GetTopValue();
        }

        protected virtual void PopulateExceptionDetails(ParserException exception)
        {
            exception.Position = CurrentPosition;
            exception.Lexeme = GetLexeme();
            exception.ParserState = GetParserState();
        }

        protected virtual ParserException CreateParserException(string message)
        {
            var error = new ParserException(message);
            PopulateExceptionDetails(error);
            return error;
        }

        protected virtual ParserException CreateParserException(Exception innerException)
        {
            var error = new ParserException("Exception has occurred.", innerException);
            PopulateExceptionDetails(error);
            return error;
        }

        protected abstract object GetTopValue();
        protected abstract string GetParserState();
        protected abstract string GetLexeme();
    }
}
