using System;
using System.Reflection;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class PortalGameplayChecks
    {
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
        private static T Read<T>(object o,string field)=>(T)o.GetType().GetField(field,Fields).GetValue(o);
        private static void Set(object o,string field,object value)=>o.GetType().GetField(field,Fields).SetValue(o,value);
        private static void Need(bool value,string text){if(!value)throw new Exception("PORTAL CHECK: "+text);}
        public static string Run()
        {
            var manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var controls=UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var player=Read<PlayerState>(manager,"playerState");var view=Read<PlayerView>(controls,"playerView");
            var energy=Read<PlayerEnergyState>(manager,"playerEnergyState");var shields=Read<ShieldState>(manager,"shieldState");
            var core=Read<CoreState>(manager,"coreState");var coreView=Read<CoreView>(manager,"coreView");
            var shieldView=Read<ShieldView>(manager,"shieldView");var score=Read<ScoreState>(manager,"scoreState");
            var turns=Read<TurnManager>(manager,"turnManager");
            void Reset(params EnemyState[] enemies)
            {
                core.Restore(new int[4]);coreView.Apply(core,false);shields.Restore(Array.Empty<GridPosition>());
                energy.Restore(4,4);turns.Restore(0);player.Restore(new GridPosition(7,7),FacingDirection.Right);view.Apply(player);
                var s=manager.Capture();Set(s,"States",enemies);Set(s,"Run",new RunState());Set(s,"Boss",new BossState());
                Set(s,"Beams",Array.Empty<ResonanceBeam>());Set(s,"Fragments",Array.Empty<GridPosition>());Set(s,"Pickups",Array.Empty<Pickup>());
                Set(s,"Augments",new AugmentCombatState());Set(s,"Portals",new PortalState());Set(s,"GameOver",false);Set(s,"TurnsUntilSpawn",1000);
                manager.Restore(s);controls.SetGameplayInput(true);controls.SetPresentationInputLock(false);
                controls.SetUndoHistory(new TurnUndoHistory(player,view,energy,core,coreView,shields,shieldView,score,turns,manager));
            }
            void Grant()
            {
                manager.Run.BeginBoss();manager.Run.CompleteBoss();
                Set(manager.Run,"rewardChoices",new[]{AugmentType.PortalKit,AugmentType.ShieldEnergy,AugmentType.LongerFreeze});
                int kits=manager.Portals.Kits;
                Need(manager.ChooseReward(AugmentType.PortalKit)&&manager.Portals.Kits==kits+1,"portal augment grants one pair");
                Need(!manager.ChooseReward(AugmentType.PortalKit)&&manager.Portals.Kits==kits+1,"cannot claim same reward twice");
            }
            void Move(Vector2Int direction)=>Need(controls.TryMove(direction),"move succeeds");

            Reset(new EnemyState(EnemyType.Straight,new GridPosition(18,18),Vector2Int.left,0));
            Need(!controls.TryDropPortal()&&turns.CurrentTurn==0,"no augment means no portal item");
            Grant();Need(manager.Run.PrimaryAugment==null&&manager.Portals.Kits==1,"portal is an item reward, not a permanent playstyle");
            Need(controls.TryDropPortal(),"E drops first endpoint");
            Need(manager.Portals.AwaitingSecond&&manager.Portals.Kits==0&&turns.CurrentTurn==1,"first placement spends kit and one turn");
            Need(controls.TryUndo()&&manager.Portals.Kits==1&&!manager.Portals.A.HasValue,"undo placement restores augment item");
            controls.TryDropPortal();int turn=turns.CurrentTurn;
            Need(!controls.TryDropPortal()&&turns.CurrentTurn==turn,"cannot place both endpoints on same cell");
            Move(Vector2Int.right);Need(controls.TryDropPortal()&&manager.Portals.Linked&&manager.Portals.RemainingUses==2,"linked pair starts with two shared uses");
            Need(player.Position.Equals(new GridPosition(8,7))&&energy.Current==4&&shields.Count==0,"placement neither teleports nor spends shield energy");
            Move(Vector2Int.up);turn=turns.CurrentTurn;Move(Vector2Int.down);
            Need(player.Position.Equals(new GridPosition(7,7))&&turns.CurrentTurn==turn+1&&manager.Portals.RemainingUses==1,"first trip consumes one use, without bounce");
            Need(controls.TryWait()&&player.Position.Equals(new GridPosition(7,7))&&manager.Portals.RemainingUses==1,"waiting does not consume a trip");
            Move(Vector2Int.up);Move(Vector2Int.down);
            Need(player.Position.Equals(new GridPosition(8,7))&&manager.Portals.RemainingUses==0&&!manager.Portals.A.HasValue&&!manager.Portals.B.HasValue,"reverse trip consumes last use and removes both endpoints");
            Need(controls.TryUndo()&&player.Position.Equals(new GridPosition(7,8))&&manager.Portals.Linked&&manager.Portals.RemainingUses==1,"undo last trip restores pair and remaining use");
            Move(Vector2Int.down);Need(!manager.Portals.Linked&&manager.Portals.RemainingUses==0,"last trip replays correctly");
            Move(Vector2Int.up);Move(Vector2Int.left);Move(Vector2Int.down);
            Need(player.Position.Equals(new GridPosition(7,7)),"expired endpoint does not teleport a third time");
            turn=turns.CurrentTurn;Need(!controls.TryDropPortal()&&turns.CurrentTurn==turn,"spent item cannot create another pair");

            Reset();Grant();controls.TryDropPortal();Move(Vector2Int.right);controls.TryDropPortal();
            Move(Vector2Int.up);Move(Vector2Int.down);Need(manager.Portals.RemainingUses==1,"replacement fixture has one use left");
            Grant();Need(controls.TryDropPortal()&&manager.Portals.AwaitingSecond&&manager.Portals.RemainingUses==0,"new augment item replaces previous pair");
            Need(controls.TryUndo()&&manager.Portals.Linked&&manager.Portals.RemainingUses==1&&manager.Portals.Kits==1,"replacement undo preserves old use count and inventory");

            Reset();Grant();player.Restore(core.GetCell(0),FacingDirection.Up);
            Need(!controls.TryDropPortal()&&manager.Portals.Kits==1,"invalid placement does not consume augment item");
            manager.Portals.Drop(new GridPosition(7,7));manager.Portals.Drop(core.GetCell(0));
            player.Restore(new GridPosition(7,8),FacingDirection.Down);Move(Vector2Int.down);
            Need(player.Position.Equals(new GridPosition(7,7))&&manager.Portals.RemainingUses==2,"blocked exit never consumes a use");

            Reset(new EnemyState(EnemyType.Straight,new GridPosition(9,7),Vector2Int.left,0));
            Set(manager.Run,"<NextDrop>k__BackingField",3);
            shields.TryAdd(new GridPosition(8,7));energy.TrySpend();shieldView.Apply(shields);
            int seed=0;for(;seed<100;seed++){UnityEngine.Random.InitState(seed);if(UnityEngine.Random.Range(0,100)<33)break;}
            UnityEngine.Random.InitState(seed);controls.TryWait();
            Need(manager.Pickups.Items.Count==1&&manager.Pickups.Items[0].Type==PickupType.TimeFreeze&&manager.Portals.Kits==0,"ordinary enemy drops never grant portals");
            RunState cycle=new();for(int i=0;i<12;i++)Need(cycle.TakeNextDrop()==(PickupType)(i%3),"only three basic pickups rotate");
            Need(Enum.GetValues(typeof(PickupType)).Length==3,"portal removed from basic pickup enum");
            Need(UnityEngine.Object.FindAnyObjectByType<PortalView>().GetComponentsInChildren<TMPro.TextMeshPro>(true).Length==0,"no world-space endpoint letters exist");
            return "PASS: augment-only item, shared two-trip limit/removal, both directions/no bounce, wait/blocked exit, placement/replacement/travel undo, no basic portal drops or endpoint labels";
        }
        public static string RewardCard()
        {
            StageThemeChecks.ShowBoss(1);
            var manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var flow=UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            for(int i=0;i<manager.Boss.CellCount;i++)while(manager.Boss.Health(i)>0)manager.Boss.TryDamage(manager.Boss.Cell(i));
            UnityEngine.Object.FindAnyObjectByType<TurnManager>().CompletePlayerAction();
            Set(manager.Run,"rewardChoices",new[]{AugmentType.PortalKit,AugmentType.ShieldEnergy,AugmentType.LongerFreeze});
            typeof(GameFlowController).GetMethod("ConfigureAugmentChoices",Fields).Invoke(flow,null);
            var button=Read<UnityEngine.UI.Button>(flow,"freezeReward");
            Need(button.transform.Find("Label").GetComponent<TMPro.TMP_Text>().text==AugmentCatalog.Name(AugmentType.PortalKit),"portal card has correct title");
            int before=manager.Portals.Kits;button.onClick.Invoke();
            Need(manager.Portals.Kits==before+1&&flow.State==GameFlowState.Playing&&manager.Run.PrimaryAugment==null,"actual portal card grants item and resumes stage");
            return "PASS: portal augment card grants one usable item";
        }
    }
}