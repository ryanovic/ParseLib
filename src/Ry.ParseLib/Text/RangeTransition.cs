namespace Ry.ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a transition to a state defined by a Unicode range.
    /// </summary>
    public sealed class RangeTransition
    {
        /// <summary>
        /// Gets a range that defines the transition.
        /// </summary>
        public UnicodeRange Range { get; }

        /// <summary>
        /// Gets a collection of transitions defined by Unicode categories which overrides the default transition once matched.
        /// </summary>
        public CategoryTransition[] Categories { get; }

        /// <summary>
        /// Gets a default transition for the range.
        /// </summary>
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
