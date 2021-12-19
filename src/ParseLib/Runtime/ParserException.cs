namespace ParseLib.Runtime
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ParserException : Exception
    {
        public int Position { get; set; }
        public string Lexeme { get; set; }
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
