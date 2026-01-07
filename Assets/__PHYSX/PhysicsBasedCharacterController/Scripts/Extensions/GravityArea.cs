using System.Collections.Generic;
using UnityEngine;


namespace PhysicsBasedCharacterController
{
    [RequireComponent(typeof(Collider))]
    public class GravityArea : MonoBehaviour
    {
        [Header("Area properties")]
        public Vector3 gravityForce = new Vector3(0f, 1.37f, 0f);


        /**/


    }
}