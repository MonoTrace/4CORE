using UnityEngine;

namespace FourCore
{
    public sealed class CoreLinkView : MonoBehaviour
    {
        private enum LinkPhase { Connected, Breaking, Disconnected, Reconnecting }
        private const int WavePointCount = 21;
        [SerializeField] private LineRenderer[] lines;
        [SerializeField] private Color coreColor = new(1f, 0.1f, 0.15f, 0.38f);
        [SerializeField] private Color playerColor = new(0.05f, 0.65f, 1f, 0.38f);
        [SerializeField, Min(0f)] private float waveAmplitude = 0.075f;
        [SerializeField, Min(0.1f)] private float waveCycles = 2.25f;
        [SerializeField, Min(0f)] private float waveSpeed = 1.8f;
        [SerializeField, Min(0f)] private float secondaryWaveAmplitude = 0.022f;
        [SerializeField, Min(0.01f)] private float zoomInDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float damageBlinkInterval = 0.04f;
        [SerializeField, Min(0.01f)] private float breakFadeDuration = 0.04f;
        [SerializeField, Min(0.01f)] private float zoomOutDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float reconnectDuration = 0.5f;
        private CoreView coreView;
        private Transform player;
        private PlayerController controls;
        private bool[] alive;
        private LinkPhase[] phases;
        private float[] ages;
        private float visibility = 1f;
        private float stopRemaining;
        private float previousTimeScale;
        private bool ownsHitStop;
        private GameFlowController gameFlow;
        private float damageSequenceAge;
        private float BreakDuration => zoomInDuration + damageBlinkInterval * 5f + breakFadeDuration;

        public void Initialize(CoreState state, CoreView view, Transform player, PlayerController controls,
            GameFlowController gameFlow = null)
        {
            if (lines == null || lines.Length != state.CellCount)
                throw new System.InvalidOperationException("Assign one connection line per core cell.");
            coreView = view; this.player = player; this.controls = controls;
            this.gameFlow = gameFlow;
            alive = new bool[state.CellCount]; phases = new LinkPhase[state.CellCount]; ages = new float[state.CellCount];
            foreach (LineRenderer line in lines)
            {
                if (line == null)
                    throw new System.InvalidOperationException("A CoreLink line reference is missing.");
                line.useWorldSpace = true;
                line.positionCount = WavePointCount;
                line.textureMode = LineTextureMode.Stretch;
                line.alignment = LineAlignment.View;
                line.numCornerVertices = 4;
                line.numCapVertices = 4;
                line.sortingOrder = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.widthMultiplier = 0.12f;
                line.widthCurve = new AnimationCurve(
                    new Keyframe(0f, 0.72f),
                    new Keyframe(0.18f, 0.92f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(0.82f, 0.92f),
                    new Keyframe(1f, 0.72f));
            }
            coreView.Applied += Sync;
            Sync(state, false);
        }

        public void SetVisibility(float value) => visibility = Mathf.Clamp01(value);

        private void Sync(CoreState state, bool animate)
        {
            if (!animate) ReleaseHitStop();
            for (int i = 0; i < alive.Length; i++)
            {
                bool next = state.IsAlive(i);
                if (!animate)
                {
                    phases[i] = next ? LinkPhase.Connected : LinkPhase.Disconnected;
                    ages[i] = 0f;
                }
                else if (next != alive[i])
                {
                    ages[i] = 0f;
                    phases[i] = next ? LinkPhase.Reconnecting : LinkPhase.Breaking;
                    if (!next) BeginHitStop();
                }
                alive[i] = next;
            }
            RenderLines();
        }

        private void BeginHitStop()
        {
            if (!ownsHitStop)
            {
                previousTimeScale = Time.timeScale;
                ownsHitStop = true;
                Time.timeScale = 0f;
                controls.SetPresentationInputLock(true);
                damageSequenceAge = 0f;
                gameFlow?.BeginCoreDamageFocus();
            }
            stopRemaining = BreakDuration + zoomOutDuration;
        }

        private void ReleaseHitStop()
        {
            if (!ownsHitStop) return;
            gameFlow?.EndCoreDamageFocus();
            Time.timeScale = previousTimeScale;
            ownsHitStop = false;
            stopRemaining = 0f;
            if (controls != null) controls.SetPresentationInputLock(false);
        }

        private void LateUpdate()
        {
            if (alive == null) return;
            float dt = Time.unscaledDeltaTime;
            if (ownsHitStop)
            {
                stopRemaining -= dt;
                damageSequenceAge += dt;
                float focus = damageSequenceAge < zoomInDuration
                    ? damageSequenceAge / zoomInDuration
                    : 1f - Mathf.Clamp01((damageSequenceAge - BreakDuration) / zoomOutDuration);
                gameFlow?.SetCoreDamageFocus(focus);
                if (stopRemaining <= 0f) ReleaseHitStop();
            }
            for (int i = 0; i < ages.Length; i++)
            {
                ages[i] += dt;
                if (phases[i] == LinkPhase.Breaking && ages[i] >= BreakDuration)
                    phases[i] = LinkPhase.Disconnected;
                if (phases[i] == LinkPhase.Reconnecting && ages[i] >= reconnectDuration)
                    phases[i] = LinkPhase.Connected;
            }
            RenderLines();
        }

        private void RenderLines()
        {
            for (int i = 0; i < lines.Length; i++)
            {
                bool visible = phases[i] != LinkPhase.Disconnected && visibility > 0f;
                float progress = 1f;
                float damageOpacity = 1f;
                if (phases[i] == LinkPhase.Breaking)
                {
                    float blinkAge = ages[i] - zoomInDuration;
                    // Keep the link visible during zoom-in, then blink three times.
                    if (blinkAge >= 0f && blinkAge < damageBlinkInterval * 5f)
                        visible &= Mathf.FloorToInt(blinkAge / damageBlinkInterval) % 2 == 0;
                    if (blinkAge >= damageBlinkInterval * 5f)
                        damageOpacity = 1f - Mathf.Clamp01((blinkAge - damageBlinkInterval * 5f) / breakFadeDuration);
                }
                if (phases[i] == LinkPhase.Reconnecting)
                {
                    progress = Mathf.SmoothStep(0f, 1f, ages[i] / reconnectDuration);
                }
                LineRenderer line = lines[i];
                line.enabled = visible;
                Vector3 from = coreView.GetCellTransform(i).position;
                Vector3 to = player.position;
                // Keep the ribbon in front of board geometry and behind the player sprite.
                from.z = to.z = -0.2f;
                Vector3 lineEnd = Vector3.Lerp(from, to, progress);
                Vector3 direction = lineEnd - from;
                Vector3 perpendicular = direction.sqrMagnitude > 0.0001f
                    ? new Vector3(-direction.y, direction.x, 0f).normalized
                    : Vector3.up;
                float time = Time.unscaledTime * waveSpeed;
                for (int pointIndex = 0; pointIndex < WavePointCount; pointIndex++)
                {
                    float t = pointIndex / (float)(WavePointCount - 1);
                    Vector3 point = Vector3.Lerp(from, lineEnd, t);
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    float phase = t * waveCycles * Mathf.PI * 2f - time;
                    float wave = Mathf.Sin(phase) * waveAmplitude
                        + Mathf.Sin(phase * 0.53f + time * 0.37f) * secondaryWaveAmplitude;
                    point += perpendicular * (wave * envelope);
                    line.SetPosition(pointIndex, point);
                }
                Color start = coreColor, end = playerColor;
                start.a *= visibility * damageOpacity; end.a *= visibility * damageOpacity;
                line.startColor = start; line.endColor = end;
            }
        }

        private void OnDisable() => ReleaseHitStop();
        private void OnDestroy()
        {
            ReleaseHitStop();
            if (coreView != null) coreView.Applied -= Sync;
        }
    }
}
