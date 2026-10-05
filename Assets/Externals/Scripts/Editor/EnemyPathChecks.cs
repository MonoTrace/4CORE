using System;
using UnityEditor;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class EnemyPathChecks
    {
        [MenuItem("4CORE/Validate Enemy Path Prediction")]
        public static void Run()
        {
            Check(new EnemyState(EnemyType.Straight, new GridPosition(5, 15), Vector2Int.down, 2),
                new[] { new GridPosition(5, 14), new GridPosition(5, 13), new GridPosition(5, 12) });
            Check(new EnemyState(EnemyType.Diagonal, new GridPosition(5, 15), new Vector2Int(1, -1), 2),
                new[] { new GridPosition(6, 14), new GridPosition(7, 13), new GridPosition(8, 12) });
            Check(new EnemyState(EnemyType.RightAngle, new GridPosition(9, 15), Vector2Int.right, 2,
                    Vector2Int.down, new GridPosition(11, 15)),
                new[] { new GridPosition(10, 15), new GridPosition(11, 15), new GridPosition(11, 14) });
            Check(new EnemyState(EnemyType.RightAngle, new GridPosition(11, 15), Vector2Int.right, 2,
                    Vector2Int.down, new GridPosition(11, 15)),
                new[] { new GridPosition(11, 14), new GridPosition(11, 13), new GridPosition(11, 12) });
            EnemyState zigzag = new(EnemyType.Zigzag, new GridPosition(8, 18), new Vector2Int(1, -1), 2,
                new Vector2Int(-1, -1));
            Check(zigzag, new[] { new GridPosition(9, 17), new GridPosition(8, 16), new GridPosition(9, 15) });
            zigzag.MoveTo(zigzag.GetNextPosition());
            Check(zigzag, new[] { new GridPosition(8, 16), new GridPosition(9, 15), new GridPosition(8, 14) });

            GridPosition[] buffer = new GridPosition[6];
            CoreState core = new();
            EnemyState incoming = new(EnemyType.Straight, new GridPosition(11, 15), Vector2Int.down, 0);
            Require(incoming.PredictPath(buffer, core) == 3, "Stop at the first live core cell");
            core.TryDamage(new GridPosition(11, 12), 0, "Path prediction check");
            Require(incoming.PredictPath(buffer, core) == 4, "Destroyed core cells are passable");
            EnemyState edge = new(EnemyType.Straight, new GridPosition(0, 0), Vector2Int.left, 0);
            Require(edge.PredictPath(buffer) == 0, "Stop outside logical bounds");
            Debug.Log("[PATH CHECKS] Passed: four types, both zigzag phases, turn point, live-state isolation, core and bounds.");
        }

        private static void Check(EnemyState state, GridPosition[] expected)
        {
            GridPosition originalPosition = state.Position;
            Vector2Int originalDirection = state.Direction;
            int delay = state.EntryDelay;
            GridPosition[] predicted = new GridPosition[expected.Length];
            Require(state.PredictPath(predicted) == expected.Length, "Prediction length");
            Require(state.Position.Equals(originalPosition) && state.Direction == originalDirection && state.EntryDelay == delay,
                "Prediction must not change live position, direction or wait time");
            EnemyState actual = state.CopyForPrediction();
            for (int i = 0; i < expected.Length; i++)
            {
                GridPosition next = actual.GetNextPosition();
                Require(predicted[i].Equals(expected[i]) && predicted[i].Equals(next), $"{state.Type} step {i}");
                actual.MoveTo(next);
            }
            GridPosition[] again = new GridPosition[expected.Length];
            state.PredictPath(again);
            for (int i = 0; i < expected.Length; i++) Require(again[i].Equals(predicted[i]), "Repeat prediction is stable");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
