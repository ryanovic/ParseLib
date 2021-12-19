namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    /// <summary>
    /// Cell wrapper which guarantees underlying store to be initialized with a type specified.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct Cell<T> : ICell, IEquatable<Cell<T>>
    {
        private readonly ICell store;

        public Cell(ICell store)
        {
            this.store = CellGuard.Check(nameof(store), store, typeof(T));
        }

        /// <inheritdoc />
        public Type CellType => store.CellType;

        /// <inheritdoc />
        public void Load(ILGenerator il) => store.Load(il);

        /// <inheritdoc />
        public void LoadAddress(ILGenerator il) => store.LoadAddress(il);

        /// <inheritdoc />
        public void Update(ILGenerator il, Action loadValue) => store.Update(il, loadValue);

        /// <inheritdoc />
        public bool Equals(Cell<T> other) => other.store == store;
    }
}
