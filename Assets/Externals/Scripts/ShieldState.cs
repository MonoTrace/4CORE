using System.Collections.Generic;

namespace FourCore
{
    public sealed class ShieldState
    {
        // Installation order makes capacity loss deterministic, including undo.
        private readonly List<GridPosition> positions = new();
        private int capacity;

        public ShieldState(int maximumCount)
        {
            capacity = maximumCount;
        }

        public IReadOnlyCollection<GridPosition> Positions => positions;
        public int Count => positions.Count;

        public void SetCapacity(int maximumCount)
        {
            capacity = System.Math.Max(0, maximumCount);
            while (positions.Count > capacity) positions.RemoveAt(positions.Count - 1);
        }

        public bool Contains(GridPosition position)
        {
            return positions.Contains(position);
        }

        public void Restore(IEnumerable<GridPosition> saved)
        {
            positions.Clear();
            foreach (GridPosition position in saved)
                if (!positions.Contains(position)) positions.Add(position);
        }

        public bool TryAdd(GridPosition position)
        {
            if (positions.Count >= capacity || positions.Contains(position)) return false;
            positions.Add(position);
            return true;
        }

        public bool TryRemove(GridPosition position)
        {
            return positions.Remove(position);
        }
    }
}
