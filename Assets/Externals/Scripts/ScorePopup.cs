using TMPro;
using UnityEngine;

namespace FourCore
{
    public sealed class ScorePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color coreColor = new(1f, 0.075f, 0.095f);
        [SerializeField, Min(0.1f)] private float lifetime = 1.3f;
        [SerializeField, Min(0f)] private float riseDistance = 0.85f;
        private Vector3 startPosition;
        private Vector3 baseScale;
        private float age;

        public void Show(int baseScore, int coreCount)
        {
            startPosition = transform.localPosition;
            baseScale = transform.localScale;
            age = 0f;
            label.text = $"{baseScore} x <color=#{ColorUtility.ToHtmlStringRGB(coreColor)}>{coreCount}</color>";
            label.alpha = 1f;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(age / lifetime);
            transform.localPosition = startPosition + Vector3.up * (riseDistance * progress);
            transform.localScale = baseScale * (1f + 0.15f * Mathf.Sin(Mathf.Clamp01(age / 0.22f) * Mathf.PI));
            label.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, progress));
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
