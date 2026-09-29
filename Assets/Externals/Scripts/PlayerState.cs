using UnityEngine;

namespace FourCore
{
    public enum FacingDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    public sealed class PlayerState
    {
        public GridPosition Position { get; private set; }
        public FacingDirection Facing { get; private set; }

        public PlayerState(GridPosition startPosition)
        {
            Position = startPosition;
            Facing = FacingDirection.Up;
        }

        public void MoveTo(GridPosition destination, Vector2Int movement)
        {
            Position = destination;
            Facing = MovementToFacing(movement);
        }

        private static FacingDirection MovementToFacing(Vector2Int movement)
        {
            if (movement == Vector2Int.right) return FacingDirection.Right;
            if (movement == Vector2Int.down) return FacingDirection.Down;
            if (movement == Vector2Int.left) return FacingDirection.Left;
            return FacingDirection.Up;
        }
    }
}
