namespace ParseLib
{
    internal static class Errors
    {
        public static string ZeroSubArray() => "Can't replace zero sized sub-array.";

        public static string RangeReversed(string from, string to) => $"[{from}-{to}] range in reverse order.";

        public static string NegativeTokenId() => "Token Id must be non-negative.";

        public static string ExceptionOccurred() => "Exception has occurred.";

        public static string ParserNotCompleted() => "Can't get the result until source is not entirely processed.";

        public static string UnresolvedShift() => "Grammar contains unresolved shift-reduce conflict.";

        public static string UnresolvedReduce() => "Grammar contains unresolved reduce-reduce conflict.";

        public static string OffsetOutOfRange() => "Offset cannot be less than zero.";

        public static string LengthOutOfRange() => "Upper bound cannot be greater than string's length.";

        public static string ExpectedParameterlessReducer(string name) => $"{name}: reducer should be parameterless.";

        public static string ReducerDefined(string name) => $"{name}: reducer is already defined.";

        public static string PrefixDefined(string symbols) => $"{symbols}: prefix handler is already defined.";

        public static string TypeExpected(string name) => $"Cell of {name} type is expected.";

        public static string TokenNotFound(string name) => $"Token '{name}' is not defined on the grammar.";

        public static string ProductionNotFound(string name) => $"Production '{name}' is not defined on the grammar.";

        public static string SymbolNotFound(string name) => $"Symbol '{name}' is not defined on the grammar.";

        public static string UnexpectedEndOfSoruce() => "Unexpected end of source encountered.";

        public static string ParserBaseExpected() => "Target base type is expected to be inherited from the ParserBase.";

        public static string InvalidState() => "Invalid state encountered.";

        public static string UnexpectedCharacter() => "Unexpected character encountered.";

        public static string TokenOutOfRange() => "The token is out of range.";

        public static string InvalidTerminal(string name) => $"'{name}' terminal is not valid due to the current state of the parser.";

        public static string InvalidNonTerminal(string name) => $"'{name}' non-terminal is not valid due to the current state of the parser.";

        public static string TerminalExpected(string name) => $"'{name}' expected to be a terminal.";

        public static string NonTerminalExpected(string name) => $"'{name}' expected to be a non-terminal.";

        public static string NullableExpression() => "Terminal requires non-nullable regular expression to initialize.";

        public static string SymbolNotAllowed(string name) => $"{name} is not allowed in the production body.";

        public static string LineBreakForbidden() => "Can't use [LB] and [NoLB] simultaneously.";

        public static string StringConstructorExpected(string type) => $"Type {type} should have constructor accepting string argument defined.";

        public static string TextReaderConstructorExpected(string type) => $"Type {type} should have constructor accepting TextReader argument defined.";

        public static string InvalidPattern() => "Invalid char-set expression.";

        public static string InvalidCategory(string name) => $"Can't find '{name}' unicode category.";

        public static string SymbolNameWhitespace() => "Whitespace characters are not allowed in a symbol name.";
    }
}
