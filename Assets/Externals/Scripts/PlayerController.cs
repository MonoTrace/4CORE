using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FourCore
{
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Held movement")]
        [SerializeField, Min(0f)] private float initialRepeatDelay = 0.45f;
        [SerializeField, Min(0.02f)] private float repeatInterval = 0.075f;

        private BoardState boardState;
        private PlayerState playerState;
        private PlayerView playerView;
        private TurnManager turnManager;
        private KeyControl heldKey;
        private Vector2Int heldDirection;
        private float nextRepeatTime;

        public Vector2Int CurrentLogicalPosition =>
            playerState == null ? Vector2Int.zero : new Vector2Int(playerState.Position.X, playerState.Position.Y);

        public int CurrentTurn => turnManager == null ? 0 : turnManager.CurrentTurn;

        public void Initialize(
            BoardState board,
            PlayerState state,
            PlayerView view,
            TurnManager turns)
        {
            boardState = board;
            playerState = state;
            playerView = view;
            turnManager = turns;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                ResetHeldInput();
                return;
            }

            if (TryStartHeldMove(keyboard.upArrowKey, Vector2Int.up) ||
                TryStartHeldMove(keyboard.downArrowKey, Vector2Int.down) ||
                TryStartHeldMove(keyboard.leftArrowKey, Vector2Int.left) ||
                TryStartHeldMove(keyboard.rightArrowKey, Vector2Int.right))
            {
                return;
            }

            if (heldKey == null || !heldKey.isPressed)
            {
                ResetHeldInput();
                return;
            }

            if (Time.unscaledTime < nextRepeatTime)
            {
                return;
            }

            TryMove(heldDirection);
            nextRepeatTime = Time.unscaledTime + repeatInterval;
        }

        private bool TryStartHeldMove(KeyControl key, Vector2Int direction)
        {
            if (!key.wasPressedThisFrame)
            {
                return false;
            }

            heldKey = key;
            heldDirection = direction;
            nextRepeatTime = Time.unscaledTime + initialRepeatDelay;
            TryMove(direction);
            return true;
        }

        private void ResetHeldInput()
        {
            heldKey = null;
            heldDirection = Vector2Int.zero;
            nextRepeatTime = 0f;
        }

        public bool TryMove(Vector2Int movement)
        {
            if (boardState == null || playerState == null || movement.sqrMagnitude != 1)
            {
                return false;
            }

            GridPosition destination = playerState.Position.Offset(movement);
            if (!boardState.CanPlayerEnter(destination))
            {
                return false;
            }

            playerState.MoveTo(destination, movement);
            playerView.Apply(playerState);
            turnManager.CompletePlayerAction();
            return true;
        }
    }
}
