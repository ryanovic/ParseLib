namespace Ry.ParseLib
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal static class Utils
    {
        public static T[] Concate<T>(T[] a, T[] b)
        {
            if (a == null || a.Length == 0) return b;
            if (b == null || b.Length == 0) return a;

            var c = new T[a.Length + b.Length];
            a.CopyTo(c, 0);
            b.CopyTo(c, a.Length);
            return c;
        }

        public static T[] Concate<T, P>(T[] a, P[] b, Func<P, T> map)
        {
            if (b == null || b.Length == 0) return a;

            return Concate(a, Transform(b, map));
        }

        public static T[] Transform<T, P>(P[] a, Func<P, T> map)
        {
            var b = new T[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                b[i] = map(a[i]);
            }

            return b;
        }

        public static T[] Append<T>(T[] list, T item)
        {
            if (list == null || list.Length == 0)
            {
                return new[] { item };
            }

            var updated = new T[list.Length + 1];
            list.CopyTo(updated, 0);
            list[list.Length - 1] = item;
            return updated;
        }

        internal static void SafeAdd<T>(ref List<T> list, T item)
        {
            if (list == null)
            {
                list = new List<T>();
            }

            list.Add(item);
        }
    }
}
