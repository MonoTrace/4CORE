using System;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;

namespace FourCore
{
    public sealed class ScoreHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TextAnimator_TMP textAnimator;
        [Header("PIXELFX")]
        [Tooltip("PIXELFX clip played over the score number when it goes up (replaces the old scorepop text tag).")]
        [SerializeField] private string scoreGainClip = PixelFx.Keys.ScoreGain;
        [SerializeField, Min(0.1f)] private float scoreGainPixelScale = 1.5f;
        private ScoreState scoreState;
        private long displayedTotal;

        public void Initialize(ScoreState state)
        {
            if (scoreText == null) throw new InvalidOperationException("Assign the scene ScoreText to ScoreHud.");
            if (textAnimator == null || textAnimator.gameObject != scoreText.gameObject)
                throw new InvalidOperationException("Assign the ScoreText's Text Animator to ScoreHud.");
            if (scoreState != null) scoreState.Changed -= HandleScoreChanged;
            scoreState = state;
            scoreState.Changed += HandleScoreChanged;
            scoreText.gameObject.SetActive(scoreState.Enabled);
            displayedTotal = scoreState.Total;
            textAnimator.SetText($"SCORE  {displayedTotal}");
        }

        private void HandleScoreChanged(long total)
        {
            scoreText.gameObject.SetActive(scoreState.Enabled);
            if (!scoreState.Enabled) { displayedTotal = total; return; }
            bool increased = total > displayedTotal;
            displayedTotal = total;
            // Keep the label still, and show the latest number immediately (no typewriter queue).
            // The PIXELFX score-gain burst (PlayScoreGain) replaces the old TMP text pop.
            string number = total.ToString();
            Color color=total<1000?Color.Lerp(Color.white,new Color(1f,.87f,.2f),total/1000f):
                total<5000?Color.Lerp(new Color(1f,.87f,.2f),new Color(.25f,1f,.55f),(total-1000)/4000f):
                Color.Lerp(new Color(.25f,1f,.55f),new Color(.65f,.5f,1f),Mathf.Clamp01((total-5000)/5000f));
            number=$"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{number}</color>";
            textAnimator.SetText($"SCORE  {number}");
            textAnimator.time.RestartTime();
            if (increased) PlayScoreGain();
        }

        private void PlayScoreGain()
        {
            if (string.IsNullOrEmpty(scoreGainClip) || scoreText.GetComponentInParent<Canvas>() == null) return;
            RectTransform rect = scoreText.rectTransform;
            scoreText.ForceMeshUpdate();
            Bounds bounds = scoreText.textBounds;
            // Burst over the number at the end of "SCORE  1234", relative to the rect's centre.
            Vector2 centerOffset = rect.rect.center;
            Vector2 at = bounds.size.x > 0f
                ? new Vector2(bounds.max.x - Mathf.Min(bounds.size.x * 0.15f, 40f), bounds.center.y)
                : centerOffset;
            PixelFxImage.Play(scoreGainClip, rect, at - centerOffset, scoreGainPixelScale);
        }

        private void OnDestroy()
        {
            if (scoreState != null) scoreState.Changed -= HandleScoreChanged;
        }
    }
}
