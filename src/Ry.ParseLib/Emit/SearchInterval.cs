namespace Ry.ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the interval of ordered indexes for a binary search.
    /// </summary>
    internal sealed class SearchInterval
    {
        public int Low { get; }
        public int Middle { get; }
        public int High { get; }

        /// <summary>
        /// Gets or sets the label for the interval.
        /// </summary>
        public Label Label { get; set; }

        /// <summary>
        /// Gets or sets the link to the [Low, Middle) interval.
        /// </summary>
        public Label Left { get; set; }

        /// <summary>
        /// Gets or sets the link to the (Middle, High] interval.
        /// </summary>
        public Label Right { get; set; }

        public bool IsSingle => Low == High;
        public int Count => High - Low + 1;

        public SearchInterval(int low, int high)
        {
            this.Low = low;
            this.High = high;
            this.Middle = (low + high) / 2;
        }

        /// <summary>
        /// Generates binary search intervals for the [0 .. <paramref name="count"/>) range.
        /// </summary>
        /// <returns>
        /// The list of intervals and the default label which is used to refer to a position outside the valid boundary.
        /// </returns>
        public static (IList<SearchInterval>, Label) CreateIntervals(ILGenerator il, int count)
        {
            var intervals = new List<SearchInterval>(count);
            var defaultLabel = il.DefineLabel();

            if (count > 0)
            {
                CreateIntervals(il, intervals, defaultLabel, 0, count - 1);
            }

            return (intervals, defaultLabel);
        }

        private static Label CreateIntervals(ILGenerator il, List<SearchInterval> intervals, Label defaultLabel, int low, int high)
        {
            var interval = new SearchInterval(low, high);
            interval.Label = il.DefineLabel();
            intervals.Add(interval);

            interval.Left = interval.Middle > low
                ? CreateIntervals(il, intervals, defaultLabel, low, interval.Middle - 1)
                : defaultLabel;

            interval.Right = interval.Middle < high
                ? CreateIntervals(il, intervals, defaultLabel, interval.Middle + 1, high)
                : defaultLabel;

            return interval.Label;
        }
    }
}

