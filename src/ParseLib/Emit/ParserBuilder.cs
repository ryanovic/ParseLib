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
    /// Implements the basic operations for generating a parser.
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

        private readonly MethodInfo onTokenCompleted;
        private readonly MethodInfo onProductionCompleted;

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
                throw new InvalidOperationException(Errors.ParserBaseExpected());
            }

            this.handleEosMthd = target.DefineMethod("HandleEndOfSource", MethodAttributes.Private, typeof(void), Type.EmptyTypes);
            this.handleLexemeMthd = target.DefineMethod("HandleLexeme", MethodAttributes.Private, typeof(void), new[] { typeof(int) });
            this.handleTerminalMthd = target.DefineMethod("HandleTerminal", MethodAttributes.Private, typeof(void), new[] { typeof(int), typeof(object) });
            this.handleNonTerminalMthd = target.DefineMethod("HandleNonTerminal", MethodAttributes.Private, typeof(void), new[] { typeof(int) });

            this.getTerminalNameMthd = target.DefineMethod("GetTerminalName", MethodAttributes.Private, typeof(string), new[] { typeof(int) });
            this.getNonTerminalNameMthd = target.DefineMethod("GetNonTerminalName", MethodAttributes.Private, typeof(string), new[] { typeof(int) });
            this.getParserStateMthd = target.DefineMethod("GetParserState", MethodAttributes.Family | MethodAttributes.Virtual, typeof(string), Type.EmptyTypes);

            this.onTokenCompleted = target.BaseType.GetMethod(
                "OnTokenCompleted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new[] { typeof(string) }, null);
            this.onProductionCompleted = target.BaseType.GetMethod(
                "OnProductionCompleted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new[] { typeof(string) }, null);

            this.LexerState = target.CreateCell<int>("lexerState");
            this.StartPosition = target.CreateCell<int>("startPosition");
            this.CurrentPosition = target.CreateCell<int>("currentPosition");

            // A cell that "remembers" if a line break was detected.
            this.isLineBreak = target.CreateCell<bool>("isNewLine");
            this.stateStack = new ParserStack(target.CreateCell("stateStack", typeof(List<int>)));
            this.dataStack = new ParserStack(target.CreateCell("dataStack", typeof(List<object>)));
        }

        /// <summary>
        /// Generates a type for the parser.
        /// </summary>
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

        /// <summary>
        /// Completes a token defined by the <paramref name="tokenId"/>.
        /// </summary>
        public virtual void CompleteToken(ILGenerator il, int tokenId)
        {
            HandleLexeme(il, tokenId);
        }

        /// <summary>
        /// Completes a token defined by the <paramref name="tokenId"/> cell.
        /// </summary>
        public virtual void CompleteToken(ILGenerator il, Cell<int> tokenId)
        {
            il.Emit(OpCodes.Ldarg_0);
            tokenId.Load(il);
            il.Emit(OpCodes.Call, handleLexemeMthd);
        }

        /// <summary>
        /// Completes the <paramref name="source"/>.
        /// </summary>
        public virtual void CompleteSource(ILGenerator il, ILexerSource source)
        {
            var handleEos = il.DefineLabel();
            var unexpectedChar = il.DefineLabel();

            // If it's not read to the end then throw an exception.
            source.CheckUpperBound(il, CurrentPosition, unexpectedChar);

            // If there is an unrecognized pending lexeme then throw an exception.
            StartPosition.Load(il);
            CurrentPosition.Load(il);
            il.Emit(OpCodes.Beq, handleEos);
            ThrowParserException(il, Errors.UnexpectedEndOfSoruce());

            il.MarkLabel(unexpectedChar);
            ThrowParserException(il, Errors.UnexpectedCharacter());

            // Emit EndOfSource token.
            il.MarkLabel(handleEos);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, handleEosMthd);
        }

        /// <summary>
        /// Generates a lexer for the parser.
        /// </summary>
        protected abstract void BuildLexer();

        /// <summary>
        /// Initializes the parser with initial values.
        /// </summary>
        protected virtual void InitializeParser(ILGenerator il)
        {
            stateStack.Initialize(il);
            dataStack.Initialize(il);
            stateStack.Push(il, () => il.Emit(OpCodes.Ldc_I4_0));
            LexerState.Update(il, States[0].LexicalState.Id);
        }

        /// <summary>
        /// Notifies the parser that the specified terminal is completed.
        /// </summary>
        /// <remarks>Exceuted after an appropriate user handler is called but before the parser state is updated.</remarks>
        protected virtual void OnTokenCompleted(ILGenerator il, Terminal terminal)
        {
            if (onTokenCompleted != null)
            {
                ExecuteMethod(il, onTokenCompleted, () =>
                {
                    il.Emit(OpCodes.Ldstr, terminal.Name);
                });
            }
        }

        /// <summary>
        /// Notifies the parser that the specified production is completed.
        /// </summary>
        /// <remarks>Exceuted after an appropriate user handler is called but before the parser state is updated.</remarks>
        protected virtual void OnProductionCompleted(ILGenerator il, Production production)
        {
            if (onProductionCompleted != null)
            {
                ExecuteMethod(il, onProductionCompleted, () =>
                {
                    il.Emit(OpCodes.Ldstr, production.Name);
                });
            }
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
            // Inherit base type constructors.
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

        /// <summary>
        /// Generates a method to handle the end of the source.
        /// </summary>
        /// <remarks>
        /// <code>void HandleEndOfSource();</code>
        /// </remarks>
        private void BuildHandleEndOfSourceMethod()
        {
            var il = handleEosMthd.GetILGenerator();
            var start = il.DefineLabel();
            var labels = il.DefineLabels(States.Length);

            il.MarkLabel(start);
            stateStack.Peek(il, 0);
            il.Emit(OpCodes.Switch, labels);
            il.ThrowInvalidOperationException(Errors.InvalidState());
            il.Mark(labels, i => HandleEndOfSource(il, States[i], start));
        }

        private void HandleEndOfSource(ILGenerator il, StateMetadata state, Label start)
        {
            if (state.ParserState.Reduce.TryGetValue(Symbol.EndOfSource, out var item))
            {
                if (item.Head == Symbol.Target)
                {
                    // Goal symbol is reduced.
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
                ThrowParserException(il, Errors.UnexpectedEndOfSoruce());
            }
        }


        /// <summary>
        /// Generates a method that processes a specific token.
        /// </summary>
        /// <remarks>
        /// <code>void HandleLexeme(int tokenId);</code>
        /// </remarks>
        private void BuildHandleLexemeMethod()
        {
            var il = handleLexemeMthd.GetILGenerator();
            var labels = il.DefineLabels(Terminals.Length);

            il.Emit(OpCodes.Ldarg_1); // token Id
            il.Emit(OpCodes.Switch, labels);
            il.ThrowArgumentOutOfRangeException("tokenId", Errors.TokenOutOfRange());
            il.Mark(labels, i =>
            {
                HandleLexeme(il, i);
                il.Emit(OpCodes.Ret);
            });
        }

        private void HandleLexeme(ILGenerator il, int tokenId)
        {
            var terminal = Terminals[tokenId];
            var method = Reducer.GetTokenReducer(terminal.Name);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, tokenId);

            if (method == null || !ExecuteMethod(il, method, Array.Empty<ParameterInfo>()))
            {
                il.Emit(OpCodes.Ldnull);
            }

            OnTokenCompleted(il, terminal);

            StartPosition.Update(il, () => CurrentPosition.Load(il));
            il.Emit(OpCodes.Call, handleTerminalMthd);
        }

        /// <summary>
        /// Generates a method that processes a terminal and updates the parser state accordingly.
        /// </summary>
        /// <remarks>
        /// <code>void HandleTerminal(int tokenId, object tokenValue);</code>
        /// </remarks>
        private void BuildHandleTerminalMethod()
        {
            var il = handleTerminalMthd.GetILGenerator();
            var start = il.DefineLabel();
            var labels = il.DefineLabels(States.Length);

            // Read most recent state.
            il.MarkLabel(start);
            stateStack.Peek(il, 0);

            il.Emit(OpCodes.Switch, labels);
            il.ThrowInvalidOperationException(Errors.InvalidState());
            il.Mark(labels, i => HandleTerminals(il, States[i], start));
        }

        private void HandleTerminals(ILGenerator il, StateMetadata state, Label start)
        {
            // Perform a binary search over the list of valid terminals.
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
                il.Emit(OpCodes.Ldstr, Errors.InvalidTerminal("{0}"));
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
                    // Put a terminal value on the stack.
                    dataStack.Push(il, () => il.Emit(OpCodes.Ldarg_2));
                }

                // Reset the value for line break cell and update the state.
                isLineBreak.Update(il, false);
                LexerState.Update(il, States[next.Id].LexicalState.Id);
                stateStack.Push(il, () => il.Emit(OpCodes.Ldc_I4, next.Id));

                // Execute user handlers for a new state constructed.
                HandlePrefixes(il, next);
                il.Emit(OpCodes.Ret);
            }
            else // ParserAction.Reduce
            {
                // Once the pruduction is processed need to apply a pending token against the updated state.
                HandleProduction(il, state.ParserState.Reduce[terminal]);
                il.Emit(OpCodes.Br, start);
            }
        }

        private void HandleWhitespace(ILGenerator il, StateMetadata state, Terminal whitespace)
        {
            if (Grammar.IsNewLine(whitespace))
            {
                // Remember that a line break terminal encountered in case it matters.
                isLineBreak.Update(il, () => il.LoadTrue());

                if (state.ParserState.Shift.TryGetValue(Symbol.LineBreak, out var nextLB))
                {
                    // A line break sensitive state. Update accordingly.
                    state = States[nextLB.Id];
                    stateStack.ReplaceTop(il, 1, () => il.Emit(OpCodes.Ldc_I4, nextLB.Id));
                }
            }

            // Reset the lexer state.
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
                        // Push an output onto the value stack.
                        dataStack.Push(il, () => ExecuteMethod(il, method, parameters));
                    }
                    else
                    {
                        // Replace the top parameters with an output.
                        dataStack.ReplaceTop(il, parameters.Length, () => ExecuteMethod(il, method, parameters));
                    }
                }
                else
                {
                    // Remove parameters from the value stack.
                    ExecuteMethod(il, method, parameters);
                    dataStack.RemoveTop(il, parameters.Length);
                }
            }

            stateStack.RemoveTop(il, production.Size);
            OnProductionCompleted(il, production);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ((NonTerminal)production.Head).Id);
            il.Emit(OpCodes.Call, handleNonTerminalMthd);
        }

        /// <summary>
        /// Generates a method that processes a non-terminal and updates the parser state accordingly.
        /// </summary>
        /// <remarks>
        /// <code>void HandleNonTerminal(int nonTerminalId);</code>
        /// </remarks>
        private void BuildHandleNonTerminalMethod()
        {
            var il = handleNonTerminalMthd.GetILGenerator();
            var labels = il.DefineLabels(States.Length);

            // Read most recent state.
            stateStack.Peek(il, 0);
            il.Emit(OpCodes.Switch, labels);
            il.ThrowInvalidOperationException(Errors.InvalidState());
            il.Mark(labels, i => HandleNonTerminals(il, States[i]));
        }

        private void HandleNonTerminals(ILGenerator il, StateMetadata state)
        {
            // Perform a binary search over the list of valid non-terminals.
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
                il.Emit(OpCodes.Ldstr, Errors.InvalidNonTerminal("{0}"));
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
                // Handle a line break sensitive state.
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

            // Iterate through the state core items and check if any handler is defined for the prefix.
            foreach (var item in state.Core)
            {
                var method = Reducer.GetPrefixHandler(item.GetPrefix());

                if (method != null && set.Add(method))
                {
                    // Remove an output from the evaluation stack if any.
                    if (ExecuteMethod(il, method))
                    {
                        il.Emit(OpCodes.Pop);
                    }
                }
            }
        }


        /// <remarks>
        /// <code>string GetTerminalName(int terminalId);</code>
        /// </remarks>
        private void BuildGetTerminalNameMethod()
        {
            var il = getTerminalNameMthd.GetILGenerator();
            var labels = il.DefineLabels(Terminals.Length);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Switch, labels);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            il.Mark(labels, i =>
            {
                il.Emit(OpCodes.Ldstr, Terminals[i].Name);
                il.Emit(OpCodes.Ret);
            });
        }

        /// <remarks>
        /// <code>string GetNonTerminalName(int nonTerminalId);</code>
        /// </remarks>
        private void BuildGetNonTerminalNameMethod()
        {
            var il = getNonTerminalNameMthd.GetILGenerator();
            var labels = il.DefineLabels(NonTerminals.Length);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Switch, labels);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            il.Mark(labels, i =>
            {
                il.Emit(OpCodes.Ldstr, NonTerminals[i].Name);
                il.Emit(OpCodes.Ret);
            });
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

        /// <summary>
        /// Concatenates current symbols on the parser state stack into a string value.
        /// </summary>
        /// <remarks>
        /// <code>string GetParserState();</code>
        /// </remarks>
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

                il.Mark(labels, i =>
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

        /// <remarks>
        /// <code>int StartPosition { get; }</code>
        /// </remarks>
        private void BuildStartPositionProperty()
        {
            var method = Target.DefineMethod("get_StartPosition",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes);
            var il = method.GetILGenerator();

            StartPosition.Load(il);
            il.Emit(OpCodes.Ret);
        }

        /// <remarks>
        /// <code>int CurrentPosition { get; }</code>
        /// </remarks>
        private void BuildCurrentPositionProperty()
        {
            var method = Target.DefineMethod("get_CurrentPosition",
                MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes);
            var il = method.GetILGenerator();

            CurrentPosition.Load(il);
            il.Emit(OpCodes.Ret);
        }

        /// <remarks>
        /// <code>bool IsCompleted { get; }</code>
        /// </remarks>
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

        /// <summary>
        /// Generates the method that return a value form the top of the value stack or <c>null</c> if the stack is emtpy.
        /// </summary>
        /// <remarks>
        /// <code>object GetTopValue();</code>
        /// </remarks>
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
            ExecuteMethod(il, method, () =>
            {
                for (int i = 1; i <= parameters.Length; i++)
                {
                    dataStack.Peek(il, parameters.Length - i);
                    il.ConvertFromObject(parameters[i - 1].ParameterType);
                }
            });

            if (method.ReturnType != typeof(void))
            {
                il.ConvertToObject(method.ReturnType);
                return true;
            }

            return false;
        }

        private void ExecuteMethod(ILGenerator il, MethodInfo method, Action loadArgs)
        {
            if (method.IsStatic)
            {
                loadArgs();
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                loadArgs();
                il.Emit(OpCodes.Callvirt, method);
            }
        }
    }
}
