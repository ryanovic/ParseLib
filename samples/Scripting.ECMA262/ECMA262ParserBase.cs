namespace Scripting.ECMA262
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Ry.ParseLib.Runtime;
    using Scripting.ECMA262.AST;

    public abstract class ECMA262ParserBase : TextParser
    {
        protected ECMA262ParserBase(TextReader reader) : base(reader)
        {
        }

        [CompleteToken("id")]
        public Leaf CreateIdentifier() => new Leaf(LeafType.Id, GetValue());

        [CompleteToken("num-dec")]
        [CompleteToken("num-bin")]
        [CompleteToken("num-oct")]
        [CompleteToken("num-hex")]
        [CompleteToken("num-dec-big")]
        [CompleteToken("num-bin-big")]
        [CompleteToken("num-oct-big")]
        [CompleteToken("num-hex-big")]
        public Leaf CreateNumber() => new Leaf(LeafType.Num, GetValue());

        [CompleteToken("str")]
        public Leaf CreateString() => new Leaf(LeafType.Str, GetValue());

        [CompleteToken("null")]
        public Leaf CreateNull() => new Leaf(LeafType.Null);

        [CompleteToken("true")]
        public Leaf CreateTrue() => new Leaf(LeafType.True);

        [CompleteToken("false")]
        public Leaf CreateFalse() => new Leaf(LeafType.False);

        [CompleteToken("this")]
        public Leaf CreateThis() => new Leaf(LeafType.This);

        [CompleteToken("rex")]
        public Leaf CreateRegularExpression() => new Leaf(LeafType.Rex, GetValue());

        [CompleteToken("template-single")]
        [CompleteToken("template-start")]
        [CompleteToken("template-middle")]
        [CompleteToken("template-end")]
        public Leaf CreateTemplatePart() => new Leaf(LeafType.Template, GetValue());

        [Reduce("arr-items:none")]
        [Reduce("binding-elem-list:empty")]
        public List<INode> CreateNodeList() => new List<INode> { null };

        [Reduce("args-list:item")]
        [Reduce("expr-list:item")]
        [Reduce("class-items:item")]
        [Reduce("arr-items:expr")]
        [Reduce("prop-list:prop")]
        [Reduce("parameters-list:item")]
        [Reduce("binding-elem-list:item")]
        [Reduce("binding-prop-list:item")]
        public List<INode> CreateNodeList(INode item) => new List<INode> { item };

        [Reduce("args-list:rest")]
        [Reduce("arr-items:extend")]
        public List<INode> CreateNodeListExtend(INode expr) => new List<INode> { new ExtendExpression(expr) };

        [Reduce("arr-items:append-none")]
        [Reduce("binding-elem-list:append-empty")]
        public List<INode> AppendNodeList(List<INode> list) => Append(list, null);

        [Reduce("args-list:append-item")]
        [Reduce("expr-list:append")]
        [Reduce("class-items:append")]
        [Reduce("arr-items:append-expr")]
        [Reduce("prop-list:append")]
        [Reduce("parameters-list:append")]
        [Reduce("binding-elem-list:append")]
        [Reduce("binding-prop-list:append")]
        public List<INode> AppendNodeList(List<INode> list, INode item) => Append(list, item);

        [Reduce("args-list:append-rest")]
        [Reduce("arr-items:append-extend")]
        public List<INode> AppendNoteListExtend(List<INode> list, INode expr) => Append(list, new ExtendExpression(expr));

        [Reduce("arr:empty")]
        public ArrayExpression CreateArray() => new ArrayExpression(new List<INode>());

        [Reduce("arr:items")]
        public ArrayExpression CreateArray(List<INode> list) => new ArrayExpression(list);

        [Reduce("prop:init")]
        [Reduce("prop:name")]
        [Reduce("binding-prop:id-init")]
        [Reduce("binding-prop:key-value")]
        [Reduce("binding-elem:id-init")]
        [Reduce("binding-elem:pattern-init")]
        public AssignExpression CreateBindingElement(INode id, INode value) => new AssignExpression(id, value);

        [Reduce("prop:extend")]
        public ExtendExpression CreateObjectExtend(INode expr) => new ExtendExpression(expr);

        [Reduce("binding-prop-rest")]
        [Reduce("binding-elem-rest:id")]
        [Reduce("binding-elem-rest:pattern")]
        public ExtendExpression CreateBindingRest(INode expr) => new ExtendExpression(expr);

        [Reduce("binding-arr:list-rest")]
        [Reduce("binding-obj:list-rest")]
        public Binding CreateBindingArray(List<INode> list, INode rest) => new Binding(list, rest);

        [Reduce("binding-obj:list")]
        [Reduce("binding-arr:list")]
        public Binding CreateBindingArray(List<INode> list) => new Binding(list, null);

        [Reduce("binding-obj:rest")]
        [Reduce("binding-arr:rest")]
        public Binding CreateBindingArray(INode rest) => new Binding(null, rest);

        [Reduce("binding-obj:empty")]
        [Reduce("binding-arr:empty")]
        public Binding CreateBindingArray() => new Binding(null, null);

        [Reduce("binding-obj:list~")]
        public Binding CreateBindingObjectExtra(List<INode> list) => new Binding(Append(list, null), null);

        [Reduce("parameters:list-rest")]
        public Parameters CreateParameters(List<INode> list, INode rest) => new Parameters(Append(list, rest));

        [Reduce("args:list")]
        [Reduce("parameters:list")]
        public Parameters CreateParameters(List<INode> list) => new Parameters(list);

        [Reduce("args:list~")]
        [Reduce("parameters:list~")]
        public Parameters CreateParametersExtra(List<INode> list) => new Parameters(Append(list, null));

        [Reduce("parameters:rest")]
        public Parameters CreateParameters(INode rest) => new Parameters(new List<INode> { rest });

        [Reduce("args:empty")]
        [Reduce("parameters:empty")]
        public Parameters CreateParameters() => new Parameters(null);

        [Reduce("func-body:empty")]
        public FunctionBody CreateFunctionBody() => new FunctionBody(null);

        [Reduce("func-body:stmnts")]
        public FunctionBody CreateFunctionBody(INode body) => new FunctionBody(body);

        [Reduce("mthd:func")]
        public FunctionDefinition CreateMethod(INode name, INode parameters, INode body) => new FunctionDefinition(name, parameters, body);

        [Reduce("mthd:gen")]
        public FunctionDefinition CreateGenMethod(INode name, INode parameters, INode body) => new FunctionDefinition(name, parameters, body, isGenerator: true);

        [Reduce("mthd:async")]
        public FunctionDefinition CreateAsyncMethod(INode name, INode parameters, INode body) => new FunctionDefinition(name, parameters, body, isAsync: true);

        [Reduce("mthd:gen-async")]
        public FunctionDefinition CreateGenAsyncMethod(INode name, INode parameters, INode body) => new FunctionDefinition(name, parameters, body, isAsync: true, isGenerator: true);

        [Reduce("mthd:get")]
        public FunctionGet CreateGetMethod(INode name, INode body) => new FunctionGet(name, body);

        [Reduce("mthd:set")]
        public FunctionSet CreateSetMethod(INode name, INode parameter, INode body) => new FunctionSet(name, parameter, body);

        [Reduce("obj:empty")]
        public ObjectExpression CreateObject() => new ObjectExpression(new List<INode>());

        [Reduce("obj:items")]
        [Reduce("obj:items~")]
        public ObjectExpression CreateObject(List<INode> items) => new ObjectExpression(items);

        [Reduce("class:id")]
        public ClassExpression CreateClassExpression(Leaf id, ClassBody body) => new ClassExpression(id, body);

        [Reduce("class:no-id")]
        public ClassExpression CreateClassExpression(ClassBody body) => new ClassExpression(null, body);

        [Reduce("class-tail:empty")]
        public ClassBody CreateClassBody() => new ClassBody(null, null);

        [Reduce("class-tail:heritage-empty")]
        public ClassBody CreateClassBody(INode parent) => new ClassBody(parent, null);

        [Reduce("class-tail:body")]
        public ClassBody CreateClassBody(List<INode> items) => new ClassBody(null, items);

        [Reduce("class-tail:heritage-body")]
        public ClassBody CreateClassBody(INode parent, List<INode> items) => new ClassBody(parent, items);

        [Reduce("class-item:empty")]
        public INode CreateClassEmptyMember() => null;

        [Reduce("expr-or-parameters:empty")]
        public ExpressionOrParameters CreateExpressionOrParameters() => new ExpressionOrParameters(null);

        [Reduce("expr-or-parameters:exprs")]
        public ExpressionOrParameters CreateExpressionOrParameters(List<INode> items) => new ExpressionOrParameters(items);

        [Reduce("expr-or-parameters:exprs~")]
        public ExpressionOrParameters CreateExpressionOrParametersExtra(List<INode> items) => new ExpressionOrParameters(Append(items, null));

        [Reduce("expr-or-parameters:rest-id")]
        [Reduce("expr-or-parameters:rest-binding")]
        public ExpressionOrParameters CreateExpressionOrParametersRest(INode rest) => new ExpressionOrParameters(new List<INode> { new ExtendExpression(rest) });

        [Reduce("expr-or-parameters:exprs-rest-id")]
        [Reduce("expr-or-parameters:exprs-rest-binding")]
        public ExpressionOrParameters CreateExpressionOrParametersRest(List<INode> items, INode rest) => new ExpressionOrParameters(Append(items, new ExtendExpression(rest)));

        [Reduce("expr-member:id")]
        [Reduce("expr-call:id")]
        public MemberExpression CreateMemberByIdExpression(INode member, INode id) => new MemberExpression(member, id);

        [Reduce("expr-member:expr")]
        [Reduce("expr-call:expr")]
        public MemberExpression CreateMemberByExprExpression(INode member, List<INode> exprs) => new MemberExpression(member, exprs);

        [Reduce("expr-member:template")]
        [Reduce("expr-call:template")]
        public TemplateExpression CreateMemberTemplateExpression(INode member, TemplateExpression template) => template.Tag(member);

        [Reduce("expr-member:super-id")]
        public SuperExpression CreateSuperByIdExpression(INode id) => new SuperExpression(id);

        [Reduce("expr-member:super-expr")]
        public SuperExpression CreateSuperByExprExpression(List<INode> expr) => new SuperExpression(expr);

        [Reduce("expr-member:import-meta")]
        public ImportMetaExpression CreateImportMetaExpression() => new ImportMetaExpression();

        [Reduce("expr-member:new-target")]
        public NewTargetExpression CreateNewTargetExpression() => new NewTargetExpression();

        [Reduce("expr-member:new")]
        public NewExpression CreateNewExpression(INode member, Parameters args) => new NewExpression(member, args);

        [Reduce("expr-new:new")]
        public NewExpression CreateNewExpression(INode member) => new NewExpression(member, null);

        [Reduce("expr-call:call")]
        [Reduce("expr-call:member")]
        public CallExpression CreateCallExpression(INode member, Parameters args) => new CallExpression(member, args);

        [Reduce("template-items:item")]
        public List<INode> CreateTemplateMiddleList(Leaf template, List<INode> exprs) => AppendRange(new List<INode> { template }, exprs);

        [Reduce("template-items:append")]
        public List<INode> CreateTemplateMiddleList(List<INode> items, Leaf template, List<INode> exprs) => AppendRange(Append(items, template), exprs);

        [Reduce("template:single")]
        public TemplateExpression CreateTemplateExpression(Leaf template) => new TemplateExpression(new List<INode> { template });

        [Reduce("template:double")]
        public TemplateExpression CreateTemplateExpression(Leaf start, List<INode> exprs, Leaf end) => new TemplateExpression(Append(AppendRange(new List<INode> { start }, exprs), end));

        [Reduce("template:multiple")]
        public TemplateExpression CreateTemplateExpression(Leaf start, List<INode> exprs, List<INode> middle, Leaf end) => new TemplateExpression(Append(AppendRange(AppendRange(new List<INode> { start }, exprs), middle), end));

        [Reduce("expr-call:super")]
        public CallSuperExpression CreateCallSuperExpression(Parameters args) => new CallSuperExpression(args);

        [Reduce("expr-call:import")]
        public ImportExpression CreateImportExpression(INode expr) => new ImportExpression(expr);

        [Reduce("expr-opt-chain:id")]
        public List<INode> CreateOptionalChain(INode id) => new List<INode> { new OptionalChainMember(id) };

        [Reduce("expr-opt-chain:expr")]
        public List<INode> CreateOptionalChain(List<INode> exprs) => new List<INode> { new OptionalChainMember(exprs) };

        [Reduce("expr-opt-chain:template")]
        public List<INode> CreateOptionalChain(TemplateExpression template) => new List<INode> { template };

        [Reduce("expr-opt-chain:call")]
        public List<INode> CreateOptionalChain(Parameters args) => new List<INode> { new OptionalChainCall(args) };

        [Reduce("expr-opt-chain:append-id")]
        public List<INode> CreateOptionalChain(List<INode> chain, INode id) => Append(chain, new OptionalChainMember(id));

        [Reduce("expr-opt-chain:append-expr")]
        public List<INode> CreateOptionalChain(List<INode> chain, List<INode> exprs) => Append(chain, new OptionalChainMember(exprs));

        [Reduce("expr-opt-chain:append-template")]
        public List<INode> CreateOptionalChain(List<INode> chain, TemplateExpression template) => Append(chain, template);

        [Reduce("expr-opt-chain:append-call")]
        public List<INode> CreateOptionalChain(List<INode> chain, Parameters args) => Append(chain, new OptionalChainCall(args));

        [Reduce("expr-opt:member")]
        [Reduce("expr-opt:call")]
        [Reduce("expr-opt:opt")]
        public OptionalExpression CreateOptionalExpression(INode expr, List<INode> chain) => new OptionalExpression(expr, chain);

        [Reduce("expr-upd:inc-prefix")]
        public UnaryExpression CreateIncOp(INode a) => new UnaryExpression(a, "++");
        [Reduce("expr-upd:dec-prefix")]
        public UnaryExpression CreateDecOp(INode a) => new UnaryExpression(a, "--");
        [Reduce("expr-upd:inc-postfix")]
        public UnaryExpression CreateIncPostOp(INode a) => new UnaryExpression(a, "++", true);
        [Reduce("expr-upd:dec-postfix")]
        public UnaryExpression CreateDecPostOp(INode a) => new UnaryExpression(a, "--", true);

        [Reduce("expr-unary:add")]
        public UnaryExpression CreateUnaryAdd(INode a) => new UnaryExpression(a, "+");
        [Reduce("expr-unary:sub")]
        public UnaryExpression CreateUnarySub(INode a) => new UnaryExpression(a, "-");
        [Reduce("expr-unary:neg")]
        public UnaryExpression CreateUnaryNeg(INode a) => new UnaryExpression(a, "~");
        [Reduce("expr-unary:not")]
        public UnaryExpression CreateUnaryNot(INode a) => new UnaryExpression(a, "!");

        [Reduce("expr-binary:exp")]
        public BinaryExpression CreateBinaryExp(INode a, INode b) => new BinaryExpression(a, b, "**");
        [Reduce("expr-binary:mul")]
        public BinaryExpression CreateBinaryMul(INode a, INode b) => new BinaryExpression(a, b, "*");
        [Reduce("expr-binary:div")]
        public BinaryExpression CreateBinaryDiv(INode a, INode b) => new BinaryExpression(a, b, "/");
        [Reduce("expr-binary:mod")]
        public BinaryExpression CreateBinaryMod(INode a, INode b) => new BinaryExpression(a, b, "%");
        [Reduce("expr-binary:add")]
        public BinaryExpression CreateBinaryAdd(INode a, INode b) => new BinaryExpression(a, b, "+");
        [Reduce("expr-binary:sub")]
        public BinaryExpression CreateBinarySub(INode a, INode b) => new BinaryExpression(a, b, "-");

        private static List<T> Append<T>(List<T> list, T item)
        {
            list.Add(item);
            return list;
        }

        private static List<T> AppendRange<T>(List<T> list, IEnumerable<T> other)
        {
            list.AddRange(other);
            return list;
        }

        // When a method with below signature is defined on the parser it would be executed for every production reduced.
        protected void OnProductionCompleted(string name)
        {
            //Console.WriteLine($"production: {name}");
        }
    }
}
