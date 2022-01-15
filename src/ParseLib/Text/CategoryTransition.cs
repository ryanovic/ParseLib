namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a transition to a state defined by a set of Unicode categories.
    /// </summary>
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
