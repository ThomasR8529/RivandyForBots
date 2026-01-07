using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PhysicsBasedCharacterController
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Platform movement")]
        public Vector3[] destinations;
        public float timeDelay;
        public float timeDelayBeginningEnd;
        public float platformSpeedDamp;
        public bool smoothMovement;
        [Space(10)]

        public bool canTranslate = true;


        [Header("Platform rotation")]
        public Vector3 rotationSpeed;
        [Space(10)]

        public bool canRotate = true;
        public bool canBeMoved = false;

        /**/


    }
}