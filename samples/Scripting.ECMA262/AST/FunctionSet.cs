using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class FunctionSet : INode
    {
        private readonly INode name;
        private readonly INode parameter;
        private readonly INode body;

        public FunctionSet(INode name, INode parameter, INode body)
        {
            this.name = name;
            this.parameter = parameter;
            this.body = body;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder("set ");
            if (name != null) buffer.Append(name);
            buffer.Append($"({parameter})");
            buffer.Append(body.ToString());
            return buffer.ToString();
        }
    }
}
