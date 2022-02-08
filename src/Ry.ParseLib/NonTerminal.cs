namespace Ry.ParseLib
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Text;

    /// <summary>
    /// Represents a non-terminal symbol.
    /// </summary>
    public class NonTerminal : Symbol, IEnumerable<Production>
    {
        private readonly Grammar grammar;
        private readonly Dictionary<string, Production> productions;

        public int Id { get; }
        public IEnumerable<string> Keys => productions.Keys;
        public Production this[string name] => productions[name];

        public NonTerminal(string name, int id, Grammar grammar)
            : base(name, SymbolType.NonTerminal)
        {
            if (grammar == null) throw new NullReferenceException(nameof(grammar));

            this.Id = id;
            this.grammar = grammar;
            this.productions = new Dictionary<string, Production>();
        }

        public bool ContainsProduction(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return productions.ContainsKey(name);
        }

        public Production GetProduction(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (productions.TryGetValue(name, out var production))
            {
                return production;
            }

            throw new InvalidOperationException(Errors.ProductionNotFound(name));
        }

        public bool TryGetProduction(string name, out Production production)
        {
            return productions.TryGetValue(name, out production);
        }

        public ProductionBuilder AddProduction(string name, string body)
        {
            return AddProduction(name, grammar.ParseSymbols(body));
        }

        public ProductionBuilder AddProduction(string name, params object[] body)
        {
            return AddProduction(name, grammar.GetSymbols(body));
        }

        public ProductionBuilder AddProduction(string name, params Symbol[] body)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (body == null) throw new ArgumentNullException(nameof(body));

            if (productions.ContainsKey(name))
            {
                throw new InvalidOperationException(Errors.ProductionDefined(name));
            }

            var production = new Production(this, name, body);
            productions.Add(name, production);
            return new ProductionBuilder(grammar, production);
        }

        public IEnumerator<Production> GetEnumerator()
        {
            return productions.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return productions.Values.GetEnumerator();
        }
    }
}
