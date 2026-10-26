using UnityEngine;

namespace FourCore
{
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] private EnemyType enemyType;
        [SerializeField] private EnemyPathView pathView;
        [SerializeField] private EnemyPathView missilePathView;
        [SerializeField] private SpriteRenderer missilePrefab;
        private SpriteRenderer missile;
        private MaterialPropertyBlock themeBlock;
        private Transform body;
        private Vector3 originalBodyScale;
        [SerializeField] private LineRenderer armorOutline;
        public EnemyType Type => enemyType;

        public void Initialize(EnemyState state)
        {
            body=transform.Find("Visual");
            if(body!=null)originalBodyScale=body.localScale;
            themeBlock=new MaterialPropertyBlock();
            if (missilePrefab != null)
            {
                missile = Instantiate(missilePrefab, transform, false);
                missile.name = "Missile";
                missile.sortingOrder = 9;
                missile.transform.localScale = new Vector3(0.18f, 0.42f, 1f);
            }
            Apply(state);
        }

        public void RefreshPath(EnemyState state, CoreState core)
        {
            if (pathView != null) pathView.Apply(state, core, transform.parent);
            if (missilePathView != null) missilePathView.Apply(state, core, transform.parent);
            Apply(state);
        }

        public void Apply(EnemyState state)
        {
            transform.localPosition = GameConfig.LogicalToWorld(state.Position, -0.08f);
            if(body!=null)
            {
                body.gameObject.SetActive(state.BodyVisible);
                body.localScale=originalBodyScale*(state.IsFragment?.55f:1f);
                if(state.Traits!=EnemyTraits.None)
                {
                    Color color=(state.Traits&EnemyTraits.Phase)!=0?StageCatalog.Color(2):
                        (state.Traits&EnemyTraits.Resonance)!=0?StageCatalog.Color(3):new Color(1f,.8f,.2f);
                    themeBlock.SetColor("_BaseColor",color);themeBlock.SetColor("_Color",color);
                    foreach(var mesh in body.GetComponentsInChildren<MeshRenderer>(true))mesh.SetPropertyBlock(themeBlock);
                }
            }
            if(armorOutline!=null)armorOutline.enabled=state.Life>1&&state.BodyVisible;
            if (state.Type != EnemyType.Missile) return;
            Transform visual = transform.Find("Visual");
            if (visual != null)
                visual.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(state.FiringDirection.y, state.FiringDirection.x) * Mathf.Rad2Deg + 90f);
            if (missile == null) return;
            missile.gameObject.SetActive(state.MissileActive);
            if (!state.MissileActive) return;
            missile.transform.localPosition = GameConfig.LogicalToWorld(state.MissilePosition, -0.18f) - transform.localPosition;
            missile.transform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(state.MissileDirection.y, state.MissileDirection.x) * Mathf.Rad2Deg - 90f);
            missile.color = state.MissileReflected ? new Color(0.05f, 0.65f, 1f) : new Color(1f, 0.15f, 0.85f);
        }
    }
}
