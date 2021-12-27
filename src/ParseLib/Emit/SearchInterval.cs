namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;
    using System.Collections.Generic;

    internal sealed class SearchInterval
    {
        public int Low { get; }
        public int Middle { get; }
        public int High { get; }

        public Label Label { get; set; }
        public Label Left { get; set; }
        public Label Right { get; set; }

        public bool IsSingle => Low == High;
        public int Count => High - Low + 1;

        public SearchInterval(int low, int high)
        {
            this.Low = low;
            this.High = high;
            this.Middle = (low + high) / 2;
        }

        public static (IList<SearchInterval>, Label) CreateIntervals(ILGenerator il, int count)
        {
            var intervals = new List<SearchInterval>();
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

