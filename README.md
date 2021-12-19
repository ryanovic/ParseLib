# ParseLib
.NET runtime LALR parser generator

## Usage
Parser base:
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
Grammar initialization and parser generation:
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

var factory = grammar.CreateStringParserFactory<ExpressionParser>("expr");
var parser = factory("-2 + -(2 * 2)");

parser.Parse();
Console.WriteLine(parser.GetResult());
```