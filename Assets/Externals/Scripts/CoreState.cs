namespace FourCore
{
    public sealed class CoreState
    {
        public event System.Action Changed;
        private readonly GridPosition[] cells =
        {
            new(GameConfig.CoreMin, GameConfig.CoreMin),
            new(GameConfig.CoreMax, GameConfig.CoreMin),
            new(GameConfig.CoreMin, GameConfig.CoreMax),
            new(GameConfig.CoreMax, GameConfig.CoreMax)
        }; 

        private readonly bool[] alive = { true, true, true, true };
        private readonly int[] recoveryTurns = new int[4];
        public const int RecoveryDuration = 90;

        public int CellCount => cells.Length;

        public int AliveCount
        {
            get
            {
                int count = 0;
                foreach (bool isAlive in alive)
                {
                    if (isAlive) count++;
                }

                return count;
            }
        }

        public GridPosition GetCell(int index) => cells[index];
        public bool IsAlive(int index) => alive[index];

        public int GetRecoveryTurns(int index) => recoveryTurns[index];

        public bool TryRecoverOne()
        {
            int chosen = -1;
            for (int i = 0; i < alive.Length; i++)
                if (!alive[i] && (chosen < 0 || recoveryTurns[i] > recoveryTurns[chosen])) chosen = i;
            if (chosen < 0) return false;
            alive[chosen] = true; recoveryTurns[chosen] = 0;
            Changed?.Invoke();
            return true;
        }

        public int[] Capture() => (int[])recoveryTurns.Clone();

        public void Restore(int[] saved)
        {
            System.Array.Copy(saved, recoveryTurns, recoveryTurns.Length);
            for (int i = 0; i < alive.Length; i++) alive[i] = recoveryTurns[i] == 0;
            Changed?.Invoke();
        }

        public void AdvanceRecovery(int amount = 1)
        {
            bool recovered = false;
            for (int i = 0; i < alive.Length; i++)
            {
                if (recoveryTurns[i] <= 0) continue;
                recoveryTurns[i] = System.Math.Max(0, recoveryTurns[i] - System.Math.Max(1, amount));
                if (recoveryTurns[i] == 0) { alive[i] = true; recovered = true; }
            }
            if (recovered) Changed?.Invoke();
        }

        public bool Occupies(GridPosition position)
        {
            for (int index = 0; index < cells.Length; index++)
            {
                if (alive[index] && cells[index].Equals(position))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryDamage(GridPosition position, int turn, string reason)
        {
            for (int index = 0; index < cells.Length; index++)
            {
                if (alive[index] && cells[index].Equals(position))
                {
                    alive[index] = false;
                    recoveryTurns[index] = RecoveryDuration;
                    Changed?.Invoke();
                    UnityEngine.Debug.Log(
                        $"[CORE DESTROYED] 턴={turn} | 코어 칸={index + 1}, 논리 좌표={position} | 원인={reason}");
                    return true;
                }
            }

            return false;
        }
    }
}
