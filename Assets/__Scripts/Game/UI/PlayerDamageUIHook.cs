using UnityEngine;

/// <summary>
/// Petit pont d'appel pour l'UI de direction des dégâts.
/// A attacher sur le joueur (ou un objet central) et appeler depuis votre système de dégâts.
/// </summary>
public class PlayerDamageUIHook : MonoBehaviour
{
    [Tooltip("Référence vers le composant DamageDirectionUI du HUD")]
    private DamageDirectionUI damageUI;

    /// <summary>
    /// A appeler quand le joueur prend un dégât en fournissant la position monde de la source.
    /// </summary>

    public void OnDamagedBySourcePosition(Vector3 sourceWorldPos, float intensity = 1f)
    {
        if (damageUI == null)
        {
            damageUI = cooldownUI.instance.damageDirectionUI;
        }
        if (damageUI != null)
        {
            damageUI.ShowDamageFromWorld(sourceWorldPos, intensity);
        }
    }

    /// <summary>
    /// Variante si vous n'avez que la direction monde de l'impact (vers la source).
    /// </summary>
    public void OnDamagedByWorldDirection(Vector3 worldDirection, float intensity = 1f)
    {
        if (damageUI == null)
        {
            damageUI = cooldownUI.instance.damageDirectionUI;
        }
        if (damageUI != null)
        {
            damageUI.ShowDamageFromDirection(worldDirection, intensity);
        }
    }
}

