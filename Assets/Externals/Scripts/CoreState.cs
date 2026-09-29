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

        public GridPosition GetCell(int index) => cells[index];
        public bool IsAlive(int index) => alive[index];

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
    }
}
