using System;
using UnityEngine;
using Unity.Netcode;

namespace SurvivorMode
{
    // Drop this on your "Stone Heart" GameObject (set on layer 12 in the editor).
    // Monsters can read this singleton to focus the heart during defend events.
    public class StoneHeartTarget : MonoBehaviour
    {
        public static StoneHeartTarget Instance { get; private set; }
        public static event Action<StoneHeartTarget> OnHeartDestroyedServer;

        private NetworkObject networkObject;
        private bool isDestroyed;

        private void Awake()
        {
            networkObject = GetComponent<NetworkObject>();
        }

        private void OnEnable()
        {
            Instance = this;
            // Ensure correct layer if not set (12 expected by damage filters)
            if (gameObject.layer != 12)
            {
                Debug.LogWarning("StoneHeartTarget should be on layer 12 to receive monster attacks.");
            }
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void HandleHeartDestroyed()
        {
            if (isDestroyed)
                return;

            if (!IsServerAuthority())
                return;

            isDestroyed = true;

            if (Instance == this)
            {
                Instance = null;
            }

            OnHeartDestroyedServer?.Invoke(this);

            if (networkObject == null)
            {
                networkObject = GetComponent<NetworkObject>();
            }

            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private bool IsServerAuthority()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        }
    }
}



