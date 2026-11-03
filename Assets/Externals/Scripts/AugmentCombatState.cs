using System.Collections.Generic;
using UnityEngine;

namespace FourCore
{
    public sealed class AugmentCombatState
    {
        public readonly Dictionary<GridPosition, int> Bombs = new();
        public AugmentCombatState Copy()
        {
            var copy = new AugmentCombatState();
            foreach (var pair in Bombs) copy.Bombs.Add(pair.Key, pair.Value);
            return copy;
        }
        public void Restore(AugmentCombatState saved)
        {
            Bombs.Clear();
            if (saved == null) return;
            foreach (var pair in saved.Bombs) Bombs.Add(pair.Key, pair.Value);
        }
    }
}
