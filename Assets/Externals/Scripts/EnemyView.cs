using UnityEngine;

namespace FourCore
{
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] private EnemyType enemyType;
        [SerializeField] private EnemyPathView pathView;
        public EnemyType Type => enemyType;

        public void Initialize(EnemyState state) => Apply(state);

        public void RefreshPath(EnemyState state, CoreState core)
        {
            if (pathView != null) pathView.Apply(state, core, transform.parent);
        }

        public void Apply(EnemyState state)
        {
            transform.localPosition = GameConfig.LogicalToWorld(state.Position, -0.08f);
        }
    }
}
