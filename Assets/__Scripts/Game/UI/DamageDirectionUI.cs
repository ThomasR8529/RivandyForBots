using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Affiche un arc rouge autour du réticule indiquant la direction du dernier dégât.
/// Version simple: un seul arc réutilisé, rafraîchi à chaque impact, avec fade out.
/// </summary>
public class DamageDirectionUI : MonoBehaviour
{
    public static DamageDirectionUI instance;

    [Header("References")]
    [Tooltip("Caméra de référence pour le calcul de direction (par défaut Camera.main)")]
    public Camera cam;

    [Tooltip("RectTransform de l'arc (un Image UGUI en Filled Radial360)")]
    public RectTransform arcRect;

    [Tooltip("CanvasGroup sur l'arc pour gérer l'alpha (fade)")]
    public CanvasGroup arcCanvasGroup;
    public CanvasGroup bigHitCanvasGroup;

    [Header("Timing")]
    [Tooltip("Temps pendant lequel l'arc reste pleinement visible")]
    public float holdTime = 0.3f;

    [Tooltip("Durée du fade out (alpha 1 -> 0)")]
    public float fadeTime = 0.6f;

    [Tooltip("Utiliser le temps non-scalé (UI) pour le fade")]
    public bool useUnscaledTime = true;

    [Header("Rendering")]
    [Tooltip("Multiplier d'intensité pour l'alpha en cas de dégâts plus forts")]
    [Range(0.1f, 2f)] public float intensityAlphaMultiplier = 1f;

    [Tooltip("Décalage d'angle appliqué à l'arc (en degrés). Utilisez 180 si l'arc est à l'opposé.")]
    public float angleOffsetDeg = 180f;

    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        EnsureCamera();
        // Désactive visuellement au démarrage
        if (arcCanvasGroup != null)
        {
            arcCanvasGroup.alpha = 0f;
            bigHitCanvasGroup.alpha = 0f;
        }
        if (arcRect != null)
        {
            arcRect.gameObject.SetActive(true); // actif mais invisible via alpha
        }
    }

    /// <summary>
    /// Affiche l'indicateur depuis une position monde (position attaquant ou point d'impact).
    /// </summary>
    public void ShowDamageFromWorld(Vector3 sourceWorldPos, float intensity = 1f)
    {
        Debug.Log("ShowDamageFromWorld");
        if (!EnsureCamera() || arcRect == null || arcCanvasGroup == null)
            return;

        Debug.Log("Calcul...");
        Vector3 from = cam.transform.position;
        Vector3 toSource = (sourceWorldPos - from);
        if (toSource.sqrMagnitude < 0.0001f)
        {
            // Évite un NaN si la source == caméra
            toSource = cam.transform.forward;
        }
        ApplyDirectionAndShow(toSource.normalized, intensity);
    }

    /// <summary>
    /// Affiche l'indicateur depuis un Transform (prend sa position monde).
    /// </summary>
    public void ShowDamageFromTransform(Transform source, float intensity = 1f)
    {
        if (source == null) return;
        ShowDamageFromWorld(source.position, intensity);
    }

    /// <summary>
    /// Affiche l'indicateur en fournissant directement une direction monde (vers la source).
    /// Utile si vous n'avez que la direction du tir/impact.
    /// </summary>
    public void ShowDamageFromDirection(Vector3 worldDirection, float intensity = 1f)
    {
        if (!EnsureCamera() || arcRect == null || arcCanvasGroup == null)
            return;
        if (worldDirection.sqrMagnitude < 0.0001f)
            return;

        ApplyDirectionAndShow(worldDirection.normalized, intensity);
    }

    private void ApplyDirectionAndShow(Vector3 worldDirection, float intensity)
    {
        Debug.Log("ApplyDirectionAndShow : " + intensity);
        // Convertit la direction monde en espace caméra pour obtenir un angle écran autour du réticule
        Vector3 camSpace = cam.transform.InverseTransformDirection(worldDirection);
        float angleDeg = Mathf.Atan2(camSpace.x, camSpace.z) * Mathf.Rad2Deg + angleOffsetDeg;

        // Oriente l'arc autour du centre (réticule)
        arcRect.localEulerAngles = new Vector3(0f, 0f, -angleDeg);

        // Affiche avec alpha plein, puis relance un fade
        float baseAlpha = 1f * Mathf.Clamp01(intensity < 0.75f ? 0.75f : intensity) * intensityAlphaMultiplier;
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }
        arcCanvasGroup.alpha = Mathf.Clamp01(baseAlpha);
        bigHitCanvasGroup.alpha = Mathf.Clamp01(baseAlpha);
        _fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        // Hold
        float t = 0f;
        while (t < holdTime)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        // Fade
        float startAlpha = arcCanvasGroup.alpha;
        float d = 0f;
        while (d < fadeTime)
        {
            d += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = fadeTime > 0f ? Mathf.Clamp01(d / fadeTime) : 1f;
            // Ease-out léger
            float eased = 1f - (1f - k) * (1f - k);
            arcCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            bigHitCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            yield return null;
        }
        arcCanvasGroup.alpha = 0f;
        bigHitCanvasGroup.alpha = 0f;
        _fadeRoutine = null;
    }

    /// <summary>
    /// Tente de résoudre la caméra à partir du joueur local (PlayerReference.camera3D) sinon Camera.main.
    /// </summary>
    private bool EnsureCamera()
    {
        if (cam != null)
            return true;

        try
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.SpawnManager != null)
            {
                var localObj = nm.SpawnManager.GetLocalPlayerObject();
                if (localObj != null)
                {
                    var pr = localObj.GetComponent<PlayerReference>();
                    if (pr != null && pr.camera3D != null)
                    {
                        var foundCam = pr.camera3D.GetComponent<Camera>();
                        if (foundCam != null)
                        {
                            cam = foundCam;
                            return true;
                        }
                    }
                }
            }
        }
        catch { }

        // Fallback
        cam = Camera.main;
        return cam != null;
    }
}
