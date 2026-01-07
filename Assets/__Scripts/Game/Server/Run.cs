using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Run : NetworkBehaviour
{


    static public Run instance;
    // playerData a un gameObject en clé, en valeur le dictionnaire des données.
    [HideInInspector] public CPUcontroller CPUcontroller;

    private void Start()
    {
        instance = this;
        CPUcontroller = GetComponent<CPUcontroller>();

    }

}
