using System;
using UnityEngine;

namespace FourCore
{
    public enum PickupType { TimeFreeze, PersonalShield, Repair }
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
            FreezeMoves = FreezeMoves, PersonalShield = PersonalShield, NextDrop = NextDrop, Phase = Phase, freezeJustActivated = freezeJustActivated,NeedsReward=NeedsReward,Completed=Completed };
        public void Restore(RunState saved)
        {
            Mode = saved.Mode; Chapter = saved.Chapter; Spawned = saved.Spawned; Resolved = saved.Resolved;
            FreezeMoves = saved.FreezeMoves; PersonalShield = saved.PersonalShield;
            NextDrop = saved.NextDrop; Phase = saved.Phase;
            freezeJustActivated = saved.freezeJustActivated;
            NeedsReward=saved.NeedsReward;
            Completed=saved.Completed;
            Changed?.Invoke();
        }
        public void SpawnedEnemy() { Spawned++; Changed?.Invoke(); }
        public void ResolvedEnemy() { Resolved++; Changed?.Invoke(); }
        public void BeginBoss() { Phase = EncounterPhase.Boss; Notice?.Invoke("WARNING"); Changed?.Invoke(); }
        public void CompleteBoss()
        {
            if(IsInfinite || Completed)return;
            Completed=Chapter>=StageCatalog.Count;
            if(!Completed)Chapter++;
            Spawned = Resolved = 0; Phase = EncounterPhase.Normal;
            NeedsReward=false;
            Notice?.Invoke(Completed?"ALL STAGES CLEAR":StageLabel); Changed?.Invoke();
        }
        public PickupType TakeNextDrop()
        {
            PickupType result = (PickupType)(NextDrop % 3); NextDrop++; return result;
        }
        public void AcceptReward(){NeedsReward=false;Changed?.Invoke();}
        public void Activate(PickupType type,bool protectCurrentTurn=true)
        {
            if (type == PickupType.TimeFreeze) { FreezeMoves = Math.Max(FreezeMoves, 10); freezeJustActivated = protectCurrentTurn; }
            if (type == PickupType.PersonalShield) PersonalShield = true;
            Notice?.Invoke(type switch { PickupType.TimeFreeze => "TIME FREEZE", PickupType.PersonalShield => "SHIELD READY", _ => "CORE RESTORED" });
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
