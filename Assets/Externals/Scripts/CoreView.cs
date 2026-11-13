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
        [Header("PIXELFX")]
        [SerializeField, Min(0.01f)] private float breakEffectScale = 0.8f;
        [SerializeField, Min(0.01f)] private float recoverEffectScale = 0.8f;
        private MaterialPropertyBlock properties;
        private bool[] previousAlive;
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
            if (!animate) SpawnPulse.Clear(transform);
            bool hasPrevious = previousAlive != null;
            previousAlive ??= new bool[state.CellCount];
            properties ??= new MaterialPropertyBlock();
            for (int index = 0; index < state.CellCount; index++)
            {
                bool alive = state.IsAlive(index);
                if (animate && hasPrevious && alive && !previousAlive[index])
                    SpawnPulse.Play(transform, cells[index].transform.position, new Color(.15f, .55f, 1f), 1.15f);
                if (animate && hasPrevious && alive != previousAlive[index])
                    PlayCellEffect(index, alive ? PixelFx.Keys.CoreRecover : PixelFx.Keys.CoreBreak,
                        alive ? recoverEffectScale : breakEffectScale);
                previousAlive[index] = alive;
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

        private void PlayCellEffect(int index, string key, float scale)
        {
            // Parent to the board root so the effect isn't scaled with the core prefab.
            Transform parent = transform.parent;
            Vector3 position = cells[index].transform.position;
            position.z = -0.35f;
            PixelFx.Play(key, parent, parent != null ? parent.InverseTransformPoint(position) : position, 0f, scale);
        }
    }
}
