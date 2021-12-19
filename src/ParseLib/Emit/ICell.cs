namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;

    /// <summary>
    /// Defines the interface to access the data of a specific type.
    /// </summary>
    public interface ICell
    {
        /// <summary>
        /// Type of the cell.
        /// </summary>
        Type CellType { get; }

        /// <summary>
        /// Loads the cell value and puts it on the stack.
        /// </summary>
        void Load(ILGenerator il);

        /// <summary>
        /// Loads the cell address and puts reference on the stack.
        /// </summary>
        void LoadAddress(ILGenerator il);


        /// <summary>
        /// Updates the cell value.
        /// </summary>
        /// <param name="loadValue">Action which puts desired value on the stack.</param>
        void Update(ILGenerator il, Action loadValue);
    }
}
