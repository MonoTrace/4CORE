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
            displayedTotal = scoreState.Total;
            textAnimator.SetText($"SCORE  {displayedTotal}");
        }

        private void HandleScoreChanged(long total)
        {
            bool increased = total > displayedTotal;
            displayedTotal = total;
            // Keep the label still, and show the latest number immediately (no typewriter queue).
            string number = increased && !string.IsNullOrWhiteSpace(scoreGainEffect)
                ? $"<{scoreGainEffect}>{total}</{scoreGainEffect}>"
                : total.ToString();
            textAnimator.SetText($"SCORE  {number}");
            textAnimator.time.RestartTime();
        }

        private void OnDestroy()
        {
            if (scoreState != null) scoreState.Changed -= HandleScoreChanged;
        }
    }
}
