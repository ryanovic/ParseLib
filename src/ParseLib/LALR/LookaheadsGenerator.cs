namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class LookaheadsGenerator
    {
        private readonly List<ParserState> states;
        private readonly IConflictResolver conflictResolver;

        // Store the list of productions started per state and the link to a state where it's completed.
        private List<(Production, ParserState)>[] finalStatesByState;

        public LookaheadsGenerator(List<ParserState> states, IConflictResolver conflictResolver)
        {
            this.states = states;
            this.conflictResolver = conflictResolver;
            this.finalStatesByState = new List<(Production, ParserState)>[states.Count];
        }

        public void GenerateLookaheads()
        {
            CollectFinalStates();

            var root = states[0].Core[0];
            states[0].Shift[root.Symbol].Reduce.Add(Symbol.EndOfSource, root.Production);

            var updated = GenerateLookaheads(firstRound: true);

            while (updated)
            {
                updated = GenerateLookaheads(firstRound: false);
            }
        }

        private bool GenerateLookaheads(bool firstRound)
        {
            var updated = false;

            for (int i = 0; i < finalStatesByState.Length; i++)
            {
                var state = states[i];
                var finalStates = finalStatesByState[i];

                foreach ((var production, var final) in finalStates)
                {
                    if (state.Shift.TryGetValue(production.Head, out var next))
                    {
                        updated |= GenerateLookaheads(next, production, final, firstRound);
                    }

                    if (next != null && next.Shift.TryGetValue(Symbol.LineBreak, out var nextLB))
                    {
                        updated |= GenerateLookaheads(nextLB, production, final, firstRound);
                    }
                }
            }

            return updated;
        }

        private bool GenerateLookaheads(ParserState next, Production production, ParserState final, bool firstRound)
        {
            var updated = false;

            if ((final.LineBreak | next.LineBreak) != LineBreakModifier.Forbidden)
            {
                var filter = production.Lookaheads;

                if (filter != null)
                {
                    if ((final.LineBreak == LineBreakModifier.LineBreak && filter.Contains(Symbol.LineBreak))
                        || (final.LineBreak == LineBreakModifier.NoLineBreak && filter.Contains(Symbol.NoLineBreak)))
                    {
                        filter = null;
                    }
                }

                updated |= GenerateLookaheads(next, production, final, filter, firstRound);
            }

            return updated;
        }

        private bool GenerateLookaheads(ParserState next, Production production, ParserState final, ISet<Symbol> filter, bool firstRound)
        {
            var updated = false;

            if (firstRound)
            {
                foreach (var symbol in next.Shift.Keys.OfType<Terminal>())
                {
                    if (filter == null || filter.Contains(symbol))
                    {
                        updated |= CreateReduceRule(final, production, symbol);
                    }
                }
            }

            foreach (var symbol in next.Reduce.Keys)
            {
                if (filter == null || filter.Contains(symbol))
                {
                    updated |= CreateReduceRule(final, production, symbol);
                }
            }

            return updated;
        }

        private bool CreateReduceRule(ParserState state, Production production, Symbol symbol)
        {
            if (state.Reduce.TryGetValue(symbol, out var existing))
            {
                if (existing == production
                    || existing == conflictResolver.ResolveReduceConflict(symbol, existing, production))
                {
                    return false;
                }

                state.Reduce[symbol] = production;
                return true;
            }

            state.Reduce.Add(symbol, production);
            return true;
        }

        private void CollectFinalStates()
        {
            for (int i = 0; i < finalStatesByState.Length; i++)
            {
                finalStatesByState[i] = new List<(Production, ParserState)>();
                CollectFinalStates(states[i], finalStatesByState[i]);
            }
        }

        private static void CollectFinalStates(ParserState state, List<(Production, ParserState)> finalStates)
        {
            foreach (var next in state.Shift)
            {
                foreach (var item in next.Value.Core.Where(x => x.Index == 1))
                {
                    CollectFinalStates(next.Value, item, finalStates);
                }

                foreach (var production in state.Completed.Where(x => x.Size == 0))
                {
                    finalStates.Add((production, state));
                }
            }
        }

        private static void CollectFinalStates(ParserState state, ParserItem item, List<(Production, ParserState)> finalStates)
        {
            if (state.Shift.TryGetValue(Symbol.LineBreak, out var stateLB))
            {
                CollectFinalStates(stateLB, item, finalStates);
            }

            if (item.IsAllowed(state) && state.Core.Contains(item))
            {
                if (item.Index == item.Production.Size)
                {
                    finalStates.Add((item.Production, state));
                }
                else if (state.Shift.TryGetValue(item.Symbol, out var next))
                {
                    CollectFinalStates(next, item.CreateNextItem(), finalStates);
                }
            }
        }
    }
}
