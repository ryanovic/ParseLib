namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Represents a collection of grammar reducers.
    /// </summary>
    public interface IParserReducer
    {
        MethodInfo GetTokenReducer(string tokenName);
        MethodInfo GetProductionReducer(string productionName);
        MethodInfo GetPrefixHandler(Symbol[] symbols);
    }
}
