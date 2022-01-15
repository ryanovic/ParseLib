namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    /// <summary>
    /// Represents a generic store for a value of <typeparamref name="T"/> type.
    /// </summary>
    public readonly struct Cell<T> : ICell, IEquatable<Cell<T>>
    {
        private readonly ICell store;

        public Cell(ICell store)
        {
            this.store = CellGuard.Check(nameof(store), store, typeof(T));
        }

        public Type CellType => store.CellType;

        public void Load(ILGenerator il) => store.Load(il);
        public void LoadAddress(ILGenerator il) => store.LoadAddress(il);
        public void Update(ILGenerator il, Action loadValue) => store.Update(il, loadValue);

        public bool Equals(Cell<T> other) => other.store == store;
    }
}
