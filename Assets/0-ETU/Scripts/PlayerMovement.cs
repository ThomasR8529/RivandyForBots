using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{

    public Follow follow;

    [Header("Movements")]
    public Rigidbody rb;
    public Transform cameraJoueur;
    public GameObject cameraMinimap;
    public GameObject playerUi;

    public PlayerReference playerReference;


    [SerializeField]
    private AudioSource audioSrc;
    [SerializeField]
    private AudioClip[] footSteps;
    [SerializeField]
    private int stepIndex;


    // Liste des etats qui affectent la moveSpeed: Liste<MoveFactor (multiplicateur), MoveSeconds (dur�e), MoveTime (temps attribu�)
    public List<List<float>> movementSpeedFactors;
    public List<List<float>> jumpFactors;
    // Blockers windows: each entry stores [durationSeconds, startTime]
    public List<List<float>> jumpBlockers;
    public List<List<float>> dashBlockers;

    public float defaultJumpHeight = 5f;
    public float jumpHeight = 5f;

    private float defaultSpeed;


    float nextTime;

    public bool parachute = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); // Assurez-vous que le Rigidbody est attach� au GameObject

        playerReference = GetComponent<PlayerReference>();
        movementSpeedFactors = new List<List<float>>();
        jumpFactors = new List<List<float>>();
        jumpBlockers = new List<List<float>>();
        dashBlockers = new List<List<float>>();
        if (playerReference.characterBrain != null && playerReference.characterBrain.normalMovement != null)
        {
            defaultSpeed = playerReference.characterBrain.normalMovement.planarMovementParameters.baseSpeedLimit;
        }
    }

    private void PlayFootSounds()
    {
        while (playerReference.characterActor.IsGrounded && playerReference.characterActor.RigidbodyComponent.Velocity.x != 0 && nextTime < Time.time || (playerReference.characterActor.IsGrounded && playerReference.characterActor.RigidbodyComponent.Velocity.z != 0 && nextTime < Time.time))
        {
            nextTime = Time.time + 0.15f;
            if (!audioSrc.isPlaying)
            {
                stepIndex = stepIndex < 4 ? stepIndex + 1 : 0;
                audioSrc.clip = footSteps[stepIndex];
                audioSrc.Play();
            }
        }
    }


    public override void OnNetworkSpawn()
    {
        if (!IsHost)
        {
            if (!playerReference) return;
            if (!playerReference.playerClasses.isMonster)
            {
                if (!IsLocalPlayer)
                {
                    // controller.enabled = false;
                    if (cameraJoueur != null) cameraJoueur.GetComponent<Camera>().enabled = false;
                    if (cameraMinimap != null) cameraMinimap.SetActive(false);
                    if (playerUi != null) playerUi.SetActive(false);
                    // On ne veut pas que le joueur soit visible dans la minimap

                }
                else
                {
                    // SkinnedMeshRenderer skin = transform.Find("Skin").GetComponent<SkinnedMeshRenderer>();
                    // skin.enabled = false;
                }
            }
        }
    }

    FixedJoint fixedJoint;// Pour coller les joueurs entre eux.

    // Update is called once per frame
    void Update()
    {
        if (IsClient && IsLocalPlayer)
        {
            // Gestion du facteur de mouvement
            MoveStateChanging();
            // Gestion du facteur de la taille du saut
            JumpStateChanging();
            // Gestion du facteur de l'avalement
            StayBehindPlayer();
            // Gestion du facteur du bouclier
            ShieldChanging();


            movePlayer();
            PlayFootSounds();

            if (playerReference.playerStatistics.isBehindWho != null)
            {
                if (fixedJoint == null)
                {
                    fixedJoint = gameObject.AddComponent<FixedJoint>();
                    fixedJoint.connectedBody = playerReference.playerStatistics.isBehindWho.rigidBody;
                    Debug.Log("On colle on colle");
                }

            }
            else
            {
                if (fixedJoint != null)
                {
                    Destroy(fixedJoint);
                }
            }

        }
        if (IsServer)
        {
            if (playerReference.playerClasses.isMonster)
            {
                StayBehindMonster();
            }
            //movePlayer();
            if (transform.position.y < -250)
            {
                transform.position = new Vector3(transform.position.x, 500, transform.position.z);
            }
        }

        if (playerReference.playerStatistics.ReincarnationPower < 100)
        {
            playerReference.playerStatistics.ReincarnationPower += 2 * Time.deltaTime;
        }
    }

    private void MoveStateChanging()
    {
        if (playerReference.characterBrain == null || playerReference.characterBrain.normalMovement == null)
            return;

        float factor = 1f;
        foreach (var f in movementSpeedFactors.ToList())
        {
            if (f[2] + f[1] <= Time.time)
                movementSpeedFactors.Remove(f);
            else
                factor *= f[0];
        }

        playerReference.characterBrain.normalMovement.planarMovementParameters.baseSpeedLimit = defaultSpeed * factor;
    }

    private void StayBehindPlayer()
    {
        /*
         *     // Liste<MoveFactor (multiplicateur), MoveSeconds (dur�e), MoveTime (temps attribu�)
         */
        if (playerReference.playerStatistics.isBehindWho != null)
        {
            transform.position = playerReference.playerStatistics.isBehindWho.transform.position + playerReference.playerStatistics.isBehindVector;
        }
    }

    private void StayBehindMonster()
    {
        if (playerReference.playerStatistics.isBehindWho != null)
        {
            playerReference.follow.GetAgent().enabled = false;
            playerReference.transform.position = playerReference.playerStatistics.isBehindWho.transform.position + playerReference.playerStatistics.isBehindVector;
        }
    }

    private void JumpStateChanging()
    {

        if (jumpFactors.Count > 0)
        {
            float jumpmoy = defaultJumpHeight;
            foreach (List<float> facteur in jumpFactors.ToList())
            {
                if (Time.time > facteur[2] + facteur[1])
                {
                    jumpFactors.Remove(facteur);
                    continue;
                }
                jumpmoy *= facteur[0];
            }
            jumpHeight = jumpmoy;
        }
        else
        {
            jumpHeight = defaultJumpHeight;
        }
    }

    public bool IsJumpBlocked()
    {
        // Cleanup expired
        foreach (var b in jumpBlockers.ToList())
        {
            if (Time.time > b[1] + b[0])
            {
                jumpBlockers.Remove(b);
            }
        }
        return jumpBlockers.Any(b => Time.time <= b[1] + b[0]);
    }

    public bool IsDashBlocked()
    {
        // Cleanup expired
        foreach (var b in dashBlockers.ToList())
        {
            if (Time.time > b[1] + b[0])
            {
                dashBlockers.Remove(b);
            }
        }
        return dashBlockers.Any(b => Time.time <= b[1] + b[0]);
    }

    private void ShieldChanging()
    {
        if (playerReference.playerStatistics.shieldList.Count > 0)
        {
            float totalShield = 0;
            foreach (List<float> shield in playerReference.playerStatistics.shieldList.ToList())
            {
                if (shield[2] + shield[1] < Time.time)
                {
                    playerReference.playerStatistics.playerStatData.shield -= shield[2];
                    playerReference.playerStatistics.shieldList.Remove(shield);
                }
                else
                {
                    totalShield += shield[0];
                }
            }
            playerReference.playerStatistics.playerStatData.shield = totalShield;
        }
        if (playerReference.playerStatistics.playerStatData.shield > 0)
        {
            if (!cooldownUI.instance.localPlayerShield.gameObject.activeSelf) cooldownUI.instance.localPlayerShield.gameObject.SetActive(true);
            if (playerReference.playerStatistics.shieldBar != null)
            {
                playerReference.playerStatistics.shieldBar.value = playerReference.playerStatistics.playerStatData.shield;
                if (playerReference.networkObject.IsLocalPlayer)
                {
                    cooldownUI.instance.localPlayerShield.fillAmount = playerReference.playerStatistics.playerStatData.shield / 100f;
                    cooldownUI.instance.localPlayerShieldText.text = playerReference.playerStatistics.playerStatData.shield.ToString();
                }
                else
                {
                    Debug.Log("Affichage shieldbar");
                    playerReference.playerStatistics.shieldBar.gameObject.SetActive(true);
                }
            }
        }
        if (playerReference.playerStatistics.playerStatData.shield <= 0)
        {
            if (playerReference.playerStatistics.shieldBar != null)
            {
                playerReference.playerStatistics.shieldBar.value = 0;
                if (playerReference.networkObject.IsLocalPlayer)
                {
                    cooldownUI.instance.localPlayerShield.fillAmount = 0;
                    cooldownUI.instance.localPlayerShieldText.text = "0";
                    cooldownUI.instance.localPlayerShield.gameObject.SetActive(false);
                }
                else
                {
                    if (playerReference.playerStatistics.shieldBar != null)
                    {
                        Debug.Log("shieldbar cach�");
                        playerReference.playerStatistics.shieldBar.gameObject.SetActive(false);
                    }
                }
            }
        }
    }


    void movePlayer()
    {

        /*        if (isGrounded && velocity.y < 0)
                {
                    velocity.y = -2f;
                }*/

        if (!playerReference.playerClasses.isMonster)
        {
            if (playerReference.playerClasses.animator != null && playerReference.playerClasses.animator.isActiveAndEnabled)
            {
                // S'il vient de spawn;
                if (parachute)
                {

                    if (parachute && playerReference.characterActor.IsGrounded) parachute = false;
                    playerReference.playerClasses.animator.SetBool("parachute", !playerReference.characterActor.IsGrounded);
                    ChangeAnimationServerRpc("parachute", !playerReference.characterActor.IsGrounded);
                }
                else
                {
                    if (!playerReference.playerClasses.animator.GetBool("spellPriority") && playerReference.playerClasses.animator.GetBool("isFalling") != !playerReference.characterActor.IsGrounded)
                    {
                        playerReference.playerClasses.animator.SetBool("isFalling", !playerReference.characterActor.IsGrounded);
                        ChangeAnimationServerRpc("isFalling", !playerReference.characterActor.IsGrounded);
                    }
                }
                if (playerReference.playerClasses.animator.GetBool("spellPriority") && playerReference.playerClasses.animator.GetBool("isFalling"))
                {
                    playerReference.playerClasses.animator.SetBool("isFalling", false);
                    ChangeAnimationServerRpc("isFalling", false);
                }
            }


            if (playerReference.playerClasses.animator != null && playerReference.playerClasses.animator.isInitialized)
            {
                if (!((InputSystemCustom)playerReference.characterBrain.inputHandlerSettings.InputHandler).IsBlocked() && !(playerReference.playerStatistics.StunAirSeconds > 0 || playerReference.playerStatistics.StunSeconds > 0 || playerReference.playerStatistics.FreezeSeconds > 0 || playerReference.playerStatistics.SleepSeconds > 0 || playerReference.playerStatistics.ParaSeconds > 0 || playerReference.playerStatistics.isBehindWho || playerReference.playerStatistics.ServerBlockSeconds > 0f))
                {
                    // Si on ne se d�place pas (et que le Vector3 est donc � z�ro, on ne met pas l'animation de marche).
                    if (playerReference.playerClasses.animator.isInitialized)
                    {
                        if (playerReference.characterActor.RigidbodyComponent.Velocity.x != 0 && playerReference.characterActor.RigidbodyComponent.Velocity.z != 0)
                        {
                            // Fonctionnement normal en r�incarnation
                            if (playerReference.PlayerReincarnation.IsReincarnation)
                            {
                                if (!playerReference.playerClasses.animator.GetBool("isWalking"))
                                {
                                    playerReference.playerClasses.animator.SetBool("isWalking", true);
                                    ChangeAnimationServerRpc("isWalking", true);
                                }
                            }
                            // Fonctionnement personnage
                            else
                            {
                                Vector3 localVelocity = transform.InverseTransformDirection(playerReference.characterActor.Velocity);
                                Vector3 velocityRotation = localVelocity;
                                // En arri�re
                                if (velocityRotation.z < -4)
                                {
                                    DisableFront();
                                    DisableLeft();
                                    DisableRight();
                                    if (!playerReference.playerClasses.animator.GetBool("isWalkingBack"))
                                    {
                                        playerReference.playerClasses.animator.SetBool("isWalkingBack", true);
                                        ChangeAnimationServerRpc("isWalkingBack", true);
                                    }
                                }

                                // En avant
                                else
                                {
                                    // Vers la gauche
                                    if (velocityRotation.x < -4)
                                    {
                                        DisableFront();
                                        DisableBack();
                                        DisableRight();
                                        if (!playerReference.playerClasses.animator.GetBool("isWalkingLeft"))
                                        {
                                            playerReference.playerClasses.animator.SetBool("isWalkingLeft", true);
                                            ChangeAnimationServerRpc("isWalkingLeft", true);
                                        }
                                    }
                                    // Vers la droite
                                    else if (velocityRotation.x > 4)
                                    {
                                        DisableFront();
                                        DisableBack();
                                        DisableLeft();
                                        if (!playerReference.playerClasses.animator.GetBool("isWalkingRight"))
                                        {
                                            playerReference.playerClasses.animator.SetBool("isWalkingRight", true);
                                            ChangeAnimationServerRpc("isWalkingRight", true);
                                        }
                                    }
                                    // Tout droit
                                    else
                                    {
                                        DisableBack();
                                        DisableLeft();
                                        DisableRight();
                                        if (!playerReference.playerClasses.animator.GetBool("isWalking"))
                                        {
                                            playerReference.playerClasses.animator.SetBool("isWalking", true);
                                            ChangeAnimationServerRpc("isWalking", true);
                                        }
                                    }
                                }
                            }

                        }
                        else
                        {
                            if (!(playerReference.PlayerReincarnation.IsReincarnation || (playerReference.playerStatistics.playerDataGame.classId != 0 && playerReference.playerStatistics.playerDataGame.classId != 8)))
                            {
                                DisableFront();
                                DisableBack();
                                DisableLeft();
                                DisableRight();
                            }
                            else
                            {
                                DisableFront();
                            }

                        }

                    }
                }
                else
                {
                    if (!(playerReference.PlayerReincarnation.IsReincarnation || (playerReference.playerStatistics.playerDataGame.classId != 0 && playerReference.playerStatistics.playerDataGame.classId != 8)))
                    {
                        DisableFront();
                        DisableBack();
                        DisableLeft();
                        DisableRight();
                    }
                    else
                    {
                        DisableFront();
                    }
                }
            }

        }
    }

    private void DisableFront()
    {
        if (playerReference.playerClasses.animator.GetBool("isWalking"))
        {
            playerReference.playerClasses.animator.SetBool("isWalking", false);
            ChangeAnimationServerRpc("isWalking", false);
        }
    }

    private void DisableBack()
    {
        if (playerReference.playerClasses.animator.GetBool("isWalkingBack"))
        {
            playerReference.playerClasses.animator.SetBool("isWalkingBack", false);
            ChangeAnimationServerRpc("isWalkingBack", false);
        }
    }

    private void DisableLeft()
    {
        if (playerReference.playerClasses.animator.GetBool("isWalkingLeft"))
        {
            playerReference.playerClasses.animator.SetBool("isWalkingLeft", false);
            ChangeAnimationServerRpc("isWalkingLeft", false);
        }
    }

    private void DisableRight()
    {
        if (playerReference.playerClasses.animator.GetBool("isWalkingRight"))
        {
            playerReference.playerClasses.animator.SetBool("isWalkingRight", false);
            ChangeAnimationServerRpc("isWalkingRight", false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeAnimationServerRpc(string animationName, bool status, ServerRpcParams serverParam = default)
    {
        List<ulong> liste = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        liste.Remove(serverParam.Receive.SenderClientId);
        ClientRpcParams clientParam = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = liste
            }
        };
        ApplyAnimationClientRpc(animationName, status, clientParam);
    }

    [ClientRpc]
    public void ApplyAnimationClientRpc(string animationName, bool status, ClientRpcParams _ = default)
    {
        if (playerReference.playerClasses.animator != null)
        {
            playerReference.playerClasses.animator.SetBool(animationName, status);
        }
    }

    [ClientRpc]
    public void ApplyAnimationMonsterClientRpc(NetworkObjectReference netObject, string animationName, bool status)
    {
        if (netObject.TryGet(out NetworkObject monsterNet))
        {
            GameObject monster = monsterNet.gameObject;
            Animator animMonster = monster.GetComponent<Animator>();
            if (animMonster != null)
            {
                animMonster.SetBool(animationName, status);
            }
        }
    }
}

