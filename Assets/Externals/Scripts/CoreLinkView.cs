using UnityEngine;

namespace FourCore
{
    public sealed class CoreLinkView : MonoBehaviour
    {
        private enum LinkPhase { Connected, Breaking, Disconnected, Reconnecting }
        [SerializeField] private LineRenderer[] lines;
        [SerializeField] private Color coreColor = new(1f, 0.1f, 0.15f, 0.45f);
        [SerializeField] private Color playerColor = new(0.05f, 0.65f, 1f, 0.55f);
        [SerializeField, Min(0.01f)] private float hitStopDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float blinkInterval = 0.1f;
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

        public void Initialize(CoreState state, CoreView view, Transform player, PlayerController controls)
        {
            if (lines == null || lines.Length != state.CellCount)
                throw new System.InvalidOperationException("Assign one connection line per core cell.");
            coreView = view; this.player = player; this.controls = controls;
            alive = new bool[state.CellCount]; phases = new LinkPhase[state.CellCount]; ages = new float[state.CellCount];
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
            }
            stopRemaining = hitStopDuration;
        }

        private void ReleaseHitStop()
        {
            if (!ownsHitStop) return;
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
                if (stopRemaining <= 0f) ReleaseHitStop();
            }
            for (int i = 0; i < ages.Length; i++)
            {
                ages[i] += dt;
                if (phases[i] == LinkPhase.Breaking && ages[i] >= hitStopDuration + blinkInterval * 6f)
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
                bool bright = false;
                if (phases[i] == LinkPhase.Breaking)
                {
                    float blinkAge = ages[i] - hitStopDuration;
                    // Three explicit ON/OFF pairs after the hit stop, no smooth pulsing.
                    if (blinkAge >= 0f) visible &= Mathf.FloorToInt(blinkAge / blinkInterval) % 2 == 0;
                    bright = true;
                }
                if (phases[i] == LinkPhase.Reconnecting)
                {
                    progress = Mathf.SmoothStep(0f, 1f, ages[i] / reconnectDuration);
                    bright = true;
                }
                LineRenderer line = lines[i];
                line.enabled = visible;
                Vector3 from = coreView.GetCellTransform(i).position;
                Vector3 to = player.position;
                from.z = to.z = -0.03f;
                line.SetPosition(0, from);
                line.SetPosition(1, Vector3.Lerp(from, to, progress));
                Color start = coreColor, end = playerColor;
                if (bright) { start.a = end.a = 1f; }
                start.a *= visibility; end.a *= visibility;
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
