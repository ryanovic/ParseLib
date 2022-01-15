namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;

    /// <summary>
    /// Represents a generic value store.
    /// </summary>
    public interface ICell
    {
        Type CellType { get; }

        void Load(ILGenerator il);
        void LoadAddress(ILGenerator il);
        void Update(ILGenerator il, Action loadValue);
    }
}
