﻿namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ParserStatesBuilder
    {
        private readonly IConflictResolver conflictResolver;
        private readonly List<ParserState> states = new List<ParserState>();
        private readonly Dictionary<Symbol, List<ParserState>> statesBySymbol = new Dictionary<Symbol, List<ParserState>>();
        private readonly Queue<ParserState> queue = new Queue<ParserState>();

        public ParserStatesBuilder(Symbol goal, IConflictResolver conflictResolver = null)
        {
            this.conflictResolver = conflictResolver ?? ConflictResolver.Default;
            CreateRootState(goal ?? throw new ArgumentNullException(nameof(goal)));
        }

        public IParserStates CreateStates()
        {
            while (queue.Count > 0)
            {
                var state = queue.Dequeue();
                InitializeState(state);
            }

            GenerateLookaheads();
            GenerateActions();

            return new ParserStates(states);
        }

        private void InitializeState(ParserState state)
        {
            var items = GetItems(state);

            if (items.Any(x => x.LineBreak != LineBreakModifier.None))
            {
                var stateLB = CreateLineBreakState(state);

                InitializeState(state, GetItems(state));
                InitializeState(stateLB, GetItems(stateLB));
            }
            else
            {
                InitializeState(state, items);

                if (state.Completed.Any(x => IsLineBreakSensitiveOnReduce(x)))
                {
                    CreateLineBreakState(state);
                }
            }
        }

        private void InitializeState(ParserState state, List<ParserItem> items)
        {
            if (items.Count > 0)
            {
                items.Sort(ParserItem.CompareBySymbol);
                var symbol = items[0].Symbol;

                for (int lo = 0, hi = 1; lo < items.Count; hi++)
                {
                    if (hi == items.Count || items[hi].Symbol != symbol)
                    {
                        if (symbol == Symbol.EndOfProduction)
                        {
                            state.Completed = new Production[hi - lo];

                            for (int i = 0; i < state.Completed.Length; i++)
                            {
                                state.Completed[i] = items[lo + i].Production;
                            }
                        }
                        else
                        {
                            var nextCore = CreateNextCore(symbol, items, lo, hi - lo);

                            if (nextCore.Length > 0)
                            {
                                state.Shift.Add(symbol, CreateState(symbol, nextCore));
                            }
                        }

                        if ((lo = hi) < items.Count)
                        {
                            symbol = items[lo].Symbol;
                        }
                    }
                }
            }
        }

        private ParserItem[] CreateNextCore(Symbol symbol, List<ParserItem> items, int index, int length)
        {
            var core = new ParserItem[length];

            for (int i = 0; i < core.Length; i++)
            {
                core[i] = items[index + i].CreateNextItem();
            }

            core = conflictResolver.ResolveCoreConflicts(symbol, core) ?? Array.Empty<ParserItem>();
            Array.Sort(core, ParserItem.CompareByProduction);
            return core;
        }

        private List<ParserItem> GetItems(ParserState state)
        {
            var queue = new Queue<ParserItem>();
            var set = new HashSet<Symbol>();
            var items = new List<ParserItem>();

            // Initialize the Queue.
            foreach (var item in state.Core.Where(x => x.IsAllowed(state)))
            {
                queue.Enqueue(item);
                items.Add(item);
            }

            // Expand. 
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();

                if (item.Symbol is NonTerminal nonTerminal && set.Add(nonTerminal))
                {
                    foreach (var subItem in nonTerminal.Select(x => new ParserItem(x)).Where(x => x.IsAllowed(state)))
                    {
                        items.Add(subItem);
                        queue.Enqueue(subItem);
                    }
                }
            }

            return items;
        }

        private void GenerateLookaheads()
        {
            var lhGenerator = new LookaheadsGenerator(states, conflictResolver);
            lhGenerator.GenerateLookaheads();
        }

        private void GenerateActions()
        {
            foreach (var state in states)
            {
                GenerateActions(state);
            }
        }

        private void GenerateActions(ParserState state)
        {
            // Add shift actions.
            foreach (var symbol in state.Shift.Keys.OfType<Terminal>())
            {
                state.Actions.Add(symbol, ParserAction.Shift);
            }

            // Merge in reduce actions.
            foreach (var reduce in state.Reduce)
            {
                if (state.Actions.ContainsKey(reduce.Key))
                {
                    state.Actions[reduce.Key] = conflictResolver.ResolveShiftConflict(reduce.Key, reduce.Value);
                }
                else
                {
                    state.Actions.Add(reduce.Key, ParserAction.Reduce);
                }
            }
        }

        private ParserState CreateState(Symbol shiftOn, ParserItem[] core)
        {
            if (!statesBySymbol.TryGetValue(shiftOn, out var states))
            {
                states = new List<ParserState>();
                statesBySymbol.Add(shiftOn, states);
            }

            foreach (var state in states)
            {
                if (core.SequenceEqual(state.Core))
                {
                    return state;
                }
            }

            var newState = CreateNewState(core);
            states.Add(newState);
            queue.Enqueue(newState);
            return newState;
        }

        private ParserState CreateRootState(Symbol goal)
        {
            var core = new[] { new ParserItem(new Production(Symbol.Target, Symbol.Target.Name, new[] { goal })) };
            var root = CreateNewState(core);
            queue.Enqueue(root);
            return root;
        }

        private ParserState CreateNewState(ParserItem[] core)
        {
            var state = new ParserState(states.Count, core);
            states.Add(state);
            return state;
        }

        private ParserState CreateLineBreakState(ParserState state)
        {
            var stateLB = new ParserState(states.Count, state);
            states.Add(stateLB);

            state.LineBreak = LineBreakModifier.NoLineBreak;
            stateLB.LineBreak = LineBreakModifier.LineBreak;
            state.Shift.Add(Symbol.LineBreak, stateLB);

            return stateLB;
        }

        private static bool IsLineBreakSensitiveOnReduce(Production production)
        {
            if (production.Lookaheads == null)
            {
                return false;
            }

            return production.Lookaheads.Contains(Symbol.LineBreak)
                || production.Lookaheads.Contains(Symbol.NoLineBreak);
        }
    }
}
