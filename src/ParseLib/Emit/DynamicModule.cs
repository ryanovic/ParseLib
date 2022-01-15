namespace ParseLib.Emit
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
        private static readonly object syncRoot = new object();
        private static ModuleBuilder module;

        public static Type CreateStringParser(Type parent, Grammar grammar, string goal)
        {
            lock (syncRoot)
            {
                var target = DefineType(parent);
                var reducer = ParserReducer.CreateReducer(parent, grammar);
                var builder = new StringParserBuilder(target, reducer, grammar, goal);
                return builder.Build();
            }
        }

        public static Type CreateTextParser(Type parent, Grammar grammar, string goal)
        {
            lock (syncRoot)
            {
                var target = DefineType(parent);
                var reducer = ParserReducer.CreateReducer(parent, grammar);
                var builder = new SequentialParserBuilder(target, reducer, grammar, goal);
                return builder.Build();
            }
        }

        private static TypeBuilder DefineType(Type parent)
        {
            if (module == null)
            {
                var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ParseLib.Dynamic"), AssemblyBuilderAccess.Run);
                module = assembly.DefineDynamicModule("ParseLib.Dynamic");
            }

            return module.DefineType($"Type_{Guid.NewGuid()}", TypeAttributes.Public | TypeAttributes.Sealed, parent);
        }
    }
}
