using UnityEngine;

namespace FourCore
{
    // A delayed ray. Warnings remain visible even when its emitter phases out.
    public sealed class ResonanceBeam
    {
        public GridPosition Origin;
        public Vector2Int Direction;
        public int Remaining=2;
        public int BossCell=-1;
        public ResonanceBeam Copy()=>(ResonanceBeam)MemberwiseClone();
    }
}
