using UnityEngine;

namespace SurvivorMode
{
    // Attach to a trigger collider that represents the safe zone area.
    // Sets PlayerStatistics.isInsideZone and can fail the "danger zone" challenge on exit.
    [RequireComponent(typeof(Collider))]
    public class DangerZoneArea : MonoBehaviour
    {
        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var pr = other.GetComponent<PlayerReference>();
            if (pr == null || pr.playerStatistics == null) return;
            pr.playerStatistics.isInsideZone = true;
        }

        private void OnTriggerExit(Collider other)
        {
            var pr = other.GetComponent<PlayerReference>();
            if (pr == null || pr.playerStatistics == null) return;
            pr.playerStatistics.isInsideZone = false;

            // If challenge active: leaving the zone fails it (server authority)
            if (Run.instance != null && Run.instance.IsServer && SurvivorModifiers.dangerZoneActiveThisWave)
            {
                if (pr.playerClasses != null && !pr.playerClasses.isMonster)
                {
                    SurvivorEventSystem.Instance?.ReportChallengeFailed();
                }
            }
        }
    }
}

