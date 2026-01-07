using UnityEngine;

namespace SurvivorMode.Events
{
    [CreateAssetMenu(fileName = "EV_DoubleSpiritDamage", menuName = "Survivor/Events/Double Spirit Damage")]
    public class DoubleSpiritDamageEvent : SurvivorEvent
    {

        public override void Apply()
        {
            SurvivorMode.SurvivorModifiers.spiritDamageMultiplier = 2f;
            SurvivorMode.SurvivorModifiers.humanDamageMultiplier = 0.5f;
        }

        public override void Revert()
        {
            SurvivorMode.SurvivorModifiers.spiritDamageMultiplier = 1f;
            SurvivorMode.SurvivorModifiers.humanDamageMultiplier = 1f;
        }
    }
}


