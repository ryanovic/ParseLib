namespace ParseLib
{
    using System;
    using ParseLib.Text;

    public sealed class Terminal : Symbol
    {
        public int Id { get; }
        internal Position[] First { get; }

        public Terminal(string lexeme, int id)
            : this(lexeme, Rex.Text(lexeme), id)
        {
        }

        public Terminal(string name, RexNode expression, int id, bool lazy = false)
            : base(name, SymbolType.Terminal)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (expression.Nullable) throw new ArgumentException("Terminal requires non-nullable regular expression to initialize.", nameof(expression));

            this.Id = id;
            this.First = expression.Complete(id, lazy);
        }
    }
}
