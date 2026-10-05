using System;
using UnityEngine;

namespace FourCore
{
    public sealed class TurnManager : MonoBehaviour
    {
        public int CurrentTurn { get; private set; }
        public event Action<int> TurnCompleted;

        // Restoring history must not trigger enemy movement/spawning.
        public void Restore(int turn) => CurrentTurn = turn;

        public void CompletePlayerAction()
        {
            CurrentTurn++;
            TurnCompleted?.Invoke(CurrentTurn);
        }
    }
}
