using System.Collections;
using System.Reflection;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class IntroSequenceChecks
    {
        public static string Result { get; private set; } = "Not run";

        public static void Run()
        {
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            if (flow == null || flow.State != GameFlowState.Title)
                throw new System.InvalidOperationException("Run in a fresh title-screen Play session.");
            Result = "Running";
            flow.StartCoroutine(Check(flow));
        }

        private static IEnumerator Check(GameFlowController flow)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            CanvasGroup hud = (CanvasGroup)typeof(GameFlowController).GetField("hudGroup", flags).GetValue(flow);
            GameObject core = (GameObject)typeof(GameFlowController).GetField("core", flags).GetValue(flow);
            EnemyManager enemies = Object.FindAnyObjectByType<EnemyManager>();
            PlayerController controls = Object.FindAnyObjectByType<PlayerController>();
            Material grid = Object.FindAnyObjectByType<BoardView>().GetComponentInChildren<MeshRenderer>().sharedMaterial;
            bool partialGrid = false, partialEntities = false;
            float deadline = Time.realtimeSinceStartup + 10f;
            flow.BeginGame();
            flow.BeginGame();
            while (flow.State != GameFlowState.Playing && Time.realtimeSinceStartup < deadline)
            {
                string failure = null;
                if (controls.GameplayInputEnabled || controls.CurrentTurn != 0) failure = "Input unlocked before intro ends";
                if (flow.Stage == IntroStage.Camera && (grid.color.a != 0f || core.activeSelf || hud.alpha != 0f || enemies.ActiveEnemyCount != 0))
                    failure = "Content appeared during camera movement";
                if (flow.Stage == IntroStage.Grid)
                {
                    if (Mathf.Abs(Camera.main.orthographicSize - 12.5f) > 0.01f || core.activeSelf || hud.alpha != 0f || enemies.ActiveEnemyCount != 0)
                        failure = "Grid fade must follow camera and precede content";
                    partialGrid |= grid.color.a > 0f && grid.color.a < 0.89f;
                }
                if (flow.Stage == IntroStage.Entities)
                {
                    if (Mathf.Abs(grid.color.a - 0.9f) > 0.01f || !core.activeSelf || enemies.ActiveEnemyCount != 1)
                        failure = "Entities must follow completed grid";
                    float alpha = core.GetComponentInChildren<MeshRenderer>().sharedMaterial.color.a;
                    if (Mathf.Abs(alpha - hud.alpha) > 0.01f) failure = "Core and HUD fade differ";
                    EnemyView enemy = enemies.GetComponentInChildren<EnemyView>();
                    if (enemy != null && Mathf.Abs(enemy.GetComponentInChildren<MeshRenderer>().sharedMaterial.color.a - hud.alpha) > 0.01f)
                        failure = "Enemy and HUD fade differ";
                    partialEntities |= hud.alpha > 0f && hud.alpha < 1f;
                }
                if (failure != null) { Result = "FAILED: " + failure; yield break; }
                yield return null;
            }
            bool passed = flow.State == GameFlowState.Playing && controls.GameplayInputEnabled &&
                partialGrid && partialEntities && hud.alpha == 1f && enemies.ActiveEnemyCount == 1;
            foreach (MeshRenderer renderer in core.GetComponentsInChildren<MeshRenderer>())
                passed &= UnityEditor.AssetDatabase.Contains(renderer.sharedMaterial);
            Result = passed ? "PASSED: camera -> grid -> core/enemy/HUD; input gate and original materials restored" : "FAILED: completion or fade samples";
            Debug.Log("[INTRO CHECKS] " + Result);
        }
    }
}
