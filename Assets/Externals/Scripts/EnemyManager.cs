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
        private PlayerEnergyState playerEnergyState;
        private ShieldState shieldState;
        private ShieldView shieldView;
        private TurnManager turnManager;
        private ScoreState scoreState;
        private ScorePopup scorePopupPrefab;
        private readonly Dictionary<EnemyType, EnemyView> prefabs = new();
        private int spawnIndex;
        private int turnsUntilSpawn;

        public int ActiveEnemyCount => enemies.Count;

        public sealed class Snapshot
        {
            internal EnemyState[] States;
            internal int SpawnIndex;
            internal int TurnsUntilSpawn;
        }

        public Snapshot Capture()
        {
            EnemyState[] states = new EnemyState[enemies.Count];
            for (int i = 0; i < states.Length; i++) states[i] = enemies[i].State.CopyForPrediction();
            return new Snapshot { States = states, SpawnIndex = spawnIndex, TurnsUntilSpawn = turnsUntilSpawn };
        }

        public void Restore(Snapshot snapshot)
        {
            foreach (EnemyRuntime enemy in enemies)
            {
                enemy.View.gameObject.SetActive(false);
                Destroy(enemy.View.gameObject);
            }
            enemies.Clear();
            removedEnemies.Clear();
            foreach (ScorePopup popup in GetComponentsInChildren<ScorePopup>())
            {
                popup.gameObject.SetActive(false);
                Destroy(popup.gameObject);
            }
            spawnIndex = snapshot.SpawnIndex;
            turnsUntilSpawn = snapshot.TurnsUntilSpawn;
            foreach (EnemyState saved in snapshot.States)
            {
                EnemyState state = saved.CopyForPrediction();
                EnemyView view = Instantiate(prefabs[state.Type], transform, false);
                view.name = $"{state.Type} Enemy (Restored)";
                view.Initialize(state);
                enemies.Add(new EnemyRuntime(state, view));
            }
            RefreshPaths();
        }

        public void Initialize(
            CoreState core,
            CoreView coreDisplay,
            PlayerState player,
            PlayerEnergyState playerEnergy,
            ShieldState shields,
            ShieldView shieldsDisplay,
            TurnManager turns,
            ScoreState score,
            EnemyView[] enemyPrefabs,
            ScorePopup popupPrefab = null)
        {
            coreState = core;
            coreView = coreDisplay;
            playerState = player;
            playerEnergyState = playerEnergy;
            shieldState = shields;
            shieldView = shieldsDisplay;
            turnManager = turns;
            scoreState = score;
            scorePopupPrefab = popupPrefab;
            prefabs.Clear();
            foreach (EnemyView prefab in enemyPrefabs)
            {
                if (prefab == null || prefabs.ContainsKey(prefab.Type))
                    throw new System.InvalidOperationException("Enemy prefabs must be non-null with unique types.");
                prefabs.Add(prefab.Type, prefab);
            }
            foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
                if (!prefabs.ContainsKey(type))
                    throw new System.InvalidOperationException($"Missing prefab for enemy type: {type}");
            turnManager.TurnCompleted += HandleTurnCompleted;

            SpawnNextEnemy();
            turnsUntilSpawn = spawnIntervalTurns;
            RefreshPaths();
        }

        private void HandleTurnCompleted(int currentTurn)
        {
            StepEnemies();

            turnsUntilSpawn--;
            if (turnsUntilSpawn <= 0)
            {
                SpawnNextEnemy();
                turnsUntilSpawn = spawnIntervalTurns;
            }
            RefreshPaths();
        }

        private void RefreshPaths()
        {
            foreach (EnemyRuntime enemy in enemies)
                enemy.View.RefreshPath(enemy.State, coreState);
        }

        private void StepEnemies()
        {
            removedEnemies.Clear();

            foreach (EnemyRuntime enemy in enemies)
            {
                if (enemy.State.Position.Equals(playerState.Position))
                {
                    DamagePlayerAndRemove(enemy);
                    continue;
                }

                if (enemy.State.WaitBeforeMoving()) continue;

                GridPosition destination = enemy.State.GetNextPosition();
                if (shieldState.Contains(destination))
                {
                    shieldState.TryRemove(destination);
                    shieldView.Apply(shieldState);
                    playerEnergyState.TryRestore();
                    int coreCount = coreState.AliveCount;
                    long awarded = scoreState.AddEnemyDefeat(enemy.State.BaseScore, coreCount);
                    if (awarded > 0 && scorePopupPrefab != null)
                    {
                        // Keep the popup alive independently of the destroyed enemy.
                        ScorePopup popup = Instantiate(scorePopupPrefab, transform, false);
                        popup.transform.localPosition = GameConfig.LogicalToWorld(destination, -0.3f);
                        popup.Show(enemy.State.BaseScore, coreCount);
                    }
                    Debug.Log($"[SCORE] Enemy={enemy.State.Type} | {enemy.State.BaseScore} x {coreCount} = +{awarded} | Total={scoreState.Total}");
                    removedEnemies.Add(enemy);
                    continue;
                }

                if (coreState.Occupies(destination))
                {
                    coreState.TryDamage(
                        destination,
                        turnManager.CurrentTurn,
                        $"적 충돌 | 종류={enemy.State.Type} | 적={enemy.View.name} | 이동={enemy.State.Position} -> {destination}");
                    coreView.Apply(coreState);
                    removedEnemies.Add(enemy);
                    continue;
                }

                if (destination.Equals(playerState.Position))
                {
                    DamagePlayerAndRemove(enemy);
                    continue;
                }

                enemy.State.MoveTo(destination);
                enemy.View.Apply(enemy.State);
            }

            foreach (EnemyRuntime enemy in removedEnemies)
            {
                enemies.Remove(enemy);
                enemy.View.gameObject.SetActive(false);
                Destroy(enemy.View.gameObject);
            }
        }

        private void DamagePlayerAndRemove(EnemyRuntime enemy)
        {
            playerEnergyState.TryDamageTotal();
            removedEnemies.Add(enemy);
        }

        private void SpawnNextEnemy()
        {
            EnemyType type = (EnemyType)(spawnIndex % 4);
            EnemyState state = CreateEnemyState(type);
            spawnIndex++;

            EnemyView view = Instantiate(prefabs[type], transform, false);
            view.name = $"{type} Enemy {spawnIndex}";
            view.Initialize(state);
            enemies.Add(new EnemyRuntime(state, view));
        }

        private EnemyState CreateEnemyState(EnemyType type)
        {
            return EnemySpawnFactory.Create(type, entryDelayTurns, Random.Range(0, 4));
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
