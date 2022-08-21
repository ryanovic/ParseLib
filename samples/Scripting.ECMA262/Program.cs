using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.IO;
using System.Text;
using Ry.ParseLib;
using Ry.ParseLib.Runtime;

namespace Scripting.ECMA262
{
    class Program
    {
        static void Main(string[] args)
        {
            var grammar = new ECMAGrammar();
            var expr_list = grammar.CreateExpression(ECMAGrammarContext.None);
            var factory = grammar.CreateTextParserFactory<ECMA262ParserBase>(expr_list.Name, typeName: "ECMA262Parser");

            var timer = Stopwatch.StartNew();
            var parser = factory(new StringReader(@"expr.id1['12'](false, -2 + -2 * 2)`a${1 ** x}`"));
            parser.Parse();
            timer.Stop();
            Console.WriteLine($"{parser.GetResult()} in {timer.ElapsedMilliseconds}ms");
        }
    }
}
