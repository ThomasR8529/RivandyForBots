using System.Collections.Generic;
using UnityEngine;


namespace PhysicsBasedCharacterController
{
    [RequireComponent(typeof(Collider))]
    public class Ladder : MonoBehaviour
    {
        [Header("Climb properties")]
        public float climbSpeed = 7f;
        public float forceOnDismount = -200f;
        public InputReader input;

    }
}