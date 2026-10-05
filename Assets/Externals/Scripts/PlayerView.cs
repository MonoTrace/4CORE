using UnityEngine;

namespace FourCore
{
    // The prefab's Visual child owns its renderer, mesh/sprite and appearance.
    public sealed class PlayerView : MonoBehaviour
    {
        public void Initialize(PlayerState state) => Apply(state);

        public void Apply(PlayerState state)
        {
            transform.localPosition = GameConfig.LogicalToWorld(state.Position, -0.1f);
            float angle = state.Facing switch
            {
                FacingDirection.Right => -90f,
                FacingDirection.Down => 180f,
                FacingDirection.Left => 90f,
                _ => 0f
            };
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
