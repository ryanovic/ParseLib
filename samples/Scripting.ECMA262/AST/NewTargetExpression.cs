using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class NewTargetExpression : INode
    {
        public override string ToString() => "new . target";
    }
}
