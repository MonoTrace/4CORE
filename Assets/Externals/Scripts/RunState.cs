using System;
using UnityEngine;

namespace FourCore
{
    public enum PickupType { TimeFreeze, PersonalShield, Repair }
    public enum AugmentType { ShieldEnergy = 0, CoreRecovery = 1, LongerFreeze = 2, ChainBarrier = 6, TimedBarrier = 7, PortalKit = 8 }
    public enum EncounterPhase { Normal, Boss }
    public enum GameMode { Stage, Infinite }

    // Pure gameplay state: no renderers or real-time clocks, so undo restores it exactly.
    public sealed class RunState
    {
        public int Chapter { get; private set; } = 1;
        public GameMode Mode { get; private set; } = GameMode.Stage;
        public int Spawned { get; private set; }
        public int Resolved { get; private set; }
        public int FreezeMoves { get; private set; }
        public bool PersonalShield { get; private set; }
        public int NextDrop { get; private set; }
        public bool NeedsReward { get; private set; }
        public bool Completed { get; private set; }
        public int BonusShieldEnergy { get; private set; }
        public int BonusCoreRecovery { get; private set; }
        public int BonusFreezeMoves { get; private set; }
        public AugmentType? PrimaryAugment { get; private set; }
        private AugmentType[] rewardChoices = Array.Empty<AugmentType>();
        public System.Collections.Generic.IReadOnlyList<AugmentType> RewardChoices => rewardChoices;
        public bool Has(AugmentType type) => PrimaryAugment == type;
        public string StageLabel => IsInfinite ? "INFINITE" : $"STAGE {Chapter} - {StageCatalog.Name(Chapter)}";
        private bool freezeJustActivated;
        public EncounterPhase Phase { get; private set; }
        public const int EnemiesPerChapter = 30;
        public event Action Changed;
        public event Action<string> Notice;
        public void Announce(string text)=>Notice?.Invoke(text);
        public bool CanSpawn => !Completed && !NeedsReward && Phase == EncounterPhase.Normal && (IsInfinite || Spawned < EnemiesPerChapter);
        public bool BossReady => !Completed && !IsInfinite && Phase == EncounterPhase.Normal && Spawned >= EnemiesPerChapter && Resolved >= EnemiesPerChapter;
        public bool IsInfinite => Mode == GameMode.Infinite;

        public void ConfigureMode(GameMode mode) => Restore(new RunState
        {
            Mode = mode,
            Chapter = mode == GameMode.Infinite ? 3 : 1
        });

        public RunState Copy() => new() { Mode = Mode, Chapter = Chapter, Spawned = Spawned, Resolved = Resolved,
            FreezeMoves = FreezeMoves, PersonalShield = PersonalShield, NextDrop = NextDrop, Phase = Phase, freezeJustActivated = freezeJustActivated,NeedsReward=NeedsReward,Completed=Completed,
            BonusShieldEnergy = BonusShieldEnergy, BonusCoreRecovery = BonusCoreRecovery, BonusFreezeMoves = BonusFreezeMoves,
            PrimaryAugment = PrimaryAugment, rewardChoices = (AugmentType[])rewardChoices.Clone() };
        public void Restore(RunState saved)
        {
            Mode = saved.Mode; Chapter = saved.Chapter; Spawned = saved.Spawned; Resolved = saved.Resolved;
            FreezeMoves = saved.FreezeMoves; PersonalShield = saved.PersonalShield;
            NextDrop = saved.NextDrop; Phase = saved.Phase;
            freezeJustActivated = saved.freezeJustActivated;
            NeedsReward=saved.NeedsReward;
            Completed=saved.Completed;
            BonusShieldEnergy = saved.BonusShieldEnergy;
            BonusCoreRecovery = saved.BonusCoreRecovery;
            BonusFreezeMoves = saved.BonusFreezeMoves;
            PrimaryAugment = saved.PrimaryAugment;
            rewardChoices = (AugmentType[])saved.rewardChoices.Clone();
            Changed?.Invoke();
        }
        public void SpawnedEnemy() { Spawned++; Changed?.Invoke(); }
        public void ResolvedEnemy() { Resolved++; Changed?.Invoke(); }
        public void BeginBoss() { Phase = EncounterPhase.Boss; Notice?.Invoke("WARNING"); Changed?.Invoke(); }
        public void CompleteBoss()
        {
            if(IsInfinite || Completed || NeedsReward)return;
            Completed=Chapter>=StageCatalog.Count;
            Spawned = Resolved = 0; Phase = EncounterPhase.Normal;
            NeedsReward=!Completed;
            rewardChoices = Completed ? Array.Empty<AugmentType>() : AugmentCatalog.Offer(PrimaryAugment.HasValue);
            if (Completed) Notice?.Invoke("ALL STAGES CLEAR");
            Changed?.Invoke();
        }
        public PickupType TakeNextDrop()
        {
            PickupType result = (PickupType)(NextDrop % 3); NextDrop++; return result;
        }
        public bool AcceptReward(AugmentType type)
        {
            if (!NeedsReward || Array.IndexOf(rewardChoices, type) < 0) return false;
            switch (type)
            {
                case AugmentType.ShieldEnergy: BonusShieldEnergy++; break;
                case AugmentType.CoreRecovery: BonusCoreRecovery++; break;
                case AugmentType.LongerFreeze: BonusFreezeMoves += 2; break;
                case AugmentType.PortalKit: break;
                default: PrimaryAugment = type; break;
            }
            NeedsReward = false;
            Chapter++;
            Notice?.Invoke(StageLabel);
            Changed?.Invoke();
            return true;
        }
        public void Activate(PickupType type,bool protectCurrentTurn=true)
        {
            if (type == PickupType.TimeFreeze) { FreezeMoves = Math.Max(FreezeMoves, 10 + BonusFreezeMoves); freezeJustActivated = protectCurrentTurn; }
            if (type == PickupType.PersonalShield) PersonalShield = true;
            Changed?.Invoke();
        }
        public bool AbsorbHit()
        {
            if (!PersonalShield) return false;
            PersonalShield = false; Notice?.Invoke("SHIELD BREAK"); Changed?.Invoke(); return true;
        }
        public void ConsumeFrozenMove()
        {
            if (FreezeMoves <= 0) return;
            if (freezeJustActivated) { freezeJustActivated = false; return; }
            FreezeMoves--; Changed?.Invoke();
        }
        public int SpawnGap()
        {
            int pressure = Mathf.Min(3, Chapter - 1 + (IsInfinite ? Spawned / 30 : 0));
            return UnityEngine.Random.Range(Mathf.Max(2, 5 - pressure), Mathf.Max(4, 9 - pressure));
        }
        public EnemyType ChooseEnemy()
        {
            if(!IsInfinite)return StageCatalog.ChooseEnemy(Chapter,Spawned);
            return (EnemyType)UnityEngine.Random.Range(0, 5);
        }
    }
}
