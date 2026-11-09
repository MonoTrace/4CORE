using System;
using UnityEngine;

namespace FourCore
{
    public sealed class BossProjectile
    {
        public GridPosition Position;
        public Vector2Int Direction;
        public bool Active, Reflected;
        public int SourceCell;
        public BossProjectile Copy() => (BossProjectile)MemberwiseClone();
    }

    public sealed class BossState
    {
        private readonly int[] health = { 2, 2, 2, 2, 0 };
        public readonly BossProjectile[] Missiles = { new(), new(), new(), new() };
        public bool IsCross { get; private set; }
        public int StageNumber { get; private set; } = 1;
        public StageTheme Theme => StageCatalog.Theme(StageNumber);
        public int Rotation { get; private set; }
        public int MoveCount { get; private set; }
        public int VolleyCount { get; private set; }
        public int PendingSplits { get; private set; }
        public bool WaitingForPulse { get; private set; }
        public bool BodyVisible => true;
        public bool UsesResonance => Theme==StageTheme.Artillery && VolleyCount%2==1;
        public bool UsesStraightCharge => Theme==StageTheme.Assault;
        public bool UsesDiagonalCharge => StageNumber == 2;
        public bool UsesCharge => UsesStraightCharge || UsesDiagonalCharge;
        public void BeginPulseWait()=>WaitingForPulse=true;
        public void EndPulseWait()=>WaitingForPulse=false;
        public void EndVolley()
        {
            VolleyCount++;
        }
        public void Relocate(int side)=>Teleport(side);
        public int CellCount => IsCross ? 5 : 4;
        public int PatternStep { get; private set; }
        public bool Active { get; private set; }
        public GridPosition Origin { get; private set; }
        public int Side { get; private set; }
        public int RestTurns { get; private set; }
        public int TeleportSerial { get; private set; }
        public GridPosition LastDestroyedCell { get; private set; }
        public int Health(int index) => health[index];
        public bool Defeated => Array.TrueForAll(health, hp => hp <= 0);
        public bool HasMissiles => Array.Exists(Missiles, m => m.Active);
        public GridPosition Cell(int index) => CellAt(Origin,index);
        public GridPosition CellAt(GridPosition origin,int index)
        {
            Vector2Int offset = !IsCross?new Vector2Int(index%2,index/2):index switch { 0 => new Vector2Int(1,1), 1 => new Vector2Int(0,1),
                2 => new Vector2Int(2,1), 3 => new Vector2Int(1,0), _ => new Vector2Int(1,2) };
            for(int turn=0;turn<Rotation;turn++)offset=new Vector2Int((IsCross?2:1)-offset.y,offset.x);
            return origin.Offset(offset);
        }
        public GridPosition NextCharge()
        {
            if (UsesStraightCharge)
                return Origin.Offset(Side switch { 0=>Vector2Int.down,1=>Vector2Int.left,2=>Vector2Int.up,_=>Vector2Int.right });
            return Origin.Offset(Side switch { 0 => new Vector2Int(-1,-1), 1 => new Vector2Int(-1,1),
                2 => new Vector2Int(1,1), _ => new Vector2Int(1,-1) });
        }
        public void MoveCharge(GridPosition destination) { Origin=destination;PatternStep++;MoveCount++;RestTurns=1; }
        public bool Occupies(GridPosition position)
        {
            if (!Active) return false;
            for (int i = 0; i < CellCount; i++) if (health[i] > 0 && Cell(i).Equals(position)) return true;
            return false;
        }
        public static GridPosition Site(int side,bool cross=false) => cross ? side switch
        {
            0 => new GridPosition(GameConfig.CoreMax+4,GameConfig.CoreMax+4),
            1 => new GridPosition(GameConfig.CoreMax+4,GameConfig.CoreMin-6),
            2 => new GridPosition(GameConfig.CoreMin-6,GameConfig.CoreMin-6),
            _ => new GridPosition(GameConfig.CoreMin-6,GameConfig.CoreMax+4)
        } : side switch
        {
            0 => new GridPosition(GameConfig.CoreMin, GameConfig.CoreMax + 5),
            1 => new GridPosition(GameConfig.CoreMax + 5, GameConfig.CoreMin),
            2 => new GridPosition(GameConfig.CoreMin, GameConfig.CoreMin - 6),
            _ => new GridPosition(GameConfig.CoreMin - 6, GameConfig.CoreMin)
        };
        public void Begin(int side, int chapter)
        {
            Active = true;
            StageNumber=Mathf.Clamp(chapter,1,StageCatalog.Count);
            IsCross=StageNumber==2;PatternStep=0;
            Rotation=MoveCount=VolleyCount=PendingSplits=0;WaitingForPulse=false;
            for (int i = 0; i < health.Length; i++) health[i] = i<CellCount ? StageNumber == 1 ? 2 : 3 : 0;
            Teleport(side);
            LastDestroyedCell = Origin;
        }
        public void Teleport(int side)
        {
            Side = side; Origin = Site(side,IsCross); RestTurns = 1; TeleportSerial++;PatternStep=0;
            foreach (BossProjectile missile in Missiles) missile.Active = false;
        }
        public bool ConsumeRest()
        {
            if (RestTurns <= 0) return false;
            RestTurns--; return true;
        }
        public bool TryDamage(GridPosition position)
        {
            for (int i = 0; i < CellCount; i++)
                if (health[i] > 0 && Cell(i).Equals(position))
                {
                    health[i]--;
                    if (health[i] == 0) LastDestroyedCell = position;
                    return true;
                }
            return false;
        }
        public GridPosition? TakeSplit()
        {
            for(int i=0;i<CellCount;i++)if((PendingSplits&(1<<i))!=0)
            {PendingSplits&=~(1<<i);return Cell(i);}
            return null;
        }
        public void Finish() { Active = false;WaitingForPulse=false; foreach (BossProjectile m in Missiles) m.Active = false; }
        public void Fire(CoreState core)
        {
            foreach(BossProjectile m in Missiles)m.Active=false;
            if(UsesCharge)return;
            Vector2Int direction = Side switch { 0 => Vector2Int.down, 1 => Vector2Int.left,
                2 => Vector2Int.up, _ => Vector2Int.right };
            for (int lane = 0; lane < 2; lane++)
            {
                BossProjectile missile = Missiles[lane]; missile.Active = false;
                int source=-1,best=int.MaxValue;
                for(int i=0;i<CellCount;i++)
                {
                    GridPosition candidate=Cell(i);
                    if(health[i]<=0 || (direction.x==0?candidate.X!=Origin.X+lane:candidate.Y!=Origin.Y+lane))continue;
                    int distance=Mathf.Abs(candidate.X-GameConfig.CoreMin)+Mathf.Abs(candidate.Y-GameConfig.CoreMin);
                    if(distance<best){best=distance;source=i;}
                }
                if (source < 0) continue;
                GridPosition p = Cell(source);
                bool target = false;
                for (int step = 0; step < 10; step++)
                {
                    p = p.Offset(direction);
                    if (core.Occupies(p)) { target = true; break; }
                }
                if (!target) continue;
                missile.Position = Cell(source); missile.Direction = direction;
                missile.Active = true; missile.Reflected = false; missile.SourceCell = source;
            }
        }
        public BossState Copy()
        {
            BossState result = new() { Active = Active, Origin = Origin, Side = Side,
                RestTurns = RestTurns, TeleportSerial = TeleportSerial,IsCross=IsCross,PatternStep=PatternStep,
                StageNumber=StageNumber,Rotation=Rotation,MoveCount=MoveCount,VolleyCount=VolleyCount,
                PendingSplits=PendingSplits,WaitingForPulse=WaitingForPulse,LastDestroyedCell=LastDestroyedCell };
            Array.Copy(health, result.health, health.Length);
            for (int i = 0; i < Missiles.Length; i++) result.Missiles[i] = Missiles[i].Copy();
            return result;
        }
        public void Restore(BossState saved)
        {
            Active = saved.Active; Origin = saved.Origin; Side = saved.Side;
            RestTurns = saved.RestTurns; TeleportSerial = saved.TeleportSerial;
            IsCross=saved.IsCross;PatternStep=saved.PatternStep;
            StageNumber=saved.StageNumber;Rotation=saved.Rotation;MoveCount=saved.MoveCount;
            LastDestroyedCell=saved.LastDestroyedCell;
            VolleyCount=saved.VolleyCount;PendingSplits=saved.PendingSplits;WaitingForPulse=saved.WaitingForPulse;
            Array.Copy(saved.health, health, health.Length);
            for (int i = 0; i < Missiles.Length; i++) Missiles[i] = saved.Missiles[i].Copy();
        }
    }
}
