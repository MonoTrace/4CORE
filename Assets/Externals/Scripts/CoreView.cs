using System;
using UnityEngine;

namespace FourCore
{
    public sealed class CoreView : MonoBehaviour
    {
        [Tooltip("Core cells in state order: bottom-left, bottom-right, top-left, top-right.")]
        [SerializeField] private GameObject[] cells;
        [SerializeField] private MeshRenderer[] cellRenderers;
        [SerializeField] private TMPro.TMP_Text[] recoveryLabels;
        [SerializeField] private Color brokenColor = new(0.22f, 0.035f, 0.045f, 1f);
        private MaterialPropertyBlock properties;
        public event Action<CoreState, bool> Applied;

        public Transform GetCellTransform(int index) => cells[index].transform;

        public void Initialize(CoreState state)
        {
            if (cells == null || cells.Length != state.CellCount || Array.Exists(cells, cell => cell == null))
                throw new InvalidOperationException("Assign all four core cells on the Core prefab.");

            Apply(state);
        }

        public void Apply(CoreState state, bool animate = true)
        {
            properties ??= new MaterialPropertyBlock();
            for (int index = 0; index < state.CellCount; index++)
            {
                bool alive = state.IsAlive(index);
                cells[index].SetActive(true);
                if (cellRenderers != null && index < cellRenderers.Length && cellRenderers[index] != null)
                {
                    properties.Clear();
                    if (!alive)
                    {
                        properties.SetColor("_BaseColor", brokenColor);
                        properties.SetColor("_Color", brokenColor);
                    }
                    cellRenderers[index].SetPropertyBlock(alive ? null : properties);
                }
                if (recoveryLabels != null && index < recoveryLabels.Length && recoveryLabels[index] != null)
                {
                    recoveryLabels[index].gameObject.SetActive(!alive);
                    if (!alive) recoveryLabels[index].text = state.GetRecoveryTurns(index).ToString();
                }
            }
            Applied?.Invoke(state, animate);
        }
    }
}
