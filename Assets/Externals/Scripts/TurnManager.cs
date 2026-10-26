using System;
using UnityEngine;

namespace FourCore
{
    public enum PlayerActionKind { Movement, Wait }
    public sealed class TurnManager : MonoBehaviour
    {
        public int CurrentTurn { get; private set; }
        public PlayerActionKind LastAction { get; private set; }
        public event Action<int> TurnCompleted;

        // Restoring history must not trigger enemy movement/spawning.
        public void Restore(int turn) => CurrentTurn = turn;

        public void CompletePlayerAction(PlayerActionKind kind = PlayerActionKind.Movement)
        {
            LastAction = kind;
            CurrentTurn++;
            TurnCompleted?.Invoke(CurrentTurn);
        }
    }
}
