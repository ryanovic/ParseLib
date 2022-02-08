namespace Ry.ParseLib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ry.ParseLib.Text;
    using Ry.ParseLib.LALR;

    /// <summary>
    /// Represents a set of terminals and non-terminals that define a target grammar.
    /// </summary>
    public class Grammar
    {
        private int nonTerminalId = 0;
        private int terminalId = 0;

        /// <summary>
        /// Gets a value indicating whether every terminal should be treated as case-insensitive.
        /// </summary>
        public bool IgnoreCase { get; }

        /// <summary>
        /// Gets a custom conflict resolver, if one is defined.
        /// </summary>
        public IConflictResolver ConflictResolver { get; }

        /// <summary>
        /// Gets a set of all symbols defined in the grammar.
        /// </summary>
        public IDictionary<string, Symbol> Symbols { get; } = new Dictionary<string, Symbol>();

        /// <summary>
        /// Gets a set of all whitespaces defined in the grammar. A boolean value indicates whether the terminal is a line-break;
        /// </summary>
        public IDictionary<Terminal, bool> Whitespaces { get; } = new Dictionary<Terminal, bool>();

        public Grammar(IConflictResolver conflictResolver = null, bool ignoreCase = false)
        {
            this.ConflictResolver = conflictResolver;
            this.IgnoreCase = ignoreCase;

            AddSymbol(Symbol.LineBreak);
            AddSymbol(Symbol.NoLineBreak);
            AddSymbol(Symbol.EndOfSource);
        }


        /// <summary>
        /// Gets a symbol by a specified name. Throws an exception, if the symbol is not defined.
        /// </summary>
        public Symbol this[string name]
        {
            get
            {
                if (Symbols.TryGetValue(name, out var symbol))
                {
                    return symbol;
                }

                throw new KeyNotFoundException(Errors.SymbolNotFound(name));
            }
        }

        /// <summary>
        /// Gets a value indicating whether a symbol with a specified name is defined in the grammar.
        /// </summary>
        public bool Contains(string name) => Symbols.ContainsKey(name);

        /// <summary>
        /// Gets a value indicating whether a terminal with a specified name is defined in the grammar.
        /// </summary>
        public bool ContainsTerminal(string name) => Symbols.TryGetValue(name, out var symbol) && symbol.Type == SymbolType.Terminal;

        /// <summary>
        /// Gets a value indicating whether a non-terminal with a specified name is defined in the grammar.
        /// </summary>
        public bool ContainsNonTerminal(string name) => Symbols.TryGetValue(name, out var symbol) && symbol.Type == SymbolType.NonTerminal;

        /// <summary>
        /// Gets a value indicating whether a specified terminal is a line-break;
        /// </summary>
        public bool IsNewLine(Terminal terminal) => Whitespaces.TryGetValue(terminal, out var newLine) && newLine;

        /// <summary>
        /// Gets a non-terminal by a specified name. Throws an exception if the non-terminal can't be resolved.
        /// </summary>
        public NonTerminal GetNonTerminal(string name)
        {
            var symbol = Symbols[name];

            if (symbol is NonTerminal nonTerminal)
            {
                return nonTerminal;
            }

            throw new InvalidOperationException(Errors.NonTerminalExpected(symbol.Name));
        }

        /// <summary>
        /// Gets a terminal by a specified name. Throws an exception if the terminal can't be resolved.
        /// </summary>
        public Terminal GetTerminal(string name)
        {
            var symbol = Symbols[name];

            if (symbol is Terminal terminal)
            {
                return terminal;
            }

            throw new InvalidOperationException(Errors.TerminalExpected(symbol.Name));
        }

        /// <summary>
        /// Gets a set of terminals defined in the grammar. A terminal ID corresponds to an index in the array.
        /// </summary>
        public Terminal[] GetTerminals()
        {
            var symbols = new Terminal[terminalId];

            foreach (var terminal in Symbols.Values.OfType<Terminal>())
            {
                symbols[terminal.Id] = terminal;
            }

            return symbols;
        }

        /// <summary>
        /// Gets a set of non-terminals defined in the grammar. A non-terminal ID corresponds to an index in the array.
        /// </summary>
        public NonTerminal[] GetNonTerminals()
        {
            var symbols = new NonTerminal[nonTerminalId];

            foreach (var nonTerminal in Symbols.Values.OfType<NonTerminal>())
            {
                symbols[nonTerminal.Id] = nonTerminal;
            }

            return symbols;
        }

        /// <summary>
        /// Creates a new whitespace with a specified name.
        /// </summary>
        public Terminal CreateWhitespace(string name, RexNode expression, bool isLineBreak = false, bool lazy = false)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var ws = CreateTerminal(name, expression, lazy);
            Whitespaces.Add(ws, isLineBreak);
            return ws;
        }

        /// <summary>
        /// Creates a new terminal with a specified name.
        /// </summary>
        public Terminal CreateTerminal(string name, RexNode expression, bool lazy = false)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var terminal = new Terminal(name, expression, terminalId++, lazy);
            AddSymbol(terminal);
            return terminal;
        }

        /// <summary>
        /// Creates a list of terminals with a name equal to the regular expression.
        /// </summary>
        public void CreateTerminals(params string[] lexemes)
        {
            if (lexemes == null) throw new ArgumentNullException(nameof(lexemes));

            foreach (var lexeme in lexemes)
            {
                AddSymbol(new Terminal(lexeme, terminalId++));
            }
        }

        /// <summary>
        /// Creates a terminal with a name equal to the regular expression.
        /// </summary>
        public Terminal CreateTerminal(string lexeme)
        {
            if (lexeme == null) throw new ArgumentNullException(nameof(lexeme));

            var terminal = new Terminal(lexeme, terminalId++);
            AddSymbol(terminal);
            return terminal;
        }

        /// <summary>
        /// Creates a non-terminal with a specified name.
        /// </summary>
        public NonTerminal CreateNonTerminal(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var nonTerminal = new NonTerminal(name, nonTerminalId++, this);
            AddSymbol(nonTerminal);
            return nonTerminal;
        }

        /// <summary>
        /// Creates a list of non-terminals.
        /// </summary>
        public void CreateNonTerminals(params string[] names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            foreach (var name in names)
            {
                CreateNonTerminal(name);
            }
        }

        /// <summary>
        /// Parses a set of symbols defined in the grammar.
        /// </summary>
        /// <param name="body">A space separated list of symbols that defines a production body.</param>
        public Symbol[] ParseSymbols(string body)
        {
            var symbols = new List<Symbol>();

            for (int i = 0, start = 0; i < body.Length; i++)
            {
                if (!Char.IsWhiteSpace(body[i]))
                {
                    if (i == 0 || Char.IsWhiteSpace(body[i - 1]))
                    {
                        start = i;
                    }

                    if (i == body.Length - 1 || Char.IsWhiteSpace(body[i + 1]))
                    {
                        symbols.Add(this[body.Substring(start, i - start + 1)]);
                    }
                }
            }

            return symbols.ToArray();
        }

        /// <summary>
        /// Resolves a set of symbols defined in the grammar.
        /// </summary>
        /// <param name="body">A space separated list of symbols that can be represented either by a <see cref="Symbol"/> instance or its name.</param>
        public Symbol[] GetSymbols(params object[] body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            return Utils.Transform(body, nameOrSymbol => GetSymbol(nameOrSymbol));
        }

        private Symbol GetSymbol(object nameOrSymbol)
        {
            if (nameOrSymbol == null)
            {
                throw new ArgumentNullException(nameof(nameOrSymbol));
            }

            if (nameOrSymbol is string name)
            {
                return Symbols[name];
            }

            if (nameOrSymbol is Symbol symbol)
            {
                return symbol;
            }

            throw new InvalidOperationException();
        }

        private Symbol AddSymbol(Symbol symbol)
        {
            if (Symbols.ContainsKey(symbol.Name))
            {
                throw new InvalidOperationException(Errors.SymbolDefined(symbol.Name));
            }

            if (symbol.Name.Any(x => Char.IsWhiteSpace(x)))
            {
                throw new ArgumentException(Errors.SymbolNameWhitespace(), nameof(symbol));
            }

            Symbols.Add(symbol.Name, symbol);
            return symbol;
        }
    }
}
