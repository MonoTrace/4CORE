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

        public bool TrySpend()
        {
            if (Current <= 0) return false;
            Current--;
            Changed?.Invoke(Current, Total);
            return true;
        }

        public bool TryRestore()
        {
            if (Current >= Total) return false;
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
