using System;
using System.Collections.Generic;
using System.Text;

namespace ParseLib.Runtime
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HandleAttribute : Attribute
    {
        public string Prefix { get; }

        public HandleAttribute(string prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            this.Prefix = prefix;
        }
    }
}
