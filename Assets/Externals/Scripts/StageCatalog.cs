using UnityEngine;

namespace FourCore
{
    [System.Flags]
    public enum EnemyTraits { None=0, Phase=1, Split=2, Resonance=4 }
    public enum StageTheme { Assault=1, Phase, Artillery }

    public static class StageCatalog
    {
        public const int Count=3;
        public static StageTheme Theme(int stage)=>(StageTheme)Mathf.Clamp(stage,1,Count);
        public static string Name(int stage)=>Theme(stage) switch
        {
            StageTheme.Assault=>"ASSAULT",StageTheme.Phase=>"PHASE",_=>"ARTILLERY"
        };
        public static Color Color(int stage)=>Theme(stage) switch
        {
            StageTheme.Assault=>new Color(1f,.2f,.55f),StageTheme.Phase=>new Color(.7f,.4f,1f),
            _=>new Color(1f,.5f,.15f)
        };
        public static EnemyType ChooseEnemy(int stage,int spawned)
        {
            return Theme(stage) switch
            {
                StageTheme.Assault=>EnemyType.Straight,
                StageTheme.Phase=>spawned%2==0?EnemyType.Diagonal:EnemyType.Zigzag,
                _=>EnemyType.Missile
            };
        }
        public static EnemyTraits Traits(int stage,EnemyType type,int spawned=0)
        {
            if(type==EnemyType.Straight)return EnemyTraits.None;
            return Theme(stage) switch
            {
                StageTheme.Phase when type==EnemyType.Diagonal=>EnemyTraits.Phase,
                StageTheme.Artillery when type==EnemyType.Missile && spawned%2==1=>EnemyTraits.Resonance,
                _=>EnemyTraits.None
            };
        }
    }
}
