using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class ScorePopupSetup
    {
        [MenuItem("4CORE/Set Up Score Popup")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            GameBootstrap bootstrap = UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            if (bootstrap == null) throw new InvalidOperationException("GameBootstrap is required.");
            const string path = "Assets/Externals/Prefabs/Gameplay/ScorePopup.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                GameObject root = new("ScorePopup");
                try
                {
                    TextMeshPro text = root.AddComponent<TextMeshPro>();
                    text.font = TMP_Settings.defaultFontAsset;
                    text.fontSize = 4.5f;
                    text.enableAutoSizing = false;
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.fontStyle = FontStyles.Bold;
                    text.color = Color.white;
                    text.richText = true;
                    text.rectTransform.sizeDelta = new Vector2(4f, 1f);
                    text.text = "10 x <color=#FF1318>4</color>";
                    text.GetComponent<MeshRenderer>().sortingOrder = 30;
                    ScorePopup popup = root.AddComponent<ScorePopup>();
                    SerializedObject fields = new(popup);
                    fields.FindProperty("label").objectReferenceValue = text;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            SerializedObject settings = new(bootstrap);
            settings.FindProperty("scorePopupPrefab").objectReferenceValue = prefab.GetComponent<ScorePopup>();
            settings.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
        }
    }
}
