using System.Collections.Generic;
using UnityEngine;

namespace FourCore
{
    public sealed partial class EnemyManager
    {
        public AugmentCombatState Augments { get; } = new();
        public bool CanPlaceShield(GridPosition p) => GameConfig.IsInsidePlayable(p) && !coreState.Occupies(p) && !Boss.Occupies(p);
        public void ShieldPlaced(GridPosition p)
        {
            if (Run.Has(AugmentType.TimedBarrier)) Augments.Bombs[p] = 3;
            shieldView.Apply(shieldState);
        }
        public void ShieldRecalled(GridPosition p) { Augments.Bombs.Remove(p); shieldView.Apply(shieldState); }
        private bool HasShield(GridPosition p) => shieldState.Contains(p);
        private bool RemoveShield(GridPosition p)
        {
            if (!shieldState.TryRemove(p)) return false;
            Augments.Bombs.Remove(p);
            playerEnergyState.TryRestore(shieldState.Count);
            return true;
        }
        private bool ConsumeShield(GridPosition p)
        {
            if (!shieldState.Contains(p)) return false;
            List<GridPosition> linked = new() { p };
            if (Run.Has(AugmentType.ChainBarrier))
            {
                for (int i = 0; i < linked.Count; i++)
                    foreach (GridPosition other in shieldState.Positions)
                        if (!linked.Contains(other) && Mathf.Abs(other.X-linked[i].X)+Mathf.Abs(other.Y-linked[i].Y)==1) linked.Add(other);
            }
            foreach (GridPosition cell in linked)
                if (RemoveShield(cell)) shieldView.ShowBreak(cell);
            if (Run.Has(AugmentType.ChainBarrier)) Explode(linked);
            shieldView.Apply(shieldState);
            return true;
        }
        private void ResolveBodyShield(EnemyRuntime enemy, GridPosition position)
        {
            ConsumeShield(position);
            if (!Run.Has(AugmentType.ChainBarrier)) DamageEnemy(enemy, position);
        }
        private void Explode(IReadOnlyList<GridPosition> origins)
        {
            bool InBlast(GridPosition p)
            {
                foreach (GridPosition origin in origins)
                    if (Mathf.Abs(p.X-origin.X) <= 1 && Mathf.Abs(p.Y-origin.Y) <= 1) return true;
                return false;
            }
            foreach (EnemyRuntime enemy in enemies)
                if (!removedEnemies.Contains(enemy) && InBlast(enemy.State.Position)) DamageEnemy(enemy, enemy.State.Position);
            if (Boss.Active)
                for (int i = 0; i < Boss.CellCount; i++)
                    if (Boss.Health(i) > 0 && InBlast(Boss.Cell(i))) Boss.TryDamage(Boss.Cell(i));
            shieldView.ShowBlast(origins);
        }
        private void AdvanceAugments()
        {
            List<GridPosition> due = new();
            foreach (GridPosition p in new List<GridPosition>(Augments.Bombs.Keys))
            {
                if (!shieldState.Contains(p)) { Augments.Bombs.Remove(p); continue; }
                if (--Augments.Bombs[p] <= 0) due.Add(p);
            }
            foreach (GridPosition p in due) RemoveShield(p);
            if (due.Count > 0) Explode(due);
            shieldView.Apply(shieldState);
        }
    }
}
