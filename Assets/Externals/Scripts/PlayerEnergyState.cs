using System;

namespace FourCore
{
    public sealed class PlayerEnergyState
    {
        public int Current { get; private set; }
        public int Total { get; private set; }
        public event Action<int, int> Changed;

        public PlayerEnergyState(int maximum)
        {
            Total = Math.Max(1, maximum);
            Current = Total;
        }

        public bool TryDamageTotal()
        {
            if (Total <= 0) return false;
            Total--;
            Current = Math.Min(Current, Total);
            Changed?.Invoke(Current, Total);
            return true;
        }

        public void SetMaximumFromCores(int aliveCores, int deployedShields = 0)
        {
            int maximum = Math.Max(0, aliveCores);
            int recovered = Math.Max(0, maximum - Total);
            int available = Math.Max(0, maximum - deployedShields);
            int current = Math.Clamp(Current + recovered, 0, available);
            if (maximum == Total && current == Current) return;
            Total = maximum;
            Current = current;
            Changed?.Invoke(Current, Total);
        }

        public void Restore(int current, int total)
        {
            Total = Math.Max(0, total);
            Current = Math.Clamp(current, 0, Total);
            Changed?.Invoke(Current, Total);
        }

        public bool TrySpend()
        {
            if (Current <= 0) return false;
            Current--;
            Changed?.Invoke(Current, Total);
            return true;
        }

        public bool TryRestore(int deployedShields = 0)
        {
            if (Current >= Math.Max(0, Total - deployedShields)) return false;
            Current++;
            Changed?.Invoke(Current, Total);
            return true;
        }

        public void IncreaseTotal()
        {
            Total++;
            Current++;
            Changed?.Invoke(Current, Total);
        }
    }
}
