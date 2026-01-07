using UnityEngine;

namespace SurvivorMode.Events
{
    [CreateAssetMenu(fileName = "EV_Pacifist30s", menuName = "Survivor/Events/Pacifist 30s")]
    public class Pacifist30sEvent : SurvivorEvent
    {


        public override void Apply()
        {
            SurvivorMode.SurvivorModifiers.pacifist30sChallengeThisWave = true;
        }

        public override void Revert()
        {
            SurvivorMode.SurvivorModifiers.pacifist30sChallengeThisWave = false;
        }

        public override void OnChallengeFailed()
        {
            SurvivorMode.SurvivorModifiers.permanentMonsterUpgradeLevel++;
            SurvivorMode.SurvivorEventSystem.RequestBroadcastMonsterBonus();
        }
    }
}
