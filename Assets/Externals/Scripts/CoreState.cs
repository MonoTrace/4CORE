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

        public bool[] Capture() => (bool[])alive.Clone();

        public void Restore(bool[] saved) => System.Array.Copy(saved, alive, alive.Length);

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
                    UnityEngine.Debug.Log(
                        $"[CORE DESTROYED] 턴={turn} | 코어 칸={index + 1}, 논리 좌표={position} | 원인={reason}");
                    return true;
                }
            }

            return false;
        }
    }
}
