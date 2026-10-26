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
        [SerializeField] private string scoreGainEffect = "scorepop";
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
            string number = increased && !string.IsNullOrWhiteSpace(scoreGainEffect)
                ? $"<{scoreGainEffect}>{total}</{scoreGainEffect}>"
                : total.ToString();
            Color color=total<1000?Color.Lerp(Color.white,new Color(1f,.87f,.2f),total/1000f):
                total<5000?Color.Lerp(new Color(1f,.87f,.2f),new Color(.25f,1f,.55f),(total-1000)/4000f):
                Color.Lerp(new Color(.25f,1f,.55f),new Color(.65f,.5f,1f),Mathf.Clamp01((total-5000)/5000f));
            number=$"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{number}</color>";
            textAnimator.SetText($"SCORE  {number}");
            textAnimator.time.RestartTime();
        }

        private void OnDestroy()
        {
            if (scoreState != null) scoreState.Changed -= HandleScoreChanged;
        }
    }
}
