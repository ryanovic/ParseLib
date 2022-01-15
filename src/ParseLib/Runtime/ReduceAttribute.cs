namespace ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Defiens a target handler for a specified production.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ReduceAttribute : Attribute
    {
        public string Production { get; }

        public ReduceAttribute(string production)
        {
            if (production == null)
            {
                throw new ArgumentNullException(nameof(production));
            }

            this.Production = production;
        }
    }
}
