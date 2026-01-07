using System.Collections;
using UnityEngine;


namespace PhysicsBasedCharacterController
{
    public class SelfDestruct : MonoBehaviour
    {
        [Header("Self destruction parameters")]
        public float timeDelay = 2f;

        public bool serverOnly;

        private void Start()
        {
            if (!serverOnly)
            {
                StartCoroutine(WaitSeconds(timeDelay));
            }

#if UNITY_SERVER
                if(serverOnly)
                StartCoroutine(WaitSeconds(timeDelay)); 

#endif
        }


        private IEnumerator WaitSeconds(float _time)
        {
            yield return new WaitForSeconds(_time);
            Destroy(gameObject);
        }
    }
}