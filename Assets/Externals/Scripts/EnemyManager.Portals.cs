using UnityEngine;

namespace FourCore
{
    public sealed partial class EnemyManager
    {
        public void ConfigurePortals(PlayerController controls)
        {
            GameObject root = new("PortalNetwork"); root.transform.SetParent(transform,false);
            portalView = root.AddComponent<PortalView>(); portalView.Initialize(Portals,controls);
        }
        public bool CanDropPortal(GridPosition position) => CanPlaceShield(position) && !shieldState.Contains(position) && Portals.CanDrop(position);
        public bool DropPortal(GridPosition position) => CanDropPortal(position) && Portals.Drop(position);
        public GridPosition ResolvePortalEntry(GridPosition entry)
        {
            // Resolve once per move. Arrival never recursively triggers the opposite end.
            if (Portals.TryDestination(entry,out GridPosition exit) && CanPlaceShield(exit))
            {
                Portals.ConsumeUse();
                return exit;
            }
            return entry;
        }
    }
}
