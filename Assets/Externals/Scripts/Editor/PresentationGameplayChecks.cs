using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class PresentationGameplayChecks
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Read<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
        private static void Need(bool value, string message) { if (!value) throw new Exception(message); }

        private static bool captureFrames;
        public static void Start(bool capture = false)
        {
            captureFrames = capture;
            SessionState.SetString("4CORE.PresentationChecks", "RUNNING");
            UnityEngine.Object.FindAnyObjectByType<EnemyManager>().StartCoroutine(Report());
        }
        public static string Result() => SessionState.GetString("4CORE.PresentationChecks", "NOT STARTED");
        private static IEnumerator Report()
        {
            IEnumerator checks = Run();
            while (true)
            {
                object next;
                try { if (!checks.MoveNext()) break; next = checks.Current; }
                catch (Exception error)
                {
                    SessionState.SetString("4CORE.PresentationChecks", "FAIL: " + error);
                    Debug.LogException(error); yield break;
                }
                yield return next;
            }
            const string result = "PASS: pickup effects/undo, diagonal boss combat, death-centered black iris/full coverage, cards rising from below screen, selection gate, shrinking card flight/player absorption/next round";
            SessionState.SetString("4CORE.PresentationChecks", result);
            Debug.Log(result);
        }
        private static void Capture(string name)
        {
            if (captureFrames) ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.temporaryCachePath, name + ".png"));
        }
        private static IEnumerator Run()
        {
            var manager = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            var flow = UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            var controls = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var presentation = UnityEngine.Object.FindAnyObjectByType<RunPresentation>();
            var core = Read<CoreState>(manager, "coreState");
            var coreView = Read<CoreView>(manager, "coreView");
            var player = Read<PlayerState>(manager, "playerState");
            var playerView = Read<PlayerView>(controls, "playerView");
            var shields = Read<ShieldState>(manager, "shieldState");
            var turns = Read<TurnManager>(manager, "turnManager");
            if (flow.State == GameFlowState.Title) flow.BeginStageMode();
            float deadline = Time.realtimeSinceStartup + 8f;
            while (flow.State == GameFlowState.Starting && Time.realtimeSinceStartup < deadline) yield return null;
            Need(flow.State == GameFlowState.Playing, "intro did not finish");
            StageThemeChecks.Pure();

            StageThemeChecks.ShowBoss(1); manager.Boss.Finish();
            manager.Run.ConfigureMode(GameMode.Stage);
            core.Restore(new[] { 90, 0, 0, 0 }); coreView.Apply(core, false);
            Need(manager.Pickups.TryAdd(player.Position, PickupType.Repair), "repair fixture");
            manager.CollectPickup();
            Need(core.AliveCount == 4 && coreView.GetComponentsInChildren<SpawnPulse>().Length == 1, "repair must emit one pulse on regenerated core");
            core.Restore(new[] { 90, 0, 0, 0 }); coreView.Apply(core, false);
            Need(coreView.GetComponentsInChildren<SpawnPulse>().Length == 0, "restoring core must clear recovery pulse");
            core.Restore(new int[4]); coreView.Apply(core, false);
            Need(controls.TryMove(Vector2Int.right), "shield undo fixture move");
            Need(manager.Pickups.TryAdd(player.Position, PickupType.PersonalShield), "shield fixture");
            manager.CollectPickup(); yield return null;
            Need(manager.Run.PersonalShield && playerView.GetComponentsInChildren<SpawnPulse>().Length > 0, "shield spawn pulse");
            Need(Read<LineRenderer>(presentation, "personalShield").startWidth >= .06f && Read<LineRenderer>(presentation, "shieldGlow").enabled, "thicker glowing shield");
            Need(controls.TryUndo() && !manager.Run.PersonalShield, "shield undo state");
            Need(playerView.GetComponentsInChildren<SpawnPulse>().Length == 0, "undo clears shield pulse");

            StageThemeChecks.ShowBoss(2);
            GridPosition origin = manager.Boss.Origin;
            turns.CompletePlayerAction(PlayerActionKind.Wait); turns.CompletePlayerAction(PlayerActionKind.Wait);
            Need(manager.Boss.Origin.X == origin.X - 1 && manager.Boss.Origin.Y == origin.Y - 1 && !manager.Boss.HasMissiles, "diagonal body must charge after warning");
            var snapshot = manager.Capture();
            turns.CompletePlayerAction(PlayerActionKind.Wait); turns.CompletePlayerAction(PlayerActionKind.Wait); manager.Restore(snapshot);
            Need(manager.Boss.Origin.X == origin.X - 1 && manager.Boss.BodyVisible, "diagonal undo");
            for (int i = 0; i < 18 && core.AliveCount == 4; i++) turns.CompletePlayerAction(PlayerActionKind.Wait);
            Need(core.AliveCount < 4, "unblocked diagonal charge must damage cores");
            yield return new WaitForSecondsRealtime(.4f);

            StageThemeChecks.ShowBoss(2);
            for (int turn = 0; turn < 240 && manager.Boss.Active; turn++)
            {
                shields.Restore(Array.Empty<GridPosition>());
                for (int i = 0; i < manager.Boss.CellCount; i++)
                {
                    if (manager.Boss.Health(i) <= 0) continue;
                    GridPosition next = manager.Boss.CellAt(manager.Boss.NextCharge(), i);
                    if (!manager.Boss.Occupies(next) && !core.Occupies(next) && GameConfig.IsInsidePlayable(next)) shields.TryAdd(next);
                }
                turns.CompletePlayerAction(PlayerActionKind.Wait);
                Need(core.AliveCount == 4 && !manager.Boss.HasMissiles && manager.Beams.Count == 0, "shield counters protect core; no legacy attacks");
            }
            Need(!manager.Boss.Active && manager.Run.NeedsReward && flow.State == GameFlowState.Reward, "defeated boss opens reward");
            var group = Read<CanvasGroup>(flow, "rewardGroup");
            var button = Read<UnityEngine.UI.Button>(flow, "shieldReward");
            var iris = Read<RewardIris>(flow, "rewardIris");
            var card = button.GetComponent<AugmentCardView>();
            var rect = (RectTransform)button.transform;
            Need(iris.Radius == 0f && !group.interactable && !controls.GameplayInputEnabled, "reward begins at zero-radius iris with input blocked");
            Need(rect.anchoredPosition.y + rect.rect.height * .5f < ((RectTransform)group.transform).rect.yMin, "cards start entirely below screen");
            Vector2 deathPoint = (Vector2)typeof(GameFlowController).GetMethod("RewardPoint", Fields).Invoke(flow, new object[] { manager.LastDeathWorldPosition });
            Need(Vector2.Distance(iris.Center, deathPoint) < .01f, "iris centered on final destruction");
            Need(Vector3.Distance(manager.LastDeathWorldPosition, manager.transform.TransformPoint(GameConfig.LogicalToWorld(manager.Boss.LastDestroyedCell))) < .01f, "final boss cell recorded");
            button.onClick.Invoke(); Need(manager.Run.NeedsReward, "cannot choose during reveal");
            yield return new WaitForSecondsRealtime(.3f);
            float cover = (float)typeof(GameFlowController).GetMethod("IrisCoverRadius", Fields).Invoke(flow, new object[] { iris.Center });
            Need(iris.Radius > 0f && iris.Radius < cover, "black iris expands over time");
            Capture("reward-iris");
            yield return new WaitForSecondsRealtime(.4f);
            Need(iris.Radius >= cover && !group.interactable, "screen fully black before cards can be chosen");
            Need(rect.anchoredPosition.y + rect.rect.height * .5f < ((RectTransform)group.transform).rect.yMin, "cards stay below screen until iris finishes");
            Capture("reward-covered");
            yield return new WaitForSecondsRealtime(.2f);
            Need(rect.anchoredPosition.y + rect.rect.height * .5f < ((RectTransform)group.transform).rect.yMin, "short black hold before cards enter");
            yield return new WaitForSecondsRealtime(.5f);
            Need(rect.anchoredPosition.y < card.RestingPosition.y && !group.interactable, "cards slide upward after full cover");
            Capture("reward-rising");
            yield return new WaitForSecondsRealtime(.65f);
            Need(group.interactable && group.alpha == 1f, "reward becomes selectable after reveal");
            foreach (var text in group.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if (text.name == "SelectionHint" || text.name == "FooterHint") Need(!text.gameObject.activeSelf, "selection hints removed");
            button.onClick.Invoke();
            Need(manager.Run.Chapter == 3 && flow.State == GameFlowState.Reward && !controls.GameplayInputEnabled, "choice starts transition before gameplay");
            button.onClick.Invoke(); Need(manager.Run.Chapter == 3, "duplicate selection ignored");
            Vector2 selectedOrigin = rect.anchoredPosition;
            yield return new WaitForSecondsRealtime(.15f);
            Need(rect.localScale.x < .9f && rect.localScale.x > .23f && group.gameObject.activeSelf, "selected card shrinks before flight");
            yield return new WaitForSecondsRealtime(.35f);
            Vector2 playerPoint = (Vector2)typeof(GameFlowController).GetMethod("RewardPoint", Fields).Invoke(flow, new object[] { playerView.transform.position });
            Need(rect.localScale.x < .24f && Vector2.Distance(rect.anchoredPosition, playerPoint) < Vector2.Distance(selectedOrigin, playerPoint), "small card travels toward player");
            Need(!controls.GameplayInputEnabled && iris.color.a < .2f, "world revealed while flight still blocks gameplay");
            Capture("reward-absorption");
            yield return new WaitForSecondsRealtime(.85f);
            Need(flow.State == GameFlowState.Playing && controls.GameplayInputEnabled && !group.gameObject.activeSelf, "next round resumes after transition");
            Need(playerView.GetComponentsInChildren<SpawnPulse>().Length > 0, "player absorption pulse on arrival");
        }
    }
}
