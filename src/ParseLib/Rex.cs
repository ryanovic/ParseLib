namespace ParseLib
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;
    using ParseLib.Emit;
    using ParseLib.Text;

    public delegate int RexEvaluator(ReadOnlySpan<char> content);

    public static class Rex
    {
        /// <summary>
        /// Creates an expression that matches any single Unicode character.
        /// </summary>
        public static RexNode AnyChar { get; } = new RexChar(CharSet.Any);

        /// <summary>
        /// Creates an expression that matches any Unicode character sequence.
        /// </summary>
        public static RexNode AnyText { get; } = AnyChar.NoneOrMore();

        /// <summary>
        /// Creates an expression that matches a specified text.
        /// </summary>
        public static RexNode Text(string text)
        {
            return text.Select(ch => Char(ch)).Aggregate((a, b) => new RexAnd(a, b));
        }

        /// <summary>
        /// Creates an expression that matches a specified character.
        /// </summary>
        public static RexNode Char(char ch)
        {
            return new RexChar(new CharSet(new[] { new UnicodeRange(ch) }));
        }

        /// <summary>
        /// Creates an expression that matches a character represented by a specified charset.
        /// </summary>
        public static RexNode Char(CharSet cs)
        {
            return new RexChar(cs);
        }

        /// <summary>
        /// Creates an expression that matches a character represented by a specified pattern.
        /// </summary>
        public static RexNode Char(string pattern)
        {
            return new RexChar(CharSet.Parse(pattern));
        }

        /// <summary>
        /// Creates an expression that matches any Unicode character except a specified character.
        /// </summary>
        public static RexNode Except(char ch)
        {
            var cs = new CharSet(new[] { new UnicodeRange(ch) });
            return new RexChar(cs.Negate());
        }

        /// <summary>
        /// Creates an expression that matches any Unicode character except a character represented by a specified charset.
        /// </summary>
        public static RexNode Except(CharSet cs)
        {
            return new RexChar(cs.Negate());
        }

        /// <summary>
        /// Creates an expression that matches any Unicode character except a character represented by a specified pattern.
        /// </summary>
        public static RexNode Except(string pattern)
        {
            return new RexChar(CharSet.Parse(pattern).Negate());
        }

        /// <summary>
        /// Creates an expression that matches any word from a specified collection.
        /// </summary>
        public static RexNode Or(params string[] words)
        {
            return words.Select(w => Text(w)).Aggregate((a, b) => new RexOr(a, b));
        }

        /// <summary>
        /// Creates an expression that matches any character from a specified collection.
        /// </summary>
        public static RexNode Or(params char[] chars)
        {
            return chars.Select(ch => Char(ch)).Aggregate((a, b) => new RexOr(a, b));
        }

        /// <summary>
        /// Creates an expression that matches any expression from a specified collection.
        /// </summary>
        public static RexNode Or(params RexNode[] nodes)
        {
            return nodes.Aggregate((a, b) => new RexOr(a, b));
        }

        /// <summary>
        /// Creates an expression that matches a sequence of a specified set of words.
        /// </summary>
        public static RexNode Concat(params string[] words)
        {
            return words.Select(w => Text(w)).Aggregate((a, b) => new RexAnd(a, b));
        }

        /// <summary>
        /// Creates an expression that matches a sequence of a specified set of characters.
        /// </summary>
        public static RexNode Concat(params char[] chars)
        {
            return chars.Select(ch => Char(ch)).Aggregate((a, b) => new RexAnd(a, b));
        }

        /// <summary>
        /// Creates an expression that matches a sequence of a specified set of expressions.
        /// </summary>
        public static RexNode Concat(params RexNode[] nodes)
        {
            return nodes.Aggregate((a, b) => new RexAnd(a, b));
        }

        /// <summary>
        /// Creates a conditional expression that allows execution only if a specified expression doesn't match.
        /// </summary>
        public static RexNode IfNot(RexNode node)
        {
            return new RexSentinel(node, positive: false);
        }

        /// <summary>
        /// Creates a conditional expression defined by a specified character that allows execution only if the expression doesn't match.
        /// </summary>
        public static RexNode IfNot(char ch)
        {
            return new RexSentinel(Char(ch), positive: false);
        }

        /// <summary>
        /// Creates a conditional expression defined by a specified word that allows execution only if the expression doesn't match.
        /// </summary>
        public static RexNode IfNot(string text)
        {
            return new RexSentinel(Text(text), positive: false);
        }

        /// <summary>
        /// Creates a conditional expression that allows execution only if a specified expression matches.
        /// </summary>
        public static RexNode If(RexNode node)
        {
            return new RexSentinel(node, positive: true);
        }

        /// <summary>
        /// Creates a conditional expression defined by a specified character that allows execution only if the expression matches.
        /// </summary>
        public static RexNode If(char ch)
        {
            return new RexSentinel(Char(ch), positive: true);
        }

        /// <summary>
        /// Creates a conditional expression defined by a specified text that allows execution only if the expression matches.
        /// </summary>
        public static RexNode If(string text)
        {
            return new RexSentinel(Text(text), positive: true);
        }

        /// <summary>
        /// Compiles a specified expression into a delegate method.
        /// </summary>
        public static RexEvaluator Compile(RexNode node, bool lazy = false, bool ignoreCase = false)
        {
            return RexEvaluatorBuilder.CreateDelegate(node, lazy, ignoreCase);
        }
    }
}
