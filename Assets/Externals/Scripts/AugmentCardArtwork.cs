using UnityEngine;

namespace FourCore
{
    // Resolution-independent card frame and emblems, drawn directly by the UI canvas.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AugmentCardArtwork : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField] private AugmentType augment;
        [SerializeField] private Color accent = new(1f, 0.78f, 0.36f);
        private float emphasis;
        public void Configure(AugmentType type, Color tint) { augment = type; accent = tint; SetVerticesDirty(); }
        public void SetEmphasis(float value)
        {
            if (Mathf.Abs(value - emphasis) < .001f) return;
            emphasis = value; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            float w = r.width, h = r.height;
            Vector2 center = r.center;
            Color luminous = Color.Lerp(accent, Color.white, .32f);
            for (int i = 5; i >= 1; i--)
                Ring(mesh, center, w + i * 7, h + i * 7, 19 + i * 3, 7,
                    Tint(accent, (.012f + emphasis * .035f) * (6 - i)));
            Polygon(mesh, Bevel(center, w, h, 20), new Color(.018f, .026f, .057f, 1));
            // A tinted upper field gives the emblem space above the text.
            Quad(mesh, center + new Vector2(-w / 2 + 6, 5), new Vector2(w - 12, h / 2 - 11),
                Tint(accent, .015f), Tint(accent, .12f + emphasis * .09f));
            Ring(mesh, center, w, h, 20, 2.5f + emphasis, Tint(luminous, .72f + emphasis * .28f));
            Ring(mesh, center, w - 17, h - 17, 14, .8f, Tint(accent, .28f + emphasis * .27f));
            // Fine corner hardware and the top/bottom diamonds echo a collectible card.
            foreach (int side in new[] { -1, 1 })
            {
                float x = center.x + side * (w / 2 - 18);
                Line(mesh, new Vector2(x, center.y + h / 2 - 52), new Vector2(x, center.y + h / 2 - 25), 3, luminous);
                Line(mesh, new Vector2(x, center.y - h / 2 + 25), new Vector2(x, center.y - h / 2 + 52), 3, luminous);
            }
            Diamond(mesh, center + new Vector2(0, h / 2), 10, luminous);
            Diamond(mesh, center + new Vector2(0, -h / 2), 8, luminous);
            Vector2 icon = center + new Vector2(0, 109);
            Halo(mesh, icon, 91, Tint(accent, .17f + emphasis * .16f));
            RingDiamond(mesh, icon, 91, 1.2f, Tint(accent, .28f));
            RingDiamond(mesh, icon, 76, 1.5f, Tint(accent, .5f + emphasis * .3f));
            DrawEmblem(mesh, icon, luminous);
            Line(mesh, center + new Vector2(-w * .33f, -99), center + new Vector2(-12, -99), 1, Tint(accent, .42f));
            Line(mesh, center + new Vector2(12, -99), center + new Vector2(w * .33f, -99), 1, Tint(accent, .42f));
            Diamond(mesh, center + new Vector2(0, -99), 4, luminous);
            // Moving light across the frame only appears during focus/hover.
            if (emphasis > .01f)
            {
                float y = Mathf.Lerp(-h / 2 + 36, h / 2 - 36, Mathf.PingPong(Time.unscaledTime * .22f, 1));
                foreach (int side in new[] { -1, 1 })
                    Diamond(mesh, center + new Vector2(side * (w / 2 - 1), y), 4, Tint(Color.white, emphasis * .85f));
            }
        }

        private void Update() { if (emphasis > .01f) SetVerticesDirty(); }

        private void DrawEmblem(UnityEngine.UI.VertexHelper mesh, Vector2 p, Color c)
        {
            if (augment == AugmentType.PortalKit)
            {
                RingDiamond(mesh,p + Vector2.left*25,31,3,c);
                RingDiamond(mesh,p + Vector2.right*25,31,3,Tint(c,.65f));
                Line(mesh,p+Vector2.left*14,p+Vector2.right*14,3,c);
                return;
            }
            if (augment == AugmentType.ChainBarrier)
            {
                Line(mesh,p+Vector2.left*40,p+Vector2.right*40,3,c);
                for(int i=-1;i<=1;i++) RingDiamond(mesh,p+Vector2.right*i*35,19,4,c);
                return;
            }
            if (augment == AugmentType.TimedBarrier)
            {
                RingDiamond(mesh,p,42,4,c);
                Line(mesh,p,p+Vector2.up*27,4,c); Line(mesh,p,p+Vector2.right*19,4,c);
                Line(mesh,p+new Vector2(-13,53),p+new Vector2(13,53),4,c);
                return;
            }
            if (augment == AugmentType.LongerFreeze)
            {
                Line(mesh, p + new Vector2(-32, 41), p + new Vector2(32, 41), 5, c);
                Line(mesh, p + new Vector2(-32, -41), p + new Vector2(32, -41), 5, c);
                foreach (int side in new[] { -1, 1 })
                {
                    Line(mesh, p + new Vector2(side * 26, 35), p, 3, c);
                    Line(mesh, p, p + new Vector2(side * 26, -35), 3, c);
                }
                Polygon(mesh, new[] { p + new Vector2(-18, 26), p + new Vector2(18, 26), p + new Vector2(0, 5) }, Tint(c, .7f));
                Polygon(mesh, new[] { p + new Vector2(-21, -31), p + new Vector2(21, -31), p + new Vector2(0, -9) }, c);
            }
            else if (augment == AugmentType.ShieldEnergy)
            {
                Vector2[] shield = { p + new Vector2(0, 46), p + new Vector2(36, 30), p + new Vector2(30, -15), p + new Vector2(0, -47), p + new Vector2(-30, -15), p + new Vector2(-36, 30) };
                Polygon(mesh, shield, Tint(c, .14f));
                for (int i = 0; i < shield.Length; i++) Line(mesh, shield[i], shield[(i + 1) % shield.Length], 4, c);
                Polygon(mesh, new[] { p + new Vector2(8, 29), p + new Vector2(-16, -3), p + new Vector2(0, -3), p + new Vector2(-6, -28), p + new Vector2(19, 8), p + new Vector2(3, 8) }, c);
            }
            else
            {
                RingDiamond(mesh, p, 47, 3, c);
                foreach (int side in new[] { -1, 1 })
                {
                    Line(mesh, p + new Vector2(side * 56, -8), p + new Vector2(side * 56, 17), 2, Tint(c, .7f));
                    Diamond(mesh, p + new Vector2(side * 56, 23), 4, c);
                }
                Quad(mesh, p + new Vector2(-7, -25), new Vector2(14, 50), c, c);
                Quad(mesh, p + new Vector2(-25, -7), new Vector2(50, 14), c, c);
            }
        }

        private static Color Tint(Color c, float alpha) { c.a = alpha; return c; }
        private static Vector2[] Bevel(Vector2 c, float w, float h, float cut) => new[] {
            c + new Vector2(-w/2+cut,-h/2), c + new Vector2(w/2-cut,-h/2), c + new Vector2(w/2,-h/2+cut), c + new Vector2(w/2,h/2-cut),
            c + new Vector2(w/2-cut,h/2), c + new Vector2(-w/2+cut,h/2), c + new Vector2(-w/2,h/2-cut), c + new Vector2(-w/2,-h/2+cut) };
        private static void Polygon(UnityEngine.UI.VertexHelper mesh, Vector2[] points, Color c)
        {
            int start = mesh.currentVertCount;
            foreach (Vector2 p in points) mesh.AddVert(p, c, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }
        private static void Quad(UnityEngine.UI.VertexHelper mesh, Vector2 bottomLeft, Vector2 size, Color bottom, Color top)
        {
            int i = mesh.currentVertCount;
            mesh.AddVert(bottomLeft, bottom, Vector2.zero); mesh.AddVert(bottomLeft + new Vector2(size.x, 0), bottom, Vector2.zero);
            mesh.AddVert(bottomLeft + size, top, Vector2.zero); mesh.AddVert(bottomLeft + new Vector2(0, size.y), top, Vector2.zero);
            mesh.AddTriangle(i, i+1, i+2); mesh.AddTriangle(i, i+2, i+3);
        }
        private static void Line(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, float width, Color c)
        {
            Vector2 n = new Vector2(-(b-a).y, (b-a).x).normalized * width / 2;
            Polygon(mesh, new[] { a-n, b-n, b+n, a+n }, c);
        }
        private static void Diamond(UnityEngine.UI.VertexHelper mesh, Vector2 c, float radius, Color color) =>
            Polygon(mesh, new[] { c+Vector2.down*radius, c+Vector2.right*radius, c+Vector2.up*radius, c+Vector2.left*radius }, color);
        private static void RingDiamond(UnityEngine.UI.VertexHelper mesh, Vector2 c, float radius, float width, Color color)
        {
            Vector2[] p = { c+Vector2.down*radius, c+Vector2.right*radius, c+Vector2.up*radius, c+Vector2.left*radius };
            for (int i = 0; i < 4; i++) Line(mesh, p[i], p[(i+1)%4], width, color);
        }
        private static void Halo(UnityEngine.UI.VertexHelper mesh, Vector2 c, float radius, Color color)
        {
            int start = mesh.currentVertCount; mesh.AddVert(c, color, Vector2.zero);
            color.a = 0;
            for (int i = 0; i <= 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48;
                mesh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a))*radius, color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start+i, start+i+1);
            }
        }
        private static void Ring(UnityEngine.UI.VertexHelper mesh, Vector2 c, float w, float h, float cut, float width, Color color)
        {
            Vector2[] outer = Bevel(c,w,h,cut), inner = Bevel(c,w-width*2,h-width*2,Mathf.Max(1,cut-width));
            for (int i = 0; i < 8; i++) Polygon(mesh, new[] { outer[i], outer[(i+1)%8], inner[(i+1)%8], inner[i] }, color);
        }
    }
}
