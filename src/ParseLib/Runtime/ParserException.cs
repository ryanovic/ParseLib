namespace ParseLib.Runtime
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a runtime exception that occurred during a parse operation.
    /// </summary>
    [Serializable]
    public class ParserException : Exception
    {
        /// <summary>
        /// Gets or sets a position in the source where the exception occured.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets or sets a token value that was recognized before the exception occured.
        /// </summary>
        public string Lexeme { get; set; }

        /// <summary>
        /// Gets or sets a string represeting all grammar symbols are recognized by the parser. 
        /// </summary>
        public string ParserState { get; set; }

        public ParserException()
        {
        }

        public ParserException(string message) : base(message)
        {
        }

        public ParserException(string message, Exception innerException) : base(message, innerException)
        {
        }


        protected ParserException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Position = info.GetInt32(nameof(Position));
            Lexeme = info.GetString(nameof(Lexeme));
            ParserState = info.GetString(nameof(ParserState));
        }

        public override string Message
        {
            get
            {
                var message = base.Message + Environment.NewLine + $"Position: {Position}";

                if (!String.IsNullOrEmpty(Lexeme))
                {
                    message += Environment.NewLine + $"Lexeme: {Lexeme}";
                }

                if (!String.IsNullOrEmpty(ParserState))
                {
                    message += Environment.NewLine + $"State: {ParserState}";
                }

                return message;
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Position), Position);
            info.AddValue(nameof(Lexeme), Lexeme);
            info.AddValue(nameof(ParserState), ParserState);
        }
    }
}
