using System;
using UnityEngine;

namespace FourCore
{
    public sealed class CoreView : MonoBehaviour
    {
        [Tooltip("Core cells in state order: bottom-left, bottom-right, top-left, top-right.")]
        [SerializeField] private GameObject[] cells;

        public void Initialize(CoreState state)
        {
            if (cells == null || cells.Length != state.CellCount || Array.Exists(cells, cell => cell == null))
                throw new InvalidOperationException("Assign all four core cells on the Core prefab.");

            Apply(state);
        }

        public void Apply(CoreState state)
        {
            for (int index = 0; index < state.CellCount; index++)
                cells[index].SetActive(state.IsAlive(index));
        }
    }
}
