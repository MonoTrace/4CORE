using System;
using System.Collections.Generic;
using UnityEngine;

namespace FourCore
{
    public sealed class ShieldView : MonoBehaviour
    {
        private readonly Dictionary<GridPosition, GameObject> visuals = new();
        private GameObject shieldPrefab;
        private RunState run;
        private AugmentCombatState augments;
        private readonly List<(LineRenderer Line, float Started)> blasts = new();
        private Material effectMaterial;

        public void BindAugments(RunState runState, AugmentCombatState state, TurnManager turns)
        { run = runState; augments = state; }

        public void Initialize(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            shieldPrefab = prefab;
        }

        public void Apply(ShieldState state)
        {
            List<GridPosition> removed = new();
            List<GridPosition> cells = new(state.Positions);
            foreach (KeyValuePair<GridPosition, GameObject> entry in visuals)
                if (!cells.Contains(entry.Key)) removed.Add(entry.Key);

            foreach (GridPosition position in removed)
            {
                visuals[position].SetActive(false);
                Destroy(visuals[position]);
                visuals.Remove(position);
            }

            foreach (GridPosition position in cells)
            {
                if (!visuals.TryGetValue(position, out GameObject visual))
                {
                    visual = Instantiate(shieldPrefab, transform, false);
                    visual.name = $"Shield {position}";
                    visual.transform.localPosition = GameConfig.LogicalToWorld(position, -0.04f);
                    visuals.Add(position, visual);
                }
                if (run?.PrimaryAugment != null)
                {
                    Color color = AugmentCatalog.Color(run.PrimaryAugment.Value);
                    MaterialPropertyBlock block = new(); block.SetColor("_Color", color); block.SetColor("_BaseColor", color);
                    foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
                        renderer.SetPropertyBlock(block);
                }
                else foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(null);
                int remaining = augments != null && augments.Bombs.TryGetValue(position, out int count) ? count : 0;
                var label = visual.transform.Find("AugmentCountdown")?.GetComponent<TMPro.TextMeshPro>();
                if (remaining > 0 && label == null)
                {
                    GameObject obj = new("AugmentCountdown"); obj.transform.SetParent(visual.transform, false);
                    obj.transform.localPosition = new Vector3(0, 0, -.35f);
                    label = obj.AddComponent<TMPro.TextMeshPro>(); label.fontSize = 6.5f; label.fontStyle = TMPro.FontStyles.Bold;
                    label.alignment = TMPro.TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(1, 1);
                    label.GetComponent<MeshRenderer>().sortingOrder = 20;
                }
                if (label != null) { label.gameObject.SetActive(remaining > 0); label.text = remaining.ToString(); }
            }
        }

        private LineRenderer NewLine(string name, float width)
        {
            if (effectMaterial == null) effectMaterial = new Material(Shader.Find("Sprites/Default"));
            GameObject obj = new(name); obj.transform.SetParent(transform, false);
            var line = obj.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.sharedMaterial = effectMaterial;
            line.startWidth = line.endWidth = width; line.sortingOrder = 18;
            return line;
        }
        public void ShowBlast(IReadOnlyList<GridPosition> positions)
        {
            foreach (GridPosition p in positions)
            {
                LineRenderer line = NewLine("Barrier explosion", .08f);
                line.transform.localPosition = GameConfig.LogicalToWorld(p, -.3f);
                line.loop = true; line.positionCount = 32; blasts.Add((line, Time.unscaledTime));
            }
        }
        private void Update()
        {
            for (int i = blasts.Count-1; i >= 0; i--)
            {
                float t = (Time.unscaledTime-blasts[i].Started)/.35f;
                if (t >= 1) { Destroy(blasts[i].Line.gameObject); blasts.RemoveAt(i); continue; }
                var line = blasts[i].Line; float radius = Mathf.Lerp(.3f, 1.5f, t);
                Color color = new(1,.5f,.25f,1-t); line.startColor = line.endColor = color;
                for (int n = 0; n < 32; n++)
                { float angle = n*Mathf.PI*2/32; line.SetPosition(n,new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*radius); }
            }
        }
        public void ClearTransient()
        {
            foreach (var blast in blasts) { blast.Line.gameObject.SetActive(false); Destroy(blast.Line.gameObject); }
            blasts.Clear();
        }
        private void OnDestroy() { if (effectMaterial != null) Destroy(effectMaterial); }
    }
}
