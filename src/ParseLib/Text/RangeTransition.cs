using System;
using System.Collections.Generic;
using System.Text;

namespace ParseLib.Text
{
    public sealed class RangeTransition
    {
        public UnicodeRange Range { get; }
        public CategoryTransition[] Categories { get; }
        public LexicalState Default { get; }


        public RangeTransition(UnicodeRange range, CategoryTransition[] categories, LexicalState state)
        {
            this.Range = range;
            this.Categories = categories;
            this.Default = state;
        }

        public override string ToString()
        {
            return Default == null
                 ? $"{Range} => null"
                 : $"{Range} => {Default}";
        }
    }
}
