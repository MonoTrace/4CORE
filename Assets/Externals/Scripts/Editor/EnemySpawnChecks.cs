using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class EnemySpawnChecks
    {
        [MenuItem("4CORE/Validate Four Direction Spawns")]
        public static void Run()
        {
            CoreState core = new();
            foreach (EnemyType type in Enum.GetValues(typeof(EnemyType)))
            {
                HashSet<GridPosition> starts = new();
                for (int rotation = 0; rotation < 4; rotation++)
                {
                    EnemyState state = EnemySpawnFactory.Create(type, 1, rotation);
                    starts.Add(state.Position);
                    int last = GameConfig.LogicalGridSize - 1;
                    Require(state.Position.X == 0 || state.Position.X == last || state.Position.Y == 0 || state.Position.Y == last,
                        "Spawn must be on the outer edge");
                    GridPosition start = state.Position;
                    Require(state.WaitBeforeMoving() && state.Position.Equals(start) && !state.WaitBeforeMoving(), "Entry delay preserved");
                    bool reached = false;
                    GridPosition[] prediction = new GridPosition[6];
                    for (int step = 0; step < 64; step++)
                    {
                        Require(state.PredictPath(prediction, core) > 0, $"{type}/{rotation} left board before reaching core");
                        GridPosition next = state.GetNextPosition();
                        Require(next.Equals(prediction[0]), "Path preview mismatch");
                        state.MoveTo(next);
                        if (core.Occupies(next)) { reached = true; break; }
                    }
                    Require(reached, $"{type}/{rotation} must reach core");
                }
                Require(starts.Count == 4, $"{type} must have four distinct spawn directions");
            }
            EnemyPathChecks.Run();
            Debug.Log("[SPAWN CHECKS] All 16 type/direction combinations reach the core and match path predictions.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
