namespace ParseLib.Text
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Disjoint ordered set of unicode ranges united with unicode categories.
    /// </summary>
    public sealed class CharSet
    {
        public static CharSet Any { get; } = new CharSet(UnicodeRanges.All);

        public UnicodeCategories Categories { get; }
        public UnicodeRange[] Ranges { get; }
        public CharSet Except { get; }

        public static CharSet Parse(string pattern) => new CharSetParser(pattern).Parse();

        public CharSet(UnicodeRange[] ranges, CharSet except = null)
            : this(ranges, UnicodeCategories.Empty, except)
        {
        }

        public CharSet(UnicodeRange[] ranges, UnicodeCategories categories, CharSet except = null)
        {
            if (ranges == null) throw new ArgumentNullException(nameof(ranges));

            this.Ranges = ranges;
            this.Categories = categories;
            this.Except = except;
        }

        public CharSet Negate() => new CharSet(UnicodeRanges.All, except: this);

        public CharSet ToAnyCase() => new CharSet(UnicodeRanges.ToAnyCase(Ranges), Categories.ToAnyCase(), Except?.ToAnyCase());
    }
}