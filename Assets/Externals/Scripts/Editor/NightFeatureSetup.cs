using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class NightFeatureSetup
    {
        private const string Art = "Assets/Externals/Datas/PrototypeVisuals/";
        private const string Prefabs = "Assets/Externals/Prefabs/Gameplay/";
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)
        {
            SerializedObject config = new(target); config.FindProperty(field).objectReferenceValue = value; config.ApplyModifiedPropertiesWithoutUndo();
        }
        private static SpriteRenderer BuildPickup(PickupType type)
        {
            string path = Prefabs + type + "Pickup.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<SpriteRenderer>();
            Texture2D texture = new(32,32,TextureFormat.RGBA32,false) { name = type + "Icon",filterMode=FilterMode.Point };
            Color[] pixels = new Color[1024];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                int dx = x-16, dy=y-16;
                bool on = type switch
                {
                    PickupType.TimeFreeze => (Mathf.Abs(dy)==10 && Mathf.Abs(dx)<=9) || (Mathf.Abs(dx)==Mathf.Abs(dy)*7/10 && Mathf.Abs(dy)<=9),
                    PickupType.PersonalShield => Mathf.Abs(dx)+Mathf.Abs(dy)>=10 && Mathf.Abs(dx)+Mathf.Abs(dy)<=13,
                    _ => (Mathf.Abs(dx)<=2 && Mathf.Abs(dy)<=11) || (Mathf.Abs(dy)<=2 && Mathf.Abs(dx)<=11)
                };
                pixels[y*32+x] = on ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels); texture.Apply(); AssetDatabase.CreateAsset(texture,Art+type+"Icon.asset");
            Sprite sprite = Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),32f,0,SpriteMeshType.FullRect);
            sprite.name=type+"Icon"; AssetDatabase.AddObjectToAsset(sprite,texture);
            GameObject obj = new(type+"Pickup",typeof(SpriteRenderer));
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite=sprite;
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"PathCell.mat"); renderer.sortingOrder=7;
            renderer.color=type switch { PickupType.TimeFreeze => new Color(.72f,.4f,1f),
                PickupType.PersonalShield => new Color(.8f,.75f,1f), _=>new Color(.05f,.65f,1f) };
            obj.transform.localScale=Vector3.one*.72f;
            GameObject prefab=PrefabUtility.SaveAsPrefabAsset(obj,path); UnityEngine.Object.DestroyImmediate(obj);
            return prefab.GetComponent<SpriteRenderer>();
        }
        public static void UpdatePickupPrefabs()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            SpriteRenderer[] prefabs = { BuildPickup(PickupType.TimeFreeze), BuildPickup(PickupType.PersonalShield), BuildPickup(PickupType.Repair) };
            string path=Prefabs+"PickupView.prefab";
            GameObject root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var config=new SerializedObject(root.GetComponent<PickupView>());var array=config.FindProperty("pickupPrefabs");
                array.arraySize=prefabs.Length;
                for(int i=0;i<prefabs.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=prefabs[i];
                config.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
        private static TMP_Text WorldText(Transform parent,string name)
        {
            GameObject obj=new(name); obj.transform.SetParent(parent,false);
            TextMeshPro text=obj.AddComponent<TextMeshPro>(); text.font=TMP_Settings.defaultFontAsset;
            text.fontSize=4f; text.alignment=TextAlignmentOptions.Center; text.fontStyle=FontStyles.Bold;
            text.rectTransform.sizeDelta=new Vector2(.7f,.7f); text.GetComponent<MeshRenderer>().sortingOrder=12;
            text.transform.localPosition=new Vector3(0f,0f,-.2f); return text;
        }
        private static UnityEngine.UI.Image ScreenImage(Transform parent,string name)
        {
            GameObject obj=new(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent,false);
            RectTransform rect=obj.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            UnityEngine.UI.Image image=obj.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget=false; obj.transform.SetAsFirstSibling(); return image;
        }
        [MenuItem("4CORE/Set Up Run Features")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before setup.");
            GameBootstrap boot=UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            if(boot==null)throw new InvalidOperationException("Gameplay scene required.");
            Mesh playerMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"PlayerTriangle.asset");
            if(playerMesh!=null&&playerMesh.vertexCount==3)
            {
                Vector3[] vertices=playerMesh.vertices;Vector3 center=(vertices[0]+vertices[1]+vertices[2])/3f;
                if(center.sqrMagnitude>.000001f)
                {
                    Undo.RecordObject(playerMesh,"Center player triangle");
                    for(int i=0;i<vertices.Length;i++)vertices[i]-=center;
                    playerMesh.vertices=vertices;playerMesh.RecalculateBounds();EditorUtility.SetDirty(playerMesh);
                }
            }
            SpriteRenderer[] pickupPrefabs={BuildPickup(PickupType.TimeFreeze),BuildPickup(PickupType.PersonalShield),BuildPickup(PickupType.Repair)};
            string pickupPath=Prefabs+"PickupView.prefab";
            GameObject pickupRoot=AssetDatabase.LoadAssetAtPath<GameObject>(pickupPath);
            if(pickupRoot==null)
            {
                GameObject temporary=new("Pickups",typeof(PickupView));
                SerializedObject config=new(temporary.GetComponent<PickupView>()); SerializedProperty array=config.FindProperty("pickupPrefabs");
                array.arraySize=3;for(int i=0;i<3;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=pickupPrefabs[i];
                config.ApplyModifiedPropertiesWithoutUndo(); pickupRoot=PrefabUtility.SaveAsPrefabAsset(temporary,pickupPath); UnityEngine.Object.DestroyImmediate(temporary);
            }
            UpdatePickupPrefabs();
            string bossPath=Prefabs+"FirstBoss.prefab";
            GameObject bossRoot=AssetDatabase.LoadAssetAtPath<GameObject>(bossPath);
            if(bossRoot==null)
            {
                Material material=BoardView.CreateUnlitMaterial(new Color(1f,.25f,.5f),"Boss"); AssetDatabase.CreateAsset(material,Art+"Boss.mat");
                GameObject temporary=new("FirstBoss",typeof(BossView)); SerializedObject config=new(temporary.GetComponent<BossView>());
                SerializedProperty cells=config.FindProperty("cellRenderers"),labels=config.FindProperty("healthLabels");cells.arraySize=labels.arraySize=4;
                for(int i=0;i<4;i++)
                {
                    GameObject cell=new("Cell "+(i+1),typeof(MeshFilter),typeof(MeshRenderer));cell.transform.SetParent(temporary.transform,false);
                    cell.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"Quad.asset");
                    MeshRenderer renderer=cell.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=8;
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    cells.GetArrayElementAtIndex(i).objectReferenceValue=renderer;
                    labels.GetArrayElementAtIndex(i).objectReferenceValue=WorldText(cell.transform,"Health");
                }
                SpriteRenderer pathCell=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"PathCell.prefab").GetComponent<SpriteRenderer>();
                config.FindProperty("projectilePrefab").objectReferenceValue=pathCell;config.FindProperty("pathCellPrefab").objectReferenceValue=pathCell;
                config.ApplyModifiedPropertiesWithoutUndo();bossRoot=PrefabUtility.SaveAsPrefabAsset(temporary,bossPath);UnityEngine.Object.DestroyImmediate(temporary);
            }
            // Upgrade only the additional cell; preserve existing prefab edits.
            GameObject edit=PrefabUtility.LoadPrefabContents(bossPath);
            try
            {
                SerializedObject config=new(edit.GetComponent<BossView>());
                SerializedProperty cells=config.FindProperty("cellRenderers"),labels=config.FindProperty("healthLabels");
                if(cells.arraySize<5)
                {
                    GameObject cell=new("Cell 5",typeof(MeshFilter),typeof(MeshRenderer));cell.transform.SetParent(edit.transform,false);
                    cell.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"Quad.asset");
                    MeshRenderer renderer=cell.GetComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"Boss.mat");renderer.sortingOrder=8;
                    cells.arraySize=labels.arraySize=5;cells.GetArrayElementAtIndex(4).objectReferenceValue=renderer;
                    labels.GetArrayElementAtIndex(4).objectReferenceValue=WorldText(cell.transform,"Health");
                    config.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(edit,bossPath);
                }
            }
            finally{PrefabUtility.UnloadPrefabContents(edit);}
            Assign(boot,"pickupViewPrefab",pickupRoot.GetComponent<PickupView>());Assign(boot,"bossViewPrefab",bossRoot.GetComponent<BossView>());
            ScoreHud hud=UnityEngine.Object.FindAnyObjectByType<ScoreHud>();Canvas canvas=hud.GetComponentInParent<Canvas>();
            RunPresentation presentation=UnityEngine.Object.FindAnyObjectByType<RunPresentation>();
            if(presentation==null)
            {
                GameObject obj=new("RunFeedback");presentation=obj.AddComponent<RunPresentation>();Undo.RegisterCreatedObjectUndo(obj,"Add run feedback");
                GameObject labelObj=new("EncounterNotice",typeof(RectTransform));labelObj.transform.SetParent(canvas.transform,false);
                TextMeshProUGUI label=labelObj.AddComponent<TextMeshProUGUI>();label.font=TMP_Settings.defaultFontAsset;
                label.fontSize=68f;label.fontStyle=FontStyles.Bold;label.color=new Color(.75f,.7f,1f);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
                label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(.5f,.5f);label.rectTransform.anchoredPosition=Vector2.zero;label.rectTransform.sizeDelta=new Vector2(900f,120f);
                Assign(presentation,"noticeText",label);
                TMP_Text counter=WorldText(obj.transform,"FreezeMoves");counter.fontSize=2.8f;Assign(presentation,"freezeCount",counter);
                GameObject shieldObj=new("PersonalShield",typeof(LineRenderer));shieldObj.transform.SetParent(obj.transform,false);
                LineRenderer ring=shieldObj.GetComponent<LineRenderer>();ring.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"CoreLink.mat");
                ring.startWidth=ring.endWidth=.04f;ring.startColor=ring.endColor=new Color(.8f,.75f,1f,.65f);ring.sortingOrder=9;
                Assign(presentation,"personalShield",ring);
                UnityEngine.UI.Image tint=ScreenImage(canvas.transform,"FreezeTint");tint.color=Color.clear;Assign(presentation,"freezeTint",tint);
                UnityEngine.UI.Image progress=ScreenImage(canvas.transform,"EncounterProgress");progress.color=new Color(.15f,.25f,.4f,.035f);
                // Filled UI requires a sprite; reuse the existing tiny white path texture.
                progress.sprite=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"PathCell.prefab").GetComponent<SpriteRenderer>().sprite;
                progress.type=UnityEngine.UI.Image.Type.Filled;progress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;progress.fillAmount=0f;
                Assign(presentation,"progress",progress);
            }
            Assign(boot,"runPresentation",presentation);
            string freezeMaterialPath=Art+"FreezeRipple.mat";
            Material freezeMaterial=AssetDatabase.LoadAssetAtPath<Material>(freezeMaterialPath);
            if(freezeMaterial==null)
            {
                freezeMaterial=new Material(Shader.Find("4CORE/Freeze Ripple")){name="Freeze Ripple"};
                AssetDatabase.CreateAsset(freezeMaterial,freezeMaterialPath);
            }
            SerializedObject feedbackConfig=new(presentation);
            UnityEngine.UI.Image freezeImage=feedbackConfig.FindProperty("freezeTint").objectReferenceValue as UnityEngine.UI.Image;
            freezeImage.material=freezeMaterial;
            string ambientPath=Prefabs+"Ambient.prefab";
            GameObject ambient=AssetDatabase.LoadAssetAtPath<GameObject>(ambientPath);
            if(ambient==null)
            {
                GameObject temporary=new("Ambient",typeof(AmbientView));
                Assign(temporary.GetComponent<AmbientView>(),"particlePrefab",AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"PathCell.prefab").GetComponent<SpriteRenderer>());
                ambient=PrefabUtility.SaveAsPrefabAsset(temporary,ambientPath);UnityEngine.Object.DestroyImmediate(temporary);
            }
            Assign(boot,"ambientPrefab",ambient.GetComponent<AmbientView>());
            foreach(EnemyType type in Enum.GetValues(typeof(EnemyType)))
            {
                string enemyPath=Prefabs+"Enemies/"+type+"Enemy.prefab";GameObject enemy=PrefabUtility.LoadPrefabContents(enemyPath);
                try
                {
                    SerializedObject config=new(enemy.GetComponent<EnemyView>());
                    if(config.FindProperty("armorOutline").objectReferenceValue==null)
                    {
                        GameObject child=new("Armor",typeof(LineRenderer));child.transform.SetParent(enemy.transform,false);
                        LineRenderer line=child.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=5;
                        line.SetPositions(new[]{new Vector3(-.4f,-.4f,-.03f),new Vector3(-.4f,.4f,-.03f),new Vector3(.4f,.4f,-.03f),new Vector3(.4f,-.4f,-.03f),new Vector3(-.4f,-.4f,-.03f)});
                        line.startWidth=line.endWidth=.035f;line.startColor=line.endColor=new Color(1f,.93f,.75f,.75f);line.sortingOrder=9;
                        line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"CoreLink.mat");line.enabled=false;
                        config.FindProperty("armorOutline").objectReferenceValue=line;config.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(enemy,enemyPath);
                    }
                }
                finally{PrefabUtility.UnloadPrefabContents(enemy);}
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(boot.gameObject.scene);EditorSceneManager.SaveScene(boot.gameObject.scene);
        }
    }
}
