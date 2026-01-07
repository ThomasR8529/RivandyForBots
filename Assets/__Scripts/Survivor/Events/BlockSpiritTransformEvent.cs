using UnityEngine;
using SurvivorMode;

namespace SurvivorMode.Events
{
    [CreateAssetMenu(fileName = "EV_BlockSpiritTransform", menuName = "Survivor/Events/Block Spirit Transform")]
    public class BlockSpiritTransformEvent : SurvivorEvent
    {
        public override void Apply()
        {
            SurvivorModifiers.spiritTransformBlocked = true;
        }

        public override void Revert()
        {
            SurvivorModifiers.spiritTransformBlocked = false;
        }
    }
}


