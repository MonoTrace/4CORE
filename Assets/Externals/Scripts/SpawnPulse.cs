using UnityEngine;

namespace FourCore
{
    // Short rings and radial sparks; unscaled so pickup and between-stage feedback stays smooth.
    public sealed class SpawnPulse : MonoBehaviour
    {
        private LineRenderer[] lines;
        private Material material;
        private Color tint;
        private float age, radius;

        public static SpawnPulse Play(Transform parent, Vector3 position, Color color, float radius = 1f)
        {
            GameObject obj = new("Spawn Pulse");
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var pulse = obj.AddComponent<SpawnPulse>();
            pulse.tint = color; pulse.radius = radius;
            pulse.material = new Material(Shader.Find("Sprites/Default"));
            pulse.lines = new LineRenderer[10];
            for (int i = 0; i < pulse.lines.Length; i++)
            {
                GameObject child = new(i < 2 ? "Ring" : "Spark");
                child.transform.SetParent(obj.transform, false);
                var line = child.AddComponent<LineRenderer>();
                line.sharedMaterial = pulse.material; line.useWorldSpace = false;
                line.sortingOrder = 22; line.numCapVertices = 3;
                line.loop = i < 2; line.positionCount = i < 2 ? 64 : 2;
                pulse.lines[i] = line;
            }
            pulse.Render();
            return pulse;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            if (age >= .65f) { gameObject.SetActive(false); Destroy(gameObject); return; }
            Render();
        }

        private void Render()
        {
            float t = Mathf.Clamp01(age / .65f);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                Color color = Color.Lerp(Color.white, tint, Mathf.Clamp01(t * 3f));
                color.a = (1f - t) * (i == 1 ? .35f : 1f);
                line.startColor = line.endColor = color;
                line.startWidth = line.endWidth = (i == 1 ? .16f : .055f) * (1f - t * .7f);
                float r = radius * Mathf.Lerp(.18f, i == 1 ? 1.2f : 1f, 1f - (1f - t) * (1f - t));
                if (i < 2)
                    for (int n = 0; n < line.positionCount; n++)
                    {
                        float a = n * Mathf.PI * 2f / line.positionCount;
                        line.SetPosition(n, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, -.35f));
                    }
                else
                {
                    float a = (i - 2) * Mathf.PI / 4f + .2f;
                    Vector3 direction = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    line.SetPosition(0, direction * r + Vector3.back * .35f);
                    line.SetPosition(1, direction * (r + radius * .25f * (1f - t)) + Vector3.back * .35f);
                }
            }
        }

        public static void Clear(Transform parent)
        {
            foreach (var pulse in parent.GetComponentsInChildren<SpawnPulse>())
            { pulse.gameObject.SetActive(false); Destroy(pulse.gameObject); }
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
