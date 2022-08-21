using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class FunctionDefinition : INode
    {
        private readonly INode name;
        private readonly INode parameters;
        private readonly INode body;
        private readonly bool isAsync;
        private readonly bool isGenerator;

        public FunctionDefinition(INode name, INode parameters, INode body, bool isAsync = false, bool isGenerator = false)
        {
            this.name = name;
            this.parameters = parameters;
            this.body = body;
            this.isAsync = isAsync;
            this.isGenerator = isGenerator;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();

            if (isAsync) buffer.Append("async ");
            if (isGenerator) buffer.Append("* ");
            if (name != null) buffer.Append(name);
            buffer.Append(parameters.ToString());
            buffer.Append(body.ToString());

            return buffer.ToString();
        }
    }
}
