namespace FourCore
{
    public sealed class CoreState
    {
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

        public int[] Capture() => (int[])recoveryTurns.Clone();

        public void Restore(int[] saved)
        {
            System.Array.Copy(saved, recoveryTurns, recoveryTurns.Length);
            for (int i = 0; i < alive.Length; i++) alive[i] = recoveryTurns[i] == 0;
        }

        public void AdvanceRecovery()
        {
            for (int i = 0; i < alive.Length; i++)
            {
                if (recoveryTurns[i] <= 0) continue;
                recoveryTurns[i]--;
                if (recoveryTurns[i] == 0) alive[i] = true;
            }
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
                    UnityEngine.Debug.Log(
                        $"[CORE DESTROYED] 턴={turn} | 코어 칸={index + 1}, 논리 좌표={position} | 원인={reason}");
                    return true;
                }
            }

            return false;
        }
    }
}
