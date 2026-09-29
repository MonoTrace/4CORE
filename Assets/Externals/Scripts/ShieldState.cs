using System.Collections.Generic;

namespace FourCore
{
    public sealed class ShieldState
    {
        private readonly HashSet<GridPosition> positions = new();
        private readonly int capacity;

        public ShieldState(int maximumCount)
        {
            capacity = maximumCount;
        }

        public IReadOnlyCollection<GridPosition> Positions => positions;
        public int Count => positions.Count;

        public bool Contains(GridPosition position)
        {
            return positions.Contains(position);
        }

        public bool TryAdd(GridPosition position)
        {
            return positions.Count < capacity && positions.Add(position);
        }

        public bool TryRemove(GridPosition position)
        {
            return positions.Remove(position);
        }
    }
}
