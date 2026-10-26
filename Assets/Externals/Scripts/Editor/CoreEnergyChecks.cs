using System;
using System.Reflection;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class CoreEnergyChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Read<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static void Need(bool value, string message)
        { if (!value) throw new InvalidOperationException("CORE ENERGY CHECK: " + message); }

        public static string Run()
        {
            EnemyManager manager = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            CoreState cores = Read<CoreState>(manager, "coreState");
            PlayerEnergyState energy = Read<PlayerEnergyState>(manager, "playerEnergyState");
            ShieldState shields = Read<ShieldState>(manager, "shieldState");
            PlayerState player = Read<PlayerState>(manager, "playerState");
            PlayerController controls = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            CoreView coreView = Read<CoreView>(manager, "coreView");
            ShieldView shieldView = Read<ShieldView>(manager, "shieldView");
            MethodInfo hit = typeof(EnemyManager).GetMethod("DamagePlayer", Private);
            MethodInfo pickup = typeof(EnemyManager).GetMethod("ApplyPickup", Private);
            void Reset()
            {
                cores.Restore(new int[4]);
                shields.Restore(Array.Empty<GridPosition>());
                energy.Restore(4, 4);
                manager.Run.ConfigureMode(GameMode.Stage);
                manager.Run.AbsorbHit();
                coreView.Apply(cores, false);
            }
            void Damage() => hit.Invoke(manager, null);
            void Repair() => pickup.Invoke(manager, new object[] { PickupType.Repair, false });

            Reset();
            Damage();
            Need(cores.AliveCount == 3 && energy.Total == 3 && energy.Current == 3, "hit 4/4 -> 3/3");
            Repair();
            Need(cores.AliveCount == 4 && energy.Total == 4 && energy.Current == 4, "repair restores core and one energy");
            Repair();
            Need(cores.AliveCount == 4 && energy.Total == 4, "four-core cap");

            Reset();
            for (int i = 0; i < 4; i++)
            { Need(energy.TrySpend() && shields.TryAdd(new GridPosition(i, 0)), "install four shields"); }
            Damage();
            Need(cores.AliveCount == 3 && shields.Count == 3 && energy.Total == 3 && energy.Current == 0, "capacity includes deployed shields");
            Need(!shields.Contains(new GridPosition(3, 0)) && !energy.TrySpend(), "newest excess shield removed, no extra installation");
            Repair();
            Need(cores.AliveCount == 4 && shields.Count == 3 && energy.Current == 1, "repair gives exactly one slot with three deployed");

            Reset(); energy.Restore(0, 4);
            Damage(); Repair();
            Need(energy.Current == 1 && energy.Total == 4, "repair is plus one, not a full refill");

            Reset(); manager.Run.Activate(PickupType.PersonalShield); Damage();
            Need(cores.AliveCount == 4 && energy.Current == 4, "personal shield absorbs without core loss");
            Damage(); Need(cores.AliveCount == 3, "next unprotected hit destroys core");

            Reset();
            cores.TryDamage(cores.GetCell(0), 0, "energy synchronization test");
            Need(energy.Total == 3 && energy.Current == 3, "direct core damage synchronizes immediately");
            for (int i = 0; i < CoreState.RecoveryDuration; i++) cores.AdvanceRecovery();
            Need(cores.AliveCount == 4 && energy.Total == 4 && energy.Current == 4, "automatic recovery synchronizes");

            Reset(); controls.SetGameplayInput(true);
            TurnUndoHistory history = Read<TurnUndoHistory>(controls, "undoHistory");
            history.RecordBeforeMove(); Damage();
            Need(history.TryUndo() && cores.AliveCount == 4 && energy.Current == 4 && energy.Total == 4, "undo restores damage and energy together");
            Damage(); history.RecordBeforeMove(); Repair();
            Need(history.TryUndo() && cores.AliveCount == 3 && energy.Current == 3 && energy.Total == 3, "undo restores repair and core together");
            Reset();
            for (int i = 0; i < 4; i++) Damage();
            Need(cores.AliveCount == 0 && energy.Total == 0 && energy.Current == 0 && !energy.TrySpend(), "four hits exhaust all cores and shields");
            Reset(); shieldView.Apply(shields); coreView.Apply(cores, false);
            return "PASS: player hit, core hit, plus-one repair, maximum four, deployed shield capacity, personal protection, automatic recovery, undo damage/repair, zero cores";
        }
    }
}
