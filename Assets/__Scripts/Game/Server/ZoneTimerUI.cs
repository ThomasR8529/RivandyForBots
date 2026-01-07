using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ZoneTimerUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text counter;

    private Zone zone; // Référence directe au composant Zone

    private void Awake()
    {
        zone = GetComponent<Zone>();
        if (zone == null)
            Debug.LogError("[ZoneTimerUI] Aucun composant Zone trouvé sur ce GameObject.");
    }

    private void Update()
    {
        if (zone == null || NetworkManager.Singleton == null) return;

        double now = NetworkManager.Singleton.ServerTime.Time;
        double remaining = zone.GetPhaseEndTime() - now; // on ajoute un getter dans Zone
        if (remaining < 0) remaining = 0;

        if (counter != null)
            counter.text = FormatMMSS(remaining);
    }

    private static string FormatMMSS(double seconds)
    {
        int total = Mathf.CeilToInt((float)seconds);
        int mm = total / 60;
        int ss = total % 60;
        return $"{mm:00}:{ss:00}";
    }
}