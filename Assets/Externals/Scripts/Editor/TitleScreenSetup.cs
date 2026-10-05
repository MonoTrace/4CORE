using System;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class TitleScreenSetup
    {
        [MenuItem("4CORE/Set Up Title Screen")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            GameBootstrap bootstrap = UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            ScoreHud score = UnityEngine.Object.FindAnyObjectByType<ScoreHud>();
            if (bootstrap == null || score == null) throw new InvalidOperationException("Gameplay scene and HUD required.");
            Canvas hud = score.GetComponentInParent<Canvas>();
            CanvasGroup hudGroup = hud.GetComponent<CanvasGroup>();
            if (hudGroup == null) hudGroup = Undo.AddComponent<CanvasGroup>(hud.gameObject);
            GameFlowController flow = bootstrap.GetComponent<GameFlowController>();
            if (flow == null) flow = Undo.AddComponent<GameFlowController>(bootstrap.gameObject);
            SerializedObject config = new(flow);

            if (config.FindProperty("titleGroup").objectReferenceValue == null)
            {
                GameObject root = new("TitleScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup),
                    typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(root, "Create title screen");
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder = 200;
                UnityEngine.UI.CanvasScaler scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                GameObject panel = new("Menu", typeof(RectTransform));
                panel.transform.SetParent(root.transform, false);
                RectTransform rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.12f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(460f, 460f);
                Label(panel.transform, "Title", "4CORE", 88f, new Vector2(0, 160), new Vector2(460, 120), new Color(0.2f, 0.72f, 1f));
                UnityEngine.UI.Button start = Button(panel.transform, "StartButton", "START", 10f);
                UnityEngine.UI.Button quit = Button(panel.transform, "QuitButton", "QUIT", -95f);
                config.FindProperty("titleGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
                config.FindProperty("startButton").objectReferenceValue = start;
                config.FindProperty("quitButton").objectReferenceValue = quit;
            }
            config.FindProperty("hudGroup").objectReferenceValue = hudGroup;
            config.FindProperty("sceneCamera").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<CinemachineCamera>();
            config.ApplyModifiedProperties();
            SerializedObject boot = new(bootstrap);
            boot.FindProperty("gameFlow").objectReferenceValue = flow;
            boot.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
        }

        private static UnityEngine.UI.Button Button(Transform parent, string name, string title, float y)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(320f, 82f);
            UnityEngine.UI.Image image = obj.GetComponent<UnityEngine.UI.Image>();
            image.color = Color.white;
            UnityEngine.UI.Button button = obj.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.025f, 0.075f, 0.13f, 0.95f);
            colors.highlightedColor = new Color(0.045f, 0.2f, 0.34f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.03f, 0.12f, 0.22f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            Label(obj.transform, "Label", title, 32f, new Vector2(28f, 0f), new Vector2(265f, 70f), Color.white);
            return button;
        }

        private static void Label(Transform parent, string name, string text, float size, Vector2 position, Vector2 bounds, Color color)
        {
            GameObject obj = new(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = color;
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = bounds;
        }
    }
}
