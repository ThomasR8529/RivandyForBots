using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class AutoAttacks : NetworkBehaviour
{

    NetworkObject playerNetObject;
    Animator animator;
    [SerializeField]
    private int noOfClicks = 0;
    [SerializeField]
    private float lastClickedTime = 0;
    [Header("Temps avant le reset combo2 > combo0")]
    public float maxComboDelay = 0.9f;
    [Header("Secondes entre chaque auto")]
    public float comboCharacterDelay = 1.5f;
    // classInfo.getPlayerClass() == 0 ? 

    private const int AutoAttackSpeedBonusId = 4; // Survivor auto-attack speed bonus id
    // Each stack reduces base delay by 20% (linear, clamped)
    private const float AutoAttackDelayReductionPercentPerStack = 0.20f;

    private PlayerReference playerReference;


#if !UNITY_SERVER

    private void Awake()
    {
        playerReference = transform.parent.GetComponent<PlayerReference>();
    }


    public override void OnNetworkSpawn()
    {
        // IsLocalPlayer doit �tre demand� pour le networkObject en question, pas un sous-�l�ment

        playerNetObject = playerReference.networkObject;
        animator = GetComponent<Animator>();
        //InputManager.inputActions.Player.LeftClick.performed += DoAttack;
    }


    private void Update()
    {
        if (playerNetObject == null)
        {
            playerNetObject = playerReference.networkObject;
            if (!playerNetObject.IsLocalPlayer)
            {
                Destroy(this);
            }
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (playerNetObject.IsLocalPlayer)
        {
            // Si on est � un etat impossible
            if (playerReference.playerStatistics.StunAirSeconds > 0f || playerReference.playerStatistics.StunSeconds > 0f || playerReference.playerStatistics.FreezeSeconds > 0f || playerReference.playerStatistics.SleepSeconds > 0f || playerReference.playerStatistics.ParaSeconds > 0f)
            {
                return;
            }
            if (playerReference.playerStatistics.ServerBlockSeconds > 0f)
            {
                return;
            }
            if (playerReference.playerStatistics.isBehindWho) return;
            if (playerReference.playerStatistics.isDead) return;
            if (playerReference.playerStatistics.playerStatData.health <= 0f) return;
            if (Time.time - lastClickedTime > maxComboDelay)
            {
                noOfClicks = 0;
            }
            if (Input.GetMouseButton(0) && playerReference.playerShooting.enabled)
            {
                float comboDelay = GetEffectiveComboDelay();
                if (Time.time - lastClickedTime > comboDelay)
                {
                    lastClickedTime = Time.time;
                    noOfClicks++;
                    switch (noOfClicks)
                    {
                        case 1:
                            playerReference.playerClasses.CastAutoAttack(0);
                            break;
                        case 2:
                            playerReference.playerClasses.CastAutoAttack(1);
                            break;
                        case 3:
                            playerReference.playerClasses.CastAutoAttack(2);
                            noOfClicks = 0;
                            break;
                    }
                    noOfClicks = Mathf.Clamp(noOfClicks, 0, 3);
                }
            }

        }
    }

    private float GetEffectiveComboDelay()
    {
        float baseDelay = comboCharacterDelay;

        return Mathf.Max(0.05f, baseDelay);
    }

    private static bool IsSurvivorModeActive()
    {
        if (Run.instance == null || Run.instance.CPUcontroller == null)
        {
            return false;
        }

        var zone = Run.instance.CPUcontroller.zone;
        return zone != null && zone.serverMode == GameMode.Survivor;
    }

    public void ResetCombo()
    {
        noOfClicks = 0;
    }

#endif
}

