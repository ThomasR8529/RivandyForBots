using UnityEngine;

namespace SurvivorMode.Events
{
    [CreateAssetMenu(fileName = "EV_DoubleMonsterDamage", menuName = "Survivor/Events/Double Monster Damage")]
    public class DoubleMonsterDamageEvent : SurvivorEvent
    {

        public override void Apply()
        {
            SurvivorMode.SurvivorModifiers.monsterDamageMultiplier = 2f;
        }

        public override void Revert()
        {
            SurvivorMode.SurvivorModifiers.monsterDamageMultiplier = 1f;
        }
    }
}


