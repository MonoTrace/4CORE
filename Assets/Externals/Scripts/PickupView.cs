using System.Collections.Generic;
using UnityEngine;

namespace FourCore
{
    public sealed class PickupView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] pickupPrefabs;
        private readonly List<SpriteRenderer> visuals = new();
        private PickupState state;
        public void Initialize(PickupState state) { this.state = state; Apply(); }
        public void Apply()
        {
            foreach (SpriteRenderer visual in visuals) { visual.gameObject.SetActive(false); Destroy(visual.gameObject); }
            visuals.Clear();
            foreach (Pickup pickup in state.Items)
            {
                SpriteRenderer visual = Instantiate(pickupPrefabs[(int)pickup.Type], transform, false);
                visual.transform.localPosition = GameConfig.LogicalToWorld(pickup.Position, -0.12f);
                visual.name = pickup.Type.ToString(); visuals.Add(visual);
            }
        }
        private void Update()
        {
            for (int i = 0; i < visuals.Count; i++)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f + i) * 0.09f;
                visuals[i].transform.localScale = pickupPrefabs[(int)state.Items[i].Type].transform.localScale * pulse;
            }
        }
    }
}
