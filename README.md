# ParseLib

.NET library that provides components for dynamic parsers generation.

[![NuGet version (ParseLib)](https://img.shields.io/nuget/v/ParseLib.svg?style=flat-square)](https://www.nuget.org/packages/ParseLib/)

## Features

- End-to-end solution with a built-in lexical analyzer.
- Fluent Regular Expressions building interface.
- LALR(1) grammars with a flexible conflicts handling mechanism.
- Unicode support.
- In-memory dynamic type generation.
- Async sequential parser implementation out-of-the-box.

For more information check the project's [wiki](https://github.com/ryanovic/ParseLib/wiki) page or examine the _samples_ folder.

## Usage Sample

First, we need to configure a grammar itself. For sample code we will define a single _number_ terminal and a set of rules for basic math operations:

```c#
var digit = Rex.Char("0-9");

var grammar = new Grammar();

grammar.CreateTerminals("+", "-", "/", "*", "(", ")");
grammar.CreateNonTerminals("expr");
grammar.CreateWhitespace("ws", Rex.Char(' ').OneOrMore());

grammar.CreateTerminal("expr:num", digit.OneOrMore());

grammar.AddRule("expr:unary", "- expr")
    .ReduceOn("*", "/", "+", "-");

grammar.AddRule("expr:add", "expr + expr")
    .ReduceOn("+", "-")
    .ShiftOn("*", "/");

grammar.AddRule("expr:sub", "expr - expr")
    .ReduceOn("+", "-")
    .ShiftOn("*", "/");

grammar.AddRule("expr:mul", "expr * expr")
    .ReduceOn("*", "/", "+", "-");

grammar.AddRule("expr:div", "expr / expr")
    .ReduceOn("*", "/", "+", "-");

grammar.AddRule("expr:group", "( expr )");
```

Then, we need a user-defined parser class where handers for the grammar tokens and rules are implemented. The parser also determines a target type of the output:

```c#
public abstract class ExpressionParser : StringParser
{
    public ExpressionParser(string expression) : base(expression)
    {
    }

    [CompleteToken("num")]
    public int CompleteNumber() => Int32.Parse(GetLexeme());

    [Reduce("expr:add")]
    public int Add(int x, int y) => x + y;

    [Reduce("expr:sub")]
    public int Sub(int x, int y) => x - y;

    [Reduce("expr:mul")]
    public int Mul(int x, int y) => x * y;

    [Reduce("expr:div")]
    public int Div(int x, int y) => x / y;

    [Reduce("expr:unary")]
    public int Negate(int x) => -x;
}
```

Finally, based on the grammar and the parser we can build a parser factory. The factory is just a wrapper for an appropriate constructor defined by a type of the parser was generated: 

``` C#
var factory = grammar.CreateStringParserFactory<ExpressionParser>("expr");
var parser = factory("-2 + -(2 * 2)");

parser.Parse();
Console.WriteLine(parser.GetResult());
```
