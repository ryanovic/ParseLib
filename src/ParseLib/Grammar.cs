namespace ParseLib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ParseLib.Text;
    using ParseLib.LALR;

    public class Grammar
    {
        private int nonTerminalId = 0;
        private int terminalId = 0;

        public bool IgnoreCase { get; }
        public IConflictResolver ConflictResolver { get; }
        public IDictionary<string, Symbol> Symbols { get; } = new Dictionary<string, Symbol>();
        public IDictionary<Terminal, bool> Whitespaces { get; } = new Dictionary<Terminal, bool>();

        public Grammar(IConflictResolver conflictResolver = null, bool ignoreCase = false)
        {
            this.ConflictResolver = conflictResolver;
            this.IgnoreCase = ignoreCase;

            AddSymbol(Symbol.LineBreak);
            AddSymbol(Symbol.NoLineBreak);
            AddSymbol(Symbol.EndOfSource);
        }

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

        public bool Contains(string name) => Symbols.ContainsKey(name);

        public bool ContainsTerminal(string name) => Symbols.TryGetValue(name, out var symbol) && symbol.Type == SymbolType.Terminal;

        public bool ContainsNonTerminal(string name) => Symbols.TryGetValue(name, out var symbol) && symbol.Type == SymbolType.NonTerminal;

        public bool IsNewLine(Terminal terminal) => Whitespaces.TryGetValue(terminal, out var newLine) && newLine;

        public NonTerminal GetNonTerminal(string name)
        {
            var symbol = Symbols[name];

            if (symbol is NonTerminal nonTerminal)
            {
                return nonTerminal;
            }

            throw new InvalidOperationException(Errors.NonTerminalExpected(symbol.Name));
        }

        public Terminal GetTerminal(string name)
        {
            var symbol = Symbols[name];

            if (symbol is Terminal terminal)
            {
                return terminal;
            }

            throw new InvalidOperationException(Errors.TerminalExpected(symbol.Name));
        }

        public Terminal[] GetTerminals()
        {
            var symbols = new Terminal[terminalId];

            foreach (var terminal in Symbols.Values.OfType<Terminal>())
            {
                symbols[terminal.Id] = terminal;
            }

            return symbols;
        }

        public NonTerminal[] GetNonTerminals()
        {
            var symbols = new NonTerminal[nonTerminalId];

            foreach (var nonTerminal in Symbols.Values.OfType<NonTerminal>())
            {
                symbols[nonTerminal.Id] = nonTerminal;
            }

            return symbols;
        }

        public Terminal CreateWhitespace(string name, RexNode expression, bool isLineBreak = false, bool lazy = false)
        {
            var ws = CreateTerminal(name, expression, lazy);
            Whitespaces.Add(ws, isLineBreak);
            return ws;
        }

        public Terminal CreateTerminal(string name, RexNode expression, bool lazy = false)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            Terminal terminal;

            if (TryResolveComplexName(name, out var head, out var local))
            {
                terminal = new Terminal(local, expression, terminalId++, lazy);
                AddSymbol(terminal);
                AddRule(head, name, local);
            }
            else
            {
                terminal = new Terminal(name, expression, terminalId++, lazy);
                AddSymbol(terminal);
            }

            return terminal;
        }

        public void CreateTerminals(params string[] lexemes)
        {
            if (lexemes == null) throw new ArgumentNullException(nameof(lexemes));

            foreach (var lexeme in lexemes)
            {
                AddSymbol(new Terminal(lexeme, terminalId++));
            }
        }

        public NonTerminal CreateNonTerminal(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var nonTerminal = new NonTerminal(name, nonTerminalId++);
            AddSymbol(nonTerminal);
            return nonTerminal;
        }

        public void CreateNonTerminals(params string[] names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            foreach (var name in names)
            {
                CreateNonTerminal(name);
            }
        }

        public bool ContainsRule(string name)
        {
            return GetNonTerminal(GetNonTerminalName(name)).ContainsProduction(name);
        }

        public Production GetRule(string name)
        {
            return GetNonTerminal(GetNonTerminalName(name)).GetProduction(name);
        }

        public ProductionBuilder AddRule(string name, string body)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return AddRule(GetNonTerminalName(name), name, body);
        }

        public ProductionBuilder AddRule(string head, string productionName, string body)
        {
            if (head == null) throw new ArgumentNullException(nameof(head));
            if (productionName == null) throw new ArgumentNullException(nameof(productionName));
            if (body == null) throw new ArgumentNullException(nameof(body));

            return new ProductionBuilder(this, GetNonTerminal(head).AddProduction(productionName, ParseSymbols(body)));
        }

        public Symbol[] ParseSymbols(string body)
        {
            var symbols = new List<Symbol>();

            for (int i = 0, start = 0; i < body.Length; i++)
            {
                if (body[i] != ' ')
                {
                    if (i == 0 || body[i - 1] == ' ')
                    {
                        start = i;
                    }

                    if (i == body.Length - 1 || body[i + 1] == ' ')
                    {
                        symbols.Add(Symbols[body.Substring(start, i - start + 1)]);
                    }
                }
            }

            return symbols.ToArray();
        }

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
            Symbols.Add(symbol.Name, symbol);
            return symbol;
        }

        private static string GetNonTerminalName(string name)
        {
            return TryResolveComplexName(name, out var owner, out var _) ? owner : name;
        }

        private static bool TryResolveComplexName(string name, out string owner, out string local)
        {
            int index = name.IndexOf(':');

            if (index > 0)
            {
                owner = name.Substring(0, index);
                local = name.Substring(index + 1);
                return true;
            }
            else
            {
                local = owner = null;
                return false;
            }
        }
    }
}
