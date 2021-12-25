namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;

    internal static class CellGuard
    {
        /// <summary>
        /// Verifies the cell to be initialized with a type specified.
        /// </summary>
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
