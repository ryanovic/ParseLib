namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Generates a dynamic assembly and provides methods for runtime type generation. 
    /// </summary>
    internal static class DynamicModule
    {
        [ThreadStatic]
        private static ModuleBuilder module;

        public static Type CreateStringParser(Type parent, Grammar grammar, string goal)
        {
            var target = DefineType(parent);
            var reducer = ParserReducer.CreateReducer(parent, grammar);
            var builder = new ParserBuilder(target, reducer, grammar, goal);
            return builder.Build();
        }

        public static Type CreateTextParser(Type parent, Grammar grammar, string goal)
        {
            var target = DefineType(parent);
            var reducer = ParserReducer.CreateReducer(parent, grammar);
            var builder = new SequentialParserBuilder(target, reducer, grammar, goal);
            return builder.Build();
        }

        private static TypeBuilder DefineType(Type parent)
        {
            if (module == null)
            {
                var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Ry.ParseLib.Dynamic_{Environment.CurrentManagedThreadId}"), AssemblyBuilderAccess.Run);
                module = assembly.DefineDynamicModule($"Ry.ParseLib.Dynamic_{Environment.CurrentManagedThreadId}");
            }

            return module.DefineType($"Type_{Guid.NewGuid()}", TypeAttributes.Public | TypeAttributes.Sealed, parent);
        }
    }
}
