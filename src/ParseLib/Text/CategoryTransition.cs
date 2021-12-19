namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;

    public sealed class CategoryTransition
    {
        public UnicodeCategories Category { get; }
        public LexicalState State { get; }

        public CategoryTransition(UnicodeCategories category, LexicalState state)
        {
            this.Category = category;
            this.State = state;
        }

        public override string ToString()
        {
            return State == null
                 ? $"{Category} => null"
                 : $"{Category} => {State}";
        }
    }
}
