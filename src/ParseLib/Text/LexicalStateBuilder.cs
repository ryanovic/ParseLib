namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Represents a builder for a specific lexical state.
    /// </summary>
    internal sealed class LexicalStateBuilder
    {
        private LexicalState defaultState;

        // pending range
        private UnicodeRange range;

        // completed ranges
        private List<RangeTransition> ranges = new List<RangeTransition>();
        private List<RangeTransition> surrogateRanges = new List<RangeTransition>();

        // pending categories by state
        private List<LexicalState> states = new List<LexicalState>();
        private List<UnicodeCategories> categories = new List<UnicodeCategories>();

        public void SetDeafult(LexicalState state)
        {
            this.defaultState = state;
        }

        public void AddRange(UnicodeRange range, LexicalState state)
        {
            BeginRange(range, state);
            CompleteRange();
        }

        public void BeginRange(UnicodeRange range, LexicalState state)
        {
            this.range = range;
            this.defaultState = state;
        }

        public void AddCategory(UnicodeCategory uc, LexicalState state)
        {
            AddCategory(UnicodeCategories.Create(uc), state);
        }

        public void AddCategory(UnicodeCategories ucs, LexicalState state)
        {
            if (state != defaultState)
            {
                for (int i = 0; i < states.Count; i++)
                {
                    if (states[i] == state)
                    {
                        categories[i] = categories[i].Union(ucs);
                        return;
                    }
                }

                states.Add(state);
                categories.Add(ucs);
            }
        }

        public void CompleteRange()
        {
            var categories = CompleteCategories();

            if (range.To < UnicodeRange.ExtendedStart)
            {
                ranges.Add(new RangeTransition(range, categories, defaultState));
            }
            else if (range.From >= UnicodeRange.ExtendedStart)
            {
                surrogateRanges.Add(new RangeTransition(range, categories, defaultState));
            }
            else
            {
                ranges.Add(new RangeTransition(new UnicodeRange(range.From, UnicodeRange.ExtendedStart - 1), categories, defaultState));
                surrogateRanges.Add(new RangeTransition(new UnicodeRange(UnicodeRange.ExtendedStart, range.To), categories, defaultState));
            }
        }

        public void CompleteState(LexicalState state, ref LexicalState surrogate)
        {
            surrogate.Ranges = surrogateRanges.ToArray();
            surrogate.Categories = CompleteCategories();
            surrogate.Default = defaultState;
            state.Categories = surrogate.Categories;
            state.Default = defaultState;

            if (CheckIfSurrogateLeadsToDeadState(surrogate))
            {
                // There is no transition from the surrogate, so can be safely removed
                state.Ranges = new RangeTransition[ranges.Count - 1];

                for (int i = 0, j = 0; i < ranges.Count; i++)
                {
                    if (ranges[i].Range.From != UnicodeRange.SurrogateStart)
                    {
                        state.Ranges[j++] = ranges[i];
                    }
                }

                surrogate = null;
            }
            else
            {
                state.Ranges = ranges.ToArray();
            }

            ranges.Clear();
            surrogateRanges.Clear();
            defaultState = null;
        }

        private CategoryTransition[] CompleteCategories()
        {
            if (states.Count == 0)
            {
                return Array.Empty<CategoryTransition>();
            }

            var statesByCat = new CategoryTransition[states.Count];

            for (int i = 0; i < states.Count; i++)
            {
                statesByCat[i] = new CategoryTransition(categories[i], states[i]);
            }

            states.Clear();
            categories.Clear();
            return statesByCat;
        }

        private static bool CheckIfSurrogateLeadsToDeadState(LexicalState surrogate)
        {
            foreach (var range in surrogate.Ranges)
            {
                if (range.Default != null || range.Categories.Any(c => c.State != null))
                {
                    return false;
                }
            }

            // Entire extended unicode plane is full covered and produce no state.
            if (surrogate.Ranges.Length == 1
                && surrogate.Ranges[0].Range.From == UnicodeRange.ExtendedStart
                && surrogate.Ranges[0].Range.To == UnicodeRange.Max)
            {
                return true;
            }

            return surrogate.Default == null && surrogate.Categories.All(c => c.State == null);
        }
    }
}
