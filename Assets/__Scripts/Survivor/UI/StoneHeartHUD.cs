using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SurvivorMode.UI
{
    // Attach this to an always-loaded UI Canvas. Assign the panel, slider and text in inspector.
    public class StoneHeartHUD : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthLabel;

        private PlayerStatistics _heartStats;

        private void OnEnable()
        {
            SurvivorEventSystem.OnStoneHeartAnnouncedClient += OnHeartAnnounced;
            SurvivorEventSystem.OnEventEndedClient += OnEventEnded;
            Hide();
        }

        private void OnDisable()
        {
            SurvivorEventSystem.OnStoneHeartAnnouncedClient -= OnHeartAnnounced;
            SurvivorEventSystem.OnEventEndedClient -= OnEventEnded;
        }

        private void OnHeartAnnounced(NetworkObjectReference heartRef)
        {
            if (heartRef.TryGet(out NetworkObject no))
            {
                var pr = no.GetComponent<PlayerReference>();
                _heartStats = pr != null ? pr.playerStatistics : no.GetComponent<PlayerStatistics>();
                if (_heartStats != null)
                {
                    Show();
                    StartCoroutine(UpdateRoutine());
                }
            }
        }

        private void OnEventEnded()
        {
            Hide();
            _heartStats = null;
        }

        private IEnumerator UpdateRoutine()
        {
            while (panel != null && panel.activeSelf && _heartStats != null)
            {
                var stats = _heartStats.playerStatData;
                if (healthSlider != null)
                {
                    healthSlider.maxValue = Mathf.Max(1f, stats.maxHealth);
                    healthSlider.value = Mathf.Clamp(stats.health, 0f, stats.maxHealth);
                }
                if (healthLabel != null)
                {
                    healthLabel.text = $"{Mathf.RoundToInt(Mathf.Max(0, stats.health))} / {Mathf.RoundToInt(Mathf.Max(1, stats.maxHealth))}";
                }
                yield return null;
            }
        }

        private void Show()
        {
            if (panel != null) panel.SetActive(true);
        }

        private void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}

