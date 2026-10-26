using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class GameModeMenuSetup
    {
        private static GameFlowController Flow => UnityEngine.Object.FindAnyObjectByType<GameFlowController>();

        private static UnityEngine.UI.Button Button(string name)
        {
            UnityEngine.UI.Button found=null;
            SerializedObject config=new(Flow);
            CanvasGroup title=config.FindProperty("titleGroup").objectReferenceValue as CanvasGroup;
            foreach(var button in title.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if(button.name==name)
                {
                    if(found!=null)throw new InvalidOperationException("Duplicate button: "+name);
                    found=button;
                }
            return found!=null ? found : throw new InvalidOperationException("Missing button: "+name);
        }

        [MenuItem("4CORE/Connect Mode Buttons")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before wiring.");
            var stage=Button("StageModeButton");var infinite=Button("InfiniteModeButton");
            Undo.RecordObject(Flow,"Connect mode buttons");
            SerializedObject config=new(Flow);
            config.FindProperty("stageModeButton").objectReferenceValue=stage;
            config.FindProperty("infiniteModeButton").objectReferenceValue=infinite;
            config.ApplyModifiedProperties();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(Flow.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(Flow.gameObject.scene);
        }

        public static void ClickStage()=>Button("StageModeButton").onClick.Invoke();
        public static void ClickInfinite()=>Button("InfiniteModeButton").onClick.Invoke();
        public static string CheckPlayingStage()=>CheckPlaying(GameMode.Stage);
        public static string CheckPlayingInfinite()=>CheckPlaying(GameMode.Infinite);
        public static string CheckScoreMode()
        {
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var score=(ScoreState)typeof(GameFlowController).GetField("score",flags).GetValue(Flow);
            var hud=UnityEngine.Object.FindAnyObjectByType<ScoreHud>(FindObjectsInactive.Include);
            var text=(TMPro.TMP_Text)typeof(ScoreHud).GetField("scoreText",flags).GetValue(hud);
            bool expected=Flow.SelectedMode==GameMode.Infinite;
            if(score.Enabled!=expected || text.gameObject.activeSelf!=expected)throw new Exception("Score mode visibility");
            long before=score.Total;
            long award=score.AddEnemyDefeat(10,4);
            if(award!=(expected?40:0))throw new Exception("Score mode award");
            score.Restore(before);
            return $"PASS: {Flow.SelectedMode} score visibility and awarding";
        }

        public static string CheckResultScoreMode()
        {
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            long best=HighScoreStore.Best;
            typeof(GameFlowController).GetMethod("HandleGameOver",flags).Invoke(Flow,null);
            var result=(TMPro.TMP_Text)typeof(GameFlowController).GetField("resultScore",flags).GetValue(Flow);
            if(Flow.State!=GameFlowState.GameOver || result.gameObject.activeSelf!=(Flow.SelectedMode==GameMode.Infinite))
                throw new Exception("Result score mode");
            if(HighScoreStore.Best!=best)throw new Exception("Test changed saved record");
            return "PASS: result visibility; saved record unchanged";
        }

        public static string CheckScoreState()
        {
            var score=new ScoreState();score.SetEnabled(false);
            if(score.AddEnemyDefeat(200,4)!=0 || score.Total!=0)throw new Exception("Disabled score awarded");
            score.Restore(999);if(score.Total!=0)throw new Exception("Disabled score restored");
            score.SetEnabled(true);if(score.AddEnemyDefeat(10,4)!=40)throw new Exception("Infinite score disabled");
            return "PASS: disabled score, undo restore and enabled score";
        }

        private static string CheckPlaying(GameMode mode)
        {
            var manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
            if(Flow.State!=GameFlowState.Playing || Flow.SelectedMode!=mode || manager.Run.Mode!=mode ||
                manager.Run.IsInfinite!=(mode==GameMode.Infinite) || manager.ActiveEnemyCount<1)
                throw new InvalidOperationException($"Wrong start: {Flow.State}/{Flow.SelectedMode}/{manager.Run.Chapter}");
            return $"PASS: {mode} button, intro, first spawn; frame={Time.frameCount}";
        }

        public static string CheckModeState()
        {
            var run=new RunState();run.ConfigureMode(GameMode.Infinite);
            if(!run.IsInfinite || run.Chapter!=3 || run.Phase!=EncounterPhase.Normal)throw new Exception("Infinite initialization");
            var snapshot=run.Copy();run.ConfigureMode(GameMode.Stage);run.Restore(snapshot);
            if(run.Mode!=GameMode.Infinite || !run.IsInfinite)throw new Exception("Mode snapshot");
            for(int i=0;i<100;i++){run.SpawnedEnemy();run.ResolvedEnemy();}
            if(run.BossReady || !run.CanSpawn)throw new Exception("Infinite spawn cap or boss");
            run.ConfigureMode(GameMode.Stage);
            if(run.IsInfinite || run.Chapter!=1 || run.Spawned!=0 || run.FreezeMoves!=0)throw new Exception("Stage reset");
            for(int i=0;i<30;i++){run.SpawnedEnemy();run.ResolvedEnemy();}
            if(!run.BossReady || run.CanSpawn)throw new Exception("Stage boss threshold");
            return "PASS: modes, snapshot, infinite spawn without boss, stage boss threshold";
        }

        public static string Inspect()
        {
            GameFlowController flow=UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            StringBuilder result=new();
            foreach(GameObject root in flow.gameObject.scene.GetRootGameObjects())
                foreach(UnityEngine.UI.Button button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    result.AppendLine($"{button.name} parent={button.transform.parent.name} active={button.gameObject.activeSelf}");
                    for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
                        result.AppendLine($"  {button.onClick.GetPersistentTarget(i)}.{button.onClick.GetPersistentMethodName(i)}");
                }
            return result.ToString();
        }
    }
}
