using System;
using TMPro;
using UnityEngine;

namespace FourCore
{
    public sealed class EnergyHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text energyText;
        private PlayerEnergyState energyState;

        public void Initialize(PlayerEnergyState state)
        {
            if (energyText == null) throw new InvalidOperationException("Assign the scene EnergyText to EnergyHud.");
            if (energyState != null) energyState.Changed -= HandleEnergyChanged;
            energyState = state;
            energyState.Changed += HandleEnergyChanged;
            HandleEnergyChanged(energyState.Current, energyState.Total);
        }

        private void HandleEnergyChanged(int current, int total)
        {
            energyText.text = $"ENERGY  {current} / {total}";
            Debug.Log($"[ENERGY] Current / Total: {current} / {total}");
        }

        private void OnDestroy()
        {
            if (energyState != null) energyState.Changed -= HandleEnergyChanged;
        }
    }
}
