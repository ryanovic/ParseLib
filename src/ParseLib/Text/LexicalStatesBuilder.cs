namespace ParseLib.Text
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Generates lexical states.
    /// </summary>
    public sealed class LexicalStatesBuilder : ILexicalStates
    {
        private bool ignoreCase;
        private List<LexicalState> states = new List<LexicalState>();

        // state initialization queue
        private Queue<LexicalStateQueueItem> queue = new Queue<LexicalStateQueueItem>();

        // internal state cache
        private Dictionary<Position, List<LexicalStateQueueItem>> stateItemsByPosition = new Dictionary<Position, List<LexicalStateQueueItem>>();

        public bool HasLookaheads { get; private set; }
        public int Count => states.Count;
        public LexicalState this[int id] => states[id];

        public LexicalStatesBuilder(bool ignoreCase = false)
        {
            this.ignoreCase = ignoreCase;
        }

        public LexicalState CreateStates(IEnumerable<Terminal> terminals)
        {
            if (terminals == null) throw new ArgumentNullException(nameof(terminals));

            var positions = new Position[terminals.Sum(x => x.First.Length)];
            var offset = 0;

            foreach (var token in terminals)
            {
                token.First.CopyTo(positions, offset);
                offset += token.First.Length;
            }

            return CreateStates(positions);
        }

        public LexicalState CreateStates(int tokenId, RexNode expression, bool lazy = false)
        {
            return CreateStates(expression.Complete(tokenId, lazy: lazy));
        }

        public IEnumerator<LexicalState> GetEnumerator()
        {
            return states.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return states.GetEnumerator();
        }

        private LexicalState CreateStates(Position[] positions)
        {
            var state = CreateState(positions);
            var builder = new LexicalStateBuilder();

            while (queue.Count > 0)
            {
                InitializeState(queue.Dequeue(), builder);
            }

            return state;
        }

        private void InitializeState(LexicalStateQueueItem item, LexicalStateBuilder builder)
        {
            var charSets = GetCharSets(item.Positions);

            if (charSets.Count == 0)
            {
                InitializeEndState(item.State);
                return;
            }

            charSets.Add(new CharSet(UnicodeRanges.Surrogate));

            var state = item.State;
            var surrogate = state.CreateSurrogate();
            var selected = new Selection(charSets.Count); // a set indicating selected charset indexes.
            var indexesByCategory = GetIndexesByCategory(charSets); // Ll -> 0, 1, 2; Nd -> 1, 3; ...
            var rangeMinPQ = new UnicodeRangeMinPQ(GetRanges(charSets, selected)); // Unicode range splitter.

            while (!rangeMinPQ.IsEmpty)
            {
                // Get the next distinct range and select related charsets.
                (var range, var indexes) = rangeMinPQ.PopMin();
                selected.Add(indexes);

                if (range.From >= UnicodeRange.SurrogateStart && range.To <= UnicodeRange.SurrogateEnd)
                {
                    // Cut out surrogate ranges except high surrogate -> surrogate state transition.
                    if (range.To == UnicodeRange.SurrogateEnd)
                    {
                        builder.AddRange(UnicodeRanges.HighSurrogate[0], surrogate);
                    }
                }
                else if (range.Length == 1)
                {
                    // Single char range. Unicode category is known at compile time.
                    var uc = (int)GetUnicodeCategory(range.From);
                    selected.Add(indexesByCategory[uc] ?? Array.Empty<int>());
                    builder.AddRange(range, CreateNextState(item.Positions, selected));
                    selected.Remove(indexesByCategory[uc] ?? Array.Empty<int>());
                }
                else
                {
                    builder.BeginRange(range, CreateNextState(item.Positions, selected));
                    CreateStatesByCategory(builder, indexesByCategory, item.Positions, selected);
                    builder.CompleteRange();
                }

                selected.Remove(indexes);
            }

            builder.SetDeafult(CreateNextState(item.Positions, selected));
            CreateStatesByCategory(builder, indexesByCategory, item.Positions, selected);
            builder.CompleteState(state, ref surrogate);
            CompleteState(state, surrogate);
        }

        private void InitializeEndState(LexicalState state)
        {
            state.Default = null;
            state.Categories = Array.Empty<CategoryTransition>();
            state.Ranges = Array.Empty<RangeTransition>();
            CompleteState(state);
        }

        private void CompleteState(LexicalState state, LexicalState surrogate)
        {
            CompleteState(state);

            if (surrogate != null)
            {
                CompleteState(surrogate);
            }
        }

        private void CompleteState(LexicalState state)
        {
            state.Id = states.Count;
            states.Add(state);
            HasLookaheads |= state.IsLookaheadStart;
        }

        private void CreateStatesByCategory(LexicalStateBuilder builder, IList<int>[] indexesList, Position[] set, Selection selected)
        {
            for (int i = 0; i < indexesList.Length; i++)
            {
                if (indexesList[i] != null)
                {
                    selected.Add(indexesList[i]);
                    builder.AddCategory((UnicodeCategory)i, CreateNextState(set, selected));
                    selected.Remove(indexesList[i]);
                }
            }
        }

        private LexicalState CreateNextState(Position[] positions, Selection selected)
        {
            int offset = 0;
            var next = new HashSet<Position>();

            foreach (var text in positions.OfType<TextPosition>())
            {
                if (IsSelected(text.CharSet, ref offset) && text.Next != null)
                {
                    next.UnionWith(text.Next);
                }
            }

            return next.Count > 0
                ? CreateState(next.ToArray())
                : null;

            bool IsSelected(CharSet charSet, ref int index) => charSet.Except == null
               ? selected.Contains(index++)
               : selected.Contains(index++) & !IsSelected(charSet.Except, ref index);

        }

        private LexicalState CreateState(Position[] positions)
        {
            return positions.Any(p => p is SentinelPosition)
                ? CreateState(DecisionNode.Create(positions))
                : CreateState(positions, null, null);
        }

        private LexicalState CreateState(DecisionNode node)
        {
            if (node == null)
            {
                return null;
            }
            else if (node.IsLeaf)
            {
                return node.Positions.Length > 0 ? CreateState(node.Positions, null, null) : null;
            }
            else if (node.Positions.Length == 0)
            {
                return CreateState(node.Left); // does not match.
            }
            else if (node.Positions.Any(p => p is AcceptPosition))
            {
                return CreateState(node.Right); // does match at start.
            }
            else
            {
                return CreateState(node.Positions, CreateState(node.Left), CreateState(node.Right));
            }
        }

        private LexicalState CreateState(Position[] positions, LexicalState onFalse, LexicalState onTrue)
        {
            positions = RemoveLazyPositions(positions);
            var state = GetState(positions, onFalse, onTrue);

            if (state == null)
            {
                var accept = GetAcceptPosition(positions);
                state = new LexicalState(accept, onFalse, onTrue);
                var item = new LexicalStateQueueItem(state, positions);
                queue.Enqueue(item);

                foreach (var pos in positions)
                {
                    if (!stateItemsByPosition.TryGetValue(pos, out var items))
                    {
                        items = new List<LexicalStateQueueItem>();
                        stateItemsByPosition.Add(pos, items);
                    }

                    items.Add(item);
                }
            }

            return state;
        }

        private LexicalState GetState(Position[] positions, LexicalState onFalse, LexicalState onTrue)
        {
            if (stateItemsByPosition.TryGetValue(positions[0], out var items))
            {
                var hash = new HashSet<Position>(positions);

                foreach (var item in items)
                {
                    if (item.State.OnFalse == onFalse
                        && item.State.OnTrue == onTrue
                        && hash.SetEquals(item.Positions))
                    {
                        return item.State;
                    }
                }
            }

            return null;
        }

        private IList<CharSet> GetCharSets(Position[] positions)
        {
            var csets = new List<CharSet>(positions.Length);

            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i] is TextPosition text)
                {
                    var cs = text.CharSet;

                    while (cs != null)
                    {
                        csets.Add(ignoreCase ? cs.ToAnyCase() : cs);
                        cs = cs.Except;
                    }
                }
            }

            return csets;
        }

        private static UnicodeRange[][] GetRanges(IList<CharSet> charSets, Selection selected)
        {
            var ranges = new UnicodeRange[charSets.Count][];

            for (int i = 0; i < charSets.Count; i++)
            {
                if (charSets[i].Ranges == UnicodeRanges.All)
                {
                    selected.Add(i);
                    ranges[i] = UnicodeRanges.Empty;
                }
                else
                {
                    ranges[i] = charSets[i].Ranges;
                }
            }

            return ranges;
        }

        private static IList<int>[] GetIndexesByCategory(IList<CharSet> charSets)
        {
            var indexesByCat = new List<int>[UnicodeCategories.Count];

            for (int i = 0; i < charSets.Count; i++)
            {
                for (int j = 0; j < indexesByCat.Length; j++)
                {
                    var uc = 1 << j;

                    if ((charSets[i].Categories.Set & uc) == uc)
                    {
                        if (indexesByCat[j] == null)
                        {
                            indexesByCat[j] = new List<int>();
                        }

                        indexesByCat[j].Add(i);
                    }
                }
            }

            return indexesByCat;
        }

        private static AcceptPosition GetAcceptPosition(Position[] positions)
        {
            AcceptPosition min = null;

            foreach (var accept in positions.OfType<AcceptPosition>())
            {
                if (min == null || min.TokenId > accept.TokenId)
                {
                    min = accept;
                }
            }

            return min;
        }

        private static Position[] RemoveLazyPositions(Position[] positions)
        {
            var stopList = new HashSet<int>();

            foreach (var pos in positions.OfType<AcceptPosition>())
            {
                if (pos.IsLazy)
                {
                    stopList.Add(pos.TokenId);
                }
            }

            if (stopList.Count > 0)
            {
                return positions.Where(x => x is AcceptPosition || !stopList.Contains(x.TokenId)).ToArray();
            }

            return positions;
        }

        private static UnicodeCategory GetUnicodeCategory(int code)
        {
            return code < UnicodeRange.ExtendedStart
                ? CharUnicodeInfo.GetUnicodeCategory((char)code)
                : CharUnicodeInfo.GetUnicodeCategory(Char.ConvertFromUtf32(code), 0);
        }
    }
}
