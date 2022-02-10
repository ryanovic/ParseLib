namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using Ry.ParseLib.LALR;
    using Ry.ParseLib.Runtime;

    /// <summary>
    /// Implements the interface for executing handlers for recognized tokens.
    /// </summary>
    public class ParserReducer : IParserReducer
    {
        /// <summary>
        /// Generates a reducer that uses runtime attributes to map handlers in a parent type.
        /// </summary>
        /// <remarks><c>BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static</c></remarks>
        public static ParserReducer CreateReducer(TypeBuilder target, Grammar grammar)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (target.BaseType == null) throw new ArgumentNullException(nameof(target.BaseType));
            if (grammar == null) throw new ArgumentNullException(nameof(grammar));

            var reducer = new ParserReducer(target);

            foreach (var method in target.BaseType.GetMethods(BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.Static))
            {
                foreach (CompleteTokenAttribute attr in method.GetCustomAttributes(typeof(CompleteTokenAttribute)))
                {
                    reducer.AddTokenReducer(attr.Token, method, grammar.IsWhitespace(grammar.GetTerminal(attr.Token)));
                }

                foreach (ReduceAttribute attr in method.GetCustomAttributes(typeof(ReduceAttribute)))
                {
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
        private readonly ParserStack dataStack;

        public ParserReducer(TypeBuilder target)
        {
            this.tokenReducers = new Dictionary<string, MethodInfo>();
            this.productionReducers = new Dictionary<string, MethodInfo>();
            this.prefixes = new SymbolTrie<MethodInfo>();
            this.dataStack = new ParserStack(target.CreateCell("dataStack", typeof(List<object>)));
        }

        public void AddPrefixHandler(Symbol[] symbols, MethodInfo handler)
        {
            var node = prefixes.EnsurePrefix(symbols);

            if (handler.ReturnType != typeof(void))
            {
                throw new InvalidOperationException(Errors.PrefixHandlerReturnsValue());
            }

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

        public void AddTokenReducer(string tokenName, MethodInfo reducer, bool isWhitespace)
        {
            if (isWhitespace && reducer.ReturnType != typeof(void))
            {
                throw new InvalidOperationException(Errors.WhitespaceHandlerReturnsValue());
            }

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

        public void Initialize(ILGenerator il)
        {
            dataStack.Initialize(il);
        }

        public void HandleTerminal(ILGenerator il, Terminal terminal)
        {
            if (tokenReducers.TryGetValue(terminal.Name, out var method))
            {
                if (method.ReturnType != typeof(void))
                {
                    dataStack.Push(il, () =>
                    {
                        il.Execute(method);
                        il.ConvertToObject(method.ReturnType);
                    });
                }
                else
                {
                    il.Execute(method);
                }
            }
        }

        public void HandleProduction(ILGenerator il, Production production)
        {
            if (productionReducers.TryGetValue(production.Name, out var method))
            {
                var parameters = method.GetParameters();

                if (method.ReturnType != typeof(void))
                {
                    if (parameters.Length == 0)
                    {
                        // Push an output onto the value stack.
                        dataStack.Push(il, () =>
                        {
                            ExecuteMethod(il, method);
                            il.ConvertToObject(method.ReturnType);
                        });
                    }
                    else
                    {
                        // Replace the top parameters with an output.
                        dataStack.ReplaceTop(il, parameters.Length, () =>
                        {
                            ExecuteMethod(il, method);
                            il.ConvertToObject(method.ReturnType);
                        });
                    }
                }
                else
                {
                    // Remove parameters from the value stack.
                    ExecuteMethod(il, method);
                    dataStack.RemoveTop(il, parameters.Length);
                }
            }
        }

        public void HandleState(ILGenerator il, ParserState state)
        {
            var set = new HashSet<MethodInfo>();

            // Iterate through the state core items and check if any handler is defined for the prefix.
            foreach (var item in state.Core)
            {
                if (prefixes.TryGetData(item.GetPrefix(), out var method) && set.Add(method))
                {
                    ExecuteMethod(il, method);
                }
            }
        }

        private void ExecuteMethod(ILGenerator il, MethodInfo method)
        {
            il.Execute(method, () => LoadParameters(il, method));
        }

        private void LoadParameters(ILGenerator il, MethodInfo method)
        {
            var parameters = method.GetParameters();

            for (int i = 1; i <= parameters.Length; i++)
            {
                dataStack.Peek(il, parameters.Length - i);
                il.ConvertFromObject(parameters[i - 1].ParameterType);
            }
        }

        public void LoadResult(ILGenerator il)
        {
            var hasData = il.DefineLabel();
            var endMthd = il.DefineLabel();

            dataStack.GetCount(il);
            il.Emit(OpCodes.Brtrue, hasData);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Br_S, endMthd);

            il.MarkLabel(hasData);
            dataStack.Peek(il, 0);

            il.MarkLabel(endMthd);
        }
    }
}
