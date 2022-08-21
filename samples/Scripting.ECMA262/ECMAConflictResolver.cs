namespace Scripting.ECMA262
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Ry.ParseLib;
    using Ry.ParseLib.LALR;

    public sealed class ECMAConflictResolver : ConflictResolver
    {
        private readonly Dictionary<string, int> operators;

        public ECMAConflictResolver()
        {
            this.operators = new Dictionary<string, int>
            {
                { "*", 0 }, { "/", 0 }, { "%", 0 },
                { "+", 1 }, { "-", 1 },
                { ">>", 2 }, { "<<", 2 }, { ">>>", 2 },
                { "<", 3 }, { "<=", 3 }, { ">", 3 }, { ">=", 3 }, { "instanceof", 3 }, { "in", 3 },
                { "==", 4 }, { "!=", 4 }, { "===", 4 }, { "!==", 4 },
                { "&", 5 }, { "^", 5 }, { "|", 5 },
            };
        }

        public override ParserAction ResolveShiftConflict(Symbol symbol, Production production)
        {
            switch (production.Name)
            {
                case "expr-log:and":
                case "expr-log:or":
                    Debug.Assert(symbol.Name == "&&" || symbol.Name == "||");
                    return ParserAction.Reduce;
                case "expr-binary:exp":
                case "expr-binary:mul":
                case "expr-binary:div":
                case "expr-binary:mod":
                    Debug.Assert(symbol.Name != "**");
                    return ParserAction.Reduce;
                case "expr-binary:add":
                case "expr-binary:sub":
                    return operators[symbol.Name] == 0 ? ParserAction.Shift : ParserAction.Reduce;
                case "expr-binary:shift-left":
                case "expr-binary:shift-right":
                case "expr-binary:shift-right-u":
                    return operators[symbol.Name] < 2 ? ParserAction.Shift : ParserAction.Reduce;
                case "expr-binary:lt":
                case "expr-binary:gt":
                case "expr-binary:lte":
                case "expr-binary:gte":
                case "expr-binary:instanceof":
                case "expr-binary:in":
                    return operators[symbol.Name] < 3 ? ParserAction.Shift : ParserAction.Reduce;
                case "expr-binary:eq":
                case "expr-binary:not-eq":
                case "expr-binary:eq-strict":
                case "expr-binary:not-eq-strict":
                    return operators[symbol.Name] < 4 ? ParserAction.Shift : ParserAction.Reduce;
                case "expr-binary:bit-and":
                case "expr-binary:bit-or":
                case "expr-binary:bit-xor":
                    return operators[symbol.Name] < 5 ? ParserAction.Shift : ParserAction.Reduce;
            }

            return base.ResolveShiftConflict(symbol, production);
        }

        public override ParserItem[] ResolveCoreConflicts(Symbol symbol, ParserItem[] core)
        {
            switch (symbol.Name)
            {
                case "{": return Prefer(core, "func-body:");
            }

            return base.ResolveCoreConflicts(symbol, core);
        }

        static private ParserItem[] Prefer(ParserItem[] core, params string[] prefixes)
        {
            for (int i = 0; i < core.Length; i++)
            {
                foreach (var prefix in prefixes)
                {
                    if (core[i].Production.Name.StartsWith(prefix))
                    {
                        return Prefer(core, prefix, i);
                    }
                }
            }

            return core;
        }

        static private ParserItem[] Prefer(ParserItem[] core, string prefix, int index)
        {
            var items = new List<ParserItem> { core[index++] };

            for (; index < core.Length; index++)
            {
                if (core[index].Production.Name.StartsWith(prefix))
                {
                    items.Add(core[index]);
                }
            }

            return items.ToArray();
        }
    }
}
