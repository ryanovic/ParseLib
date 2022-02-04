namespace Ry.ParseLib.Text
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public class UnicodeRanges
    {
        public static UnicodeRange[] Empty { get; } = Array.Empty<UnicodeRange>();
        public static UnicodeRange[] All { get; } = new[] { new UnicodeRange(0, UnicodeRange.Max) };
        public static UnicodeRange[] HighSurrogate { get; } = new[] { new UnicodeRange(UnicodeRange.SurrogateStart, UnicodeRange.SurrogateLowStart - 1) };
        public static UnicodeRange[] LowSurrogate { get; } = new[] { new UnicodeRange(UnicodeRange.SurrogateLowStart, UnicodeRange.SurrogateEnd) };
        public static UnicodeRange[] Surrogate { get; } = new[] { new UnicodeRange(UnicodeRange.SurrogateStart, UnicodeRange.SurrogateEnd) };

        public static UnicodeRange[] Negate(UnicodeRange[] a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (a == Empty) return All;
            if (a == All) return Empty;

            int from = 0, to;
            var c = new List<UnicodeRange>();

            for (int i = 0; i < a.Length; i++)
            {
                to = a[i].From - 1;

                if (to == UnicodeRange.SurrogateEnd)
                {
                    to = UnicodeRange.SurrogateStart - 1;
                }

                if (from <= to)
                {
                    c.Add(new UnicodeRange(from, to));
                }

                from = a[i].To + 1;

                if (from == UnicodeRange.SurrogateStart)
                {
                    from = UnicodeRange.SurrogateEnd + 1;
                }
            }

            if (from <= UnicodeRange.Max)
            {
                c.Add(new UnicodeRange(from, UnicodeRange.Max));
            }

            return c.ToArray();
        }

        public static UnicodeRange[] Union(UnicodeRange[] a, UnicodeRange[] b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));

            if (a == Empty) return b;
            if (b == Empty) return a;
            if (a == All || b == All) return All;

            int i = 0, j = 0, from = -1, to = 0;
            var c = new List<UnicodeRange>();

            while (true)
            {
                if (from == -1)
                {
                    if (i < a.Length && (j == b.Length || a[i].From < b[j].From))
                    {
                        (from, to) = a[i++].Range;
                    }
                    else if (j < b.Length)
                    {
                        (from, to) = b[j++].Range;
                    }
                    else
                    {
                        break;
                    }
                }

                if (i < a.Length && a[i].To >= from - 1 && a[i].From <= to + 1)
                {
                    to = Math.Max(to, a[i++].To);
                }
                else if (j < b.Length && b[j].To >= from - 1 && b[j].From <= to + 1)
                {
                    to = Math.Max(to, b[j++].To);
                }
                else
                {
                    c.Add(new UnicodeRange(from, to));
                    from = -1;
                }
            }

            return c.ToArray();
        }

        public static UnicodeRange[] Intersect(UnicodeRange[] a, UnicodeRange[] b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));

            if (a == Empty || b == Empty) return Empty;
            if (a == All) return b;
            if (b == All) return a;

            int i = 0, j = 0;
            var c = new List<UnicodeRange>();

            while (i < a.Length && j < b.Length)
            {
                if (a[i].To < b[j].From)
                {
                    i++;
                }
                else if (a[i].From > b[j].To)
                {
                    j++;
                }
                else
                {
                    var from = Math.Max(a[i].From, b[j].From);

                    if (a[i].To > b[j].To)
                    {
                        c.Add(new UnicodeRange(from, b[j++].To));
                    }
                    else
                    {
                        c.Add(new UnicodeRange(from, a[i++].To));
                    }
                }
            }

            return c.Count == 0 ? Empty : c.ToArray();
        }

        public static UnicodeRange[] Subtract(UnicodeRange[] a, UnicodeRange[] b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));

            if (b == Empty || a == Empty) return a;

            int i = 0, j = 0, from = 0, to;
            var c = new List<UnicodeRange>();

            while (i < a.Length)
            {
                if (j == b.Length || a[i].To < b[j].From)
                {
                    if (from <= a[i].To)
                    {
                        c.Add(new UnicodeRange(Math.Max(from, a[i].From), a[i].To));
                    }

                    i++;
                }
                else if (a[i].From > b[j].To)
                {
                    j++;
                }
                else
                {
                    if (a[i].From < b[j].From)
                    {
                        to = b[j].From - 1;

                        if (to == UnicodeRange.SurrogateEnd)
                        {
                            to = UnicodeRange.SurrogateStart - 1;
                        }

                        if (from <= to)
                        {
                            c.Add(new UnicodeRange(Math.Max(from, a[i].From), to));
                        }
                    }

                    from = b[j++].To + 1;

                    if (from == UnicodeRange.SurrogateStart)
                    {
                        from = UnicodeRange.SurrogateEnd + 1;
                    }
                }
            }

            return c.Count == 0 ? Empty : c.ToArray();
        }

        public static UnicodeRange[] ToAnyCase(UnicodeRange[] a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (a == Empty || a == All) return a;

            var builder = new CharSetBuilder();
            int i = 0, j = 0;

            while (i < a.Length && j < AnyCase.Ranges.Length)
            {
                if (AnyCase.Ranges[j].From > a[i].To)
                {
                    i++;
                }
                else if (a[i].From > AnyCase.Ranges[j].To)
                {
                    j = a[i].From < UnicodeRange.Latin1Max
                        ? j + 1
                        : FindAnyCase(a[i].From, j + 1);
                }
                else
                {
                    int from = Math.Max(a[i].From, AnyCase.Ranges[j].From);
                    int to = Math.Min(a[i].To, AnyCase.Ranges[j].To);

                    var range = AnyCase.Ranges[j].Transfrom(from, to);

                    if (range.From < a[i].From || range.To > a[i].To)
                    {
                        builder.Add(range);
                    }

                    if (a[i].To <= to)
                    {
                        i++;
                    }

                    if (AnyCase.Ranges[j].To <= to)
                    {
                        j++;
                    }
                }
            }

            return Union(a, builder.CreateRanges());
        }

        private static int FindAnyCase(int code, int start)
        {
            int high = AnyCase.Ranges.Length - 1;

            while (start <= high)
            {
                int mid = (start + high) / 2;

                if (AnyCase.Ranges[mid].From > code)
                {
                    high = mid - 1;
                }
                else if (AnyCase.Ranges[mid].To < code)
                {
                    start = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            return start;
        }
    }
}
