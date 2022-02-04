namespace Ry.ParseLib.Text
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a set of characters and Unicode categoies.
    /// </summary>
    public sealed class CharSet
    {
        /// <summary>
        /// Gets a set representing any Unicode character.
        /// </summary>
        public static CharSet Any { get; } = new CharSet(UnicodeRanges.All);

        /// <summary>
        /// Gets a set of defined Unicode categories.
        /// </summary>
        public UnicodeCategories Categories { get; }

        // Get an ordered disjoint collection of Unicode ranges.
        public UnicodeRange[] Ranges { get; }

        // Gets a set of characters and Unicode categories to exclude from consideration. 
        public CharSet Except { get; }

        // Generates a character set that matches to the specified pattern.
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

        /// <summary>
        /// Generates a character set consistsing of any characters except ones from the current set.
        /// </summary>
        public CharSet Negate() => new CharSet(UnicodeRanges.All, except: this);

        /// <summary>
        /// Generates a character set extending the current set whith <c>toUpper</c> and <c>toLower</c> variants.
        /// </summary>
        /// <returns></returns>
        public CharSet ToAnyCase() => new CharSet(UnicodeRanges.ToAnyCase(Ranges), Categories.ToAnyCase(), Except?.ToAnyCase());
    }
}