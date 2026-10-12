using UnityEngine;

namespace FourCore
{
    public enum EnemyType
    {
        Straight,
        Diagonal,
        RightAngle,
        Zigzag,
        Missile
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
            EnemyType.Missile => 40,
            _ => 0
        };
        public GridPosition Position { get; private set; }
        public Vector2Int Direction { get; private set; }
        public int EntryDelay { get; private set; }
        public int Life { get; private set; } = 1;
        public bool MissileActive { get; private set; }
        public bool MissileReflected { get; private set; }
        public GridPosition MissilePosition { get; private set; }
        public Vector2Int MissileDirection { get; private set; }
        public Vector2Int FiringDirection => secondaryDirection;
        public bool HasFired { get; private set; }

        public bool TakeDamage() => --Life <= 0;
        public void StopMissile() => MissileActive = false;
        public void MoveMissile(GridPosition position) => MissilePosition = position;
        public void ReflectMissile(GridPosition position)
        {
            MissilePosition = position;
            MissileDirection = -MissileDirection;
            MissileReflected = true;
        }

        public bool TryFire(CoreState core)
        {
            if (Type != EnemyType.Missile || HasFired || !FindTarget(Position, core, out Vector2Int aim)) return false;
            HasFired = true;
            MissileActive = true;
            MissileReflected = false;
            MissilePosition = Position;
            MissileDirection = aim;
            return true;
        }

        private bool FindTarget(GridPosition from, CoreState core, out Vector2Int aim)
        {
            aim = default;
            if (core == null) return false;
            int nearest = int.MaxValue;
            for (int i = 0; i < core.CellCount; i++)
            {
                if (!core.IsAlive(i)) continue;
                GridPosition cell = core.GetCell(i);
                int dx = cell.X - from.X, dy = cell.Y - from.Y;
                if (Direction.x != 0 ? dx != 0 : dy != 0) continue;
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (distance == 0 || distance >= nearest) continue;
                nearest = distance;
                aim = new Vector2Int(System.Math.Sign(dx), System.Math.Sign(dy));
            }
            return nearest != int.MaxValue;
        }

        public int PredictMissilePath(GridPosition[] result, CoreState core)
        {
            if (Type != EnemyType.Missile) return 0;
            GridPosition from = MissileActive ? MissilePosition : Position;
            Vector2Int aim = MissileDirection;
            if (!MissileActive)
            {
                if (HasFired) return 0;
                bool found = false;
                for (int i = 0; i < GameConfig.LogicalGridSize; i++)
                {
                    if (FindTarget(from, core, out aim)) { found = true; break; }
                    from = from.Offset(Direction);
                    if (!GameConfig.IsInsideLogical(from)) break;
                }
                if (!found) return 0;
            }
            int count = 0;
            while (count < result.Length)
            {
                from = from.Offset(aim);
                if (!GameConfig.IsInsideLogical(from)) break;
                result[count++] = from;
                if (core.Occupies(from) || (MissileActive && MissileReflected && from.Equals(Position))) break;
            }
            return count;
        }

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
