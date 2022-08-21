using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class CallExpression : INode
    {
        private readonly INode member;
        private readonly Parameters args;

        public CallExpression(INode member, Parameters args)
        {
            this.member = member;
            this.args = args;
        }

        public override string ToString() => args.IsEmpty ? $"call({member})" : $"call({member},{args})";
    }
}
