using System;

namespace FourCore
{
    public sealed class ScoreState
    {
        public long Total { get; private set; }
        public bool Enabled { get; private set; } = true;
        public event Action<long> Changed;

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            Restore(enabled ? Total : 0);
        }

        public void Restore(long total)
        {
            Total = Enabled ? total : 0;
            Changed?.Invoke(Total);
        }

        public long AddEnemyDefeat(int baseScore, int aliveCoreCount)
        {
            if (baseScore < 0) throw new ArgumentOutOfRangeException(nameof(baseScore));
            if (aliveCoreCount < 0) throw new ArgumentOutOfRangeException(nameof(aliveCoreCount));
            if (!Enabled) return 0;

            long awarded = (long)baseScore * aliveCoreCount;
            if (awarded == 0) return 0;

            Total += awarded;
            Changed?.Invoke(Total);
            return awarded;
        }
    }
}
