using System;
using UnityEditor;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class EnemyPathSetup
    {
        private const string ArtFolder = "Assets/Externals/Datas/PrototypeVisuals";
        private const string PrefabFolder = "Assets/Externals/Prefabs/Gameplay";

        [MenuItem("4CORE/Set Up Enemy Paths")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            string path = PrefabFolder + "/PathCell.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                string texturePath = ArtFolder + "/PathSquare.asset";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                Sprite sprite = null;
                if (texture == null)
                {
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "PathSquare", filterMode = FilterMode.Point };
                    texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    texture.Apply();
                    AssetDatabase.CreateAsset(texture, texturePath);
                    sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f, 0, SpriteMeshType.FullRect);
                    sprite.name = "PathSquare";
                    AssetDatabase.AddObjectToAsset(sprite, texture);
                }
                else
                    foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
                        if (asset is Sprite found) sprite = found;

                if (sprite == null) throw new InvalidOperationException("PathSquare needs a sprite subasset.");
                string materialPath = ArtFolder + "/PathCell.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")) { name = "PathCell" };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                GameObject temporary = new("PathCell", typeof(SpriteRenderer));
                try
                {
                    temporary.transform.localScale = new Vector3(0.88f, 0.88f, 1f);
                    SpriteRenderer renderer = temporary.GetComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sharedMaterial = material;
                    renderer.sortingOrder = 1;
                    renderer.color = new Color(1f, 1f, 1f, 0.4f);
                    prefab = PrefabUtility.SaveAsPrefabAsset(temporary, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(temporary); }
            }

            foreach (EnemyType type in Enum.GetValues(typeof(EnemyType)))
            {
                string enemyPath = $"{PrefabFolder}/Enemies/{type}Enemy.prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(enemyPath);
                try
                {
                    EnemyPathView view = root.GetComponentInChildren<EnemyPathView>(true);
                    if (view == null)
                    {
                        GameObject child = new("PathView");
                        child.transform.SetParent(root.transform, false);
                        view = child.AddComponent<EnemyPathView>();
                        SerializedObject settings = new(view);
                        settings.FindProperty("pathCellPrefab").objectReferenceValue = prefab.GetComponent<SpriteRenderer>();
                        settings.FindProperty("pathColor").colorValue = root.transform.Find("Visual").GetComponent<Renderer>().sharedMaterial.color;
                        settings.ApplyModifiedPropertiesWithoutUndo();
                    }
                    SerializedObject enemy = new(root.GetComponent<EnemyView>());
                    enemy.FindProperty("pathView").objectReferenceValue = view;
                    enemy.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, enemyPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
