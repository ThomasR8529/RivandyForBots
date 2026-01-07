using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

namespace Game
{
    [RequireComponent(typeof(Spell))]
    public class SpellAntiMagic : MonoBehaviour
    {
        [Header("Filtrage")]
        [Tooltip("Uniquement les objets sur ces Layers seront considérés comme un sort.")]
        public LayerMask[] spellLayers;

        [Header("Règles")]
        [Tooltip("Respecter cannotBeCancelled sur les sorts adverses.")]
        public bool respectCannotBeCancelled = true;

        [Tooltip("Détruire aussi ce sort anti-magie lorsqu'il dissipe un autre sort.")]
        public bool destroySelfOnImpact = false;

        [Header("Impact FX (instancié en ClientRpc)")]
        public GameObject impactEffectPrefab;
        public float impactEffectLifetime = 5f;

        [Header("Recherche locale (pour sorts non-réseau)")]
        [Tooltip("Rayon de recherche autour du point d'impact pour trouver des sorts locaux.")]
        public float localSearchRadius = 1.6f;

        [Header("Debug")]
        [Tooltip("Affiche des logs détaillés côté client pour suivre la dissipation.")]
        public bool debugLogs = true;

        private Spell _selfSpell;

        private void Log(string msg)
        {
            if (debugLogs)
                Debug.Log($"{msg}", this);
        }

        private void Awake()
        {
            _selfSpell = GetComponent<Spell>();
        }

        // --- Détections physiques (peu importe la side) ---
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("[ANTI MAGIC] Trigger entered " + other.gameObject.name);
            TryRequestDissipate(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log("[ANTI MAGIC] collision entered");
            var contact = collision.GetContact(0);
            TryRequestDissipate(collision.collider.gameObject);
        }

        public void ExternalDissipate(GameObject hittingSpell)
        {
            TryRequestDissipate(hittingSpell);
        }

        private void TryRequestDissipate(GameObject other)
        {
            // Vérifie si le layer de l'objet est présent dans AU MOINS un LayerMask de spellLayers
            Debug.Log("Il rentre bien dans tryrequest");
            bool inAllowedLayer = false;
            foreach (var mask in spellLayers)
            {
                if ((mask.value & (1 << other.layer)) != 0)
                {
                    inAllowedLayer = true;
                    break;
                }
            }

            if (!inAllowedLayer)
            {
                Debug.Log("not allowed trigger");
                return;
            }

            var otherSpell = other.GetComponent<Spell>();
            if (otherSpell == null || otherSpell == _selfSpell)
            {
                Log($"Impact ignoré: {(otherSpell == null ? "pas de Spell sur la cible" : "cible = self")}");
                return;
            }

            // Même caster ? On ignore (ne dissipe pas ses propres sorts)
            if (otherSpell.casterRef != null && _selfSpell.casterRef != null && otherSpell.casterRef.IsLocalPlayer && _selfSpell.casterRef.IsLocalPlayer)
            {
                return;
            }

            if (respectCannotBeCancelled && otherSpell.cannotBeCancelled)
            {
                Log("Impact ignoré: targetSpell.cannotBeCancelled == true et respect activé.");
                return;
            }
            Destroy(Instantiate(impactEffectPrefab, otherSpell.casterRef.transform), 2f);
            Destroy(other.gameObject);
            Destroy(Instantiate(impactEffectPrefab, other.transform), 2f);
        }
    }
}