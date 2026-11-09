using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class StageThemeChecks
    {
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
        private static T Read<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Fields).GetValue(obj);
        private static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Fields).SetValue(obj,value);
        private static void Need(bool value,string label){if(!value)throw new Exception("STAGE CHECK: "+label);}
        // Deterministic legacy passive fixtures; gameplay offer randomness is tested separately.
        public static void SkipBossReward(RunState run)
        {
            if (!run.NeedsReward) return;
            Set(run, "rewardChoices", new[] { AugmentType.ShieldEnergy, AugmentType.CoreRecovery, AugmentType.LongerFreeze });
            run.AcceptReward(AugmentType.LongerFreeze);
        }
        public static void AcceptAugment(AugmentType type)
        {
            var manager = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var flow = UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            Set(manager.Run, "rewardChoices", new[] { type, AugmentType.CoreRecovery, AugmentType.LongerFreeze });
            if (flow.State == GameFlowState.Reward)
            {
                typeof(GameFlowController).GetMethod("ConfigureAugmentChoices", Fields).Invoke(flow,null);
                // Synchronous combat fixtures skip real-time waits; PresentationGameplayChecks
                // separately verifies reveal, selection lock and resume on actual rendered frames.
                CompleteRewardSequence(flow, "RevealRewards", null);
                Read<UnityEngine.UI.Button>(flow,"freezeReward").onClick.Invoke();
                CompleteRewardSequence(flow, "AcceptReward", new object[] { 0, type });
            }
            else Need(manager.ChooseReward(type), "augment accepted");
        }
        private static void CompleteRewardSequence(GameFlowController flow, string method, object[] args)
        {
            flow.StopAllCoroutines();
            var sequence = (System.Collections.IEnumerator)typeof(GameFlowController).GetMethod(method, Fields).Invoke(flow,args);
            int remaining = 10000;
            while (sequence.MoveNext()) if (--remaining == 0) throw new Exception("Reward fixture did not complete: " + method);
        }
        public static string Augments()
        {
            // Each passive stacks, survives snapshots, and resets for a new run.
            foreach (AugmentType type in new[] { AugmentType.ShieldEnergy, AugmentType.CoreRecovery, AugmentType.LongerFreeze })
            {
                RunState run = new();
                Need(!run.AcceptReward(type), "cannot choose before a boss clear");
                for (int stage = 1; stage <= 2; stage++)
                {
                    run.BeginBoss(); run.CompleteBoss(); if(run.NeedsReward)Set(run,"rewardChoices",new[] { AugmentType.ShieldEnergy, AugmentType.CoreRecovery, AugmentType.LongerFreeze });
                    Need(run.Chapter == stage && run.NeedsReward && !run.CanSpawn, "selection gates next stage");
                    run.CompleteBoss();
                    Need(run.Chapter == stage, "duplicate completion cannot skip a stage");
                    Need(run.AcceptReward(type) && !run.AcceptReward(type), "exactly one augment per clear");
                }
                RunState copy = run.Copy();
                run.ConfigureMode(GameMode.Infinite); run.Restore(copy);
                Need(run.Chapter == 3 && run.BonusShieldEnergy == (type == AugmentType.ShieldEnergy ? 2 : 0)
                    && run.BonusCoreRecovery == (type == AugmentType.CoreRecovery ? 2 : 0)
                    && run.BonusFreezeMoves == (type == AugmentType.LongerFreeze ? 4 : 0), "stacked passives restored");
                run.Activate(PickupType.TimeFreeze, false);
                Need(run.FreezeMoves == 10 + run.BonusFreezeMoves, "freeze pickup uses upgraded duration");
                CoreState core = new(); core.TryDamage(core.GetCell(0), 0, "Augment check");
                core.AdvanceRecovery(1 + run.BonusCoreRecovery);
                Need(core.GetRecoveryTurns(0) == 89 - run.BonusCoreRecovery, "core recovery bonus applies");
                run.BeginBoss(); run.CompleteBoss(); if(run.NeedsReward)Set(run,"rewardChoices",new[] { AugmentType.ShieldEnergy, AugmentType.CoreRecovery, AugmentType.LongerFreeze });
                Need(run.Completed && !run.NeedsReward && !run.CanSpawn, "final boss ends without another selection");
                run.ConfigureMode(GameMode.Stage);
                Need(run.BonusShieldEnergy == 0 && run.BonusCoreRecovery == 0 && run.BonusFreezeMoves == 0, "new run resets upgrades");
            }

            var manager = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var controls = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var flow = UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            var energy = Read<PlayerEnergyState>(manager, "playerEnergyState");
            var shields = Read<ShieldState>(manager, "shieldState");
            var cores = Read<CoreState>(manager, "coreState");
            ShowBoss(1);
            for (int i = 0; i < manager.Boss.CellCount; i++)
                while (manager.Boss.Health(i) > 0) manager.Boss.TryDamage(manager.Boss.Cell(i));
            Step();
            Need(flow.State == GameFlowState.Reward && manager.Run.Chapter == 1 && manager.Run.NeedsReward, "actual boss opens selection");
            Need(!controls.TryMove(Vector2Int.right) && !controls.TryWait() && !controls.TryToggleShield(), "gameplay blocked during choice");
            int count = manager.ActiveEnemyCount;
            Step(); Step();
            Need(manager.ActiveEnemyCount == count && !manager.Run.CanSpawn, "no spawns during choice");
            Need(Read<CanvasGroup>(flow, "rewardGroup").gameObject.activeInHierarchy, "choice screen visible");
            Need(Read<UnityEngine.UI.Button>(flow, "freezeReward").interactable
                && Read<UnityEngine.UI.Button>(flow, "shieldReward").interactable
                && Read<UnityEngine.UI.Button>(flow, "repairReward").interactable, "all three options selectable");
            AcceptAugment(AugmentType.ShieldEnergy);
            Need(flow.State == GameFlowState.Playing && controls.GameplayInputEnabled && manager.Run.Chapter == 2
                && energy.Total == 5 && energy.Current == 5, "energy augment grants a usable slot and resumes play");
            for (int i = 0; i < 5; i++)
                Need(shields.TryAdd(new GridPosition(5 + i, 5)) && energy.TrySpend(), "five shields can be deployed");
            Need(!shields.TryAdd(new GridPosition(10, 5)), "capacity still enforced");
            Need(controls.TryMove(Vector2Int.right), "record undo with five deployed shields");
            cores.TryDamage(cores.GetCell(0), 0, "Augmented energy check");
            Need(energy.Total == 4 && shields.Count == 4, "core loss retains one bonus slot");
            Need(controls.TryUndo() && cores.AliveCount == 4 && energy.Total == 5 && shields.Count == 5
                && manager.Run.BonusShieldEnergy == 1, "undo restores all five shields and bonus capacity");
            shields.Restore(Array.Empty<GridPosition>()); energy.Restore(5, 5);
            manager.Run.BeginBoss(); manager.Boss.Begin(0, 2);
            for (int i = 0; i < manager.Boss.CellCount; i++)
                while (manager.Boss.Health(i) > 0) manager.Boss.TryDamage(manager.Boss.Cell(i));
            Step(); AcceptAugment(AugmentType.ShieldEnergy);
            Need(manager.Run.Chapter == 3 && manager.Run.BonusShieldEnergy == 2 && energy.Total == 6, "same augment stacks across bosses");
            return "PASS: three passives, stacking/snapshots/reset, real boss selection, input/spawn gate, five shields, core-loss/undo capacity, next-stage resume";
        }
        private static void CheckEnemy(int stage,int index,EnemyType type,EnemyTraits traits)
        {
            EnemyType expected=stage==1?EnemyType.Straight:stage==2?
                index%2==0?EnemyType.Diagonal:EnemyType.Zigzag:EnemyType.Missile;
            EnemyTraits expectedTraits=stage==2&&index%2==0?EnemyTraits.Phase:
                stage==3&&index%2==1?EnemyTraits.Resonance:EnemyTraits.None;
            Need(type==expected&&traits==expectedTraits,$"stage {stage} spawn {index}: {type}/{traits}");
        }
        public static string Pure()
        {
            Need(StageCatalog.Count==3,"three stages only");
            RunState run=new();CoreState core=new();
            for(int stage=1;stage<=StageCatalog.Count;stage++)
            {
                Need(run.Chapter==stage&&!run.Completed&&!run.IsInfinite,"progression");
                for(int index=0;index<30;index++)
                {
                    EnemyType type=run.ChooseEnemy();
                    CheckEnemy(stage,index,type,StageCatalog.Traits(stage,type,run.Spawned));
                    run.SpawnedEnemy();run.ResolvedEnemy();
                }
                Need(run.BossReady&&!run.CanSpawn,"thirty regular enemies then boss");
                BossState boss=new();boss.Begin(0,stage);boss.Fire(core);
                Need(boss.Health(0)==(stage==1?2:3),"boss health");
                if(stage==1)
                {
                    Need(boss.UsesStraightCharge&&!boss.HasMissiles,"straight boss does not fire");
                    GridPosition next=boss.NextCharge();
                    Need(next.X==boss.Origin.X&&next.Y==boss.Origin.Y-1,"cardinal core-directed step");
                }
                else if (stage == 2) Need(boss.UsesDiagonalCharge && !boss.HasMissiles, "diagonal boss never fires");
                else Need(boss.HasMissiles,"projectile boss attacks");
                run.BeginBoss();run.CompleteBoss();SkipBossReward(run);
            }
            Need(run.Completed&&!run.CanSpawn&&!run.IsInfinite&&run.Chapter==3,"third stage is final");
            BossState artillery=new();artillery.Begin(0,3);
            Need(!artillery.UsesResonance,"first volley normal missile");
            artillery.EndVolley();Need(artillery.UsesResonance,"second volley resonance");
            artillery.EndVolley();Need(!artillery.UsesResonance,"alternating volley");
            for (int side = 0; side < 4; side++)
            {
                BossState diagonal = new(); diagonal.Begin(side, 2);
                bool reached = false;
                for (int step = 0; step < 7 && !reached; step++)
                {
                    GridPosition next = diagonal.NextCharge();
                    Need(Mathf.Abs(next.X-diagonal.Origin.X)==1 && Mathf.Abs(next.Y-diagonal.Origin.Y)==1, "always diagonal");
                    for (int cell = 0; cell < diagonal.CellCount; cell++) reached |= core.Occupies(diagonal.CellAt(next,cell));
                    diagonal.MoveCharge(next); diagonal.Fire(core);
                    Need(diagonal.BodyVisible && !diagonal.HasMissiles, "no phase hiding or projectiles");
                }
                Need(reached, "all four diagonals reach the core");
                BossState copy = diagonal.Copy(); diagonal.Teleport((side+1)%4); diagonal.Restore(copy);
                Need(diagonal.Origin.Equals(copy.Origin) && diagonal.NextCharge().Equals(copy.NextCharge()), "diagonal snapshot");
            }
            run.ConfigureMode(GameMode.Infinite);
            for(int i=0;i<100;i++){run.SpawnedEnemy();run.ResolvedEnemy();}
            Need(run.CanSpawn&&!run.BossReady,"separate infinite mode unchanged");
            return "PASS: stage rosters, cardinal charge, four diagonal charges/core targeting/snapshots, artillery volleys, stage ending, infinite";
        }

        public static void ShowBoss(int stage)
        {
            var manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var core=Read<CoreState>(manager,"coreState");var coreView=Read<CoreView>(manager,"coreView");
            var controls=UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var shields=Read<ShieldState>(manager,"shieldState");var shieldView=Read<ShieldView>(manager,"shieldView");
            var energy=Read<PlayerEnergyState>(manager,"playerEnergyState");
            core.Restore(new int[4]);coreView.Apply(core,false);shields.Restore(Array.Empty<GridPosition>());shieldView.Apply(shields);energy.Restore(4,4);
            EnemyManager.Snapshot empty=manager.Capture();Set(empty,"States",Array.Empty<EnemyState>());Set(empty,"Beams",Array.Empty<ResonanceBeam>());
            Set(empty,"Fragments",Array.Empty<GridPosition>());Set(empty,"Boss",new BossState());Set(empty,"Run",new RunState());Set(empty,"GameOver",false);
            manager.Restore(empty);
            for(int i=1;i<stage;i++){manager.Run.CompleteBoss();SkipBossReward(manager.Run);}
            manager.Run.BeginBoss();manager.Boss.Begin(0,stage);
            Read<BossView>(manager,"bossView").Apply(false);
            Read<PlayerState>(manager,"playerState").Restore(new GridPosition(4,4),FacingDirection.Up);
            Read<PlayerView>(controls,"playerView").Apply(Read<PlayerState>(manager,"playerState"));
            controls.SetGameplayInput(true);
        }
        private static void Step()=>UnityEngine.Object.FindAnyObjectByType<TurnManager>().CompletePlayerAction(PlayerActionKind.Wait);

        public static string Runtime()
        {
            var manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var core=Read<CoreState>(manager,"coreState");var shields=Read<ShieldState>(manager,"shieldState");
            // Exercise actual spawning, not only the catalog; clear views between spawns.
            for(int stage=1;stage<=3;stage++)
            {
                ShowBoss(stage);manager.Boss.Finish();
                var seed=manager.Capture();Set(seed,"Run",new RunState());manager.Restore(seed);
                for(int s=1;s<stage;s++){manager.Run.CompleteBoss();SkipBossReward(manager.Run);}
                for(int index=0;index<30;index++)
                {
                    typeof(EnemyManager).GetMethod("SpawnNextEnemy",Fields).Invoke(manager,null);
                    var snapshot=manager.Capture();var states=Read<EnemyState[]>(snapshot,"States");
                    Need(states.Length==1,"one actual spawn");CheckEnemy(stage,index,states[0].Type,states[0].Traits);
                    Set(snapshot,"States",Array.Empty<EnemyState>());manager.Restore(snapshot);manager.Run.ResolvedEnemy();
                }
                Need(manager.Run.BossReady,"actual roster completes");
            }
            ShowBoss(1);GridPosition start=manager.Boss.Origin;Step();
            Need(manager.Boss.Origin.Equals(start),"charge warning rest");Step();
            Need(!manager.Boss.HasMissiles&&manager.Boss.Origin.Y==start.Y-1,"straight body advances instead of firing");
            var saved=manager.Capture();Step();Step();manager.Restore(saved);
            Need(manager.Boss.Origin.Y==start.Y-1,"charge undo snapshot");
            ShowBoss(1);int deadline=20;
            while(core.AliveCount==4&&deadline-->0)Step();
            Need(core.AliveCount<4,"unblocked straight charge damages cores");
            ShowBoss(1);GridPosition block=manager.Boss.CellAt(manager.Boss.NextCharge(),0);
            shields.TryAdd(block);int hp=manager.Boss.Health(0),side=manager.Boss.Side;Step();Step();
            Need(core.AliveCount==4&&!shields.Contains(block)&&manager.Boss.Health(0)==hp-1&&manager.Boss.Side!=side,"shield blocks charge and damages boss");
            ShowBoss(3);Step();Step();Need(manager.Boss.HasMissiles&&!manager.Boss.UsesResonance,"ordinary missiles first");
            ShowBoss(3);manager.Boss.EndVolley();Step();Step();Need(manager.Beams.Count>0,"resonance next volley");
            var beam=manager.Beams[0];GridPosition shield=beam.Origin.Offset(beam.Direction);shields.TryAdd(shield);
            int health=manager.Boss.Health(beam.BossCell);Step();Need(shields.Contains(shield),"two-turn beam warning");Step();
            Need(!shields.Contains(shield)&&manager.Boss.Health(beam.BossCell)==health-1,"beam block damages source");
            ShowBoss(2);manager.Boss.MoveCharge(manager.Boss.NextCharge());manager.Boss.MoveCharge(manager.Boss.NextCharge());
            Read<BossView>(manager,"bossView").Apply(false);
            var renderers=Read<MeshRenderer[]>(Read<BossView>(manager,"bossView"),"cellRenderers");
            Need(renderers.Count(v=>v.gameObject.activeSelf)==5,"diagonal boss stays visible");
            ShowBoss(1);
            return "PASS: actual 90-enemy rosters, straight charge/rest/core damage/shield block/undo, missile and resonance attacks, diagonal view";
        }
        public static string CounterCombat()
        {
            var manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var shields=Read<ShieldState>(manager,"shieldState");var core=Read<CoreState>(manager,"coreState");
            var view=Read<ShieldView>(manager,"shieldView");
            for(int stage=1;stage<=3;stage++)
            {
                ShowBoss(stage);
                for(int turn=0;turn<240&&manager.Boss.Active;turn++)
                {
                    shields.Restore(Array.Empty<GridPosition>());
                    foreach(var m in manager.Boss.Missiles)
                        if(m.Active&&!m.Reflected)shields.TryAdd(m.Position.Offset(m.Direction));
                    foreach(var b in manager.Beams)shields.TryAdd(b.Origin.Offset(b.Direction));
                    if(manager.Boss.UsesCharge)
                        for(int i=0;i<manager.Boss.CellCount;i++)if(manager.Boss.Health(i)>0)
                        {
                            GridPosition next=manager.Boss.CellAt(manager.Boss.NextCharge(),i);
                            if(!manager.Boss.Occupies(next)&&!core.Occupies(next)&&GameConfig.IsInsidePlayable(next))shields.TryAdd(next);
                        }
                    view.Apply(shields);Step();Need(core.AliveCount>0,"core survives counter combat");
                }
                Need(!manager.Boss.Active,"boss can be defeated: "+stage);
                if(stage<3){Need(manager.Run.Chapter==stage&&manager.Run.NeedsReward,"boss waits for augment");AcceptAugment(AugmentType.ShieldEnergy);Need(manager.Run.Chapter==stage+1&&!manager.Run.NeedsReward,"stage advances after selection");}
                else Need(manager.Run.Completed&&!manager.Run.CanSpawn,"third boss ends run");
            }
            return "PASS: all three bosses defeated using shields/reflection; third boss ends stage mode";
        }
        public static string Ending()
        {
            ShowBoss(3);var manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            for(int i=0;i<manager.Boss.CellCount;i++)while(manager.Boss.Health(i)>0)manager.Boss.TryDamage(manager.Boss.Cell(i));
            Step();Need(manager.Run.Completed&&!manager.Run.IsInfinite&&!manager.Run.CanSpawn,"stage completion");
            var flow=UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            Need(flow.State==GameFlowState.GameOver,"terminal screen");
            var title=Read<CanvasGroup>(flow,"gameOverGroup").GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Title");
            Need(title.text=="ALL STAGES CLEAR"&&!Read<TMPro.TMP_Text>(flow,"resultScore").gameObject.activeSelf,"clear title without score");
            return "PASS: third boss ends stage mode, clear screen without score";
        }
    }
}
