using System.Collections.Generic;
using UnityEngine;

namespace FourCore
{
    public sealed class EnemyManager : MonoBehaviour
    {
        private sealed class EnemyRuntime
        {
            public EnemyState State { get; }
            public EnemyView View { get; }

            public EnemyRuntime(EnemyState state, EnemyView view)
            {
                State = state;
                View = view;
            }
        }

        [SerializeField, Min(1)] private int spawnIntervalTurns = 6;
        [SerializeField, Min(0)] private int entryDelayTurns = 1;

        private readonly List<EnemyRuntime> enemies = new();
        private readonly List<EnemyRuntime> removedEnemies = new();
        private CoreState coreState;
        private CoreView coreView;
        private PlayerState playerState;
        private ShieldState shieldState;
        private ShieldView shieldView;
        private TurnManager turnManager;
        private int spawnIndex;
        private int turnsUntilSpawn;

        public int ActiveEnemyCount => enemies.Count;

        public void Initialize(
            CoreState core,
            CoreView coreDisplay,
            PlayerState player,
            ShieldState shields,
            ShieldView shieldsDisplay,
            TurnManager turns)
        {
            coreState = core;
            coreView = coreDisplay;
            playerState = player;
            shieldState = shields;
            shieldView = shieldsDisplay;
            turnManager = turns;
            turnManager.TurnCompleted += HandleTurnCompleted;

            SpawnStraightEnemy();
            turnsUntilSpawn = spawnIntervalTurns;
        }

        private void HandleTurnCompleted(int currentTurn)
        {
            StepEnemies();

            turnsUntilSpawn--;
            if (turnsUntilSpawn <= 0)
            {
                SpawnStraightEnemy();
                turnsUntilSpawn = spawnIntervalTurns;
            }
        }

        private void StepEnemies()
        {
            removedEnemies.Clear();

            foreach (EnemyRuntime enemy in enemies)
            {
                if (enemy.State.WaitBeforeMoving()) continue;

                GridPosition destination = enemy.State.GetNextPosition();
                if (shieldState.Contains(destination))
                {
                    shieldState.TryRemove(destination);
                    shieldView.Apply(shieldState);
                    removedEnemies.Add(enemy);
                    continue;
                }

                if (coreState.Occupies(destination))
                {
                    coreState.TryDamage(destination);
                    coreView.Apply(coreState);
                    removedEnemies.Add(enemy);
                    continue;
                }

                enemy.State.MoveTo(destination);
                enemy.View.Apply(enemy.State);
            }

            foreach (EnemyRuntime enemy in removedEnemies)
            {
                enemies.Remove(enemy);
                Destroy(enemy.View.gameObject);
            }
        }

        private void SpawnStraightEnemy()
        {
            (GridPosition position, Vector2Int direction) spawn = GetSpawn(spawnIndex++ % 4);
            EnemyState state = new(EnemyType.Straight, spawn.position, spawn.direction, entryDelayTurns);

            GameObject enemyObject = new($"Straight Enemy {spawnIndex}");
            enemyObject.transform.SetParent(transform, false);
            EnemyView view = enemyObject.AddComponent<EnemyView>();
            view.Initialize(state);
            enemies.Add(new EnemyRuntime(state, view));
        }

        private static (GridPosition position, Vector2Int direction) GetSpawn(int index)
        {
            return index switch
            {
                0 => (new GridPosition(GameConfig.CoreMin, GameConfig.LogicalGridSize - 1), Vector2Int.down),
                1 => (new GridPosition(GameConfig.LogicalGridSize - 1, GameConfig.CoreMax), Vector2Int.left),
                2 => (new GridPosition(GameConfig.CoreMax, 0), Vector2Int.up),
                _ => (new GridPosition(0, GameConfig.CoreMin), Vector2Int.right)
            };
        }

        private void OnDestroy()
        {
            if (turnManager != null)
            {
                turnManager.TurnCompleted -= HandleTurnCompleted;
            }
        }
    }
}
