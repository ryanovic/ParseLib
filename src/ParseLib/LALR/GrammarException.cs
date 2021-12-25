namespace ParseLib.LALR
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class GrammarException : Exception
    {
        public string Symbol { get; }
        public string[] Productions { get; }

        public GrammarException()
        {
        }

        public GrammarException(string message) : base(message)
        {
        }

        public GrammarException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public GrammarException(string message, Symbol symbol, params Production[] productions) : base(message)
        {
            if (symbol != null)
            {
                this.Symbol = symbol.Name;
            }

            if (productions != null && productions.Length > 0)
            {
                this.Productions = Utils.Transform(productions, x => x.Name);
            }
        }

        protected GrammarException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.Symbol = info.GetString(nameof(Symbol));
            this.Productions = (string[])info.GetValue(nameof(Productions), typeof(string[]));
        }

        public override string Message
        {
            get
            {
                var msg = base.Message;

                if (!String.IsNullOrEmpty(msg))
                {
                    msg += Environment.NewLine + "Symbol: " + Symbol;
                }

                if (Productions != null && Productions.Length == 1)
                {
                    msg += Environment.NewLine + "Production: " + Productions[0];
                }

                if (Productions != null && Productions.Length > 1)
                {
                    msg += Environment.NewLine + "Productions: "
                        + Environment.NewLine + String.Join(Environment.NewLine, Productions);
                }

                return msg;
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Symbol), Symbol);
            info.AddValue(nameof(Productions), Productions);
        }
    }
}
