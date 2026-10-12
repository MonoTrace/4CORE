using System;
using UnityEngine;

namespace FourCore
{
    public static class EnemySpawnFactory
    {
        // Rotate the complete pattern around the board centre, including its turn point.
        // The random choice belongs to the spawner, never movement/path prediction.
        public static EnemyState Create(EnemyType type, int entryDelay, int quarterTurns, int missileDistance = 5)
        {
            if (quarterTurns < 0 || quarterTurns > 3)
                throw new ArgumentOutOfRangeException(nameof(quarterTurns));

            GridPosition start = new(GameConfig.CoreMin, GameConfig.LogicalGridSize - 1);
            Vector2Int direction = Vector2Int.down;
            Vector2Int secondary = default;
            GridPosition? turn = null;
            switch (type)
            {
                case EnemyType.Straight:
                    break;
                case EnemyType.Missile:
                    start = new GridPosition(0, GameConfig.CoreMax + Mathf.Clamp(missileDistance, 5, 7));
                    direction = Vector2Int.right;
                    secondary = Vector2Int.down;
                    break;
                case EnemyType.Diagonal:
                    start = new GridPosition(0, GameConfig.LogicalGridSize - 1);
                    direction = new Vector2Int(1, -1);
                    break;
                case EnemyType.RightAngle:
                    start = new GridPosition(0, GameConfig.CoreMax + 3);
                    direction = Vector2Int.right;
                    secondary = Vector2Int.down;
                    turn = new GridPosition(GameConfig.CoreMin, GameConfig.CoreMax + 3);
                    break;
                case EnemyType.Zigzag:
                    direction = new Vector2Int(1, -1);
                    secondary = new Vector2Int(-1, -1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            for (int i = 0; i < quarterTurns; i++)
            {
                start = RotatePosition(start);
                direction = new Vector2Int(-direction.y, direction.x);
                secondary = new Vector2Int(-secondary.y, secondary.x);
                if (turn.HasValue) turn = RotatePosition(turn.Value);
            }
            return new EnemyState(type, start, direction, entryDelay, secondary, turn);
        }

        private static GridPosition RotatePosition(GridPosition position)
            => new(GameConfig.LogicalGridSize - 1 - position.Y, position.X);
    }
}
