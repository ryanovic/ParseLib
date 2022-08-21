using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class CallSuperExpression : INode
    {
        private readonly Parameters args;

        public CallSuperExpression(Parameters args)
        {
            this.args = args;
        }

        public override string ToString() => $"super{args}";
    }
}
