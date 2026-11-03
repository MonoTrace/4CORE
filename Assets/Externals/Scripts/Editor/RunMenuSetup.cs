using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class RunMenuSetup
    {
        private const string FontPath="Assets/Externals/Datas/PrototypeVisuals/KoreanUI.asset";
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)
        {SerializedObject config=new(target);config.FindProperty(field).objectReferenceValue=value;config.ApplyModifiedPropertiesWithoutUndo();}
        private static TMP_FontAsset KoreanFont()
        {
            TMP_FontAsset font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);if(font!=null)return font;
            font=TMP_FontAsset.CreateFontAsset("Malgun Gothic","Regular",48);
            if(font==null)throw new InvalidOperationException("Malgun Gothic system font required for Korean UI.");
            font.name="Korean UI (System)";font.isMultiAtlasTexturesEnabled=false;
            SerializedObject fontSettings=new(font);fontSettings.FindProperty("m_ClearDynamicDataOnBuild").boolValue=true;fontSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(font,FontPath);AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(Texture2D texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
            return font;
        }
        private static GameObject CanvasRoot(string name,int order)
        {
            GameObject root=new(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root,"Create "+name);root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=order;
            UnityEngine.UI.CanvasScaler scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            GameObject backdrop=new("Backdrop",typeof(RectTransform),typeof(UnityEngine.UI.Image));backdrop.transform.SetParent(root.transform,false);
            RectTransform rect=backdrop.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            backdrop.GetComponent<UnityEngine.UI.Image>().color=new Color(.006f,.014f,.03f,.94f);return root;
        }
        private static TMP_Text Label(Transform parent,string name,string value,int size,float x,float y,float width,float height,TMP_FontAsset font)
        {
            GameObject obj=new(name,typeof(RectTransform));obj.transform.SetParent(parent,false);TextMeshProUGUI text=obj.AddComponent<TextMeshProUGUI>();
            text.font=font;text.text=value;text.fontSize=size;text.color=Color.white;text.raycastTarget=false;text.enableAutoSizing=false;
            text.textWrappingMode=TextWrappingModes.Normal;text.alignment=TextAlignmentOptions.Center;
            RectTransform rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);return text;
        }
        private static UnityEngine.UI.Button Button(Transform parent,string name,string value,float x,float y,TMP_FontAsset font)
        {
            GameObject obj=new(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));obj.transform.SetParent(parent,false);
            RectTransform rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(420,85);
            UnityEngine.UI.Image image=obj.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.025f,.09f,.17f,.98f);
            UnityEngine.UI.Button button=obj.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;Label(obj.transform,"Label",value,28,0,0,400,80,font).fontStyle=FontStyles.Bold;return button;
        }
        [MenuItem("4CORE/Set Up Reward Menu")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            GameBootstrap boot=UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();GameFlowController flow=boot.GetComponent<GameFlowController>();
            TMP_FontAsset font=KoreanFont();SerializedObject flowConfig=new(flow);
            if(flowConfig.FindProperty("resultScore").objectReferenceValue==null)
            {
                CanvasGroup over=flowConfig.FindProperty("gameOverGroup").objectReferenceValue as CanvasGroup;
                TMP_Text result=Label(over.transform.Find("Menu"),"ResultScore","SCORE 0     HIGH 0",23,0,65,800,32,TMP_Settings.defaultFontAsset);
                Assign(flow,"resultScore",result);
            }
            if(flowConfig.FindProperty("rewardGroup").objectReferenceValue==null)
            {
                GameObject reward=CanvasRoot("RewardScreen",290);
                Label(reward.transform,"Title","STAGE CLEAR",58,0,270,1000,90,font).fontStyle=FontStyles.Bold;
                Label(reward.transform,"Prompt","증강 하나를 선택하세요 · 이번 플레이 동안 효과가 누적됩니다",28,0,175,1400,65,font);
                UnityEngine.UI.Button freeze=Button(reward.transform,"FreezeReward","시간 정지 +2턴",-470,15,font);
                UnityEngine.UI.Button shield=Button(reward.transform,"ShieldReward","쉴드 에너지 +1",0,15,font);
                UnityEngine.UI.Button repair=Button(reward.transform,"RepairReward","코어 회복 속도 +1",470,15,font);
                Label(reward.transform,"FreezeDescription","시간 정지 지속 시간 증가\n10턴 → 12턴",25,-470,-90,410,95,font);
                Label(reward.transform,"ShieldDescription","에너지·쉴드 설치 한도 +1\n누적 추가 에너지 +0 → +1",25,0,-90,410,95,font);
                Label(reward.transform,"RepairDescription","이동할 때마다 코어 회복량 +1\n회복량 1 → 2",25,470,-90,410,95,font);
                Assign(flow,"rewardGroup",reward.GetComponent<CanvasGroup>());Assign(flow,"freezeReward",freeze);Assign(flow,"shieldReward",shield);Assign(flow,"repairReward",repair);reward.SetActive(false);
            }
            AugmentCardSetup.Apply(flow);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(boot.gameObject.scene);EditorSceneManager.SaveScene(boot.gameObject.scene);
        }
    }
}
