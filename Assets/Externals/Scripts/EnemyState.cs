using UnityEngine;

namespace FourCore
{
    public enum EnemyType
    {
        Straight
    }

    public sealed class EnemyState
    {
        public EnemyType Type { get; }
        public GridPosition Position { get; private set; }
        public Vector2Int Direction { get; }
        public int EntryDelay { get; private set; }

        public EnemyState(
            EnemyType type,
            GridPosition startPosition,
            Vector2Int direction,
            int entryDelay)
        {
            Type = type;
            Position = startPosition;
            Direction = direction;
            EntryDelay = entryDelay;
        }

        public bool WaitBeforeMoving()
        {
            if (EntryDelay <= 0) return false;
            EntryDelay--;
            return true;
        }

        public GridPosition GetNextPosition()
        {
            return Position.Offset(Direction);
        }

        public void MoveTo(GridPosition position)
        {
            Position = position;
        }
    }
}
