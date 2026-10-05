using System;

namespace FourCore
{
    public sealed class ScoreState
    {
        public long Total { get; private set; }
        public event Action<long> Changed;

        public void Restore(long total)
        {
            Total = total;
            Changed?.Invoke(Total);
        }

        public long AddEnemyDefeat(int baseScore, int aliveCoreCount)
        {
            if (baseScore < 0) throw new ArgumentOutOfRangeException(nameof(baseScore));
            if (aliveCoreCount < 0) throw new ArgumentOutOfRangeException(nameof(aliveCoreCount));

            long awarded = (long)baseScore * aliveCoreCount;
            if (awarded == 0) return 0;

            Total += awarded;
            Changed?.Invoke(Total);
            return awarded;
        }
    }
}
