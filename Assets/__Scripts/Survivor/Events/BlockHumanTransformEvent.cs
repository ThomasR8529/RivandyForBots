using UnityEngine;
using SurvivorMode;

namespace SurvivorMode.Events
{
    [CreateAssetMenu(fileName = "EV_BlockHumanTransform", menuName = "Survivor/Events/Block Human Transform")]
    public class BlockHumanTransformEvent : SurvivorEvent
    {


        public override void Apply()
        {
            SurvivorModifiers.humanTransformBlocked = true;
        }

        public override void Revert()
        {
            SurvivorModifiers.humanTransformBlocked = false;
        }
    }
}


