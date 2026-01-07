using UnityEngine;
using SurvivorMode;

namespace SurvivorMode.Events
{
    [CreateAssetMenu(fileName = "EV_StayInCircle", menuName = "Survivor/Events/Stay In Circle")]
    public class StayInCircleEvent : SurvivorEvent
    {
        public override void Apply()
        {
            SurvivorModifiers.dangerZoneActiveThisWave = true;
#if UNITY_SERVER
            var zone = Run.instance?.CPUcontroller?.zone;
            if (zone != null && zone.IsServer)
            {
                zone.MoveSurvivorZoneToRandomMonsterPoint(teleportPlayers: true, avoidCurrent: true);
            }
            else
            {
                Debug.LogWarning("[StayInCircleEvent] Impossible de dÇ¸placer la zone Survivor: zone introuvable ou non serveur.");
            }
#endif
        }

        public override void Revert()
        {
            SurvivorModifiers.dangerZoneActiveThisWave = false;
        }
    }
}


