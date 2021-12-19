namespace ParseLib
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;
    using ParseLib.Emit;
    using ParseLib.Text;

    public delegate int RexEvaluator(string content, int offset, int length);

    public static class Rex
    {
        public static RexNode Text(string text)
        {
            return text.Select(ch => Char(ch)).Aggregate((a, b) => new RexAnd(a, b));
        }

        public static RexNode Char(char ch)
        {
            return new RexChar(new CharSet(new[] { new UnicodeRange(ch) }));
        }

        public static RexNode Char(CharSet cs)
        {
            return new RexChar(cs);
        }

        public static RexNode Char(string pattern)
        {
            return new RexChar(CharSet.Parse(pattern));
        }

        public static RexNode Except(char ch)
        {
            var cs = new CharSet(new[] { new UnicodeRange(ch) });
            return new RexChar(cs.Negate());
        }
        public static RexNode Except(CharSet cs)
        {
            return new RexChar(cs.Negate());
        }

        public static RexNode Except(string pattern)
        {
            return new RexChar(CharSet.Parse(pattern).Negate());
        }

        public static RexNode Or(params string[] words)
        {
            return words.Select(w => Text(w)).Aggregate((a, b) => new RexOr(a, b));
        }

        public static RexNode Or(params char[] chars)
        {
            return chars.Select(ch => Char(ch)).Aggregate((a, b) => new RexOr(a, b));
        }

        public static RexNode Or(params RexNode[] nodes)
        {
            return nodes.Aggregate((a, b) => new RexOr(a, b));
        }

        public static RexNode Concat(params string[] words)
        {
            return words.Select(w => Text(w)).Aggregate((a, b) => new RexAnd(a, b));
        }

        public static RexNode Concat(params char[] chars)
        {
            return chars.Select(ch => Char(ch)).Aggregate((a, b) => new RexAnd(a, b));
        }

        public static RexNode Concat(params RexNode[] nodes)
        {
            return nodes.Aggregate((a, b) => new RexAnd(a, b));
        }

        public static RexNode IfNot(RexNode node)
        {
            return new RexSentinel(node, positive: false);
        }

        public static RexNode IfNot(char ch)
        {
            return new RexSentinel(Char(ch), positive: false);
        }

        public static RexNode IfNot(string text)
        {
            return new RexSentinel(Text(text), positive: false);
        }

        public static RexNode If(RexNode node)
        {
            return new RexSentinel(node, positive: true);
        }

        public static RexNode If(char ch)
        {
            return new RexSentinel(Char(ch), positive: true);
        }

        public static RexNode If(string text)
        {
            return new RexSentinel(Text(text), positive: true);
        }

        public static RexEvaluator Compile(RexNode node, bool lazy = false, bool ignoreCase = false)
        {
            return RexEvaluatorBuilder.CreateDelegate(node, lazy, ignoreCase);
        }
    }
}
