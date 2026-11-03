using TMPro;
using UnityEngine;

namespace FourCore
{
    public sealed class PortalView : MonoBehaviour
    {
        private PortalState state;
        private PlayerController controls;
        private readonly LineRenderer[] rings = new LineRenderer[4];
        private Material material;
        private TMP_Text status;
        public void Initialize(PortalState portalState, PlayerController playerControls)
        {
            state = portalState; controls = playerControls;
            material = new Material(Shader.Find("Sprites/Default"));
            for (int i = 0; i < rings.Length; i++)
            {
                GameObject obj = new(i < 2 ? "Portal A ring" : "Portal B ring"); obj.transform.SetParent(transform,false);
                LineRenderer ring = obj.AddComponent<LineRenderer>(); ring.sharedMaterial = material;
                ring.useWorldSpace = false; ring.loop = true; ring.positionCount = 48; ring.sortingOrder = 8;
                ring.startWidth = ring.endWidth = i%2==0 ? .07f : .025f;
                rings[i] = ring;
            }
            GameObject hud = new("PortalStatus",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
            hud.transform.SetParent(transform,false);
            var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30;
            var scaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            GameObject label = new("PortalHint",typeof(RectTransform),typeof(CanvasRenderer)); label.transform.SetParent(hud.transform,false);
            status = label.AddComponent<TextMeshProUGUI>(); status.fontSize = 23; status.color = new Color(.5f,.84f,1f);
            status.raycastTarget = false; status.alignment = TextAlignmentOptions.Left;
            var rect = status.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,0);
            rect.anchoredPosition = new Vector2(48,40); rect.sizeDelta = new Vector2(1400,46);
        }
        private void Update()
        {
            if (state == null) return;
            for (int end = 0; end < 2; end++)
            {
                GridPosition? cell = end==0 ? state.A : state.B;
                Color color = end==0 ? new Color(.25f,.85f,1f) : new Color(.8f,.42f,1f);
                for (int layer = 0; layer < 2; layer++)
                {
                    LineRenderer ring = rings[end*2+layer]; ring.gameObject.SetActive(cell.HasValue);
                    if (!cell.HasValue) continue;
                    ring.transform.localPosition = GameConfig.LogicalToWorld(cell.Value,-.18f);
                    float radius = (layer==0 ? .4f : .28f) + Mathf.Sin(Time.unscaledTime*3+end)*.025f;
                    color.a = state.Linked ? 1f : .55f;
                    ring.startColor = ring.endColor = color;
                    for(int p=0;p<48;p++)
                    {
                        float angle=p*Mathf.PI*2/48 + Time.unscaledTime*(layer==0?.35f:-.35f);
                        ring.SetPosition(p,new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*radius);
                    }
                }
            }
            status.gameObject.SetActive(controls.GameplayInputEnabled && (state.Kits > 0 || state.A.HasValue));
            status.text = state.AwaitingSecond ? $"PORTAL  |  E : PLACE SECOND   |   PAIRS {state.Kits}"
                : state.Linked ? $"PORTAL  |  USES {state.RemainingUses} / 2" + (state.Kits > 0 ? $"   |   E : NEW PAIR ({state.Kits})" : "")
                : $"PORTAL  |  E : PLACE FIRST   |   PAIRS {state.Kits}";
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
