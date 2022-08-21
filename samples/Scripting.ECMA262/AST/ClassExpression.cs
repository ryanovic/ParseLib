using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class ClassExpression : INode
    {
        private readonly Leaf id;
        private readonly ClassBody body;

        public ClassExpression(Leaf id, ClassBody body)
        {
            this.id = id;
            this.body = body;
        }

        public override string ToString()
        {
            return id == null ? $"class {body}" : $"class {id.Value} {body}";
        }
    }
}
