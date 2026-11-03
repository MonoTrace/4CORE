using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FourCore.EditorTools
{
    // Run in a disposable Play Mode session after the title transition.
    public static class AugmentGameplayChecks
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Read<T>(object owner, string name) => (T)owner.GetType().GetField(name, Fields).GetValue(owner);
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Fields).SetValue(owner, value);
        private static void Need(bool value, string label) { if (!value) throw new Exception("PLAYSTYLE CHECK: " + label); }

        public static string Run()
        {
            var manager = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var controls = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var player = Read<PlayerState>(manager,"playerState");
            var view = Read<PlayerView>(controls,"playerView");
            var energy = Read<PlayerEnergyState>(manager,"playerEnergyState");
            var shields = Read<ShieldState>(manager,"shieldState");
            var shieldView = Read<ShieldView>(manager,"shieldView");
            var core = Read<CoreState>(manager,"coreState");
            var coreView = Read<CoreView>(manager,"coreView");
            var turns = Read<TurnManager>(manager,"turnManager");
            var score = Read<ScoreState>(manager,"scoreState");
            EnemyState[] Live() => Read<EnemyState[]>(manager.Capture(),"States");
            void Reset(AugmentType type, params EnemyState[] enemies)
            {
                core.Restore(new int[4]); coreView.Apply(core,false);
                shields.Restore(Array.Empty<GridPosition>()); energy.Restore(4,4); turns.Restore(0);
                player.Restore(new GridPosition(7,7),FacingDirection.Right); view.Apply(player);
                score.SetEnabled(true); score.Restore(0);
                RunState run = new(); run.CompleteBoss();
                Set(run,"rewardChoices",new[] { type,AugmentType.ShieldEnergy,AugmentType.LongerFreeze });
                Need(run.AcceptReward(type),"fixture accepts primary");
                var snap = manager.Capture(); Set(snap,"States",enemies); Set(snap,"Run",run);
                Set(snap,"Boss",new BossState()); Set(snap,"Beams",Array.Empty<ResonanceBeam>());
                Set(snap,"Fragments",Array.Empty<GridPosition>()); Set(snap,"Pickups",Array.Empty<Pickup>());
                Set(snap,"GameOver",false); Set(snap,"TurnsUntilSpawn",1000); Set(snap,"Augments",new AugmentCombatState());
                manager.Restore(snap); controls.SetGameplayInput(true); controls.SetPresentationInputLock(false);
                controls.SetUndoHistory(new TurnUndoHistory(player,view,energy,core,coreView,shields,shieldView,score,turns,manager));
            }
            void Place(int x, int y)
            {
                GridPosition p = new(x,y);
                Need(shields.TryAdd(p) && energy.TrySpend(),"fixture deploys shield"); manager.ShieldPlaced(p);
            }
            EnemyState Enemy(int x,int y,Vector2Int d,int delay=0) => new(EnemyType.Straight,new GridPosition(x,y),d,delay);
            void Wait() => Need(controls.TryWait(),"wait advances enemy turn");

            Reset(AugmentType.ChainBarrier,Enemy(9,7,Vector2Int.left),Enemy(8,8,Vector2Int.up),Enemy(15,15,Vector2Int.left));
            Place(7,7); Place(8,7); Place(10,10); Wait();
            Need(manager.ActiveEnemyCount==1&&shields.Count==1&&shields.Contains(new GridPosition(10,10))&&energy.Current==3,"connected chain blasts nearby enemies and preserves disconnected shield");
            Need(controls.TryUndo()&&manager.ActiveEnemyCount==3&&shields.Count==3,"chain undo restores complete network");

            Reset(AugmentType.TimedBarrier,Enemy(8,8,Vector2Int.up,10),Enemy(15,15,Vector2Int.left,10));
            controls.TryToggleShield(); Wait(); Need(manager.Augments.Bombs[new GridPosition(7,7)]==2,"bomb countdown first turn");
            Wait(); Need(manager.ActiveEnemyCount==2,"bomb does not fire early"); Wait();
            Need(manager.ActiveEnemyCount==1&&shields.Count==0&&energy.Current==4,"bomb explodes on third turn and refunds");
            Need(controls.TryUndo()&&manager.Augments.Bombs[new GridPosition(7,7)]==1&&manager.ActiveEnemyCount==2,"undo restores bomb countdown and blast victims");
            Wait(); Need(manager.ActiveEnemyCount==1,"bomb replay");
            Reset(AugmentType.TimedBarrier); controls.TryToggleShield(); manager.Run.Activate(PickupType.TimeFreeze,false);
            controls.TryMove(Vector2Int.up); Need(manager.Augments.Bombs[new GridPosition(7,7)]==3,"time freeze pauses bomb countdown");

            Reset(AugmentType.TimedBarrier); manager.Run.BeginBoss(); manager.Boss.Begin(0,2);
            typeof(BossState).GetProperty("RestTurns").SetValue(manager.Boss,100);
            Place(16,16); Wait(); Wait(); Wait();
            Need(manager.Boss.Health(0)==2&&manager.Boss.Health(2)==3&&core.AliveCount==4,"timed area damage hits nearby boss cells without friendly damage");
            var seen = new System.Collections.Generic.HashSet<AugmentType>();
            UnityEngine.Random.InitState(144);
            for (int i = 0; i < 128; i++)
            {
                RunState run = new(); run.BeginBoss(); run.CompleteBoss();
                Need(run.RewardChoices.Count==3&&run.RewardChoices.Distinct().Count()==3,"three unique offers from remaining six augments");
                foreach(var choice in run.RewardChoices) seen.Add(choice);
                Need(!run.AcceptReward((AugmentType)999),"unoffered augment rejected");
                var copy = run.Copy(); run.Restore(copy);
                Need(run.RewardChoices.SequenceEqual(copy.RewardChoices),"offers survive snapshot without reroll");
                AugmentType chosen = run.RewardChoices[0];
                Need(run.AcceptReward(chosen)&&!run.AcceptReward(chosen),"one reward per boss");
                if(AugmentCatalog.IsPlaystyle(chosen))
                {
                    run.BeginBoss();run.CompleteBoss();
                    Need(run.RewardChoices.All(t=>!AugmentCatalog.IsPlaystyle(t)),"later rewards support selected playstyle");
                }
            }
            Need(seen.Count==6&&Enum.GetValues(typeof(AugmentType)).Length==6,"only the six current augments exist");
            return "PASS: chain network, 3-turn bombs, freeze, boss area damage, energy/undo, remaining six-augment pool and saved offers";
        }

        public static string Cards()
        {
            StageThemeChecks.ShowBoss(1);
            var manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var flow=UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            for(int i=0;i<manager.Boss.CellCount;i++)while(manager.Boss.Health(i)>0)manager.Boss.TryDamage(manager.Boss.Cell(i));
            UnityEngine.Object.FindAnyObjectByType<TurnManager>().CompletePlayerAction();
            Need(flow.State==GameFlowState.Reward&&manager.Run.RewardChoices.Count==3,"real boss creates random choice screen");
            string[] fields={"freezeReward","shieldReward","repairReward"};
            for(int i=0;i<3;i++)
            {
                var button=Read<UnityEngine.UI.Button>(flow,fields[i]);
                Need(button.transform.Find("Label").GetComponent<TMPro.TMP_Text>().text==AugmentCatalog.Name(manager.Run.RewardChoices[i]),"card title matches offer slot");
                Need(button.GetComponentInChildren<AugmentCardArtwork>(true)!=null&&button.interactable,"card art and interaction ready");
            }
            AugmentType chosen=manager.Run.RewardChoices[1];
            Read<UnityEngine.UI.Button>(flow,fields[1]).onClick.Invoke();
            Need(manager.Run.Chapter==2&&flow.State==GameFlowState.Playing&&(!AugmentCatalog.IsPlaystyle(chosen)||manager.Run.PrimaryAugment==chosen),"middle card grants its actual offer");
            return "PASS: random offer card labels/art and actual selected-slot behavior";
        }
    }
}
