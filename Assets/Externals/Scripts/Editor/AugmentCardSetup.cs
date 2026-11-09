using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class AugmentCardSetup
    {
        private static readonly Color Ice = new(.39f, .84f, 1f);
        private static readonly Color Gold = new(1f, .77f, .35f);
        private static readonly Color Violet = new(.77f, .57f, 1f);

        [MenuItem("4CORE/Style Augment Cards")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Apply(flow);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            EditorSceneManager.SaveScene(flow.gameObject.scene);
        }

        public static void Apply(GameFlowController flow)
        {
            SerializedObject config = new(flow);
            CanvasGroup group = config.FindProperty("rewardGroup").objectReferenceValue as CanvasGroup;
            if (group == null) throw new System.InvalidOperationException("Set up the reward menu first.");
            UnityEngine.UI.Button freeze = (UnityEngine.UI.Button)config.FindProperty("freezeReward").objectReferenceValue;
            UnityEngine.UI.Button shield = (UnityEngine.UI.Button)config.FindProperty("shieldReward").objectReferenceValue;
            UnityEngine.UI.Button repair = (UnityEngine.UI.Button)config.FindProperty("repairReward").objectReferenceValue;
            TMP_FontAsset font = shield.GetComponentInChildren<TMP_Text>(true).font;
            var title = group.transform.Find("Title").GetComponent<TMP_Text>();
            Place(title.rectTransform, group.transform, 0, 387, 1300, 82);
            title.fontSize = 54; title.color = new Color(.96f, .93f, .84f);
            var prompt = group.transform.Find("Prompt").GetComponent<TMP_Text>();
            Place(prompt.rectTransform, group.transform, 0, 307, 1450, 52);
            prompt.fontSize = 24; prompt.color = new Color(.60f, .66f, .77f);
            var backdrop = group.transform.Find("Backdrop").GetComponent<UnityEngine.UI.Image>();
            Undo.RecordObject(backdrop, "Darken augment backdrop");
            backdrop.color = new Color(.008f, .012f, .028f, .975f);
            Text(group.transform, "AugmentEyebrow", "A U G M E N T S", font, 20, 0, 452, 900, 30, Gold);
            Transform footer = group.transform.Find("FooterHint");
            if (footer != null) { footer.gameObject.SetActive(false); EditorUtility.SetDirty(footer.gameObject); }

            Card(group.transform, freeze, "FreezeDescription", AugmentType.LongerFreeze, Ice, -460, "UTILITY  /  01", font);
            Card(group.transform, shield, "ShieldDescription", AugmentType.ShieldEnergy, Gold, 0, "DEFENSE  /  02", font);
            Card(group.transform, repair, "RepairDescription", AugmentType.CoreRecovery, Violet, 460, "RECOVERY  /  03", font);
            UnityEngine.UI.Button[] buttons = { freeze, shield, repair };
            for (int i = 0; i < buttons.Length; i++)
            {
                var navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? buttons[i - 1] : null, selectOnRight = i < 2 ? buttons[i + 1] : null };
                buttons[i].navigation = navigation;
                EditorUtility.SetDirty(buttons[i]);
            }
        }

        private static void Card(Transform root, UnityEngine.UI.Button button, string descriptionName,
            AugmentType type, Color accent, float x, string category, TMP_FontAsset font)
        {
            Undo.RecordObject(button, "Style augment card");
            RectTransform rect = (RectTransform)button.transform;
            Place(rect, root, x, -35, 390, 570);
            // Keep the existing Image as the Button's full-card hit target.
            var image = button.GetComponent<UnityEngine.UI.Image>();
            Undo.RecordObject(image, "Set augment hit area"); image.color = Color.clear; image.raycastTarget = true;
            button.transition = UnityEngine.UI.Selectable.Transition.None;

            RectTransform artRect = Child(rect, "Artwork");
            artRect.SetAsFirstSibling();
            artRect.anchorMin = Vector2.zero; artRect.anchorMax = Vector2.one;
            artRect.offsetMin = artRect.offsetMax = Vector2.zero;
            if (artRect.GetComponent<CanvasRenderer>() == null) Undo.AddComponent<CanvasRenderer>(artRect.gameObject);
            var art = artRect.GetComponent<AugmentCardArtwork>();
            if (art == null) art = Undo.AddComponent<AugmentCardArtwork>(artRect.gameObject);
            art.raycastTarget = false; art.Configure(type, accent); EditorUtility.SetDirty(art);

            TMP_Text name = rect.Find("Label").GetComponent<TMP_Text>();
            Place(name.rectTransform, rect, 0, -45, 344, 82);
            name.fontSize = 30; name.fontStyle = FontStyles.Bold; name.color = new Color(.96f, .96f, 1f);
            name.raycastTarget = false; name.textWrappingMode = TextWrappingModes.Normal;
            TMP_Text description = FindDescription(root, descriptionName);
            Place(description.rectTransform, rect, 0, -165, 344, 108);
            description.fontSize = 22; description.lineSpacing = 4;
            description.color = new Color(.69f, .74f, .84f); description.raycastTarget = false;
            Text(rect, "Category", category, font, 18, 0, 236, 340, 30, accent);
            TMP_Text hint = rect.Find("SelectionHint")?.GetComponent<TMP_Text>();
            if (hint != null) { hint.gameObject.SetActive(false); EditorUtility.SetDirty(hint.gameObject); }
            var hover = button.GetComponent<AugmentCardView>();
            if (hover == null) hover = Undo.AddComponent<AugmentCardView>(button.gameObject);
            hover.Configure(art, hint); EditorUtility.SetDirty(hover);
            EditorUtility.SetDirty(name); EditorUtility.SetDirty(description); EditorUtility.SetDirty(image);
        }

        private static TMP_Text FindDescription(Transform root, string name)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                if (text.name == name) return text;
            throw new System.InvalidOperationException("Missing augment description: " + name);
        }
        private static RectTransform Child(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;
            GameObject obj = new(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, "Add augment decoration");
            obj.transform.SetParent(parent, false); return (RectTransform)obj.transform;
        }
        private static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font,
            int size, float x, float y, float w, float h, Color color)
        {
            RectTransform rect = Child(parent, name);
            var text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null) text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            Undo.RecordObject(text, "Set augment label");
            Place(rect, parent, x, y, w, h);
            text.font = font; text.fontSize = size; text.text = value; text.color = color;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            EditorUtility.SetDirty(text); return text;
        }
        private static void Place(RectTransform rect, Transform parent, float x, float y, float w, float h)
        {
            Undo.RecordObject(rect, "Layout augment card");
            if (rect.parent != parent) Undo.SetTransformParent(rect, parent, "Move description into card");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
            rect.localScale = Vector3.one; EditorUtility.SetDirty(rect);
        }
    }
}
