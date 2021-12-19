namespace ParseLib
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Text;

    public class NonTerminal : Symbol, IEnumerable<Production>
    {
        private readonly Dictionary<string, Production> productions;

        public int Id { get; }
        public IEnumerable<string> Keys => productions.Keys;
        public Production this[string name] => productions[name];

        public NonTerminal(string name, int id)
            : base(name, SymbolType.NonTerminal)
        {
            this.Id = id;
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

            return productions[name];
        }

        public Production AddProduction(string name, Symbol[] body)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (body == null) throw new ArgumentNullException(nameof(body));

            var production = new Production(this, name, body);
            productions.Add(name, production);
            return production;
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
