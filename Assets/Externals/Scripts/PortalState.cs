namespace FourCore
{
    // One augment grants a pair, sharing two trips across both directions.
    public sealed class PortalState
    {
        public int Kits { get; private set; }
        public GridPosition? A { get; private set; }
        public GridPosition? B { get; private set; }
        public int RemainingUses { get; private set; }
        public bool AwaitingSecond => A.HasValue && !B.HasValue;
        public bool Linked => A.HasValue && B.HasValue;
        public void Collect() => Kits++;
        public bool IsEndpoint(GridPosition p) => (A.HasValue && A.Value.Equals(p)) || (B.HasValue && B.Value.Equals(p));
        public bool CanDrop(GridPosition p) => AwaitingSecond ? !A.Value.Equals(p) : Kits > 0;
        public bool Drop(GridPosition p)
        {
            if (!CanDrop(p)) return false;
            if (AwaitingSecond) { B = p; RemainingUses = 2; }
            else { Kits--; A = p; B = null; RemainingUses = 0; }
            return true;
        }
        public bool TryDestination(GridPosition entry, out GridPosition destination)
        {
            destination = entry;
            if (!Linked || RemainingUses <= 0) return false;
            if (A.Value.Equals(entry)) { destination = B.Value; return true; }
            if (B.Value.Equals(entry)) { destination = A.Value; return true; }
            return false;
        }
        public void ConsumeUse()
        {
            if (!Linked || RemainingUses <= 0) return;
            RemainingUses--;
            if (RemainingUses == 0) { A = null; B = null; }
        }
        public PortalState Copy() => new() { Kits = Kits, A = A, B = B, RemainingUses = RemainingUses };
        public void Restore(PortalState saved) { Kits = saved?.Kits ?? 0; A = saved?.A; B = saved?.B; RemainingUses = saved?.RemainingUses ?? 0; }
    }
}
