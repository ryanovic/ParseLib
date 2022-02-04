namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;

    internal static class CellGuard
    {
        /// <summary>
        /// Checks if the <paramref name="cell"/> matches the <paramref name="expectedType"/> type.
        /// </summary>
        /// <returns>The original <paramref name="cell"/> from the method's arguments.</returns>
        public static ICell Check(string paramName, ICell cell, Type expectedType)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (cell.CellType != expectedType)
            {
                throw new ArgumentException(Errors.TypeExpected(expectedType.Name), paramName);
            }

            return cell;
        }
    }
}
