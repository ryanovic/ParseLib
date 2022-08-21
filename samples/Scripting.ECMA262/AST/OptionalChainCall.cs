using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class OptionalChainCall : INode
    {
        private readonly Parameters args;

        public OptionalChainCall(Parameters args)
        {
            this.args = args;
        }

        public override string ToString() => $"call{args}";
    }
}
