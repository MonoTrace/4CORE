using System.Collections.Generic;
using UnityEngine;

namespace FourCore
{
    public sealed partial class EnemyManager : MonoBehaviour
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

        [SerializeField, Min(0)] private int entryDelayTurns = 1;

        private readonly List<EnemyRuntime> enemies = new();
        private readonly List<EnemyRuntime> removedEnemies = new();
        private readonly List<GridPosition> pendingFragments = new();
        private readonly List<ResonanceBeam> beams = new();
        public IReadOnlyList<ResonanceBeam> Beams => beams;
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
        private bool spawningStarted;
        private bool gameOver;
        public event System.Action AllCoresDestroyed;
        public event System.Action AllStagesCleared;

        public int ActiveEnemyCount => enemies.Count;
        public RunState Run { get; } = new();
        public PickupState Pickups { get; } = new();
        public BossState Boss { get; } = new();
        public PortalState Portals { get; } = new();
        private PortalView portalView;
        private PickupView pickupView;
        private BossView bossView;
        public event System.Action PlayerDamaged;
        public event System.Action RewardRequested;
        public bool BossOccupies(GridPosition position) => Boss.Occupies(position);
        public bool RepairUseful=>coreState.AliveCount<4||playerEnergyState.Current<Mathf.Max(0,playerEnergyState.Total-shieldState.Count);

        public sealed class Snapshot
        {
            internal EnemyState[] States;
            internal int SpawnIndex;
            internal int TurnsUntilSpawn;
            internal RunState Run;
            internal Pickup[] Pickups;
            internal BossState Boss;
            internal bool GameOver;
            internal GridPosition[] Fragments;
            internal ResonanceBeam[] Beams;
            internal AugmentCombatState Augments;
            internal PortalState Portals;
        }

        public Snapshot Capture()
        {
            EnemyState[] states = new EnemyState[enemies.Count];
            for (int i = 0; i < states.Length; i++) states[i] = enemies[i].State.CopyForPrediction();
            return new Snapshot { States = states, SpawnIndex = spawnIndex, TurnsUntilSpawn = turnsUntilSpawn,
                Run = Run.Copy(), Pickups = Pickups.Capture(), Boss = Boss.Copy(), GameOver = gameOver,
                Fragments=pendingFragments.ToArray(),Beams=beams.ConvertAll(b=>b.Copy()).ToArray(), Augments = Augments.Copy(), Portals = Portals.Copy() };
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
            pendingFragments.Clear();beams.Clear();
            if(snapshot.Fragments!=null)pendingFragments.AddRange(snapshot.Fragments);
            if(snapshot.Beams!=null)foreach(var beam in snapshot.Beams)beams.Add(beam.Copy());
            foreach (ScorePopup popup in GetComponentsInChildren<ScorePopup>())
            {
                popup.gameObject.SetActive(false);
                Destroy(popup.gameObject);
            }
            spawnIndex = snapshot.SpawnIndex;
            turnsUntilSpawn = snapshot.TurnsUntilSpawn;
            if (snapshot.Run != null) Run.Restore(snapshot.Run);
            Augments.Restore(snapshot.Augments);
            Portals.Restore(snapshot.Portals);
            shieldView.ClearTransient();
            if (snapshot.Pickups != null) Pickups.Restore(snapshot.Pickups);
            if (snapshot.Boss != null) Boss.Restore(snapshot.Boss);
            gameOver = snapshot.GameOver;
            foreach (EnemyState saved in snapshot.States)
            {
                EnemyState state = saved.CopyForPrediction();
                EnemyView view = Instantiate(prefabs[state.Type], transform, false);
                view.name = $"{state.Type} Enemy (Restored)";
                view.Initialize(state);
                enemies.Add(new EnemyRuntime(state, view));
            }
            RefreshPaths();
            SynchronizeCoreEnergy();
            pickupView?.Apply(); bossView?.Apply(false);
        }

        public void ConfigureFeatures(PickupView pickups, BossView boss)
        {
            pickupView = pickups; bossView = boss;
            pickupView?.Initialize(Pickups);
            bossView?.Initialize(Boss, coreState,beams);
        }

        public void CollectPickup(bool protectCurrentTurn=true)
        {
            if (!Pickups.TryCollect(playerState.Position, out PickupType type)) return;
            ApplyPickup(type,protectCurrentTurn);pickupView?.Apply();
        }
        public bool ChooseReward(AugmentType type)
        {
            if (!Run.AcceptReward(type)) return false;
            if (type == AugmentType.PortalKit) Portals.Collect();
            if (AugmentCatalog.IsPlaystyle(type))
            {
                shieldState.Restore(System.Array.Empty<GridPosition>());
                Augments.Restore(null);
                playerEnergyState.Restore(playerEnergyState.Total, playerEnergyState.Total);
            }
            SynchronizeCoreEnergy();
            return true;
        }
        private void ApplyPickup(PickupType type,bool protectCurrentTurn)
        {
            if (type == PickupType.Repair)
            {
                // Recovering a core supplies exactly one additional energy slot.
                if (!coreState.TryRecoverOne()) playerEnergyState.TryRestore(shieldState.Count);
                coreView.Apply(coreState);
            }
            Run.Activate(type,protectCurrentTurn);
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
            ScorePopup popupPrefab = null,
            bool startImmediately = true)
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
            shieldView.BindAugments(Run, Augments, turns);
            coreState.Changed += SynchronizeCoreEnergy;
            SynchronizeCoreEnergy();
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

            if (startImmediately) StartSpawning();
        }

        public void StartSpawning()
        {
            if (spawningStarted) return;
            spawningStarted = true;
            SpawnNextEnemy();
            turnsUntilSpawn = Run.SpawnGap();
            RefreshPaths();
        }

        private void HandleTurnCompleted(int currentTurn)
        {
            if (!spawningStarted || gameOver || Run.Completed || Run.NeedsReward) return;
            if (Run.FreezeMoves > 0)
            {
                if (turnManager.LastAction == PlayerActionKind.Movement) Run.ConsumeFrozenMove();
                RefreshPaths(); bossView?.Apply(); return;
            }
            // Tick existing damage before enemy attacks, so a new hit displays 90.
            if(turnManager.LastAction==PlayerActionKind.Movement)coreState.AdvanceRecovery(1 + Run.BonusCoreRecovery);
            coreView.Apply(coreState);
            playerEnergyState.SetMaximumFromCores(coreState.AliveCount, shieldState.Count, Run.BonusShieldEnergy);
            AdvanceAugments();
            CleanupEnemies();
            AdvanceBeams();
            CleanupEnemies();
            StepEnemies();
            SpawnPendingFragments();
            if (Run.Phase == EncounterPhase.Boss) StepBoss();
            CleanupEnemies();
            CollectPickup(false);

            if (coreState.AliveCount == 0)
            {
                gameOver = true;
                RefreshPaths();
                AllCoresDestroyed?.Invoke();
                return;
            }
            if(Run.NeedsReward){RewardRequested?.Invoke();RefreshPaths();return;}
            if(Run.Completed){gameOver=true;RefreshPaths();AllStagesCleared?.Invoke();return;}

            if (Run.BossReady && enemies.Count == 0 && beams.Count==0 && pendingFragments.Count==0 && bossView != null)
            {
                Run.BeginBoss(); Boss.Begin(ChooseBossSite(), Run.Chapter); bossView.Apply();
            }
            turnsUntilSpawn--;
            if (turnsUntilSpawn <= 0)
            {
                if (Run.CanSpawn) SpawnNextEnemy();
                turnsUntilSpawn = Run.SpawnGap();
            }
            RefreshPaths();bossView?.Apply();
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
                if (removedEnemies.Contains(enemy)) continue;
                if (HasShield(enemy.State.Position))
                {
                    ResolveBodyShield(enemy, enemy.State.Position);
                    continue;
                }
                if (enemy.State.Position.Equals(playerState.Position))
                {
                    DamagePlayerAndRemove(enemy);
                    continue;
                }

                if (enemy.State.WaitBeforeMoving()) continue;

                if (enemy.State.MissileActive)
                {
                    StepMissile(enemy);
                    continue;
                }
                if (enemy.State.TryFire(coreState))
                {
                    if((enemy.State.Traits&EnemyTraits.Resonance)!=0)
                    {
                        QueueBeam(enemy.State.Position,enemy.State.MissileDirection);
                        enemy.State.StopMissile();
                    }
                    continue;
                }

                GridPosition destination = enemy.State.GetNextPosition();
                bool occupied = false;
                foreach (EnemyRuntime other in enemies)
                    if (other != enemy && !removedEnemies.Contains(other) && other.State.Position.Equals(destination)) { occupied = true; break; }
                if (occupied) continue;
                if (!GameConfig.IsInsideLogical(destination))
                {
                    removedEnemies.Add(enemy);
                    continue;
                }
                if (HasShield(destination))
                {
                    ResolveBodyShield(enemy, destination);
                    continue;
                }

                if (coreState.Occupies(destination))
                {
                    coreState.TryDamage(
                        destination,
                        turnManager.CurrentTurn,
                        $"적 충돌 | 종류={enemy.State.Type} | 적={enemy.View.name} | 이동={enemy.State.Position} -> {destination}");
                    coreView.Apply(coreState);
                    playerEnergyState.SetMaximumFromCores(coreState.AliveCount, shieldState.Count, Run.BonusShieldEnergy);
                    removedEnemies.Add(enemy);
                    continue;
                }

                if (destination.Equals(playerState.Position))
                {
                    DamagePlayerAndRemove(enemy);
                    continue;
                }

                enemy.State.MoveTo(destination);
                if((enemy.State.Traits&EnemyTraits.Resonance)!=0 && enemy.State.Type!=EnemyType.Missile && enemy.State.MoveCount%3==0)
                    QueueAimedBeam(destination);
                enemy.View.Apply(enemy.State);
            }

            // Also catch an enemy moving onto a reflected projectile later in this turn.
            foreach (EnemyRuntime owner in enemies)
            {
                if (removedEnemies.Contains(owner) || !owner.State.MissileActive || !owner.State.MissileReflected) continue;
                TryHitEnemyWithMissile(owner, owner.State.MissilePosition);
            }

            CleanupEnemies();
        }

        private void CleanupEnemies()
        {
            foreach (EnemyRuntime enemy in removedEnemies)
            {
                if (!enemies.Remove(enemy)) continue;
                if(enemy.State.CountsTowardStage)Run.ResolvedEnemy();
                enemy.View.gameObject.SetActive(false);
                Destroy(enemy.View.gameObject);
            }
            removedEnemies.Clear();
        }

        private void DamagePlayerAndRemove(EnemyRuntime enemy)
        {
            DamagePlayer();
            removedEnemies.Add(enemy);
            if((enemy.State.Traits&EnemyTraits.Split)!=0)
            {pendingFragments.Add(enemy.State.Position);pendingFragments.Add(enemy.State.Position);}
        }

        private void DamagePlayer()
        {
            if(Run.AbsorbHit())return;
            // Destroy one live core, rather than only spending deployable energy.
            // Prefer the closest connected core; cell order breaks distance ties.
            int chosen = -1;
            int closest = int.MaxValue;
            for (int i = 0; i < coreState.CellCount; i++)
            {
                if (!coreState.IsAlive(i)) continue;
                GridPosition cell = coreState.GetCell(i);
                int distance = Mathf.Abs(cell.X - playerState.Position.X) + Mathf.Abs(cell.Y - playerState.Position.Y);
                if (distance < closest) { chosen = i; closest = distance; }
            }
            if (chosen < 0) return;
            coreState.TryDamage(coreState.GetCell(chosen), turnManager.CurrentTurn, "플레이어 피격");
            coreView.Apply(coreState);
            PlayerDamaged?.Invoke();
        }

        private void SynchronizeCoreEnergy()
        {
            shieldState.SetCapacity(coreState.AliveCount + Run.BonusShieldEnergy);
            playerEnergyState.SetMaximumFromCores(coreState.AliveCount, shieldState.Count, Run.BonusShieldEnergy);
            shieldView.Apply(shieldState);
        }

        private void DamageEnemy(EnemyRuntime enemy, GridPosition position)
        {
            if (removedEnemies.Contains(enemy) || !enemy.State.TakeDamage()) return;
            removedEnemies.Add(enemy);
            if((enemy.State.Traits&EnemyTraits.Split)!=0)
            {pendingFragments.Add(position);pendingFragments.Add(position);}
            int coreCount = coreState.AliveCount;
            long awarded = scoreState.AddEnemyDefeat(enemy.State.BaseScore, coreCount);
            if (Random.Range(0,100) < 33 && !coreState.Occupies(position) && !Portals.IsEndpoint(position))
            {
                PickupType type = (PickupType)(Run.NextDrop % 3);
                if (Pickups.TryAdd(position,type)) { Run.TakeNextDrop(); pickupView?.Apply(); }
            }
            if (awarded > 0 && scorePopupPrefab != null)
            {
                ScorePopup popup = Instantiate(scorePopupPrefab, transform, false);
                popup.transform.localPosition = GameConfig.LogicalToWorld(position, -0.3f);
                popup.Show(enemy.State.BaseScore, coreCount);
            }
        }

        private bool TryHitEnemyWithMissile(EnemyRuntime owner, GridPosition position)
        {
            foreach (EnemyRuntime target in enemies)
            {
                if (removedEnemies.Contains(target) || !target.State.Position.Equals(position)) continue;
                owner.State.StopMissile();
                DamageEnemy(target, position);
                return true;
            }
            return false;
        }

        private void StepMissile(EnemyRuntime owner)
        {
            EnemyState state = owner.State;
            // The player may have stepped onto a projectile before this turn's enemy update.
            if (!state.MissileReflected && state.MissilePosition.Equals(playerState.Position))
            {
                state.StopMissile();
                DamagePlayer();
                return;
            }
            GridPosition next = state.MissilePosition.Offset(state.MissileDirection);
            if (!GameConfig.IsInsideLogical(next)) { state.StopMissile(); return; }
            if (ConsumeShield(next))
            {
                state.ReflectMissile(next);
                return;
            }
            if (state.MissileReflected && TryHitEnemyWithMissile(owner, next)) return;
            if (!state.MissileReflected && next.Equals(playerState.Position))
            {
                state.StopMissile();
                DamagePlayer();
                return;
            }
            if (coreState.Occupies(next))
            {
                state.StopMissile();
                coreState.TryDamage(next, turnManager.CurrentTurn, $"미사일 피격 | 발사 적={owner.View.name}");
                coreView.Apply(coreState);
                playerEnergyState.SetMaximumFromCores(coreState.AliveCount, shieldState.Count, Run.BonusShieldEnergy);
                return;
            }
            state.MoveMissile(next);
        }

        private void SpawnNextEnemy()
        {
            if (!Run.CanSpawn) return;
            EnemyType type = Run.ChooseEnemy();
            EnemyState state = null;
            for (int attempt = 0; attempt < 32; attempt++)
            {
                EnemyState candidate = CreateEnemyState(type);
                if (HasAvailableRoute(candidate)) { state = candidate; break; }
            }
            if (state == null) return;
            if(!Run.IsInfinite)state.WithTraits(StageCatalog.Traits(Run.Chapter,type,Run.Spawned));
            if(Run.IsInfinite && Random.Range(0,100)<Mathf.Min(45,15+Run.Spawned/3))state.WithArmor();
            spawnIndex++;
            Run.SpawnedEnemy();

            EnemyView view = Instantiate(prefabs[type], transform, false);
            view.name = $"{type} Enemy {spawnIndex}";
            view.Initialize(state);
            enemies.Add(new EnemyRuntime(state, view));
        }

        private EnemyState CreateEnemyState(EnemyType type)
        {
            return EnemySpawnFactory.Create(type, entryDelayTurns, Random.Range(0, 4),
                type == EnemyType.Missile ? Random.Range(5, 8) : 5);
        }

        private bool HasAvailableRoute(EnemyState candidate)
        {
            GridPosition[] a = new GridPosition[48];
            int length = candidate.Type == EnemyType.Missile ? candidate.PredictMissilePath(a,coreState) : candidate.PredictPath(a,coreState);
            if (length == 0 || !coreState.Occupies(a[length-1])) return false;
            foreach (EnemyRuntime existing in enemies)
            {
                if (existing.State.Position.Equals(candidate.Position)) return false;
                if (existing.State.Type != candidate.Type) continue;
                GridPosition[] b = new GridPosition[48];
                int other = existing.State.Type == EnemyType.Missile ? existing.State.PredictMissilePath(b,coreState) : existing.State.PredictPath(b,coreState);
                int shared = 0;
                for (int i = 0; i < length; i++) for (int j = 0; j < other; j++)
                    if (a[i].Equals(b[j]) && !coreState.Occupies(a[i])) shared++;
                if (shared > 1) return false;
            }
            return true;
        }

        private int ChooseBossSite()
        {
            int first = Random.Range(0,4);
            for (int n = 0; n < 4; n++)
            {
                int side = (first + n) % 4;
                bool cross=Boss.Active?Boss.IsCross:Run.Chapter==2;
                GridPosition site = BossState.Site(side,cross);
                int size=cross?2:1;
                bool playerInside = playerState.Position.X >= site.X && playerState.Position.X <= site.X+size &&
                    playerState.Position.Y >= site.Y && playerState.Position.Y <= site.Y+size;
                if (!playerInside && (!Boss.Active || side != Boss.Side)) return side;
            }
            return (Boss.Side + 2) % 4;
        }

        private void StepBoss()
        {
            if (!Boss.Active) return;
            DrainBossSplits();
            if(Boss.Defeated){FinishBoss();bossView?.Apply();return;}
            if(Boss.WaitingForPulse)
            {
                if(!beams.Exists(b=>b.BossCell>=0)){Boss.EndPulseWait();AfterBossVolley();}
                bossView?.Apply();return;
            }
            if (Boss.HasMissiles)
            {
                foreach (BossProjectile missile in Boss.Missiles)
                {
                    if (!missile.Active) continue;
                    if (!missile.Reflected && missile.Position.Equals(playerState.Position))
                    { missile.Active = false; DamagePlayer(); continue; }
                    GridPosition next = missile.Position.Offset(missile.Direction);
                    if (!GameConfig.IsInsideLogical(next)) { missile.Active = false; continue; }
                    if (ConsumeShield(next))
                    {
                        missile.Position = next; missile.Direction = -missile.Direction; missile.Reflected = true; continue;
                    }
                    if (missile.Reflected && Boss.TryDamage(next)) { missile.Active = false; continue; }
                    if (!missile.Reflected && next.Equals(playerState.Position))
                    { missile.Active = false; DamagePlayer(); continue; }
                    if (coreState.Occupies(next))
                    {
                        missile.Active = false; coreState.TryDamage(next,turnManager.CurrentTurn,"보스 미사일 피격");
                        coreView.Apply(coreState); playerEnergyState.SetMaximumFromCores(coreState.AliveCount,shieldState.Count,Run.BonusShieldEnergy); continue;
                    }
                    missile.Position = next;
                }
                if (Boss.Defeated)
                {
                    DrainBossSplits();
                    FinishBoss();
                }
                else if (!Boss.HasMissiles)
                {
                    AfterBossVolley();
                }
            }
            else if (!Boss.ConsumeRest())
            {
                if(Boss.UsesStraightCharge)
                {AdvanceBossCharge();bossView?.Apply();return;}
                Boss.Fire(coreState);
                if(Boss.UsesResonance && Boss.HasMissiles)
                {
                    foreach(var missile in Boss.Missiles)if(missile.Active)
                    {QueueBeam(missile.Position,missile.Direction,missile.SourceCell);missile.Active=false;}
                    Boss.BeginPulseWait();
                }
                else if (!Boss.HasMissiles) Boss.Relocate(ChooseBossSite());
            }
            bossView?.Apply();
        }

        private void AfterBossVolley()
        {
            bool charge=Boss.UsesCharge;Boss.EndVolley();
            if(charge)AdvanceBossCharge();else Boss.Relocate(ChooseBossSite());
        }

        private void QueueBeam(GridPosition origin,Vector2Int direction,int bossCell=-1)
        {
            if(direction==Vector2Int.zero || beams.Count>=16)return;
            beams.Add(new ResonanceBeam{Origin=origin,Direction=direction,BossCell=bossCell});
        }
        private void QueueAimedBeam(GridPosition origin)
        {
            for(int i=0;i<coreState.CellCount;i++)
            {
                if(!coreState.IsAlive(i))continue;
                GridPosition cell=coreState.GetCell(i);int dx=cell.X-origin.X,dy=cell.Y-origin.Y;
                if(dx==0&&dy==0)continue;
                if(dx!=0&&dy!=0&&Mathf.Abs(dx)!=Mathf.Abs(dy))continue;
                QueueBeam(origin,new Vector2Int(System.Math.Sign(dx),System.Math.Sign(dy)));return;
            }
        }
        private void AdvanceBeams()
        {
            for(int i=beams.Count-1;i>=0;i--)
            {
                ResonanceBeam beam=beams[i];if(--beam.Remaining>0)continue;
                GridPosition cell=beam.Origin;
                for(int step=0;step<GameConfig.LogicalGridSize;step++)
                {
                    cell=cell.Offset(beam.Direction);if(!GameConfig.IsInsideLogical(cell))break;
                    if(ConsumeShield(cell))
                    {
                        if(beam.BossCell>=0&&Boss.Active)Boss.TryDamage(Boss.Cell(beam.BossCell));
                        Run.Announce("RESONANCE BLOCKED");break;
                    }
                    if(cell.Equals(playerState.Position)){DamagePlayer();break;}
                    if(coreState.Occupies(cell))
                    {
                        coreState.TryDamage(cell,turnManager.CurrentTurn,"Resonance beam");coreView.Apply(coreState);
                        playerEnergyState.SetMaximumFromCores(coreState.AliveCount,shieldState.Count,Run.BonusShieldEnergy);break;
                    }
                }
                beams.RemoveAt(i);
            }
        }

        private void DrainBossSplits()
        {
            GridPosition? cell;
            while((cell=Boss.TakeSplit()).HasValue)
            {pendingFragments.Add(cell.Value);pendingFragments.Add(cell.Value);}
            SpawnPendingFragments();
        }
        private void SpawnPendingFragments()
        {
            for(int request=pendingFragments.Count-1;request>=0;request--)
            {
                GridPosition origin=pendingFragments[request];GridPosition? free=null;
                for(int radius=1;radius<=4&&!free.HasValue;radius++)
                    for(int y=-radius;y<=radius&&!free.HasValue;y++)for(int x=-radius;x<=radius;x++)
                    {
                        if(Mathf.Abs(x)+Mathf.Abs(y)!=radius)continue;
                        GridPosition p=origin.Offset(new Vector2Int(x,y));
                        if(!GameConfig.IsInsidePlayable(p)||coreState.Occupies(p)||Boss.Occupies(p)||
                            shieldState.Contains(p)||p.Equals(playerState.Position)||enemies.Exists(e=>e.State.Position.Equals(p)))continue;
                        free=p;break;
                    }
                if(!free.HasValue)continue;
                GridPosition from=free.Value,target=default;int distance=int.MaxValue;
                for(int i=0;i<coreState.CellCount;i++)if(coreState.IsAlive(i))
                {
                    GridPosition c=coreState.GetCell(i);int d=Mathf.Abs(c.X-from.X)+Mathf.Abs(c.Y-from.Y);
                    if(d<distance){distance=d;target=c;}
                }
                if(distance==int.MaxValue)continue;
                Vector2Int horizontal=new(System.Math.Sign(target.X-from.X),0),vertical=new(0,System.Math.Sign(target.Y-from.Y));
                EnemyState state=horizontal==Vector2Int.zero||vertical==Vector2Int.zero?
                    new EnemyState(EnemyType.Straight,from,horizontal+vertical,1):
                    new EnemyState(EnemyType.RightAngle,from,vertical,1,horizontal,new GridPosition(from.X,target.Y));
                state.AsFragment();EnemyView view=Instantiate(prefabs[state.Type],transform,false);view.name="Fragment";view.Initialize(state);
                enemies.Add(new EnemyRuntime(state,view));pendingFragments.RemoveAt(request);
            }
        }

        private void AdvanceBossCharge()
        {
            GridPosition next=Boss.NextCharge();bool blocked=false,nearCore=false;
            for(int i=0;i<Boss.CellCount;i++)
            {
                if(Boss.Health(i)<=0)continue;
                GridPosition cell=Boss.CellAt(next,i);
                if(ConsumeShield(cell))
                {
                    Boss.TryDamage(Boss.Cell(i));blocked=true;
                }
                if(cell.Equals(playerState.Position)){DamagePlayer();blocked=true;}
                if(coreState.Occupies(cell)||!GameConfig.IsInsidePlayable(cell))nearCore=true;
            }
            shieldView.Apply(shieldState);
            if(blocked){Run.Announce("CHARGE BLOCKED");Boss.Teleport(ChooseBossSite());}
            else if(nearCore||Boss.PatternStep>=6)
            {
                if (Boss.UsesStraightCharge && nearCore)
                    for (int i=0;i<Boss.CellCount;i++)
                        if(Boss.Health(i)>0)
                            coreState.TryDamage(Boss.CellAt(next,i),turnManager.CurrentTurn,"직선 돌진 보스 충돌");
                coreView.Apply(coreState);
                Boss.Teleport(ChooseBossSite());
            }
            else Boss.MoveCharge(next);
            if(Boss.Defeated)FinishBoss();
        }

        private void FinishBoss()
        {
            int count=coreState.AliveCount;long awarded=scoreState.AddEnemyDefeat(200,count);
            if(awarded>0&&scorePopupPrefab!=null)
            {
                ScorePopup popup=Instantiate(scorePopupPrefab,transform,false);
                popup.transform.localPosition=GameConfig.LogicalToWorld(Boss.Origin,-.3f);popup.Show(200,count);
            }
            Boss.Finish();beams.Clear();Run.CompleteBoss();turnsUntilSpawn=4;
            coreState.TryRecoverOne();coreView.Apply(coreState);
            playerEnergyState.SetMaximumFromCores(coreState.AliveCount,shieldState.Count,Run.BonusShieldEnergy);
        }

        private void OnDestroy()
        {
            if (coreState != null) coreState.Changed -= SynchronizeCoreEnergy;
            if (turnManager != null)
            {
                turnManager.TurnCompleted -= HandleTurnCompleted;
            }
        }
    }
}
