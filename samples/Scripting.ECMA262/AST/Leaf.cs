using System;

namespace Scripting.ECMA262.AST
{
    public enum LeafType { Num, Str, Null, True, False, This, Id, Rex, Template, Get, Set }

    public class Leaf : INode
    {
        public LeafType Kind { get; }
        public string Value { get; }

        public Leaf(LeafType kind, string value = null)
        {
            this.Kind = kind;
            this.Value = value;
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case LeafType.Num: return $"num({Value})";
                case LeafType.Str: return $"str({Value})";
                case LeafType.Null: return $"null";
                case LeafType.Id: return $"id({Value})";
                case LeafType.Rex: return $"rex({Value})";
                case LeafType.Template: return $"template({Value})";
                case LeafType.True: return $"true";
                case LeafType.False: return $"false";
                case LeafType.This: return $"this";
                case LeafType.Get: return "get";
                case LeafType.Set: return "set";
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
