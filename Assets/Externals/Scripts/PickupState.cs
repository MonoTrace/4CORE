using System.Collections.Generic;

namespace FourCore
{
    public readonly struct Pickup
    {
        public GridPosition Position { get; }
        public PickupType Type { get; }
        public Pickup(GridPosition position, PickupType type) { Position = position; Type = type; }
    }
    public sealed class PickupState
    {
        private readonly List<Pickup> pickups = new();
        public IReadOnlyList<Pickup> Items => pickups;
        public Pickup[] Capture() => pickups.ToArray();
        public void Restore(Pickup[] saved) { pickups.Clear(); pickups.AddRange(saved); }
        public bool TryAdd(GridPosition position, PickupType type)
        {
            if (!GameConfig.IsInsidePlayable(position) || pickups.Count >= 8) return false;
            foreach (Pickup p in pickups) if (p.Position.Equals(position)) return false;
            pickups.Add(new Pickup(position, type)); return true;
        }
        public bool TryCollect(GridPosition position, out PickupType type)
        {
            for (int i = 0; i < pickups.Count; i++)
                if (pickups[i].Position.Equals(position))
                { type = pickups[i].Type; pickups.RemoveAt(i); return true; }
            type = default; return false;
        }
    }
}
