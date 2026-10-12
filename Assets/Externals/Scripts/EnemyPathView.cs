using UnityEngine;

namespace FourCore
{
    public sealed class EnemyPathView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer pathCellPrefab;
        [SerializeField] private Color pathColor = Color.red;
        [SerializeField] private bool missilePath;
        [SerializeField] private float[] opacityByStep = { 0.40f, 0.30f, 0.22f, 0.16f, 0.11f, 0.07f };
        private SpriteRenderer[] cells;
        private GridPosition[] positions;

        public void Apply(EnemyState state, CoreState core, Transform gridSpace)
        {
            if (cells == null)
            {
                if (pathCellPrefab == null)
                    throw new System.InvalidOperationException("Assign the PathCell prefab to EnemyPathView.");
                positions = new GridPosition[opacityByStep.Length];
                cells = new SpriteRenderer[positions.Length];
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i] = Instantiate(pathCellPrefab, transform, false);
                    cells[i].name = $"PathCell {i + 1}";
                }
            }

            int count = missilePath ? state.PredictMissilePath(positions, core) : state.PredictPath(positions, core);
            for (int i = 0; i < cells.Length; i++)
            {
                bool visible = i < count && GameConfig.IsInsidePlayable(positions[i]);
                cells[i].gameObject.SetActive(visible);
                if (!visible) continue;
                // Independent of the enemy's position/facing; same coordinate space as EnemyView.
                Vector3 position = GameConfig.LogicalToWorld(positions[i], -0.02f);
                cells[i].transform.position = gridSpace != null ? gridSpace.TransformPoint(position) : position;
                cells[i].transform.rotation = gridSpace != null ? gridSpace.rotation : Quaternion.identity;
                Color color = missilePath && state.MissileReflected ? new Color(0.05f, 0.65f, 1f) : pathColor;
                color.a = Mathf.Clamp01(opacityByStep[i]);
                cells[i].color = color;
            }
        }
    }
}
