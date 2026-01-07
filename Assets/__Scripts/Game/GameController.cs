using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameController : NetworkBehaviour
{
    public static GameController instance;

    public GameObject obj;
    public NetworkObject netobj;
    public PlayerClasses classes;
    public PlayerMovement move;
    public PlayerStatistics stats;
    public PlayerShooting shoot;
    public CPUcontroller cpu;
    public PlayerReference playerReference;

    private void Awake() {
        if(instance == null) {
            instance = this;
        }
    }

    private void Start() {
        cpu = GetComponent<CPUcontroller>();
    }

    internal void LinkEverything() {

        classes = obj.GetComponent<PlayerClasses>();
        move = obj.GetComponent<PlayerMovement>();
        stats = obj.GetComponent<PlayerStatistics>();
        shoot = obj.GetComponent<PlayerShooting>();
        playerReference = obj.GetComponent<PlayerReference>();

        if (cooldownUI.instance != null) {
            if (IsServer && !IsHost) {
                Destroy(cooldownUI.instance);
            }
        }
    }
}
