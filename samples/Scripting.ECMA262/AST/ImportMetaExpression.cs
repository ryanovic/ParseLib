using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class ImportMetaExpression : INode
    {
        public override string ToString() => "import.meta";
    }
}
