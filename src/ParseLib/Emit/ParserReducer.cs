namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using ParseLib.Runtime;

    /// <summary>
    /// Defines default parser reducer constructed based on the taget's base type metadata.
    /// </summary>
    public class ParserReducer : IParserReducer
    {
        public static ParserReducer CreateReducer(Type target, Grammar grammar, bool skipValidation = false)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (grammar == null) throw new ArgumentNullException(nameof(grammar));

            var reducer = new ParserReducer();

            foreach (var method in target.GetMethods(BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.Static))
            {
                foreach (CompleteTokenAttribute attr in method.GetCustomAttributes(typeof(CompleteTokenAttribute)))
                {
                    if (!(skipValidation || grammar.ContainsTerminal(attr.Token)))
                    {
                        throw new InvalidOperationException(Errors.TokenNotFound(attr.Token));
                    }

                    reducer.AddTokenReducer(attr.Token, method);
                }

                foreach (ReduceAttribute attr in method.GetCustomAttributes(typeof(ReduceAttribute)))
                {
                    if (!(skipValidation || grammar.ContainsRule(attr.Production)))
                    {
                        throw new InvalidOperationException(Errors.ProductionNotFound(attr.Production));
                    }

                    reducer.AddProductionReducer(attr.Production, method);
                }

                foreach (HandleAttribute attr in method.GetCustomAttributes(typeof(HandleAttribute)))
                {
                    reducer.AddPrefixHandler(grammar.ParseSymbols(attr.Prefix), method);
                }
            }

            return reducer;
        }

        private readonly Dictionary<string, MethodInfo> tokenReducers;
        private readonly Dictionary<string, MethodInfo> productionReducers;
        private readonly SymbolTrie<MethodInfo> prefixes;

        public ParserReducer()
        {
            this.tokenReducers = new Dictionary<string, MethodInfo>();
            this.productionReducers = new Dictionary<string, MethodInfo>();
            this.prefixes = new SymbolTrie<MethodInfo>();
        }

        public MethodInfo GetPrefixHandler(Symbol[] symbols)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            return prefixes.GetPrefix(symbols)?.Data;
        }

        public MethodInfo GetProductionReducer(string productionName)
        {
            if (productionReducers.TryGetValue(productionName, out var reducer))
            {
                return reducer;
            }

            return null;
        }

        public MethodInfo GetTokenReducer(string tokenName)
        {
            if (tokenReducers.TryGetValue(tokenName, out var reducer))
            {
                return reducer;
            }

            return null;
        }

        public void AddPrefixHandler(Symbol[] symbols, MethodInfo handler)
        {
            var node = prefixes.EnsurePrefix(symbols);

            if (node.Data != null)
            {
                throw new InvalidOperationException(Errors.PrefixDefined(Symbol.ToString(symbols)));
            }

            node.Data = handler;
        }

        public void AddProductionReducer(string productionName, MethodInfo reducer)
        {
            if (productionReducers.ContainsKey(productionName))
            {
                throw new InvalidOperationException(Errors.ReducerDefined(productionName));
            }

            productionReducers.Add(productionName, reducer);
        }

        public void AddTokenReducer(string tokenName, MethodInfo reducer)
        {
            if (reducer.GetParameters().Length > 0)
            {
                throw new InvalidOperationException(Errors.ExpectedParameterlessReducer(tokenName));
            }

            if (tokenReducers.ContainsKey(tokenName))
            {
                throw new InvalidOperationException(Errors.ReducerDefined(tokenName));
            }

            tokenReducers.Add(tokenName, reducer);
        }
    }
}
