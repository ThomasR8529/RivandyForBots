using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.SceneManagement;
using Game;

public class SpellDamageCollision : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField]
    private Spell spell;
    private Dictionary<ulong, float> touchedPlayers;


    float timeDid;

    private void Awake()
    {
        if (spell == null) spell = GetComponent<Spell>();
        touchedPlayers = new Dictionary<ulong, float>();
    }

    private void Update()
    {
        // Pour chaque joueur de la liste...
        // Si le Time.time > value + spell.damageEachSeconds...
        // ...Supprimer la clé.
        if (spell != null)
        {
            if (!spell.spellDamageOnce)
            {
                if (Time.time > timeDid + spell.spellDamagePerSecond)
                {
                    timeDid = Time.time;
                    foreach (var item in touchedPlayers.ToList())
                    {
                        touchedPlayers.Remove(item.Key);
                    }

                }
            }
        }
    }

#if !UNITY_SERVER
    private void OnCollisionEnter(Collision collision)
    {
        if (spell == null)
            return;
        if (spell.GetCaster() != collision.collider.gameObject)
        {
            PlayerReference casterRef = spell.GetCaster().GetComponent<PlayerReference>();
            if (collision.collider.gameObject.layer == 3 || collision.collider.gameObject.layer == 7 || (casterRef.playerClasses.isMonster && collision.collider.gameObject.layer == 9) || collision.collider.gameObject.layer == 12)
            {

                PlayerReference otherReference = collision.collider.gameObject.GetComponent<PlayerReference>();

                if ((otherReference.playerDash?.isDashing ?? false) || (otherReference.playerStatistics?.isInvincible ?? false))
                    return;
                if (casterRef != null && otherReference != null && PlayerStatistics.AreAllies(casterRef, otherReference))
                    return;

                // Si nous n'avons pas … attaquer des monstres ou des joueurs et que la cible est un monstre ou un monstre: NOP
                if (casterRef.playerStatistics.NotAttackMonsters && collision.collider.gameObject.layer == 7)
                    return;
                if (casterRef.playerStatistics.NotAttackPlayers && collision.collider.gameObject.layer == 3)
                    return;
                if (casterRef.playerStatistics.NotAttackPlayers && collision.collider.gameObject.layer == 9)
                    return;
                if (casterRef.playerStatistics.NotAttackHeart && collision.collider.gameObject.layer == 12)
                    return;

                if (spell.spellDamageOnce)
                {
                    NetworkObject netOther = collision.collider.gameObject.GetComponent<NetworkObject>();
                    // Et que le joueur n'est pas déjà dans la liste des objets déjà touchés.
                    if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId))
                    {
                        // On l'ajoute des les objets touchés
                        touchedPlayers.Add(netOther.NetworkObjectId, Time.time);

                        SpellDamageParticle.CheckStatePlayer(otherReference, spell);

                    }
                }

            }
        }
    }
#endif


#if UNITY_SERVER

    private void OnCollisionEnter(Collision collision)
    {
        {
            if (spell == null)
                return;
            if (spell.GetCaster() != collision.collider.gameObject.gameObject)
            {
                if (collision.collider.gameObject.layer == 3 || collision.collider.gameObject.layer == 7 || (spell.GetCaster().gameObject.GetComponent<PlayerClasses>().isMonster && collision.collider.gameObject.layer == 9) || collision.collider.gameObject.layer == 12)
                {
                    PlayerReference stats = collision.collider.gameObject.GetComponent<PlayerReference>();
                    if ((stats.playerDash?.isDashing ?? false) || (stats.playerStatistics?.isInvincible ?? false))
                        return;
                    PlayerReference statsp = spell.GetCaster().GetComponent<PlayerReference>();

                    if (statsp != null && stats != null && PlayerStatistics.AreAllies(statsp, stats))
                        return;

                    // Si nous n'avons pas … attaquer des monstres ou des joueurs et que la cible est un monstre ou un monstre: NOP
                    if (statsp.playerStatistics.NotAttackMonsters && collision.collider.gameObject.layer == 7)
                        return;
                    if (statsp.playerStatistics.NotAttackPlayers && collision.collider.gameObject.layer == 3)
                        return;
                    if (statsp.playerStatistics.NotAttackPlayers && collision.collider.gameObject.layer == 9)
                        return;
                    if (statsp.playerStatistics.NotAttackHeart && collision.collider.gameObject.layer == 12)
                        return;
                    // Si on doit taper le joueur adverse une seule fois.
                    NetworkObject netOther = collision.collider.gameObject.GetComponent<NetworkObject>();

                    if (spell.spellDamageOnce)
                    {
                        // Et que le joueur n'est pas déjà dans la liste des objets déjà touchés.
                        if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId))
                        {
                            // On l'ajoute des les objets touchés
                            touchedPlayers.Add(netOther.NetworkObjectId, Time.time);

                            // On récupère et on lui applique les dégats, effets, etc.
                            stats.playerStatistics.SetAttacker(spell.GetCaster());

                            SpellDamageParticle.CheckStatePlayer(stats, spell);

                        }
                    }
                    // Si on tape un joueur qu'on peut taper plusieurs fois
                    else
                    {
                        // Et que le joueur n'est pas déjà dans la liste des objets déjà touchés.
                        if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId))
                        {
                            // On récupère et on lui applique les dégats, effets, etc.
                            stats.playerStatistics.SetAttacker(spell.GetCaster());

                            SpellDamageParticle.CheckStatePlayer(stats, spell);

                        }
                    }
                }
            }
        }
    }

#endif

}
