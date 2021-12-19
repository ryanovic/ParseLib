namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using ParseLib.LALR;
    using ParseLib.Runtime;
    using ParseLib.Text;

    /// <summary>
    /// Defines the base for parser builder.
    /// </summary>
    public abstract class ParserBuilder : ILexerTarget
    {
        private readonly Cell<bool> isLineBreak;
        private readonly ParserStack stateStack;
        private readonly ParserStack dataStack;
        private readonly ParserMetadata metadata;

        private readonly MethodBuilder handleEosMthd;
        private readonly MethodBuilder handleLexemeMthd;
        private readonly MethodBuilder handleTerminalMthd;
        private readonly MethodBuilder handleNonTerminalMthd;
        private readonly MethodBuilder getTerminalNameMthd;
        private readonly MethodBuilder getNonTerminalNameMthd;
        private readonly MethodBuilder getParserStateMthd;

        public Grammar Grammar => metadata.Grammar;
        public ILexicalStates LexicalStates => metadata.LexicalStates;
        public Terminal[] Terminals => metadata.Terminals;
        public NonTerminal[] NonTerminals => metadata.NonTerminals;
        public StateMetadata[] States => metadata.States;

        protected TypeBuilder Target { get; }
        protected IParserReducer Reducer { get; }
        protected Cell<int> LexerState { get; }
        protected Cell<int> StartPosition { get; }
        protected Cell<int> CurrentPosition { get; }

        public ParserBuilder(TypeBuilder target, IParserReducer reducer, Grammar grammar, string goal)
            : this(target, reducer, grammar.CreateParserMetadata(goal))
        {
        }

        public ParserBuilder(TypeBuilder target, IParserReducer reducer, ParserMetadata metadata)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            this.Reducer = reducer ?? throw new ArgumentNullException(nameof(reducer));
            this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

            if (Target.BaseType == null || !Target.BaseType.IsSubclassOf(typeof(ParserBase)))
            {
                throw new InvalidOperationException("Target base type is expected to be inherited from the ParserBase.");
            }

            this.handleEosMthd = target.DefineMethod("HandleEndOfSource", MethodAttributes.Private, typeof(void), Type.EmptyTypes);
            this.handleLexemeMthd = target.DefineMethod("HandleLexeme", MethodAttributes.Private, typeof(void), new[] { typeof(int) });
            this.handleTerminalMthd = target.DefineMethod("HandleTerminal", MethodAttributes.Private, typeof(void), new[] { typeof(int), typeof(object) });
            this.handleNonTerminalMthd = target.DefineMethod("HandleNonTerminal", MethodAttributes.Private, typeof(void), new[] { typeof(int) });

            this.getTerminalNameMthd = target.DefineMethod("GetTerminalName", MethodAttributes.Private, typeof(string), new[] { typeof(int) });
            this.getNonTerminalNameMthd = target.DefineMethod("GetNonTerminalName", MethodAttributes.Private, typeof(string), new[] { typeof(int) });
            this.getParserStateMthd = target.DefineMethod("GetParserState", MethodAttributes.Family | MethodAttributes.Virtual, typeof(string), Type.EmptyTypes);

            this.LexerState = target.CreateCell<int>("lexerState");
            this.StartPosition = target.CreateCell<int>("startPosition");
            this.CurrentPosition = target.CreateCell<int>("currentPosition");

            this.isLineBreak = target.CreateCell<bool>("isNewLine");
            this.stateStack = new ParserStack(target.CreateCell("stateStack", typeof(List<int>)));
            this.dataStack = new ParserStack(target.CreateCell("dataStack", typeof(List<object>)));
        }

        public virtual Type Build()
        {
            // Pipeline.            
            BuildHandleLexemeMethod();
            BuildHandleTerminalMethod();
            BuildHandleNonTerminalMethod();
            BuildHandleEndOfSourceMethod();

            // Properties.
            BuildStartPositionProperty();
            BuildCurrentPositionProperty();
            BuildIsCompletedProperty();

            // Miscellaneous.
            BuildGetTerminalNameMethod();
            BuildGetNonTerminalNameMethod();
            BuildGetParserStateMethod();
            BuildGetTopValueMethod();

            // Generate.
            BuildConstructors();
            BuildLexer();

            return Target.CreateTypeInfo();
        }

        /// <inheritdoc/>
        public virtual void CompleteToken(ILGenerator il, int tokenId)
        {
            HandleLexeme(il, tokenId);
        }

        /// <inheritdoc/>
        public virtual void CompleteToken(ILGenerator il, Cell<int> tokenId)
        {
            il.Emit(OpCodes.Ldarg_0);
            tokenId.Load(il);
            il.Emit(OpCodes.Call, handleLexemeMthd);
        }

        /// <inheritdoc/>
        public virtual void CompleteSource(ILGenerator il, ILexerSource source)
        {
            var handleEos = il.DefineLabel();
            var unexpectedChar = il.DefineLabel();

            source.CheckUpperBound(il, CurrentPosition, unexpectedChar);

            StartPosition.Load(il);
            CurrentPosition.Load(il);
            il.Emit(OpCodes.Beq, handleEos);
            ThrowParserException(il, "Unexpected end of source encountered.");

            il.MarkLabel(unexpectedChar);
            ThrowParserException(il, "Unexpected character encountered.");

            il.MarkLabel(handleEos);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, handleEosMthd);
        }

        /// <summary>
        /// Creates lexical analyzer.
        /// </summary>
        protected abstract void BuildLexer();

        /// <summary>
        /// Initialize initial parser state.
        /// </summary>
        protected virtual void InitializeParser(ILGenerator il)
        {
            stateStack.Initialize(il);
            dataStack.Initialize(il);
            stateStack.Push(il, () => il.Emit(OpCodes.Ldc_I4_0));
            LexerState.Update(il, States[0].LexicalState.Id);
        }

        protected virtual void ThrowParserException(ILGenerator il, string message)
        {
            ThrowParserException(il, () => il.Emit(OpCodes.Ldstr, message));
        }

        protected virtual void ThrowParserException(ILGenerator il, Action loadMessage)
        {
            il.Emit(OpCodes.Ldarg_0);
            loadMessage();
            il.Emit(OpCodes.Callvirt, ReflectionInfo.ParserBase_CreateParserExceptionByMessage);
            il.Emit(OpCodes.Throw);
        }

        private void BuildConstructors()
        {
            foreach (var baseCtor in Target.BaseType.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var parameterTypes = Utils.Transform(baseCtor.GetParameters(), x => x.ParameterType);
                var ctor = Target.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);
                var il = ctor.GetILGenerator();

                for (int i = 0; i <= parameterTypes.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg, i);
                }

                il.Emit(OpCodes.Call, baseCtor);
                InitializeParser(il);
                il.Emit(OpCodes.Ret);
            }
        }

        private void BuildHandleEndOfSourceMethod()
        {
            var il = handleEosMthd.GetILGenerator();
            var start = il.DefineLabel();
            var labels = il.DefineLabels(States.Length);

            il.MarkLabel(start);
            stateStack.Peek(il, 0);
            il.Emit(OpCodes.Switch, labels);
            il.ThrowInvalidOperationException("Invalid state encountered.");

            for (int i = 0; i < States.Length; i++)
            {
                il.MarkLabel(labels[i]);
                HandleEndOfSource(il, States[i], start);
            }
        }

        private void HandleEndOfSource(ILGenerator il, StateMetadata state, Label start)
        {
            if (state.ParserState.Reduce.TryGetValue(Symbol.EndOfSource, out var item))
            {
                if (item.Head == Symbol.Target)
                {
                    stateStack.ReplaceTop(il, 2, () => il.Emit(OpCodes.Ldc_I4_M1));
                    LexerState.Update(il, () => il.Emit(OpCodes.Ldc_I4_M1));
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    HandleProduction(il, item);
                    il.Emit(OpCodes.Br, start);
                }
            }
            else
            {
                ThrowParserException(il, "Unexpected end of source encountered.");
            }
        }

        private void BuildHandleLexemeMethod()
        {
            var il = handleLexemeMthd.GetILGenerator();
            var labels = il.DefineLabels(Terminals.Length);

            il.Emit(OpCodes.Ldarg_1); // token Id
            il.Emit(OpCodes.Switch, labels);
            il.ThrowArgumentOutOfRangeException("tokenId", "The token is out of range.");

            for (int i = 0; i < labels.Length; i++)
            {
                il.MarkLabel(labels[i]);
                HandleLexeme(il, i);
                il.Emit(OpCodes.Ret);
            }
        }

        private void HandleLexeme(ILGenerator il, int tokenId)
        {
            var method = Reducer.GetTokenReducer(Terminals[tokenId].Name);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, tokenId);

            if (method == null || !ExecuteMethod(il, method, Array.Empty<ParameterInfo>()))
            {
                il.Emit(OpCodes.Ldnull);
            }

            StartPosition.Update(il, () => CurrentPosition.Load(il));
            il.Emit(OpCodes.Call, handleTerminalMthd);
        }

        private void BuildHandleTerminalMethod()
        {
            var il = handleTerminalMthd.GetILGenerator();
            var start = il.DefineLabel();
            var labels = il.DefineLabels(States.Length);

            // Read most recent state.
            il.MarkLabel(start);
            stateStack.Peek(il, 0);

            il.Emit(OpCodes.Switch, labels);
            il.ThrowInvalidOperationException("Invalid state encountered.");

            for (int i = 0; i < States.Length; i++)
            {
                il.MarkLabel(labels[i]);
                HandleTerminals(il, States[i], start);
            }
        }

        private void HandleTerminals(ILGenerator il, StateMetadata state, Label start)
        {
            (var intervals, var defaultLabel) = SearchInterval.CreateIntervals(il, state.Terminals.Length);

            foreach (var interval in intervals)
            {
                var terminal = state.Terminals[interval.Middle];

                il.MarkLabel(interval.Label);

                if (interval.Low == interval.Middle)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, terminal.Id);
                    il.Emit(OpCodes.Bne_Un, interval.IsSingle ? defaultLabel : interval.Right);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, terminal.Id);
                    il.Emit(OpCodes.Blt, interval.Left);

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, terminal.Id);
                    il.Emit(OpCodes.Bgt, interval.Right);
                }

                HandleTerminal(il, state, terminal, start);
            }

            il.MarkLabel(defaultLabel);
            ThrowParserException(il, () =>
            {
                il.Emit(OpCodes.Ldstr, "'{0}' terminal is not valid due to the current state of the parser.");
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, getTerminalNameMthd);
                il.Emit(OpCodes.Call, ReflectionInfo.String_Format);
            });
        }

        private void HandleTerminal(ILGenerator il, StateMetadata state, Terminal terminal, Label start)
        {
            if (state.ParserState.Actions.TryGetValue(terminal, out var action))
            {
                HandleTerminal(il, state, terminal, action, start);
            }
            else
            {
                HandleWhitespace(il, state, terminal);
            }
        }

        private void HandleTerminal(ILGenerator il, StateMetadata state, Terminal terminal, ParserAction action, Label start)
        {
            if (action == ParserAction.Shift)
            {
                var next = state.ParserState.Shift[terminal];
                var method = Reducer.GetTokenReducer(terminal.Name);

                if (method != null && method.ReturnType != typeof(void))
                {
                    // Put terminal value on stack.
                    dataStack.Push(il, () => il.Emit(OpCodes.Ldarg_2));
                }

                isLineBreak.Update(il, false);
                LexerState.Update(il, States[next.Id].LexicalState.Id);
                stateStack.Push(il, () => il.Emit(OpCodes.Ldc_I4, next.Id));

                HandlePrefixes(il, next);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                HandleProduction(il, state.ParserState.Reduce[terminal]);
                il.Emit(OpCodes.Br, start);
            }
        }

        private void HandleWhitespace(ILGenerator il, StateMetadata state, Terminal whitespace)
        {
            // Whitespace.
            if (Grammar.IsNewLine(whitespace))
            {
                isLineBreak.Update(il, () => il.LoadTrue());

                if (state.ParserState.Shift.TryGetValue(Symbol.LineBreak, out var nextLB))
                {
                    state = States[nextLB.Id];
                    stateStack.ReplaceTop(il, 1, () => il.Emit(OpCodes.Ldc_I4, nextLB.Id));
                }
            }

            LexerState.Update(il, () => il.Emit(OpCodes.Ldc_I4, state.LexicalState.Id));
            il.Emit(OpCodes.Ret);
        }

        private void HandleProduction(ILGenerator il, Production production)
        {
            var method = Reducer.GetProductionReducer(production.Name);

            if (method != null)
            {
                var parameters = method.GetParameters();

                if (method.ReturnType != typeof(void))
                {
                    if (parameters.Length == 0)
                    {
                        dataStack.Push(il, () => ExecuteMethod(il, method, parameters));
                    }
                    else
                    {
                        dataStack.ReplaceTop(il, parameters.Length, () => ExecuteMethod(il, method, parameters));
                    }
                }
                else
                {
                    ExecuteMethod(il, method, parameters);
                    dataStack.RemoveTop(il, parameters.Length);
                }
            }

            stateStack.RemoveTop(il, production.Size);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ((NonTerminal)production.Head).Id);
            il.Emit(OpCodes.Call, handleNonTerminalMthd);
        }

        private void BuildHandleNonTerminalMethod()
        {
            var il = handleNonTerminalMthd.GetILGenerator();
            var labels = il.DefineLabels(States.Length);

            // Read most recent state.
            stateStack.Peek(il, 0);
            il.Emit(OpCodes.Switch, labels);
            il.ThrowInvalidOperationException("Invalid state encountered.");

            for (int i = 0; i < States.Length; i++)
            {
                il.MarkLabel(labels[i]);
                HandleNonTerminals(il, States[i]);
            }
        }

        private void HandleNonTerminals(ILGenerator il, StateMetadata state)
        {
            (var intervals, var defaultLabel) = SearchInterval.CreateIntervals(il, state.NonTerminals.Length);

            foreach (var interval in intervals)
            {
                var nonTerminal = state.NonTerminals[interval.Middle];

                il.MarkLabel(interval.Label);

                if (interval.Low == interval.Middle)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, nonTerminal.Id);
                    il.Emit(OpCodes.Bne_Un, interval.IsSingle ? defaultLabel : interval.Right);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, nonTerminal.Id);
                    il.Emit(OpCodes.Blt, interval.Left);

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, nonTerminal.Id);
                    il.Emit(OpCodes.Bgt, interval.Right);
                }

                HandleNonTerminal(il, state, nonTerminal);
            }


            il.MarkLabel(defaultLabel);
            ThrowParserException(il, () =>
            {
                il.Emit(OpCodes.Ldstr, "'{0}' non-terminal is not valid due to the current state of the parser.");
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, getNonTerminalNameMthd);
                il.Emit(OpCodes.Call, ReflectionInfo.String_Format);
            });
        }

        private void HandleNonTerminal(ILGenerator il, StateMetadata state, NonTerminal nonTerminal)
        {
            var next = state.ParserState.Shift[nonTerminal];

            if (next.Shift.TryGetValue(Symbol.LineBreak, out var nextLB))
            {
                var noLineBreak = il.DefineLabel();

                isLineBreak.Load(il);
                il.Emit(OpCodes.Brfalse, noLineBreak);

                stateStack.Push(il, () => il.Emit(OpCodes.Ldc_I4, nextLB.Id));
                HandlePrefixes(il, nextLB);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(noLineBreak);
            }

            stateStack.Push(il, () => il.Emit(OpCodes.Ldc_I4, next.Id));
            HandlePrefixes(il, next);
            il.Emit(OpCodes.Ret);
        }

        private void HandlePrefixes(ILGenerator il, ParserState state)
        {
            var set = new HashSet<MethodInfo>();

            foreach (var item in state.Core)
            {
                var method = Reducer.GetPrefixHandler(item.GetPrefix());

                if (method != null && set.Add(method))
                {
                    if (ExecuteMethod(il, method))
                    {
                        il.Emit(OpCodes.Pop);
                    }
                }
            }
        }

        private void BuildGetTerminalNameMethod()
        {
            var il = getTerminalNameMthd.GetILGenerator();
            var labels = il.DefineLabels(Terminals.Length);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Switch, labels);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            for (int i = 0; i < Terminals.Length; i++)
            {
                il.MarkLabel(labels[i]);
                il.Emit(OpCodes.Ldstr, Terminals[i].Name);
                il.Emit(OpCodes.Ret);
            }
        }

        private void BuildGetNonTerminalNameMethod()
        {
            var il = getNonTerminalNameMthd.GetILGenerator();
            var labels = il.DefineLabels(NonTerminals.Length);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Switch, labels);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            for (int i = 0; i < NonTerminals.Length; i++)
            {
                il.MarkLabel(labels[i]);
                il.Emit(OpCodes.Ldstr, NonTerminals[i].Name);
                il.Emit(OpCodes.Ret);
            }
        }

        private void LoadSymbolName(ILGenerator il, Symbol symbol)
        {
            if (symbol is Terminal terminal)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, terminal.Id);
                il.Emit(OpCodes.Call, getTerminalNameMthd);
            }
            else if (symbol is NonTerminal nonTerminal)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, nonTerminal.Id);
                il.Emit(OpCodes.Call, getNonTerminalNameMthd);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }
        }

        private void BuildGetParserStateMethod()
        {
            var il = getParserStateMthd.GetILGenerator();

            var names = il.CreateCell(typeof(string[]));
            var index = il.CreateCell<int>();
            var labels = il.DefineLabels(States.Length);
            var switch_end = il.DefineLabel();

            index.Update(il, 0);
            il.CreateArray(names, () => il.Decrement(() => stateStack.GetCount(il)));

            il.For(names, index, () =>
            {
                names.Load(il);
                index.Load(il);

                stateStack.GetElementAt(il, () => il.Increment(() => index.Load(il)));
                il.Emit(OpCodes.Switch, labels);

                il.Map(labels, i =>
                {
                    LoadSymbolName(il, States[i].InputSymbol);
                    il.GoTo(switch_end);
                });

                il.MarkLabel(switch_end);
                il.Emit(OpCodes.Stelem_Ref);
            });

            il.Emit(OpCodes.Ldstr, " ");
            names.Load(il);
            il.Emit(OpCodes.Call, ReflectionInfo.String_Join);
            il.Emit(OpCodes.Ret);
        }

        private void BuildStartPositionProperty()
        {
            var method = Target.DefineMethod("get_StartPosition",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes);
            var il = method.GetILGenerator();

            StartPosition.Load(il);
            il.Emit(OpCodes.Ret);
        }

        private void BuildCurrentPositionProperty()
        {
            var method = Target.DefineMethod("get_CurrentPosition",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes);
            var il = method.GetILGenerator();

            CurrentPosition.Load(il);
            il.Emit(OpCodes.Ret);
        }

        private void BuildIsCompletedProperty()
        {
            var method = Target.DefineMethod("get_IsCompleted",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(bool), Type.EmptyTypes);
            var il = method.GetILGenerator();

            stateStack.Peek(il, 0);
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ret);
        }

        private void BuildGetTopValueMethod()
        {
            var method = Target.DefineMethod("GetTopValue",
                MethodAttributes.Family | MethodAttributes.Virtual, typeof(object), Type.EmptyTypes);
            var il = method.GetILGenerator();

            var label = il.DefineLabel();

            dataStack.GetCount(il);
            il.Emit(OpCodes.Brtrue, label);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            il.MarkLabel(label);
            dataStack.Peek(il, 0);
            il.Emit(OpCodes.Ret);
        }

        private bool ExecuteMethod(ILGenerator il, MethodInfo method)
        {
            return ExecuteMethod(il, method, method.GetParameters());
        }

        private bool ExecuteMethod(ILGenerator il, MethodInfo method, ParameterInfo[] parameters)
        {
            if (!method.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            for (int i = 1; i <= parameters.Length; i++)
            {
                dataStack.Peek(il, parameters.Length - i);
                il.ConvertFromObject(parameters[i - 1].ParameterType);
            }

            il.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);

            if (method.ReturnType != typeof(void))
            {
                il.ConvertToObject(method.ReturnType);
                return true;
            }

            return false;
        }
    }
}
