using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class BattleRoyaleManager : NetworkBehaviour
{
    public float waitDuration = 30f;
    public TextMeshProUGUI countdownText;

    private bool waitingStarted;
    private bool battleStarted;
    private readonly List<PlayerReference> players = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        Debug.Log("Battle Royale Manager -- ONLINE");
    }

    public void OnPlayerAdded(PlayerReference pr)
    {
        if (!players.Contains(pr))
        {
            players.Add(pr);
        }

        if (!waitingStarted)
        {
            waitingStarted = true;
            StartCoroutine(StartBattleRoutine());
        }
    }

    private IEnumerator StartBattleRoutine()
    {
        float timeLeft = waitDuration;
        while (timeLeft > 0f)
        {
            UpdateCountdownClientRpc((int)timeLeft);
            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }
        UpdateCountdownClientRpc(0);
        StartBattle();
    }

    private void StartBattle()
    {
        battleStarted = true;
        Run.instance.CPUcontroller.zone.battleStarted = true;
        // Spawn initial Battle Royale monsters right after countdown ends
        if (Run.instance != null && Run.instance.CPUcontroller != null && Run.instance.CPUcontroller.zone != null)
        {
            Run.instance.CPUcontroller.zone.SpawnInitialBattleRoyaleMonsters();
        }
    }

    private void FreezePlayer(PlayerReference playerRef, bool freeze)
    {
        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { playerRef.OwnerClientId }
            }
        };

        playerRef.playerStatistics.ApplyStunClientRpc(
            playerRef.playerStatistics.StunSeconds,
            playerRef.playerStatistics.FreezeSeconds,
            playerRef.playerStatistics.SleepSeconds,
            playerRef.playerStatistics.ParaSeconds,
            playerRef.playerStatistics.StunAirSeconds,
            rpcParams);
    }

    [ClientRpc]
    private void UpdateCountdownClientRpc(int timeLeft)
    {
        if (countdownText != null)
        {
            PlayerReference playerReference = GameController.instance.playerReference;
            playerReference.playerStatistics.ServerBlockSeconds = timeLeft;
            playerReference.characterBrain.inputHandlerSettings.InputHandler.isServerBlock = true;
            countdownText.gameObject.SetActive(timeLeft > 0);
            countdownText.text = timeLeft.ToString();
            switch (timeLeft)
            {
                case 15:
                    Run.instance.CPUcontroller.zone.PlayZoneAudio(11);
                    break;
                case 5:
                    Run.instance.CPUcontroller.zone.PlayZoneAudio(6);
                    break;
                case 4:
                    Run.instance.CPUcontroller.zone.PlayZoneAudio(7);
                    break;
                case 3:
                    Run.instance.CPUcontroller.zone.PlayZoneAudio(8);
                    break;
                case 2:
                    Run.instance.CPUcontroller.zone.PlayZoneAudio(9);
                    break;
                case 1:
                    Run.instance.CPUcontroller.zone.PlayZoneAudio(10);
                    break;
            }
        }
    }
}
