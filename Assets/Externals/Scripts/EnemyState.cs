using UnityEngine;

namespace FourCore
{
    public enum EnemyType
    {
        Straight,
        Diagonal,
        RightAngle,
        Zigzag
    }

    public sealed class EnemyState
    {
        public EnemyType Type { get; }
        public int BaseScore => Type switch
        {
            EnemyType.Straight => 10,
            EnemyType.Diagonal => 20,
            EnemyType.RightAngle => 30,
            EnemyType.Zigzag => 40,
            _ => 0
        };
        public GridPosition Position { get; private set; }
        public Vector2Int Direction { get; private set; }
        public int EntryDelay { get; private set; }

        private readonly Vector2Int secondaryDirection;
        private readonly GridPosition? turnPosition;
        private bool useSecondaryDirection;

        public EnemyState(
            EnemyType type,
            GridPosition startPosition,
            Vector2Int direction,
            int entryDelay,
            Vector2Int secondary = default,
            GridPosition? rightAngleTurnPosition = null)
        {
            Type = type;
            Position = startPosition;
            Direction = direction;
            EntryDelay = entryDelay;
            secondaryDirection = secondary;
            turnPosition = rightAngleTurnPosition;
        }

        public bool WaitBeforeMoving()
        {
            if (EntryDelay <= 0) return false;
            EntryDelay--;
            return true;
        }

        // All fields are value types. This includes the zigzag phase and pending turn.
        public EnemyState CopyForPrediction() => (EnemyState)MemberwiseClone();

        // Predict movement cells, not waiting turns. Never mutate the live enemy.
        public int PredictPath(GridPosition[] result, CoreState core = null)
        {
            EnemyState predicted = CopyForPrediction();
            int count = 0;
            while (count < result.Length)
            {
                GridPosition next = predicted.GetNextPosition();
                if (next.X < 0 || next.Y < 0 || next.X >= GameConfig.LogicalGridSize || next.Y >= GameConfig.LogicalGridSize)
                    break;
                result[count++] = next;
                predicted.MoveTo(next);
                if (core != null && core.Occupies(next)) break;
            }
            return count;
        }

        public GridPosition GetNextPosition()
        {
            if (Type == EnemyType.RightAngle && turnPosition.HasValue && Position.Equals(turnPosition.Value))
            {
                Direction = secondaryDirection;
            }

            Vector2Int movement = Type == EnemyType.Zigzag && useSecondaryDirection
                ? secondaryDirection
                : Direction;
            return Position.Offset(movement);
        }

        public void MoveTo(GridPosition position)
        {
            Position = position;
            if (Type == EnemyType.Zigzag)
            {
                useSecondaryDirection = !useSecondaryDirection;
            }
        }
    }
}
