using UnityEngine;

namespace SurvivorMode.Events
{
    [CreateAssetMenu(fileName = "EV_TripleBoss", menuName = "Survivor/Events/Spawn 3 Bosses")]
    public class TripleBossEvent : SurvivorEvent
    {

        public override void Apply()
        {
            SurvivorMode.SurvivorModifiers.extraBossCountThisWave = 3;
        }

        public override void Revert()
        {
            SurvivorMode.SurvivorModifiers.extraBossCountThisWave = 0;
        }
    }
}


