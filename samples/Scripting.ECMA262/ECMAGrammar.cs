using System;
using System.Collections.Generic;
using System.Text;
using Ry.ParseLib;

namespace Scripting.ECMA262
{
    [Flags]
    public enum ECMAGrammarContext
    {
        None = 0,
        In = 1,
        Await = 2,
        Yield = 4,
        YieldAwait = 6,
        Return = 8
    }

    public sealed class ECMAGrammar : Grammar
    {
        public ECMAGrammar() : base(new ECMAConflictResolver())
        {
            CreateTerminals();
        }

        public Symbol CreateExpression(ECMAGrammarContext context) => GetOrCreateNonTerminal("expr", context, expr =>
        {
            var expr_in = CreateExpression(context | ECMAGrammarContext.In);
            var expr_yield = CreateExpression(context | ECMAGrammarContext.Yield);
            var expr_lhs = CreateLeftHandSideExpression(context & ECMAGrammarContext.YieldAwait);
            var expr_upd = CreateNonTerminal("expr-upd", context);
            var expr_unary = CreateNonTerminal("expr-unary", context);
            var expr_binary = CreateNonTerminal("expr-binary", context);
            var expr_log = CreateNonTerminal("expr-log", context);
            var expr_coalesce = CreateNonTerminal("expr-coalesce", context);
            var expr_condition = CreateNonTerminal("expr-condition", context);

            expr_upd.AddProduction("expr-upd:lhs", expr_lhs);
            expr_upd.AddProduction("expr-upd:inc-postfix", expr_lhs, Symbol.NoLineBreak, "++");
            expr_upd.AddProduction("expr-upd:dec-postfix", expr_lhs, Symbol.NoLineBreak, "--");
            expr_upd.AddProduction("expr-upd:inc-prefix", "++", expr_unary);
            expr_upd.AddProduction("expr-upd:dec-prefix", "--", expr_unary);

            expr_unary.AddProduction("expr-unary:upd", expr_upd);
            expr_unary.AddProduction("expr-unary:delete", "delete", expr_unary);
            expr_unary.AddProduction("expr-unary:void", "void", expr_unary);
            expr_unary.AddProduction("expr-unary:typeof", "typeof", expr_unary);
            expr_unary.AddProduction("expr-unary:add", "+", expr_unary);
            expr_unary.AddProduction("expr-unary:sub", "-", expr_unary);
            expr_unary.AddProduction("expr-unary:neg", "~", expr_unary);
            expr_unary.AddProduction("expr-unary:not", "!", expr_unary);

            if ((context & ECMAGrammarContext.Await) == ECMAGrammarContext.Await)
            {
                expr_unary.AddProduction("expr-unary:await", "await", expr_unary);
            }

            expr_binary.AddProduction("expr-binary:unary", expr_unary);
            expr_binary.AddProduction("expr-binary:exp", expr_upd, "**", expr_binary);
            expr_binary.AddProduction("expr-binary:mul", expr_binary, "*", expr_binary);
            expr_binary.AddProduction("expr-binary:div", expr_binary, "/", expr_binary);
            expr_binary.AddProduction("expr-binary:mod", expr_binary, "%", expr_binary);
            expr_binary.AddProduction("expr-binary:add", expr_binary, "+", expr_binary);
            expr_binary.AddProduction("expr-binary:sub", expr_binary, "-", expr_binary);
            expr_binary.AddProduction("expr-binary:shift-left", expr_binary, "<<", expr_binary);
            expr_binary.AddProduction("expr-binary:shift-right", expr_binary, ">>", expr_binary);
            expr_binary.AddProduction("expr-binary:shift-right-u", expr_binary, ">>>", expr_binary);
            expr_binary.AddProduction("expr-binary:lt", expr_binary, "<", expr_binary);
            expr_binary.AddProduction("expr-binary:gt", expr_binary, ">", expr_binary);
            expr_binary.AddProduction("expr-binary:lte", expr_binary, "<=", expr_binary);
            expr_binary.AddProduction("expr-binary:gte", expr_binary, ">=", expr_binary);
            expr_binary.AddProduction("expr-binary:instanceof", expr_binary, "instanceof", expr_binary);

            if ((context & ECMAGrammarContext.In) == ECMAGrammarContext.In)
            {
                expr_binary.AddProduction("expr-binary:in", expr_binary, "in", expr_binary);
            }

            expr_binary.AddProduction("expr-binary:eq", expr_binary, "==", expr_binary);
            expr_binary.AddProduction("expr-binary:not-eq", expr_binary, "!=", expr_binary);
            expr_binary.AddProduction("expr-binary:eq-strict", expr_binary, "===", expr_binary);
            expr_binary.AddProduction("expr-binary:not-eq-strict", expr_binary, "!==", expr_binary);

            expr_binary.AddProduction("expr-binary:bit-and", expr_binary, "&", expr_binary);
            expr_binary.AddProduction("expr-binary:bit-xor", expr_binary, "^", expr_binary);
            expr_binary.AddProduction("expr-binary:bit-or", expr_binary, "|", expr_binary);

            expr_coalesce.AddProduction("expr_coalesce:base", expr_binary, "??", expr_binary);
            expr_coalesce.AddProduction("expr_coalesce:expand", expr_coalesce, "??", expr_binary);

            expr_log.AddProduction("expr-log:binary", expr_binary);
            expr_log.AddProduction("expr-log:and", expr_log, "&&", expr_log);
            expr_log.AddProduction("expr-log:or", expr_log, "||", expr_log);

            expr_condition.AddProduction("expr-condition:log", expr_log);
            expr_condition.AddProduction("expr-condition:coalesce", expr_coalesce);
            expr_condition.AddProduction("expr-condition:log-condition", expr_log, "?", expr_in, ":", expr);
            expr_condition.AddProduction("expr-condition:coalesce-condition", expr_coalesce, "?", expr_in, ":", expr);

            expr.AddProduction("expr:condition", expr_condition);

            if ((context & ECMAGrammarContext.Yield) == ECMAGrammarContext.Yield)
            {
                expr.AddProduction("expr:yield", "yield");
                expr.AddProduction("expr:yield-expr", "yield", Symbol.NoLineBreak, expr_yield);
                expr.AddProduction("expr:yield-expr-gen", "yield", Symbol.NoLineBreak, "*", expr_yield);
            }

            expr.AddProduction("expr:assign", expr_lhs, "=", expr);
            expr.AddProduction("expr:assign-mul", expr_lhs, "*=", expr);
            expr.AddProduction("expr:assign-div", expr_lhs, "/=", expr);
            expr.AddProduction("expr:assign-mod", expr_lhs, "%=", expr);
            expr.AddProduction("expr:assign-add", expr_lhs, "+=", expr);
            expr.AddProduction("expr:assign-sub", expr_lhs, "-=", expr);
            expr.AddProduction("expr:assign-shift-left", expr_lhs, "<<=", expr);
            expr.AddProduction("expr:assign-shift-right", expr_lhs, ">>=", expr);
            expr.AddProduction("expr:assign-shift-right-u", expr_lhs, ">>>=", expr);
            expr.AddProduction("expr:assign-bit-and", expr_lhs, "&=", expr);
            expr.AddProduction("expr:assign-bit-xor", expr_lhs, "^=", expr);
            expr.AddProduction("expr:assign-bit-or", expr_lhs, "|=", expr);
            expr.AddProduction("expr:assign-and", expr_lhs, "&&=", expr);
            expr.AddProduction("expr:assign-or", expr_lhs, "||=", expr);
            expr.AddProduction("expr:assign-coalesce", expr_lhs, "??=", expr);

            expr.AddProduction("expr:arrow", CreateArrowFunction(context));
            expr.AddProduction("expr:arrow-async", CreateAsyncArrowFunction(context));
        });

        public Symbol CreateExpressionList(ECMAGrammarContext context) => GetOrCreateNonTerminal("expr-list", context, exprs =>
        {
            var expr = CreateExpression(context);
            exprs.AddProduction("expr-list:item", expr);
            exprs.AddProduction("expr-list:append", exprs, ",", expr);
        });

        public Symbol CreateStatement(ECMAGrammarContext context) => GetOrCreateNonTerminal("stmnt", context, stmnt =>
        {
        });

        public Symbol CreateDeclaration(ECMAGrammarContext context) => GetOrCreateNonTerminal("declaration", context, declaration =>
        {
        });

        public Symbol CreateStatementList(ECMAGrammarContext context) => GetOrCreateNonTerminal("stmnt-list", context, stmnt_list =>
        {
            var item = CreateNonTerminal("stmnt-list-item", context);
            item.AddProduction("stmnt-list-item:stmnt", CreateStatement(context));
            item.AddProduction("stmnt-list-item:declaration", CreateDeclaration(context));

            stmnt_list.AddProduction("stmnt-list:item", item);
            stmnt_list.AddProduction("stmnt-list:append", stmnt_list, item);
        });

        private void CreateTerminals()
        {
            CreateTerminals("await", "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete", "do", "else", "enum", "export",
                "extends", "false", "finally", "for", "function", "if", "import", "in", "instanceof", "new", "null", "return", "super", "switch", "this",
                "throw", "true", "try", "typeof", "var", "void", "while", "with", "yield", "async", "let");

            CreateTerminals("{", "}", "(", ")", "[", "]", ".", "...", ";", ",", "<", ">", "<=", ">=", "==", "!=", "===", "!==", "+", "-", "*", "/", "%", "**", "++",
                "--", "<<", ">>", ">>>", "&", "|", "^", "!", "~", "&&", "||", "??", "?", ":", "=", "+=", "-=", "*=", "/=", "%=", "**=", "<<=", ">>=", ">>>=", "&=",
                "|=", "^=", "&&=", "||=", "??=", "=>", "?.");

            CreateTerminals("get", "set", "target", "meta", "static");

            // Whitespace terminals can appear in any place in a production and serve as separators for grammar symbols.
            var no_line_break = Rex.Except(@"\r\n\u{2028-2029}");

            // Since the 'ws' terminal is declared earlier it will have priority over the `ws-lb`.            
            // Terminals created with `isLineBreak: true` will affect the execution of line-break sensitive productions. 
            CreateWhitespace("ws", Rex.Char(@"\u{0009|000B|000C|0020|00A0|FEFF}\p{Zs}").OneOrMore());
            CreateWhitespace("ws-lb", Rex.Char(@"\u{0009|000A|000B|000C|000D|0020|00A0|2028|2029|FEFF}\p{Zs}").OneOrMore(), isLineBreak: true);

            CreateWhitespace("comment", Rex.Text("//").Then(no_line_break.NoneOrMore()));

            // Regular expressions with `lazy: true` complete as soon as a final state reached.
            CreateWhitespace("ml-comment", Rex.Text("/*").Then(no_line_break.NoneOrMore()).Then("*/"), lazy: true);
            CreateWhitespace("ml-comment-lb", Rex.Text("/*").Then(Rex.AnyText).Then("*/"), isLineBreak: true, lazy: true);

            var digit = Rex.Char("0-9");
            var digit_sep = Rex.Char('_').Optional().Then(digit);
            var digits = digit.Then(digit_sep.NoneOrMore());
            var digit_positive = Rex.Char("1-9");
            var digit_hex = Rex.Char("0-9a-fA-F");
            var digit_hex_sep = Rex.Char('_').Optional().Then(digit_hex);
            var digit_bin = Rex.Char("0-1");
            var digit_bin_sep = Rex.Char('_').Optional().Then(digit_bin);
            var digit_oct = Rex.Char("0-7");
            var digit_oct_sep = Rex.Char('_').Optional().Then(digit_oct);

            var escHex = Rex.Or(
                Rex.Text("\\u").Then(digit_hex.Repeat(4)),
                Rex.Text("\\u{").Then(digit_hex.OneOrMore()).Then('}'));

            var id_start = Rex.Char(@"$_a-zA-Z\u{1885-1886|2118|212E|309B-309C}\p{L|Nl}").Or(escHex);
            var id_after = Rex.Char(@"$_a-zA-Z\u{200C|200D|00B7|0387|1369-1371|1885-1886|19DA|2118|212E|309B-309C}\p{L|Nl|Mn|Mc|Nd|Pc}").Or(escHex);

            CreateTerminal("id", id_start.Then(id_after.NoneOrMore()));

            var num_exp = Rex.Char("eE").Then(Rex.Char("+-").Optional()).Then(digits);
            var num_int = Rex.Or(
                Rex.Char('0'),
                digit_positive.Then(digit_sep.NoneOrMore()));
            var num_dec = Rex.Or(
                num_int.Then('.').Then(digits.Optional()).Then(num_exp.Optional()),
                Rex.Char('.').Then(digits).Then(num_exp.Optional()),
                num_int.Then(num_exp.Optional()));

            var num_bin = Rex.Char('0').Then(Rex.Char("bB")).Then(digit_bin).Then(digit_bin_sep.NoneOrMore());
            var num_oct = Rex.Char('0').Then(Rex.Char("oO")).Then(digit_oct).Then(digit_oct_sep.NoneOrMore());
            var num_hex = Rex.Char('0').Then(Rex.Char("xX")).Then(digit_hex).Then(digit_hex_sep.NoneOrMore());

            var num = CreateNonTerminal("num");
            num.AddProduction("num:dec", CreateTerminal("num-dec", num_dec));
            num.AddProduction("num:bin", CreateTerminal("num-bin", num_bin));
            num.AddProduction("num:oct", CreateTerminal("num-oct", num_oct));
            num.AddProduction("num:hex", CreateTerminal("num-hex", num_hex));

            var num_big = CreateNonTerminal("num-big");
            num_big.AddProduction("num:dec-big", CreateTerminal("num-dec-big", num_int.Then('n')));
            num_big.AddProduction("num:bin-big", CreateTerminal("num-bin-big", num_bin.Then('n')));
            num_big.AddProduction("num:oct-big", CreateTerminal("num-oct-big", num_oct.Then('n')));
            num_big.AddProduction("num:hex-big", CreateTerminal("num-hex-big", num_hex.Then('n')));

            var str_esc = Rex.Char('\\').Then(Rex.AnyChar.Or("\r\n"));
            var str_sq = Rex.Except(@"\\\r\n'").Or(str_esc);
            var str_dq = Rex.Except(@"\\\r\n""").Or(str_esc);

            CreateTerminal("str", Rex.Or(
                Rex.Char('\'').Then(str_sq.NoneOrMore()).Then('\''),
                Rex.Char('"').Then(str_dq.NoneOrMore()).Then('"')));

            var rex_esc = Rex.Char('\\').Then(Rex.Except(@"\r\n\u{2028-2029}"));

            var rex_class = Rex.Char('[').Then(Rex.Except(@"\\]\r\n\u{2028-2029}").Or(rex_esc).NoneOrMore()).Then(']');
            var rex_start = Rex.Except(@"\\[/*\r\n\u{2028-2029}").Or(rex_esc).Or(rex_class);
            var rex_next = Rex.Except(@"\\[/\r\n\u{2028-2029}").Or(rex_esc).Or(rex_class);
            var rex_flags = Rex.Char("a-z").NoneOrMore();

            CreateTerminal("rex", Rex.Char('/').Then(rex_start).Then(rex_next.NoneOrMore()).Then('/').Then(rex_flags));

            var template_chars = Rex.Or(
                Rex.Except("\\`$"),
                Rex.Char('\\').Then(Rex.AnyChar),
                Rex.Char('$').NotFollowedBy('{')).NoneOrMore();

            CreateTerminal("template-single", Rex.Char('`').Then(template_chars).Then("`"));
            CreateTerminal("template-start", Rex.Char('`').Then(template_chars).Then("${"));
            CreateTerminal("template-middle", Rex.Char('}').Then(template_chars).Then("${"));
            CreateTerminal("template-end", Rex.Char('}').Then(template_chars).Then("`"));
        }

        private Symbol CreatePropertyName(ECMAGrammarContext context) => GetOrCreateNonTerminal("prop-name", context, prop_name =>
        {
            var expr = CreateExpression(context | ECMAGrammarContext.In);

            prop_name.AddProduction("prop-name:id", "id");
            prop_name.AddProduction("prop-name:str", "str");
            prop_name.AddProduction("prop-name:num", "num");
            prop_name.AddProduction("prop-name:expr", "[", expr, "]");
        });

        private Symbol CreateBindingPattern(ECMAGrammarContext context) => GetOrCreateNonTerminal("binding", context, binding =>
        {
            var prop_name = CreatePropertyName(context);
            var expr = CreateExpression(context | ECMAGrammarContext.In);

            var binding_obj = CreateNonTerminal(CreateNonTerminalName("binding-obj", context));
            var binding_arr = CreateNonTerminal(CreateNonTerminalName("binding-arr", context));
            var binding_prop = CreateNonTerminal(CreateNonTerminalName("binding-prop", context));
            var binding_prop_list = CreateNonTerminal(CreateNonTerminalName("binding-prop-list", context));
            var binding_prop_rest = CreateNonTerminal(CreateNonTerminalName("binding-prop-rest", context));
            var binding_elem = CreateNonTerminal(CreateNonTerminalName("binding-elem", context));
            var binding_elem_list = CreateNonTerminal(CreateNonTerminalName("binding-elem-list", context));
            var binding_elem_rest = CreateNonTerminal(CreateNonTerminalName("binding-elem-rest", context));

            binding.AddProduction("binding:obj", binding_obj);
            binding.AddProduction("binding:arr", binding_arr);

            binding_obj.AddProduction("binding-obj:empty", "{ }");
            binding_obj.AddProduction("binding-obj:rest", "{", binding_prop_rest, "}");
            binding_obj.AddProduction("binding-obj:list", "{", binding_prop_list, "}");
            binding_obj.AddProduction("binding-obj:list~", "{", binding_prop_list, ",", "}");
            binding_obj.AddProduction("binding-obj:list-rest", "{", binding_prop_list, ",", binding_prop_rest, "}");

            binding_arr.AddProduction("binding-arr:empty", "[ ]");
            binding_arr.AddProduction("binding-arr:rest", "[", binding_elem_rest, "]");
            binding_arr.AddProduction("binding-arr:list", "[", binding_elem_list, "]");
            binding_arr.AddProduction("binding-arr:list-rest", "[", binding_elem_list, ",", binding_elem_rest, "]");

            binding_prop_rest.AddProduction("binding-prop-rest", "... id");

            binding_prop_list.AddProduction("binding-prop-list:item", binding_prop);
            binding_prop_list.AddProduction("binding-prop-list:append", binding_prop_list, ",", binding_prop);

            binding_elem_list.AddProduction("binding-elem-list:item", binding_elem);
            binding_elem_list.AddProduction("binding-elem-list:empty", "").AllowOn(",");
            binding_elem_list.AddProduction("binding-elem-list:append", binding_elem_list, ",", binding_elem);
            binding_elem_list.AddProduction("binding-elem-list:append-empty", binding_elem_list, ",");

            binding_prop.AddProduction("binding-prop:id", "id");
            binding_prop.AddProduction("binding-prop:id-init", "id", "=", expr);
            binding_prop.AddProduction("binding-prop:key-value", prop_name, ":", binding_elem);

            binding_elem.AddProduction("binding-elem:id", "id");
            binding_elem.AddProduction("binding-elem:id-init", "id", "=", expr);
            binding_elem.AddProduction("binding-elem:pattern", binding);
            binding_elem.AddProduction("binding-elem:pattern-init", binding, "=", expr);

            binding_elem_rest.AddProduction("binding-elem-rest:id", "... id");
            binding_elem_rest.AddProduction("binding-elem-rest:pattern", "...", binding);
        });

        private Symbol CreateFunctionParameters(ECMAGrammarContext context) => GetOrCreateNonTerminal("parameters", context, parameters =>
        {
            CreateBindingPattern(context);
            var binding_elem = GetNonTerminal("binding-elem", context);
            var binding_elem_rest = GetNonTerminal("binding-elem-rest", context);

            var parameters_list = CreateNonTerminal("parameters-list", context);
            parameters_list.AddProduction("parameters-list:item", binding_elem);
            parameters_list.AddProduction("parameters-list:append", parameters_list, ",", binding_elem);

            parameters.AddProduction("parameters:empty", "( )");
            parameters.AddProduction("parameters:rest", "(", binding_elem_rest, ")");
            parameters.AddProduction("parameters:list", "(", parameters_list, ")");
            parameters.AddProduction("parameters:list~", "(", parameters_list, ",", ")");
            parameters.AddProduction("parameters:list-rest", "(", parameters_list, ",", binding_elem_rest, ")");
        });

        private Symbol CreateFunctionBody(ECMAGrammarContext context) => GetOrCreateNonTerminal("func-body", context, func_body =>
        {
            func_body.AddProduction("func-body:empty", "{ }");
            func_body.AddProduction("func-body:stmnts", "{", CreateStatementList(context | ECMAGrammarContext.Return), "}");
        });

        private Symbol CreateMethodDefinition(ECMAGrammarContext context) => GetOrCreateNonTerminal("mthd", context, mthd =>
        {
            CreateBindingPattern(ECMAGrammarContext.None);
            var parameter = GetNonTerminal("binding-elem", ECMAGrammarContext.None);
            var prop_name = CreatePropertyName(context);

            var parameters = CreateFunctionParameters(ECMAGrammarContext.None);
            var body = CreateFunctionBody(ECMAGrammarContext.None);
            mthd.AddProduction("mthd:func", prop_name, parameters, body);
            mthd.AddProduction("mthd:get", "get", prop_name, "(", ")", body);
            mthd.AddProduction("mthd:set", "set", prop_name, "(", parameter, ")", body);

            var parameters_gen = CreateFunctionParameters(ECMAGrammarContext.Yield);
            var body_gen = CreateFunctionBody(ECMAGrammarContext.Yield);
            mthd.AddProduction("mthd:gen", "*", prop_name, parameters_gen, body_gen);

            var parameters_async = CreateFunctionParameters(ECMAGrammarContext.Await);
            var body_async = CreateFunctionBody(ECMAGrammarContext.Await);
            mthd.AddProduction("mthd:async", "async", Symbol.NoLineBreak, prop_name, parameters_async, body_async);

            var parameters_gen_async = CreateFunctionParameters(ECMAGrammarContext.Await | ECMAGrammarContext.Yield);
            var body_gen_async = CreateFunctionBody(ECMAGrammarContext.Await | ECMAGrammarContext.Yield);
            mthd.AddProduction("mthd:gen-async", "async", Symbol.NoLineBreak, "*", prop_name, parameters_gen_async, body_gen_async);
        });

        private Symbol CreateObjectLiteral(ECMAGrammarContext context) => GetOrCreateNonTerminal("obj", context, obj =>
        {
            var prop = CreateNonTerminal("prop", context);
            var prop_list = CreateNonTerminal("prop-list", context);
            var prop_name = CreatePropertyName(context);
            var expr = CreateExpression(context | ECMAGrammarContext.In);
            var func = CreateFunctionBody(context);

            obj.AddProduction("obj:empty", "{ }");
            obj.AddProduction("obj:items", "{", prop_list, "}");
            obj.AddProduction("obj:items~", "{", prop_list, ",", "}");

            prop_list.AddProduction("prop-list:prop", prop);
            prop_list.AddProduction("prop-list:append", prop_list, ",", prop);

            prop.AddProduction("prop:id", "id");
            prop.AddProduction("prop:name", prop_name, ":", expr);
            prop.AddProduction("prop:init", "id", "=", expr);
            prop.AddProduction("prop:extend", "...", expr);
            prop.AddProduction("prop:mthd", CreateMethodDefinition(context));
        });

        private Symbol CreateArrayLiteral(ECMAGrammarContext context) => GetOrCreateNonTerminal("arr", context, arr =>
        {
            var items = CreateNonTerminal(CreateNonTerminalName("arr-items", context));
            var expr = CreateExpression(context | ECMAGrammarContext.In);

            items.AddProduction("arr-items:none", "").AllowOn(",");
            items.AddProduction("arr-items:expr", expr);
            items.AddProduction("arr-items:extend", "...", expr);
            items.AddProduction("arr-items:append-none", items, ",");
            items.AddProduction("arr-items:append-expr", items, ",", expr);
            items.AddProduction("arr-items:append-extend", items, ",", "...", expr);

            arr.AddProduction("arr:empty", "[ ]");
            arr.AddProduction("arr:items", "[", items, "]");
        });

        private Symbol CreateTemplateLiteral(ECMAGrammarContext context) => GetOrCreateNonTerminal("template", context, template =>
        {
            var exprs = CreateExpressionList(context | ECMAGrammarContext.In);
            var items = CreateNonTerminal("template-items", context);

            template.AddProduction("template:single", "template-single");
            template.AddProduction("template:double", "template-start", exprs, "template-end");
            template.AddProduction("template:multiple", "template-start", exprs, items, "template-end");

            items.AddProduction("template-items:item", "template-middle", exprs);
            items.AddProduction("template-items:append", items, "template-middle", exprs);
        });

        private Symbol CreateFunctionExpression() => GetOrCreateNonTerminal("func", ECMAGrammarContext.None, func =>
        {
            var parameters = CreateFunctionParameters(ECMAGrammarContext.None);
            var body = CreateFunctionBody(ECMAGrammarContext.None);

            func.AddProduction("func:id", "function", "id", parameters, body);
            func.AddProduction("func:no-id", "function", parameters, body);
        });

        private Symbol CreateAsyncFunctionExpression() => GetOrCreateNonTerminal("func-async", ECMAGrammarContext.None, func =>
        {
            var parameters = CreateFunctionParameters(ECMAGrammarContext.Await);
            var body = CreateFunctionBody(ECMAGrammarContext.Await);

            func.AddProduction("func-async:id", "async", Symbol.NoLineBreak, "function", "id", parameters, body);
            func.AddProduction("func-async:no-id", "async", Symbol.NoLineBreak, "function", parameters, body);
        });

        private Symbol CreateArrowFunction(ECMAGrammarContext context) => GetOrCreateNonTerminal("func-arrow", context, func =>
        {
            var parameters = CreateParenthesizedExpressionOrFunctionParameters(context & ECMAGrammarContext.YieldAwait);
            var body = CreateFunctionBody(ECMAGrammarContext.None);
            var expr = CreateExpression(context & ECMAGrammarContext.In);

            func.AddProduction("func-arrow:id-expr", "id", Symbol.NoLineBreak, "=>", expr);
            func.AddProduction("func-arrow:id-body", "id", Symbol.NoLineBreak, "=>", body);
            func.AddProduction("func-arrow:parameters-expr", parameters, Symbol.NoLineBreak, "=>", expr);
            func.AddProduction("func-arrow:parameters-body", parameters, Symbol.NoLineBreak, "=>", body);
        });

        private Symbol CreateAsyncArrowFunction(ECMAGrammarContext context) => GetOrCreateNonTerminal("func-arrow-async", context, func =>
        {
            var parameters = CreateFunctionParameters(ECMAGrammarContext.Await);
            var body = CreateFunctionBody(ECMAGrammarContext.Await);
            var expr = CreateExpression((context & ECMAGrammarContext.In) | ECMAGrammarContext.Await);

            func.AddProduction("func-arrow-async:id-expr", "async", Symbol.NoLineBreak, "id", Symbol.NoLineBreak, "=>", expr);
            func.AddProduction("func-arrow-async:id-body", "async", Symbol.NoLineBreak, "id", Symbol.NoLineBreak, "=>", "{", body, "}");
            func.AddProduction("func-arrow-async:parameters-expr", "async", Symbol.NoLineBreak, parameters, Symbol.NoLineBreak, "=>", expr);
            func.AddProduction("func-arrow-async:parameters-body", "async", Symbol.NoLineBreak, parameters, Symbol.NoLineBreak, "=>", body);
        });

        private Symbol CreateGeneratorExpression() => GetOrCreateNonTerminal("gen", ECMAGrammarContext.None, gen =>
        {
            var parameters_gen = CreateFunctionParameters(ECMAGrammarContext.Yield);
            var body_gen = CreateFunctionBody(ECMAGrammarContext.Yield);
            gen.AddProduction("gen:id", "function", "*", "id", parameters_gen, body_gen);
            gen.AddProduction("gen:no-id", "function", "*", parameters_gen, body_gen);
        });

        private Symbol CreateAsyncGeneratorExpression() => GetOrCreateNonTerminal("gen-async", ECMAGrammarContext.None, gen =>
        {
            var parameters_gen = CreateFunctionParameters(ECMAGrammarContext.Yield | ECMAGrammarContext.Await);
            var body_gen = CreateFunctionBody(ECMAGrammarContext.Yield | ECMAGrammarContext.Await);
            gen.AddProduction("gen-async:id", "async", Symbol.NoLineBreak, "function", "*", "id", parameters_gen, body_gen);
            gen.AddProduction("gen-async:no-id", "async", Symbol.NoLineBreak, "function", "*", parameters_gen, body_gen);
        });

        private Symbol CreateClassExpression(ECMAGrammarContext context) => GetOrCreateNonTerminal("class-expr", context, root =>
        {
            var mthd = CreateMethodDefinition(context);
            var heritage = CreateNonTerminal("class-heritage", context);
            var tail = CreateNonTerminal("class-tail", context);
            var items = CreateNonTerminal("class-items", context);
            var item = CreateNonTerminal("class-item", context);
            var expr = CreateLeftHandSideExpression(context);

            root.AddProduction("class:id", "class", "id", tail);
            root.AddProduction("class:no-id", "class", tail);
            tail.AddProduction("class-tail:empty", "{ }");
            tail.AddProduction("class-tail:body", "{", items, "}");
            tail.AddProduction("class-tail:heritage-empty", "extends", expr, "{", "}");
            tail.AddProduction("class-tail:heritage-body", "extends", expr, "{", items, "}");

            items.AddProduction("class-items:item", item);
            items.AddProduction("class-items:append", items, item);

            item.AddProduction("class-item:empty", ";");
            item.AddProduction("class-item:mthd", mthd);
            item.AddProduction("class-item:mthd-static", "static", mthd);
        });

        private Symbol CreateParenthesizedExpressionOrFunctionParameters(ECMAGrammarContext context) => GetOrCreateNonTerminal("expr-or-parameters", context, root =>
        {
            var exprs = CreateExpressionList(context | ECMAGrammarContext.In);
            var binding = CreateBindingPattern(context);

            root.AddProduction("expr-or-parameters:empty", "( )");
            root.AddProduction("expr-or-parameters:exprs", "(", exprs, ")");
            root.AddProduction("expr-or-parameters:exprs~", "(", exprs, ",", ")");
            root.AddProduction("expr-or-parameters:rest-id", "( ... id )");
            root.AddProduction("expr-or-parameters:rest-binding", "(", "...", binding, ")");
            root.AddProduction("expr-or-parameters:exprs-rest-id", "(", exprs, ",", "...", "id", ")");
            root.AddProduction("expr-or-parameters:exprs-rest-binding", "(", exprs, ",", "...", binding, ")");
        });

        private Symbol CreateArguments(ECMAGrammarContext context) => GetOrCreateNonTerminal("args", context, args =>
        {
            var expr = CreateExpression(context | ECMAGrammarContext.In);
            var args_list = CreateNonTerminal("args-list", context);

            args_list.AddProduction("args-list:item", expr);
            args_list.AddProduction("args-list:rest", "...", expr);
            args_list.AddProduction("args-list:append-item", args_list, ",", expr);
            args_list.AddProduction("args-list:append-rest", args_list, ",", "...", expr);

            args.AddProduction("args:empty", "( )");
            args.AddProduction("args:list", "(", args_list, ")");
            args.AddProduction("args:list~", "(", args_list, ",", ")");
        });

        private Symbol CreateLeftHandSideExpression(ECMAGrammarContext context) => GetOrCreateNonTerminal("expr-lhs", context, expr_lhs =>
        {
            var expr = CreateExpression(context | ECMAGrammarContext.In);
            var exprs = CreateExpressionList(context | ECMAGrammarContext.In);
            var template = CreateTemplateLiteral(context);
            var args = CreateArguments(context);

            var expr_primary = CreateNonTerminal("expr-primary", context);
            expr_primary.AddProduction("expr-primary:this", "this");
            expr_primary.AddProduction("expr-primary:id", "id");
            expr_primary.AddProduction("expr-primary:null", "null");
            expr_primary.AddProduction("expr-primary:true", "true");
            expr_primary.AddProduction("expr-primary:false", "false");
            expr_primary.AddProduction("expr-primary:num", "num");
            expr_primary.AddProduction("expr-primary:num-big", "num-big");
            expr_primary.AddProduction("expr-primary:str", "str");
            expr_primary.AddProduction("expr-primary:arr", CreateArrayLiteral(context));
            expr_primary.AddProduction("expr-primary:obj", CreateObjectLiteral(context));
            expr_primary.AddProduction("expr-primary:func", CreateFunctionExpression());
            expr_primary.AddProduction("expr-primary:func-async", CreateAsyncFunctionExpression());
            expr_primary.AddProduction("expr-primary:gen", CreateGeneratorExpression());
            expr_primary.AddProduction("expr-primary:gen-async", CreateAsyncGeneratorExpression());
            expr_primary.AddProduction("expr-primary:class", CreateClassExpression(context));
            expr_primary.AddProduction("expr-primary:rex", "rex");
            expr_primary.AddProduction("expr-primary:template", template);
            expr_primary.AddProduction("expr-primary:parenthesized", CreateParenthesizedExpressionOrFunctionParameters(context));

            var expr_member = CreateNonTerminal("expr-member", context);
            expr_member.AddProduction("expr-member:primary", expr_primary);
            expr_member.AddProduction("expr-member:expr", expr_member, "[", exprs, "]"); ;
            expr_member.AddProduction("expr-member:id", expr_member, ".", "id");
            expr_member.AddProduction("expr-member:template", expr_member, template);
            expr_member.AddProduction("expr-member:new", "new", expr_member, args);
            expr_member.AddProduction("expr-member:super-expr", "super", "[", exprs, "]");
            expr_member.AddProduction("expr-member:super-id", "super", ".", "id");
            expr_member.AddProduction("expr-member:import-meta", "import", ".", "meta");
            expr_member.AddProduction("expr-member:new-target", "new", ".", "target");            

            var expr_new = CreateNonTerminal("expr-new", context);
            expr_new.AddProduction("expr-new:member", expr_member);
            expr_new.AddProduction("expr-new:new", "new", expr_new);

            var expr_call = CreateNonTerminal("expr-call", context);
            expr_call.AddProduction("expr-call:member", expr_member, args);
            expr_call.AddProduction("expr-call:super", "super", args);
            expr_call.AddProduction("expr-call:import", "import", "(", expr, ")");
            expr_call.AddProduction("expr-call:call", expr_call, args);
            expr_call.AddProduction("expr-call:expr", expr_call, "[", exprs, "]"); ;
            expr_call.AddProduction("expr-call:id", expr_call, ".", "id");
            expr_call.AddProduction("expr-call:template", expr_call, template);

            var expr_opt = CreateNonTerminal("expr-opt", context);
            var expr_opt_chain = CreateNonTerminal("expr-opt-chain", context);
            expr_opt_chain.AddProduction("expr-opt-chain:call", "?.", args);
            expr_opt_chain.AddProduction("expr-opt-chain:expr", "?.", "[", exprs, "]");
            expr_opt_chain.AddProduction("expr-opt-chain:id", "?.", "id");
            expr_opt_chain.AddProduction("expr-opt-chain:template", "?.", template);
            expr_opt_chain.AddProduction("expr-opt-chain:append-call", expr_opt_chain, args);
            expr_opt_chain.AddProduction("expr-opt-chain:append-expr", expr_opt_chain, "[", exprs, "]");
            expr_opt_chain.AddProduction("expr-opt-chain:append-id", expr_opt_chain, ".", "id");
            expr_opt_chain.AddProduction("expr-opt-chain:append-template", expr_opt_chain, template);

            expr_opt.AddProduction("expr-opt:member", expr_member, expr_opt_chain);
            expr_opt.AddProduction("expr-opt:call", expr_call, expr_opt_chain);
            expr_opt.AddProduction("expr-opt:opt", expr_opt, expr_opt_chain);

            expr_lhs.AddProduction("expr-lhs:new", expr_new);
            expr_lhs.AddProduction("expr-lhs:call", expr_call);
            expr_lhs.AddProduction("expr-lhs:opt", expr_opt);
        });

        private NonTerminal GetOrCreateNonTerminal(string prefix, ECMAGrammarContext context, Action<NonTerminal> builder)
        {
            var name = CreateNonTerminalName(prefix, context);

            if (Symbols.TryGetValue(name, out var symbol))
            {
                return (NonTerminal)symbol;
            }

            var nonTerminal = CreateNonTerminal(name);
            builder(nonTerminal);
            return nonTerminal;
        }

        private NonTerminal GetNonTerminal(string prefix, ECMAGrammarContext context)
        {
            return base.GetNonTerminal(CreateNonTerminalName(prefix, context));
        }

        private NonTerminal CreateNonTerminal(string prefix, ECMAGrammarContext context)
        {
            return base.CreateNonTerminal(CreateNonTerminalName(prefix, context));
        }

        private static string CreateNonTerminalName(string prefix, ECMAGrammarContext context)
        {
            var buffer = new StringBuilder(prefix);

            if ((context & ECMAGrammarContext.Await) == ECMAGrammarContext.Await) buffer.Append("-await");
            if ((context & ECMAGrammarContext.Yield) == ECMAGrammarContext.Yield) buffer.Append("-yield");
            if ((context & ECMAGrammarContext.Return) == ECMAGrammarContext.Return) buffer.Append("-return");
            if ((context & ECMAGrammarContext.In) == ECMAGrammarContext.In) buffer.Append("-in");

            return buffer.ToString();
        }
    }
}
