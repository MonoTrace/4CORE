using System;
using System.Reflection;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class MissileEnemyChecks
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Read<T>(object owner, string name) => (T)owner.GetType().GetField(name, Fields).GetValue(owner);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        // Run in a disposable Play Mode session after the title transition.
        public static string Run()
        {
            EnemyManager manager = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            CoreState core = Read<CoreState>(manager, "coreState");
            CoreView coreView = Read<CoreView>(manager, "coreView");
            PlayerState player = Read<PlayerState>(manager, "playerState");
            PlayerEnergyState energy = Read<PlayerEnergyState>(manager, "playerEnergyState");
            ShieldState shields = Read<ShieldState>(manager, "shieldState");
            TurnManager turns = Read<TurnManager>(manager, "turnManager");
            ScoreState score = Read<ScoreState>(manager, "scoreState");
            GridPosition firing = new(GameConfig.CoreMin, GameConfig.CoreMax + 5);
            GridPosition barrier = firing.Offset(Vector2Int.down * 2);

            EnemyState Shooter()
            {
                EnemyState state = EnemySpawnFactory.Create(EnemyType.Missile, 0, 0);
                state.MoveTo(firing);
                return state;
            }
            void Reset(params EnemyState[] states)
            {
                core.Restore(new int[4]);
                coreView.Apply(core, false);
                shields.Restore(Array.Empty<GridPosition>());
                Read<ShieldView>(manager, "shieldView").Apply(shields);
                player.Restore(new GridPosition(5, 5), FacingDirection.Up);
                energy.Restore(4, 4);
                score.Restore(0);
                turns.Restore(0);
                EnemyManager.Snapshot snapshot = manager.Capture();
                typeof(EnemyManager.Snapshot).GetField("States", Fields).SetValue(snapshot, states);
                typeof(EnemyManager.Snapshot).GetField("TurnsUntilSpawn", Fields).SetValue(snapshot, 1000);
                manager.Restore(snapshot);
            }
            EnemyState Live() => Read<EnemyState[]>(manager.Capture(), "States")[0];
            void Step(int count = 1) { for (int i = 0; i < count; i++) turns.CompletePlayerAction(); }

            Reset(Shooter());
            GridPosition[] path = new GridPosition[24];
            Require(Live().PredictMissilePath(path, core) == 5, "Preview must stop at the nearest living core.");
            shields.TryAdd(barrier); energy.TrySpend();
            Step();
            Require(Live().MissileActive && Live().Position.Equals(firing), "Shooter must fire and stop.");
            Step(2);
            Require(Live().MissileReflected && shields.Count == 0 && energy.Current == 4, "Shield reflection/refund failed.");
            Require(Live().Direction == Vector2Int.right && Live().FiringDirection == Vector2Int.down, "Reflection rotated the shooter.");
            EnemyManager.Snapshot reflected = manager.Capture();
            player.Restore(firing.Offset(Vector2Int.down), FacingDirection.Up);
            Step();
            Require(energy.Current == 4, "Reflected missile damaged player.");
            Step();
            Require(manager.ActiveEnemyCount == 0 && score.Total == 160, "Reflected missile must defeat shooter and score.");
            manager.Restore(reflected);
            Require(Live().MissileReflected && Live().MissilePosition.Equals(barrier), "Missile snapshot restore failed.");

            Reset(Shooter()); Step(); Step(5);
            Require(core.AliveCount == 3 && !Live().MissileActive, "Missile must damage core.");
            Step();
            Require(Live().Position.Equals(firing.Offset(Vector2Int.right)) && Live().HasFired, "Shooter must resume its original direction.");

            Reset(Shooter()); Step();
            player.Restore(firing.Offset(Vector2Int.down), FacingDirection.Up);
            Step();
            Require(energy.Current == 3 && !Live().MissileActive, "Hostile missile player collision failed.");

            EnemyState shooter = Shooter(); shooter.TryFire(core); shooter.ReflectMissile(barrier);
            EnemyState other = new(EnemyType.Straight, firing.Offset(Vector2Int.down), Vector2Int.left, 10);
            Reset(shooter, other); Step();
            Require(manager.ActiveEnemyCount == 1 && score.Total == 40, "Reflected missile must hit other enemy types.");

            for (int rotation = 0; rotation < 4; rotation++)
            {
                EnemyState rotated = EnemySpawnFactory.Create(EnemyType.Missile, 0, rotation, 7);
                Require(rotated.Direction.x * rotated.FiringDirection.x + rotated.Direction.y * rotated.FiringDirection.y == 0, "Launcher orientation must remain perpendicular.");
                Require(rotated.PredictMissilePath(path, core) > 0, "Rotated missile lane has no target.");
            }
            Reset();
            return "PASS: preview, firing, stopping, reflection/refund, friendly safety, shooter/other enemy damage, score, snapshot, core damage, resume, player hit, 4 orientations";
        }
    }
}
