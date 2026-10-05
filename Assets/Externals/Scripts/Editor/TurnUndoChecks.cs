using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class TurnUndoChecks
    {
        private const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;

        // Run only in a fresh disposable Play session: this exercises real movement/events.
        public static void Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            EnemyManager manager = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            Require(player.CurrentTurn == 0, "Run in a fresh Play session");
            Require(!player.TryUndo(), "Empty history must reject undo");
            Require(!player.TryMove(Vector2Int.zero), "Invalid move must fail");
            Require(!player.TryUndo(), "Failed moves must not enter history");

            List<string> states = new() { Fingerprint(player, manager) };
            for (int i = 0; i < 8; i++)
            {
                Require(player.TryMove(i % 2 == 0 ? Vector2Int.right : Vector2Int.left), "Test movement blocked");
                states.Add(Fingerprint(player, manager));
            }
            for (int i = 7; i >= 3; i--)
            {
                Require(player.TryUndo(), "Undo should succeed");
                Require(Fingerprint(player, manager) == states[i], $"State differs after undo to turn {i}");
            }
            Require(!player.TryUndo() && player.CurrentTurn == 3, "History must stop at five moves");
            for (int i = 3; i < 8; i++)
            {
                player.TryMove(i % 2 == 0 ? Vector2Int.right : Vector2Int.left);
                Require(Fingerprint(player, manager) == states[i + 1], "Replay must reproduce movement, spawn and RNG");
            }

            // Simulate all mutable outcomes of a turn, including non-turn shield actions.
            string before = Fingerprint(player, manager);
            player.TryMove(Vector2Int.right);
            Get<CoreState>(manager, "coreState").TryDamage(new GridPosition(GameConfig.CoreMin, GameConfig.CoreMin), 9, "Undo check");
            Get<PlayerEnergyState>(player, "playerEnergyState").TryDamageTotal();
            Get<ScoreState>(manager, "scoreState").AddEnemyDefeat(40, 3);
            player.TryToggleShield();
            Require(player.TryUndo(), "Composite undo failed");
            Require(Fingerprint(player, manager) == before, "Core/energy/score/shields were not restored");

            Require(player.TryToggleShield(), "Install shield before move");
            string shielded = Fingerprint(player, manager);
            player.TryMove(Vector2Int.right);
            Require(player.TryUndo() && Fingerprint(player, manager) == shielded, "Pre-move shield must remain");
            Debug.Log("[UNDO CHECKS] Passed: five-turn cap, failed moves, deterministic spawn replay, core/energy/score/shields and enemy path states.");
        }

        private static T Get<T>(object owner, string field) => (T)owner.GetType().GetField(field, Fields).GetValue(owner);

        private static string Fingerprint(PlayerController player, EnemyManager manager)
        {
            PlayerState state = Get<PlayerState>(player, "playerState");
            PlayerEnergyState energy = Get<PlayerEnergyState>(player, "playerEnergyState");
            CoreState core = Get<CoreState>(manager, "coreState");
            StringBuilder value = new();
            value.Append($"{player.CurrentTurn}|{state.Position}|{state.Facing}|{energy.Current}/{energy.Total}|{Get<ScoreState>(manager, "scoreState").Total}|");
            for (int i = 0; i < core.CellCount; i++) value.Append(core.IsAlive(i));
            List<string> shields = new();
            foreach (GridPosition p in Get<ShieldState>(player, "shieldState").Positions) shields.Add(p.ToString());
            shields.Sort(StringComparer.Ordinal);
            value.Append(string.Join(";", shields));
            value.Append($"|{Get<int>(manager, "spawnIndex")}/{Get<int>(manager, "turnsUntilSpawn")}");
            foreach (object entry in Get<IEnumerable>(manager, "enemies"))
            {
                EnemyState enemy = (EnemyState)entry.GetType().GetProperty("State").GetValue(entry);
                value.Append($"|{enemy.Type}:{enemy.Position}:{enemy.Direction}:{enemy.EntryDelay}");
                GridPosition[] path = new GridPosition[6];
                int count = enemy.PredictPath(path, core);
                for (int i = 0; i < count; i++) value.Append(path[i]);
            }
            value.Append(JsonUtility.ToJson(UnityEngine.Random.state));
            return value.ToString();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
