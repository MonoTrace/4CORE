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
            Shader energyShader = Shader.Find("4CORE/Core Energy Waves");
            if (energyShader == null) throw new InvalidOperationException("The 4CORE/Core Energy Waves shader failed to load.");
            if (material == null)
            {
                material = new Material(energyShader) { name = "CoreLink" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else material.shader = energyShader;
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
                        line.useWorldSpace = true;
                        line.sortingOrder = 2;
                        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        line.receiveShadows = false;
                        line.enabled = false;
                    }
                    else line = existing.GetComponent<LineRenderer>();
                    line.sharedMaterial = material;
                    line.useWorldSpace = true;
                    line.positionCount = 21;
                    line.SetPositions(new Vector3[21]);
                    line.widthMultiplier = 0.12f;
                    line.sortingOrder = 2;
                    line.widthCurve = new AnimationCurve(
                        new Keyframe(0f, 0.72f),
                        new Keyframe(0.18f, 0.92f),
                        new Keyframe(0.5f, 1f),
                        new Keyframe(0.82f, 0.92f),
                        new Keyframe(1f, 0.72f));
                    line.numCornerVertices = 4;
                    line.numCapVertices = 4;
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
