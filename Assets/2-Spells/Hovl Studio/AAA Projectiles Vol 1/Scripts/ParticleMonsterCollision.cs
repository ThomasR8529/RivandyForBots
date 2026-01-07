using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class ParticleMonsterCollision : NetworkBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (IsHost)
        {
            if (GetComponent<ParticleSystem>().isEmitting)
            {
                if (other.gameObject.layer == 3)
                {
                    Debug.Log("[HOST] Monster: Nous avons trouvé un joueur");
                    Debug.Log(other);
                    ParticleServerRpc(other.gameObject.name);
                }
            }

        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (IsHost)
        {
            if (GetComponent<ParticleSystem>().isEmitting)
            {
                if (other.gameObject.layer == 3)
                {
                    Debug.Log("[HOST] Monster: Nous avons trouvé un joueur");
                    Debug.Log(other);
                    ParticleServerRpc(other.gameObject.name);
                }
            }

        }
    }

    [ServerRpc]
    void ParticleServerRpc(string playername)
    {
        // ParticleServerRpc est exécuté sur le serveur: le serveur cherche le joueur
        // et execute TakeDamage() de son côté.
        // Si la cible est un joueur: OK on peut envoyer l'info au serveur.
        // Debug.Log(playername + "doit être utilisé pour trouver les stats");

        // Je dois partitionner en ClientSide car on peut pas chercher un GameObject côté serveur.
        ParticleClientSide(playername, 10f);
    }

    void ParticleClientSide(string playername, float damage)
    {
        GameObject player = GameObject.Find(playername);
        var statistics = player.GetComponent<PlayerStatistics>();
/*        statistics.TakeDamage(damage);*/
        // On doit remplacer le take damage par un un truc comme dans SpellDamageTrigger
    }
}