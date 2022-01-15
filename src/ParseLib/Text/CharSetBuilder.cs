namespace ParseLib.Text
{
    using System;
    using System.Globalization;
    using System.Collections.Generic;

    /// <summary>
    /// Generates a user defined character set.
    /// </summary>
    public sealed class CharSetBuilder
    {
        private List<UnicodeRange> ranges = new List<UnicodeRange>();
        private int categories = 0;

        public void Add(int code)
        {
            ranges.Add(new UnicodeRange(code));
        }

        public void Add(int from, int to)
        {
            ranges.Add(new UnicodeRange(from, to));
        }

        public void Add(UnicodeRange range)
        {
            ranges.Add(range);
        }

        public void Add(UnicodeRange[] set)
        {
            ranges.AddRange(set);
        }

        public void Add(UnicodeCategory uc)
        {
            categories |= UnicodeCategories.Create(uc).Set;
        }

        public void Add(UnicodeCategories ucs)
        {
            categories |= ucs.Set;
        }

        public UnicodeRange[] CreateRanges()
        {
            return ranges.Count == 0 ? UnicodeRanges.Empty : Normalize();
        }

        public UnicodeCategories CreateCategories()
        {
            return categories == 0 ? UnicodeCategories.Empty : new UnicodeCategories(categories);
        }

        public CharSet CreateCharSet(CharSet except = null)
        {
            var cs_ranges = CreateRanges();
            var cs_categories = CreateCategories();

            if (cs_ranges == UnicodeRanges.All)
            {
                return except == null ? CharSet.Any : new CharSet(cs_ranges, except);
            }

            return new CharSet(cs_ranges, cs_categories, except);
        }

        public void Reset()
        {
            ranges.Clear();
            categories = 0;
        }

        private UnicodeRange[] Normalize()
        {
            if (ranges.Count == 0) return UnicodeRanges.Empty;

            ranges.Sort((a, b) => a.From - b.From);

            int i = 0;

            for (int j = 1; j < ranges.Count; j++)
            {
                if (ranges[j].From <= ranges[i].To + 1)
                {
                    ranges[i] = new UnicodeRange(ranges[i].From, Math.Max(ranges[i].To, ranges[j].To));
                }
                else
                {
                    ranges[++i] = ranges[j];
                }
            }

            ranges.RemoveRange(i + 1, ranges.Count - i - 1);

            if (ranges.Count == 1 && ranges[0].Equals(UnicodeRanges.All[0]))
            {
                return UnicodeRanges.All;
            }

            return ranges.ToArray();
        }
    }
}
