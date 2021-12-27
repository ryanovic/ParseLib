namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public interface IParserReducer
    {
        MethodInfo GetTokenReducer(string tokenName);
        MethodInfo GetProductionReducer(string productionName);
        MethodInfo GetPrefixHandler(Symbol[] symbols);
    }
}
