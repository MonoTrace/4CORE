using System;
using UnityEngine;

namespace FourCore
{
    public sealed class TurnManager : MonoBehaviour
    {
        public int CurrentTurn { get; private set; }
        public event Action<int> TurnCompleted;

        public void CompletePlayerAction()
        {
            CurrentTurn++;
            TurnCompleted?.Invoke(CurrentTurn);
        }
    }
}
