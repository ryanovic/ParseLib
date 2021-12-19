using System;
using System.Collections.Generic;
using System.Text;

namespace ParseLib.Runtime
{
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
