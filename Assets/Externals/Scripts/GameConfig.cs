using UnityEngine;

namespace FourCore
{
    public static class GameConfig
    {
        public const int PlayableGridSize = 16;
        public const int BufferSize = 4;
        public const int LogicalGridSize = PlayableGridSize + BufferSize * 2;
        public const int VisibleMin = BufferSize;
        public const int VisibleMax = VisibleMin + PlayableGridSize - 1;
        public const int CoreMin = 11;
        public const int CoreMax = 12;
        public const int MaxShieldCount = 4;
        public const float CellSize = 1f;

        public static readonly GridPosition PlayerStart = new(11, 14);

        public static bool IsInsidePlayable(GridPosition position)
        {
            return position.X >= VisibleMin && position.X <= VisibleMax &&
                   position.Y >= VisibleMin && position.Y <= VisibleMax;
        }

        public static Vector3 LogicalToWorld(GridPosition position, float z = 0f)
        {
            float halfBoard = PlayableGridSize * CellSize * 0.5f;
            int localX = position.X - VisibleMin;
            int localY = position.Y - VisibleMin;

            return new Vector3(
                -halfBoard + (localX + 0.5f) * CellSize,
                -halfBoard + (localY + 0.5f) * CellSize,
                z);
        }
    }
}
