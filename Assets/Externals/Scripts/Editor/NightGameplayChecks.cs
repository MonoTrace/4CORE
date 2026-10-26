using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class NightGameplayChecks
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Read<T>(object owner,string field) => (T)owner.GetType().GetField(field,Fields).GetValue(owner);
        private static void Set(object owner,string field,object value) => owner.GetType().GetField(field,Fields).SetValue(owner,value);
        private static void Require(bool condition,string description)
        { if(!condition)throw new InvalidOperationException("NIGHT CHECK: "+description); }

        public static string Run()
        {
            EnemyManager manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            PlayerController controls=UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            CoreState core=Read<CoreState>(manager,"coreState");CoreView coreView=Read<CoreView>(manager,"coreView");
            PlayerState player=Read<PlayerState>(manager,"playerState");PlayerView playerView=Read<PlayerView>(controls,"playerView");
            PlayerEnergyState energy=Read<PlayerEnergyState>(manager,"playerEnergyState");
            ShieldState shields=Read<ShieldState>(manager,"shieldState");ShieldView shieldView=Read<ShieldView>(manager,"shieldView");
            ScoreState score=Read<ScoreState>(manager,"scoreState");TurnManager turns=Read<TurnManager>(manager,"turnManager");
            void Reset(params EnemyState[] states)
            {
                core.Restore(new int[4]);coreView.Apply(core,false);shields.Restore(Array.Empty<GridPosition>());shieldView.Apply(shields);
                energy.Restore(4,4);score.SetEnabled(true);score.Restore(0);turns.Restore(0);player.Restore(new GridPosition(6,6),FacingDirection.Up);playerView.Apply(player);
                EnemyManager.Snapshot snap=manager.Capture();Set(snap,"States",states);Set(snap,"Run",new RunState());
                Set(snap,"Pickups",Array.Empty<Pickup>());Set(snap,"Boss",new BossState());Set(snap,"GameOver",false);Set(snap,"TurnsUntilSpawn",1000);
                manager.Restore(snap);controls.SetUndoHistory(new TurnUndoHistory(player,playerView,energy,core,coreView,shields,shieldView,score,turns,manager));
                controls.SetGameplayInput(true);
            }
            void Step() => turns.CompletePlayerAction();
            EnemyState Live()=>Read<EnemyState[]>(manager.Capture(),"States")[0];

            Reset(new EnemyState(EnemyType.Straight,new GridPosition(11,16),Vector2Int.down,0));
            core.TryDamage(core.GetCell(0),0,"Freeze test");coreView.Apply(core,false);energy.SetMaximumFromCores(3);
            manager.Pickups.TryAdd(new GridPosition(7,6),PickupType.TimeFreeze);
            Require(controls.TryMove(Vector2Int.right),"pickup movement");
            Require(manager.Run.FreezeMoves==10 && Live().Position.Equals(new GridPosition(11,16)) && core.GetRecoveryTurns(0)==90,"freeze starts before enemy tick");
            for(int i=0;i<10;i++)Require(controls.TryMove(i%2==0?Vector2Int.right:Vector2Int.left),"frozen movement");
            Require(manager.Run.FreezeMoves==0 && Live().Position.Equals(new GridPosition(11,16)),"ten free moves");
            Require(controls.TryMove(Vector2Int.right),"resume move");
            Require(Live().Position.Equals(new GridPosition(11,15)) && core.GetRecoveryTurns(0)==89,"resume enemy/recovery");
            Require(controls.TryUndo() && Live().Position.Equals(new GridPosition(11,16)) && core.GetRecoveryTurns(0)==90,"undo resumed turn");
            Require(controls.TryUndo() && manager.Run.FreezeMoves==1,"undo restores remaining freeze moves");

            Reset();manager.Run.Activate(PickupType.PersonalShield);manager.Run.Activate(PickupType.PersonalShield);
            Require(manager.Run.AbsorbHit() && !manager.Run.AbsorbHit(),"personal shields never stack");
            Reset();core.TryDamage(core.GetCell(0),0,"Repair test");coreView.Apply(core,false);energy.Restore(1,3);
            manager.Pickups.TryAdd(new GridPosition(7,6),PickupType.Repair);controls.TryMove(Vector2Int.right);
            Require(core.AliveCount==4 && energy.Total==4 && energy.Current==2,"repair restores one core and one energy");
            Require(controls.TryUndo() && core.AliveCount==3 && energy.Current==1 && manager.Pickups.Items.Count==1,"repair undo restores consumed item");

            Reset();for(int i=0;i<30;i++){manager.Run.SpawnedEnemy();manager.Run.ResolvedEnemy();}Step();
            Require(manager.Run.Phase==EncounterPhase.Boss && manager.Boss.Active && manager.ActiveEnemyCount==0,"exclusive boss transition");
            BossState boss=manager.Boss;Step();Require(!boss.HasMissiles&&boss.MoveCount==0,"boss rest turn");
            for(int cycle=0;cycle<120 && boss.Active;cycle++)
            {
                shields.Restore(Array.Empty<GridPosition>());
                for(int i=0;i<boss.CellCount;i++)if(boss.Health(i)>0)
                {
                    GridPosition next=boss.CellAt(boss.NextCharge(),i);
                    if(!boss.Occupies(next)&&!core.Occupies(next))shields.TryAdd(next);
                }
                shieldView.Apply(shields);Step();
            }
            Require(!boss.Active && manager.Run.Chapter==2 && score.Total==800,"boss destruction and core multiplier");
            Require(!manager.Run.NeedsReward && controls.GameplayInputEnabled,"next stage without consumable reward");
            Require(core.AliveCount==4,"shield stops straight boss before cores");
            EnemyManager.Snapshot chapter2=manager.Capture();manager.Run.CompleteBoss();manager.Restore(chapter2);
            Require(manager.Run.Chapter==2,"chapter snapshot");
            manager.Run.CompleteBoss();Require(manager.Run.Chapter==3&&!manager.Run.IsInfinite&&manager.Run.CanSpawn,"third stage stays in stage mode");
            manager.Run.ConfigureMode(GameMode.Infinite);Require(manager.Run.IsInfinite&&manager.Run.CanSpawn&&!manager.Run.BossReady,"separate infinite mode");

            BossState sparse=new();sparse.Begin(0,3);
            while(sparse.Health(0)>0)sparse.TryDamage(sparse.Cell(0));
            while(sparse.Health(2)>0)sparse.TryDamage(sparse.Cell(2));sparse.Fire(core);
            Require(!sparse.Missiles[0].Active && sparse.Missiles[1].Active,"dead boss lane cannot fire");
            Reset();
            return "PASS: time freeze 10 moves, recovery halt/resume, undo freeze/items/core/energy, non-stacking shield, boss-only transition/rest/fire/reflection/HP/score, chapter/infinite snapshots, destroyed boss lane";
        }

        public static string CrossBoss()
        {
            EnemyManager manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            CoreState core=Read<CoreState>(manager,"coreState");CoreView coreView=Read<CoreView>(manager,"coreView");
            ShieldState shields=Read<ShieldState>(manager,"shieldState");ShieldView shieldView=Read<ShieldView>(manager,"shieldView");
            PlayerState player=Read<PlayerState>(manager,"playerState");TurnManager turns=Read<TurnManager>(manager,"turnManager");
            core.Restore(new int[4]);coreView.Apply(core,false);shields.Restore(Array.Empty<GridPosition>());
            player.Restore(new GridPosition(8,8),FacingDirection.Up);
            EnemyManager.Snapshot snap=manager.Capture();Set(snap,"States",Array.Empty<EnemyState>());Set(snap,"Run",new RunState());Set(snap,"TurnsUntilSpawn",1000);manager.Restore(snap);
            manager.Run.CompleteBoss();manager.Run.AcceptReward();manager.Run.BeginBoss();manager.Boss.Begin(0,2);
            BossState boss=manager.Boss;
            Require(boss.IsCross&&boss.CellCount==5,"five-cell cross boss");
            for(int i=0;i<5;i++)Require(boss.Health(i)==3,"cross boss HP3");
            turns.CompletePlayerAction();turns.CompletePlayerAction();
            int active=0;foreach(BossProjectile m in boss.Missiles)
            {
                if(!m.Active)continue;active++;
                Require(Mathf.Abs(m.Direction.x)==1&&Mathf.Abs(m.Direction.y)==1,"diagonal shots");
                shields.TryAdd(m.Position.Offset(m.Direction*2));
            }
            Require(active>=2,"cross boss attacks more than one core");shieldView.Apply(shields);
            turns.CompletePlayerAction();turns.CompletePlayerAction();turns.CompletePlayerAction();
            GridPosition next=boss.NextCharge();GridPosition blockedCell=boss.CellAt(next,1);
            shields.TryAdd(blockedCell);shieldView.Apply(shields);
            int health=boss.Health(1);turns.CompletePlayerAction();
            Require(!shields.Contains(blockedCell)&&boss.Side!=0&&boss.Health(1)<health,"shield stops charge and damages boss");
            Require(core.AliveCount==4,"cross reflected missiles protect core");
            BossState saved=boss.Copy();boss.TryDamage(boss.Cell(0));boss.Restore(saved);
            Require(boss.Health(0)==saved.Health(0)&&boss.PatternStep==saved.PatternStep,"cross snapshot");
            int cycles=0;
            while(boss.Active&&cycles++<35)
            {
                if(!boss.HasMissiles){turns.CompletePlayerAction();turns.CompletePlayerAction();}
                foreach(BossProjectile m in boss.Missiles)if(m.Active&&!m.Reflected)shields.TryAdd(m.Position.Offset(m.Direction*2));
                shieldView.Apply(shields);int deadline=24;
                while(boss.HasMissiles&&deadline-->0)turns.CompletePlayerAction();
                Require(deadline>0,"cross missiles resolve");
            }
            Require(!boss.Active&&manager.Run.Chapter==3&&!manager.Run.IsInfinite&&!manager.Run.NeedsReward,"phase boss advances to refraction stage");
            return "PASS: cross shape, five HP3 cells, diagonal volley, shield stops charge, reflected player safety, snapshots, full defeat and stage3 progression";
        }

        private static string Fingerprint(EnemyManager manager,PlayerController controls)
        {
            EnemyManager.Snapshot snapshot=manager.Capture();RunState r=manager.Run;BossState b=manager.Boss;
            PlayerState p=Read<PlayerState>(manager,"playerState");PlayerEnergyState energy=Read<PlayerEnergyState>(manager,"playerEnergyState");
            CoreState core=Read<CoreState>(manager,"coreState");ShieldState shields=Read<ShieldState>(manager,"shieldState");
            StringBuilder s=new();s.Append($"{p.Position}/{p.Facing}/{controls.CurrentTurn}/{energy.Current}/{energy.Total}/");
            s.Append($"{r.Chapter}/{r.Spawned}/{r.Resolved}/{r.FreezeMoves}/{r.PersonalShield}/{r.NextDrop}/{r.Phase}/{r.NeedsReward}/");
            s.Append($"{Read<int>(snapshot,"SpawnIndex")}/{Read<int>(snapshot,"TurnsUntilSpawn")}/");
            foreach(int recovery in core.Capture())s.Append(recovery+",");
            System.Collections.Generic.List<string> shieldPositions=new();foreach(GridPosition cell in shields.Positions)shieldPositions.Add(cell.ToString());
            shieldPositions.Sort(StringComparer.Ordinal);s.Append(string.Join(",",shieldPositions));
            foreach(EnemyState e in Read<EnemyState[]>(snapshot,"States"))
                s.Append($"{e.Type}/{e.Position}/{e.Direction}/{e.EntryDelay}/{e.Life}/{e.Armored}/{e.MissileActive}/{e.MissileReflected}/{e.MissilePosition}/{e.MissileDirection}/{e.HasFired};");
            foreach(Pickup item in manager.Pickups.Items)s.Append($"{item.Type}/{item.Position};");
            s.Append($"{b.Active}/{b.Origin}/{b.Side}/{b.RestTurns}/{b.PatternStep}/{b.IsCross}/");
            for(int i=0;i<5;i++)s.Append(b.Health(i)+",");
            foreach(BossProjectile m in b.Missiles)s.Append($"{m.Active}/{m.Position}/{m.Direction}/{m.Reflected}/{m.SourceCell};");
            s.Append(Read<ScoreState>(manager,"scoreState").Total);s.Append(JsonUtility.ToJson(UnityEngine.Random.state));return s.ToString();
        }

        public static string ReplayAndArmor()
        {
            EnemyManager manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();PlayerController controls=UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            PlayerState player=Read<PlayerState>(manager,"playerState");PlayerView view=Read<PlayerView>(controls,"playerView");
            PlayerEnergyState energy=Read<PlayerEnergyState>(manager,"playerEnergyState");CoreState core=Read<CoreState>(manager,"coreState");CoreView coreView=Read<CoreView>(manager,"coreView");
            ShieldState shields=Read<ShieldState>(manager,"shieldState");ShieldView shieldView=Read<ShieldView>(manager,"shieldView");
            ScoreState score=Read<ScoreState>(manager,"scoreState");TurnManager turns=Read<TurnManager>(manager,"turnManager");
            void Reset(EnemyState enemy,int countdown)
            {
                core.Restore(new int[4]);coreView.Apply(core,false);shields.Restore(Array.Empty<GridPosition>());shieldView.Apply(shields);
                energy.Restore(4,4);score.Restore(0);turns.Restore(0);player.Restore(new GridPosition(6,6),FacingDirection.Up);view.Apply(player);
                EnemyManager.Snapshot snap=manager.Capture();Set(snap,"States",new[]{enemy});Set(snap,"Run",new RunState());Set(snap,"Pickups",Array.Empty<Pickup>());
                Set(snap,"Boss",new BossState());Set(snap,"GameOver",false);Set(snap,"TurnsUntilSpawn",countdown);manager.Restore(snap);
                controls.SetUndoHistory(new TurnUndoHistory(player,view,energy,core,coreView,shields,shieldView,score,turns,manager));controls.SetGameplayInput(true);
            }
            Reset(new EnemyState(EnemyType.Straight,new GridPosition(7,6),Vector2Int.left,0),1);
            UnityEngine.Random.InitState(144);Require(controls.TryToggleShield(),"pre-move shield action");string before=Fingerprint(manager,controls);
            Require(controls.TryMove(Vector2Int.up),"recorded move");string after=Fingerprint(manager,controls);
            Require(controls.TryUndo()&&Fingerprint(manager,controls)==before,"full-state undo before drop/spawn");
            Require(controls.TryMove(Vector2Int.up)&&Fingerprint(manager,controls)==after,"deterministic drop and spawn replay");

            Reset(new EnemyState(EnemyType.Straight,new GridPosition(8,6),Vector2Int.left,0).WithArmor(),1000);
            GridPosition shield=new(7,6);shields.TryAdd(shield);turns.CompletePlayerAction();
            EnemyState state=Read<EnemyState[]>(manager.Capture(),"States")[0];
            Require(state.Life==1&&state.BaseScore==20&&manager.ActiveEnemyCount==1,"armor absorbs first shield, score remains double");
            shields.TryAdd(shield);turns.CompletePlayerAction();
            Require(manager.ActiveEnemyCount==0&&score.Total==80,"second shield kills armor and awards double score");
            BossState boss=new();boss.Begin(1,2);BossState copy=boss.Copy();boss.TryDamage(boss.Cell(0));
            Require(copy.Health(0)==3&&boss.Health(0)==2,"boss snapshots deep copy health");
            return "PASS: full state undo, deterministic drops/spawns, shield reservation, HP2 armor and double score, boss deep-copy isolation";
        }
    }
}
