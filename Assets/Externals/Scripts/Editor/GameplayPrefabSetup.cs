using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FourCore.EditorTools
{
    // Only creates missing assets. Existing prefab/material edits are preserved on rerun.
    public static class GameplayPrefabSetup
    {
        private const string PrefabFolder = "Assets/Externals/Prefabs/Gameplay";
        private const string ArtFolder = "Assets/Externals/Datas/PrototypeVisuals";

        [MenuItem("4CORE/Set Up Gameplay Prefabs and Scene HUD")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play mode before setting up prefabs.");

            GameBootstrap bootstrap = UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            if (bootstrap == null) throw new InvalidOperationException("The open scene needs a GameBootstrap.");
            EnsureFolder(PrefabFolder);
            EnsureFolder(ArtFolder);
            EnsureFolder(PrefabFolder + "/Enemies");

            GameObject player = GetOrCreatePrefab("Player", () =>
            {
                GameObject root = new("Player");
                root.AddComponent<PlayerView>();
                root.AddComponent<PlayerController>();
                AddVisual(root.transform, "Visual",
                    GetMesh("PlayerTriangle", PrototypeMeshes.CreateTriangleMesh),
                    GetMaterial("Player", new Color(0.05f, 0.65f, 1f)), Vector3.zero, Vector3.one, 10);
                return root;
            });

            Mesh quad = GetMesh("Quad", () => BuildQuad());
            GameObject core = GetOrCreatePrefab("Core", () =>
            {
                GameObject root = new("Core");
                CoreView view = root.AddComponent<CoreView>();
                CoreState state = new();
                SerializedObject serialized = new(view);
                SerializedProperty cells = serialized.FindProperty("cells");
                cells.arraySize = state.CellCount;
                for (int i = 0; i < state.CellCount; i++)
                {
                    GameObject cell = new($"Cell{i + 1}");
                    cell.transform.SetParent(root.transform, false);
                    cell.transform.localPosition = GameConfig.LogicalToWorld(state.GetCell(i), -0.05f);
                    AddVisual(cell.transform, "Visual", quad,
                        GetMaterial("Core", new Color(1f, 0.075f, 0.095f)),
                        Vector3.zero, new Vector3(0.84f, 0.84f, 1f), 5);
                    cells.GetArrayElementAtIndex(i).objectReferenceValue = cell;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return root;
            });

            GameObject shield = GetOrCreatePrefab("Shield", () =>
            {
                GameObject root = new("Shield");
                Material fill = GetMaterial("ShieldFill", new Color(0.05f, 0.55f, 1f, 0.5f), true);
                Material edge = GetMaterial("ShieldOutline", new Color(1f, 1f, 1f, 0.95f), true);
                AddVisual(root.transform, "Fill", quad, fill, Vector3.zero, new Vector3(0.76f, 0.76f, 1f), 6);
                AddVisual(root.transform, "Top", quad, edge, new Vector3(0f, 0.41f), new Vector3(0.88f, 0.06f, 1f), 5);
                AddVisual(root.transform, "Bottom", quad, edge, new Vector3(0f, -0.41f), new Vector3(0.88f, 0.06f, 1f), 5);
                AddVisual(root.transform, "Left", quad, edge, new Vector3(-0.41f, 0f), new Vector3(0.06f, 0.76f, 1f), 5);
                AddVisual(root.transform, "Right", quad, edge, new Vector3(0.41f, 0f), new Vector3(0.06f, 0.76f, 1f), 5);
                return root;
            });

            EnemyType[] types = (EnemyType[])Enum.GetValues(typeof(EnemyType));
            EnemyView[] enemies = new EnemyView[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                EnemyType type = types[i];
                GameObject prefab = GetOrCreatePrefab($"Enemies/{type}Enemy", () =>
                {
                    GameObject root = new($"{type}Enemy");
                    EnemyView view = root.AddComponent<EnemyView>();
                    SerializedObject serialized = new(view);
                    serialized.FindProperty("enemyType").intValue = (int)type;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    Color color = type switch
                    {
                        EnemyType.Diagonal => new Color(1f, 0.48f, 0.12f),
                        EnemyType.RightAngle => new Color(0.2f, 1f, 0.42f),
                        EnemyType.Zigzag => new Color(1f, 0.88f, 0.16f),
                        _ => new Color(1f, 0.22f, 0.3f)
                    };
                    AddVisual(root.transform, "Visual", GetMesh(type.ToString(), () => PrototypeMeshes.CreateEnemyMesh(type)),
                        GetMaterial(type.ToString(), color), Vector3.zero, Vector3.one, 8);
                    return root;
                });
                enemies[i] = prefab.GetComponent<EnemyView>();
            }

            SerializedObject boot = new(bootstrap);
            EnergyHud energy = boot.FindProperty("energyHud").objectReferenceValue as EnergyHud;
            ScoreHud score = boot.FindProperty("scoreHud").objectReferenceValue as ScoreHud;
            if (energy == null || score == null)
            {
                GameObject hud = new("HUD", typeof(RectTransform), typeof(Canvas),
                    typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(hud, "Create scene HUD");
                Canvas canvas = hud.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                UnityEngine.UI.CanvasScaler scaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                if (energy == null)
                {
                    TextMeshProUGUI label = AddText(hud.transform, "EnergyText", "ENERGY  4 / 4",
                        42f, 40f, new Vector2(440f, 80f), new Color(0.2f, 0.72f, 1f), FontStyles.Bold);
                    energy = label.gameObject.AddComponent<EnergyHud>();
                    SetReference(energy, "energyText", label);
                }
                if (score == null)
                {
                    TextMeshProUGUI label = AddText(hud.transform, "ScoreText", "SCORE  0",
                        30f, 130f, new Vector2(600f, 50f), Color.white, FontStyles.Normal);
                    score = label.gameObject.AddComponent<ScoreHud>();
                    SetReference(score, "scoreText", label);
                }
            }

            ScoreFeedbackSetup.Configure(score);

            Undo.RecordObject(bootstrap, "Assign gameplay prefabs and HUD");
            Undo.RecordObject(bootstrap.gameObject, "Rename GameRoot");
            bootstrap.name = "GameRoot";
            boot.FindProperty("playerPrefab").objectReferenceValue = player.GetComponent<PlayerView>();
            boot.FindProperty("corePrefab").objectReferenceValue = core.GetComponent<CoreView>();
            boot.FindProperty("shieldPrefab").objectReferenceValue = shield;
            SerializedProperty enemyList = boot.FindProperty("enemyPrefabs");
            enemyList.arraySize = enemies.Length;
            for (int i = 0; i < enemies.Length; i++) enemyList.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            boot.FindProperty("energyHud").objectReferenceValue = energy;
            boot.FindProperty("scoreHud").objectReferenceValue = score;
            boot.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(bootstrap.gameObject.scene))
                throw new InvalidOperationException("Could not save the scene.");
            Debug.Log("[PREFABS] Gameplay prefabs and scene HUD are configured.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }

        private static GameObject GetOrCreatePrefab(string name, Func<GameObject> build)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject temporary = build();
            try { return PrefabUtility.SaveAsPrefabAsset(temporary, path); }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }

        private static Mesh GetMesh(string name, Func<Mesh> build)
        {
            string path = $"{ArtFolder}/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = build();
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh BuildQuad()
        {
            Mesh mesh = new()
            {
                vertices = new[] { new Vector3(-0.5f,-0.5f), new Vector3(-0.5f,0.5f),
                    new Vector3(0.5f,0.5f), new Vector3(0.5f,-0.5f) },
                triangles = new[] { 0,1,2,0,2,3 }
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Material GetMaterial(string name, Color color, bool transparent = false)
        {
            string path = $"{ArtFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = BoardView.CreateUnlitMaterial(color, name);
            if (transparent)
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void AddVisual(Transform parent, string name, Mesh mesh, Material material,
            Vector3 position, Vector3 scale, int order)
        {
            GameObject visual = new(name, typeof(MeshFilter), typeof(MeshRenderer));
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            visual.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static TextMeshProUGUI AddText(Transform parent, string name, string text,
            float fontSize, float top, Vector2 size, Color color, FontStyles style)
        {
            GameObject obj = new(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = size;
            label.text = text;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
