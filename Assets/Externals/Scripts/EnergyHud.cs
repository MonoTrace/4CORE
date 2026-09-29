using TMPro;
using UnityEngine;

namespace FourCore
{
    public sealed class EnergyHud : MonoBehaviour
    {
        [SerializeField] private Color textColor = new(0.2f, 0.72f, 1f, 1f);
        [SerializeField, Min(1f)] private float fontSize = 42f;

        private PlayerEnergyState energyState;
        private TextMeshProUGUI energyText;

        public void Initialize(PlayerEnergyState state)
        {
            energyState = state;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            UnityEngine.UI.CanvasScaler scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject textObject = new("EnergyText", typeof(RectTransform));
            textObject.transform.SetParent(transform, false);
            energyText = textObject.AddComponent<TextMeshProUGUI>();

            RectTransform rectTransform = energyText.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -40f);
            rectTransform.sizeDelta = new Vector2(440f, 80f);

            energyText.alignment = TextAlignmentOptions.Center;
            energyText.color = textColor;
            energyText.fontSize = fontSize;
            energyText.fontStyle = FontStyles.Bold;
            energyText.textWrappingMode = TextWrappingModes.NoWrap;
            energyText.raycastTarget = false;

            energyState.Changed += HandleEnergyChanged;
            HandleEnergyChanged(energyState.Current, energyState.Total);
        }

        private void HandleEnergyChanged(int current, int total)
        {
            energyText.text = $"ENERGY  {current} / {total}";
            Debug.Log($"[ENERGY] Current / Total: {current} / {total}");
        }

        private void OnDestroy()
        {
            if (energyState != null)
            {
                energyState.Changed -= HandleEnergyChanged;
            }
        }
    }
}
