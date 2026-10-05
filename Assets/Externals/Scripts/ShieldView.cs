using System;
using System.Collections.Generic;
using UnityEngine;

namespace FourCore
{
    public sealed class ShieldView : MonoBehaviour
    {
        private readonly Dictionary<GridPosition, GameObject> visuals = new();
        private GameObject shieldPrefab;

        public void Initialize(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            shieldPrefab = prefab;
        }

        public void Apply(ShieldState state)
        {
            List<GridPosition> removed = new();
            foreach (KeyValuePair<GridPosition, GameObject> entry in visuals)
                if (!state.Contains(entry.Key)) removed.Add(entry.Key);

            foreach (GridPosition position in removed)
            {
                visuals[position].SetActive(false);
                Destroy(visuals[position]);
                visuals.Remove(position);
            }

            foreach (GridPosition position in state.Positions)
            {
                if (visuals.ContainsKey(position)) continue;
                GameObject visual = Instantiate(shieldPrefab, transform, false);
                visual.name = $"Shield {position}";
                visual.transform.localPosition = GameConfig.LogicalToWorld(position, -0.04f);
                visuals.Add(position, visual);
            }
        }
    }
}
