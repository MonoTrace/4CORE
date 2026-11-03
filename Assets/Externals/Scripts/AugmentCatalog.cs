using UnityEngine;

namespace FourCore
{
    public static class AugmentCatalog
    {
        public static bool IsPlaystyle(AugmentType type) => type == AugmentType.ChainBarrier || type == AugmentType.TimedBarrier;
        public static AugmentType[] Offer(bool hasPrimary)
        {
            AugmentType[] pool = hasPrimary
                ? new[] { AugmentType.ShieldEnergy, AugmentType.CoreRecovery, AugmentType.LongerFreeze, AugmentType.PortalKit }
                : new[] { AugmentType.ChainBarrier, AugmentType.TimedBarrier, AugmentType.ShieldEnergy,
                    AugmentType.CoreRecovery, AugmentType.LongerFreeze, AugmentType.PortalKit };
            for (int i = 0; i < 3; i++)
            {
                int pick = Random.Range(i, pool.Length);
                (pool[i], pool[pick]) = (pool[pick], pool[i]);
            }
            return new[] { pool[0], pool[1], pool[2] };
        }
        public static string Name(AugmentType type) => type switch
        {
            AugmentType.PortalKit => "포탈", AugmentType.ChainBarrier => "연쇄 방벽", AugmentType.TimedBarrier => "시한 방벽",
            AugmentType.ShieldEnergy => "쉴드 에너지 +1", AugmentType.CoreRecovery => "코어 회복 속도 +1", _ => "시간 정지 +2턴"
        };
        public static string Description(AugmentType type, RunState run) => type switch
        {
            AugmentType.PortalKit => "포탈 아이템 한 쌍 획득\nE로 두 칸에 설치 후 양방향 이동\n한 쌍당 총 2회 이용 후 소멸",
            AugmentType.ChainBarrier => "연결된 쉴드 하나가 막으면\n연결된 쉴드가 연쇄 폭발\n각 쉴드 주변 한 칸에 피해",
            AugmentType.TimedBarrier => "쉴드 설치 3턴 뒤 폭발\n주변 한 칸의 적과 보스에 피해\n남은 턴이 쉴드에 표시됩니다",
            AugmentType.ShieldEnergy => $"에너지·쉴드 설치 한도 +1\n추가 에너지 +{run.BonusShieldEnergy} → +{run.BonusShieldEnergy + 1}",
            AugmentType.CoreRecovery => $"이동할 때마다 코어 회복량 +1\n회복량 {1 + run.BonusCoreRecovery} → {2 + run.BonusCoreRecovery}",
            _ => $"시간 정지 지속 시간 증가\n{10 + run.BonusFreezeMoves}턴 → {12 + run.BonusFreezeMoves}턴"
        };
        public static Color Color(AugmentType type) => type switch
        {
            AugmentType.ShieldEnergy => new Color(1f,.77f,.35f),
            AugmentType.ChainBarrier or AugmentType.TimedBarrier => new Color(1f,.43f,.36f),
            AugmentType.PortalKit => new Color(.4f,.78f,1f),
            AugmentType.CoreRecovery => new Color(.77f,.57f,1f),
            _ => new Color(.39f,.84f,1f)
        };
        public static string Category(AugmentType type) => type == AugmentType.PortalKit ? "ITEM" : IsPlaystyle(type) ? "PLAYSTYLE" : "UPGRADE";
    }
}
