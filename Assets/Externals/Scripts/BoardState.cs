namespace FourCore
{
    public sealed class BoardState
    {
        private readonly CoreState coreState;

        public BoardState(CoreState core)
        {
            coreState = core;
        }

        public bool CanPlayerEnter(GridPosition position)
        {
            return GameConfig.IsInsidePlayable(position) && !coreState.Occupies(position);
        }
    }
}
