using UnityEngine;

namespace SurvivorMode.Events
{
    [CreateAssetMenu(fileName = "EV_HalfPlayerHealth", menuName = "Survivor/Events/Half Player Health")]
    public class HalfPlayerHealthEvent : SurvivorEvent
    {


        public override void Apply()
        {
            SurvivorMode.SurvivorModifiers.playerMaxHealthMultiplier = 0.5f;
        }

        public override void Revert()
        {
            SurvivorMode.SurvivorModifiers.playerMaxHealthMultiplier = 1f;
        }
    }
}


