using UnityEngine;

namespace FourCore
{
    // Canvas-space disc: the center follows the final enemy's projected position.
    public sealed class RewardIris : UnityEngine.UI.MaskableGraphic
    {
        public Vector2 Center { get; private set; }
        public float Radius { get; private set; }
        public void SetDisc(Vector2 center, float radius, float opacity = 1f)
        {
            Center = center; Radius = Mathf.Max(0f, radius);
            color = new Color(0f, 0f, 0f, opacity);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            if (Radius <= 0f) return;
            const int segments = 128;
            mesh.AddVert(Center, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                mesh.AddVert(Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Radius, color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
