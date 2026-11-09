using UnityEngine;
using UnityEngine.EventSystems;

namespace FourCore
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class AugmentCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private AugmentCardArtwork artwork;
        [SerializeField] private TMPro.TMP_Text hint;
        private RectTransform rect;
        private UnityEngine.UI.Button button;
        private Vector2 restingPosition;
        private bool pointerOver, focused, pressed;
        private float emphasis;
        private CanvasGroup presentationGroup;
        private bool presenting;

        public void Configure(AugmentCardArtwork graphic, TMPro.TMP_Text footer) { artwork = graphic; hint = footer; }
        private void Awake()
        {
            rect = (RectTransform)transform; button = GetComponent<UnityEngine.UI.Button>(); restingPosition = rect.anchoredPosition;
            presentationGroup = GetComponent<CanvasGroup>();
            if (presentationGroup == null) presentationGroup = gameObject.AddComponent<CanvasGroup>();
            if (hint != null) hint.gameObject.SetActive(false);
        }
        private void OnEnable() { if (rect != null) ResetVisual(); }
        private void OnDisable() { if (rect != null) ResetVisual(); }
        private void ResetVisual()
        {
            pointerOver = focused = pressed = false; emphasis = 0;
            presenting = false;
            if (presentationGroup != null) presentationGroup.alpha = 1f;
            rect.anchoredPosition = restingPosition; rect.localScale = Vector3.one;
            artwork?.SetEmphasis(0);
            if (hint != null) hint.alpha = .38f;
        }
        private void Update()
        {
            if (presenting) return;
            bool active = button.IsInteractable() && (pointerOver || focused);
            emphasis = Mathf.Lerp(emphasis, active ? 1f : 0f, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 13f));
            rect.anchoredPosition = restingPosition + Vector2.up * (18f * emphasis);
            float scale = 1f + .045f * emphasis - (pressed ? .025f : 0f);
            rect.localScale = Vector3.one * scale;
            artwork?.SetEmphasis(emphasis);
            if (hint != null) hint.alpha = Mathf.Lerp(.38f, 1f, emphasis);
        }
        public void Present(float opacity, float scale, float lift, float highlight)
        {
            PresentAt(opacity, scale, restingPosition + Vector2.up * lift, highlight);
        }
        public Vector2 RestingPosition => restingPosition;
        public void PresentAt(float opacity, float scale, Vector2 position, float highlight)
        {
            presenting = true;
            presentationGroup.alpha = opacity;
            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = position;
            artwork?.SetEmphasis(highlight);
        }
        public void EndPresentation() => ResetVisual();
        public void OnPointerEnter(PointerEventData e)
        {
            pointerOver = true;
            // Pointer hover replaces the default keyboard focus, avoiding two lit cards.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        public void OnPointerExit(PointerEventData e) { pointerOver = pressed = false; }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        public void OnSelect(BaseEventData e) { focused = true; }
        public void OnDeselect(BaseEventData e) { focused = false; }
    }
}
