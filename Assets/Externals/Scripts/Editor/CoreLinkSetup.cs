using System;
using UnityEditor;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class CoreLinkSetup
    {
        [MenuItem("4CORE/Set Up Core Connections")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            const string materialPath = "Assets/Externals/Datas/PrototypeVisuals/CoreLink.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default")) { name = "CoreLink" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            const string path = "Assets/Externals/Prefabs/Gameplay/Core.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CoreLinkView links = root.GetComponent<CoreLinkView>();
                if (links == null) links = root.AddComponent<CoreLinkView>();
                SerializedObject config = new(links);
                SerializedProperty array = config.FindProperty("lines");
                array.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    Transform existing = root.transform.Find($"CoreLink{i + 1}");
                    LineRenderer line;
                    if (existing == null)
                    {
                        GameObject obj = new($"CoreLink{i + 1}", typeof(LineRenderer));
                        obj.transform.SetParent(root.transform, false);
                        line = obj.GetComponent<LineRenderer>();
                        line.sharedMaterial = material;
                        line.useWorldSpace = true;
                        line.positionCount = 2;
                        line.SetPositions(new[] { Vector3.zero, Vector3.zero });
                        line.startWidth = line.endWidth = 0.035f;
                        line.numCapVertices = 3;
                        line.sortingOrder = 2;
                        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        line.receiveShadows = false;
                        line.enabled = false;
                    }
                    else line = existing.GetComponent<LineRenderer>();
                    array.GetArrayElementAtIndex(i).objectReferenceValue = line;
                }
                config.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
