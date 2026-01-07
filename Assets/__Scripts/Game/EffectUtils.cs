using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EffectUtils : MonoBehaviour
{
    [Header("Renderers to clone materials from")]
    [SerializeField] private Renderer[] renderers;

    // Noms de propriétés du shader (adaptez selon vos propres noms)
    [Header("Shader property names")]
    [SerializeField] private string cutoutProperty = "_Cutout";
    [SerializeField] private string rimLightProperty = "_RimLightUnfill";

    [Header("Valeur par défaut du RimLightUnfill")]
    [SerializeField] private float defaultRimLightValue = 6f;

    // Liste de tous les matériaux clonés
    private List<Material> clonedMaterials = new List<Material>();

    // Pour interrompre proprement les coroutines
    private Coroutine cutoutCoroutine;
    private Coroutine rimLightCoroutine;

    public bool isDead;

    public bool started;

    private void Start()
    {
#if !UNITY_SERVER
        if (!started)
        {
            started = true;
            defaultRimLightValue = 6f;
            // 1. Récupérer tous les renderers enfants
            renderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer rend in renderers)
            {
                if (rend == null || rend.gameObject.layer == 8 || rend is ParticleSystemRenderer) continue;

                // Récupération des matériaux de l’objet
                Material[] originalMats = rend.materials;
                List<Material> validMats = new List<Material>();

                // 2. Pour chaque matériau :
                for (int i = 0; i < originalMats.Length; i++)
                {
                    // Cloner le matériau
                    Material matClone = new Material(originalMats[i]);

                    // 3. Vérifier la présence de la propriété _RimLightColor
                    if (matClone.HasProperty("_RimLightColor"))
                    {
                        validMats.Add(matClone);
                        clonedMaterials.Add(matClone);
                    }
                    else
                    {
                        // S'il n'a pas la propriété, on ne l'ajoute pas.
                        // => Le sous-matériau sera donc enlevé du Renderer
                    }
                }

                // 4. Mettre à jour la liste des matériaux du renderer
                rend.materials = validMats.ToArray();
            }
            HideDissolve(0f);
            ShowDissolve(4.0f);
        }
#endif
    }

    /// <summary>
    /// Lancer un dissolve qui "cache" (Cutout 0 → 1)
    /// </summary>
    public void HideDissolve(float duration)
    {
        isDead = true;
        if (cutoutCoroutine != null) StopCoroutine(cutoutCoroutine);
        cutoutCoroutine = StartCoroutine(AnimateCutout(0f, 0.5f, duration));
    }

    /// <summary>
    /// Lancer un dissolve qui "montre" (Cutout 1 → 0)
    /// </summary>
    public void ShowDissolve(float duration)
    {
        isDead = false;
        if (cutoutCoroutine != null) StopCoroutine(cutoutCoroutine);
        cutoutCoroutine = StartCoroutine(AnimateCutout(0.5f, 0f, duration));
    }

    private IEnumerator AnimateCutout(float start, float end, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float currentValue = Mathf.Lerp(start, end, t);

            // Appliquer la valeur de cutout à tous les matériaux clonés
            foreach (var mat in clonedMaterials)
            {
                mat.SetFloat(cutoutProperty, currentValue);
            }

            yield return null;
        }

        // S'assurer qu'on finit bien la transition
        foreach (var mat in clonedMaterials)
        {
            mat.SetFloat(cutoutProperty, end);
        }
    }

    /// <summary>
    /// Lancer le "HitEffect" : RimLight qui descend de sa valeur par défaut à 0, puis remonte
    /// </summary>
    public void HitEffect(float duration)
    {
        if (rimLightCoroutine != null) StopCoroutine(rimLightCoroutine);
        rimLightCoroutine = StartCoroutine(AnimateRimLight(defaultRimLightValue, 0f, duration));
    }

    private IEnumerator AnimateRimLight(float start, float end, float duration)
    {
        Color originalColor = clonedMaterials[0].GetColor("_RimLightColor");

        Color hdrColor = new Color(5.60348749f, 1.2906481f, 0f, 1f);

        foreach (var mat in clonedMaterials)
        {
            mat.SetFloat("_RimLightSoftness", 1);
            mat.SetFloat("_RimLightInLight", 0);
            mat.SetColor("_RimLightColor", hdrColor);
        }

        // 3) Animation principale
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float currentValue = Mathf.Lerp(start, end, t);

            foreach (var mat in clonedMaterials)
            {
                mat.SetFloat(rimLightProperty, currentValue);
            }

            yield return null;
        }
        foreach (var mat in clonedMaterials)
        {
            mat.SetFloat(rimLightProperty, end);
        }

        // 4) Animation retour (par exemple sur 0.5s)
        float returnDuration = 0.5f;
        time = 0f;
        while (time < returnDuration)
        {
            time += Time.deltaTime;
            float t = time / returnDuration;
            float currentValue = Mathf.Lerp(end, start, t);

            foreach (var mat in clonedMaterials)
            {
                mat.SetFloat(rimLightProperty, currentValue);
            }

            yield return null;
        }
        foreach (var mat in clonedMaterials)
        {
            mat.SetFloat("_RimLightSoftness", 0);
            mat.SetFloat("_RimLightInLight", 1);
            mat.SetFloat(rimLightProperty, start);
        }

        for (int i = 0; i < clonedMaterials.Count; i++)
        {
            clonedMaterials[i].SetColor("_RimLightColor", originalColor);
        }
    }
}