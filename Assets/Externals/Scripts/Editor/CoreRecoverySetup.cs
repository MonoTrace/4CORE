using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class CoreRecoverySetup
    {
        [MenuItem("4CORE/Set Up Core Recovery Labels")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            const string path = "Assets/Externals/Prefabs/Gameplay/Core.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SerializedObject core = new(root.GetComponent<CoreView>());
                SerializedProperty cells = core.FindProperty("cells");
                SerializedProperty renderers = core.FindProperty("cellRenderers");
                SerializedProperty labels = core.FindProperty("recoveryLabels");
                renderers.arraySize = labels.arraySize = cells.arraySize;
                for (int i = 0; i < cells.arraySize; i++)
                {
                    GameObject cell = (GameObject)cells.GetArrayElementAtIndex(i).objectReferenceValue;
                    renderers.GetArrayElementAtIndex(i).objectReferenceValue = cell.transform.Find("Visual").GetComponent<MeshRenderer>();
                    Transform existing = cell.transform.Find("RecoveryText");
                    TextMeshPro label;
                    if (existing == null)
                    {
                        GameObject obj = new("RecoveryText");
                        obj.transform.SetParent(cell.transform, false);
                        obj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
                        label = obj.AddComponent<TextMeshPro>();
                        label.font = TMP_Settings.defaultFontAsset;
                        label.fontSize = 4.5f;
                        label.fontStyle = FontStyles.Bold;
                        label.alignment = TextAlignmentOptions.Center;
                        label.enableAutoSizing = false;
                        label.textWrappingMode = TextWrappingModes.NoWrap;
                        label.rectTransform.sizeDelta = new Vector2(0.8f, 0.8f);
                        label.color = Color.white;
                        label.text = "90";
                        label.GetComponent<MeshRenderer>().sortingOrder = 20;
                        obj.SetActive(false);
                    }
                    else label = existing.GetComponent<TextMeshPro>();
                    labels.GetArrayElementAtIndex(i).objectReferenceValue = label;
                }
                core.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            // Remove only the retired energy label, not the shared HUD Canvas/score.
            EnergyHud energy = UnityEngine.Object.FindAnyObjectByType<EnergyHud>();
            if (energy != null) Undo.DestroyObjectImmediate(energy.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }
    }
}
