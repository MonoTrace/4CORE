using TMPro;
using UnityEngine;

namespace FourCore
{
    public sealed class BossView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer[] cellRenderers;
        [SerializeField] private TMP_Text[] healthLabels;
        [SerializeField] private SpriteRenderer projectilePrefab;
        [SerializeField] private SpriteRenderer pathCellPrefab;
        private SpriteRenderer[] missiles;
        private SpriteRenderer[,] paths;
        private SpriteRenderer[] chargeCells;
        private int teleportSerial = -1;
        private float teleportAge;
        private MaterialPropertyBlock block;
        private BossState state;
        private CoreState core;
        private System.Collections.Generic.IReadOnlyList<ResonanceBeam> beams;
        private readonly System.Collections.Generic.List<SpriteRenderer[]> beamCells=new();
        public void Initialize(BossState state, CoreState core,System.Collections.Generic.IReadOnlyList<ResonanceBeam> beams=null)
        {
            this.beams=beams;
            this.state = state; this.core = core; block = new MaterialPropertyBlock();
            missiles = new SpriteRenderer[state.Missiles.Length]; paths = new SpriteRenderer[state.Missiles.Length, 24];
            for (int i = 0; i < missiles.Length; i++)
            {
                missiles[i] = Instantiate(projectilePrefab, transform, false);
                missiles[i].sortingOrder = 11; missiles[i].transform.localScale = new Vector3(0.2f, 0.45f, 1f);
                for (int j = 0; j < 24; j++) paths[i,j] = Instantiate(pathCellPrefab, transform, false);
            }
            chargeCells=new SpriteRenderer[5];
            for(int i=0;i<5;i++)chargeCells[i]=Instantiate(pathCellPrefab,transform,false);
            Apply();
        }
        public void Apply(bool animate = true)
        {
            if (state.TeleportSerial != teleportSerial)
            { teleportSerial = state.TeleportSerial; teleportAge = animate ? 0f : 1f; }
            for (int i = 0; i < cellRenderers.Length; i++)
            {
                bool visible = state.Active && state.BodyVisible && i<state.CellCount && state.Health(i) > 0;
                cellRenderers[i].gameObject.SetActive(visible);
                cellRenderers[i].transform.localPosition = GameConfig.LogicalToWorld(state.Cell(i), -0.1f);
                healthLabels[i].gameObject.SetActive(visible); healthLabels[i].text = state.Health(i).ToString();
            }
            for (int i = 0; i < missiles.Length; i++)
            {
                BossProjectile m = state.Missiles[i];
                missiles[i].gameObject.SetActive(state.Active && m.Active);
                missiles[i].transform.localPosition = GameConfig.LogicalToWorld(m.Position, -0.2f);
                missiles[i].transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(m.Direction.y, m.Direction.x) * Mathf.Rad2Deg - 90f);
                missiles[i].color = m.Reflected ? new Color(0.05f,0.65f,1f) : new Color(1f,0.3f,0.55f);
                Vector2Int direction = state.Side switch { 0 => Vector2Int.down, 1 => Vector2Int.left, 2 => Vector2Int.up, _ => Vector2Int.right };
                // Show the forthcoming volley even during the resting/aiming turn.
                BossState preview = state.Copy();
                if (!state.HasMissiles && !state.UsesResonance && !state.UsesStraightCharge) preview.Fire(core);
                BossProjectile pathMissile = state.HasMissiles ? m : preview.Missiles[i];
                GridPosition point = pathMissile.Position;
                direction = pathMissile.Direction;
                for (int j = 0; j < 24; j++)
                {
                    point = point.Offset(direction);
                    bool visible = state.Active && pathMissile.Active && GameConfig.IsInsidePlayable(point);
                    paths[i,j].gameObject.SetActive(visible);
                    paths[i,j].transform.localPosition = GameConfig.LogicalToWorld(point, -0.02f);
                    Color color = pathMissile.Reflected ? new Color(0.05f,0.65f,1f) : new Color(1f,0.3f,0.55f);
                    color.a = 0.22f; paths[i,j].color = color;
                    if ((pathMissile.Reflected && state.Occupies(point)) || core.Occupies(point))
                    { for (int k = j + 1; k < 24; k++) paths[i,k].gameObject.SetActive(false); break; }
                }
            }
            for(int i=0;i<chargeCells.Length;i++)
            {
                chargeCells[i].gameObject.SetActive(state.Active&&state.UsesCharge&&state.Health(i)>0);
                chargeCells[i].transform.localPosition=GameConfig.LogicalToWorld(state.CellAt(state.BodyVisible?state.NextCharge():state.Origin,i),-.015f);
                chargeCells[i].color=state.BodyVisible?new Color(1f,.6f,.2f,.25f):new Color(.7f,.4f,1f,.12f);
            }
            ApplyBeams();
        }
        private void ApplyBeams()
        {
            int count=beams?.Count??0;
            while(beamCells.Count<count)
            {
                var row=new SpriteRenderer[GameConfig.LogicalGridSize];
                for(int i=0;i<row.Length;i++)row[i]=Instantiate(pathCellPrefab,transform,false);
                beamCells.Add(row);
            }
            for(int i=0;i<beamCells.Count;i++)
            {
                bool stopped=i>=count;GridPosition cell=stopped?default:beams[i].Origin;
                for(int j=0;j<beamCells[i].Length;j++)
                {
                    var sprite=beamCells[i][j];if(!stopped)cell=cell.Offset(beams[i].Direction);
                    bool visible=!stopped&&GameConfig.IsInsidePlayable(cell);sprite.gameObject.SetActive(visible);
                    if(!visible)continue;
                    sprite.transform.localPosition=GameConfig.LogicalToWorld(cell,-.025f);
                    sprite.color=new Color(1f,.65f,.1f,beams[i].Remaining<=1?.55f:.28f);
                    if(core.Occupies(cell))stopped=true;
                }
            }
        }
        private void Update()
        {
            if (state == null) return;
            teleportAge += Time.unscaledDeltaTime;
            float appear = Mathf.SmoothStep(0f,1f,teleportAge / 0.3f);
            for (int i = 0; i < cellRenderers.Length; i++)
            {
                cellRenderers[i].transform.localScale = Vector3.one * Mathf.Lerp(0.15f,0.85f,appear);
                Color color = Color.Lerp(Color.white,StageCatalog.Color(state.StageNumber),appear);
                block.SetColor("_BaseColor",color); block.SetColor("_Color",color); cellRenderers[i].SetPropertyBlock(block);
            }
        }
    }
}
