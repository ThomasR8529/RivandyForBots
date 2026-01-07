using UnityEngine;

namespace SurvivorMode.Events
{
    [CreateAssetMenu(fileName = "EV_DefendStoneHeart", menuName = "Survivor/Events/Defend Stone Heart")]
    public class DefendStoneHeartEvent : SurvivorEvent
    {

        public override void Apply()
        {
            SurvivorMode.SurvivorModifiers.defendStoneHeartThisWave = true;
        }

        public override void Revert()
        {
            SurvivorMode.SurvivorModifiers.defendStoneHeartThisWave = false;
        }

        public override void OnChallengeFailed()
        {
            // Permanently increase monster upgrade level for the run
            SurvivorMode.SurvivorModifiers.permanentMonsterUpgradeLevel++;
            SurvivorMode.SurvivorEventSystem.RequestBroadcastMonsterBonus();
        }
    }
}
