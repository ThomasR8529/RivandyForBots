using TMPro;
using UnityEngine;

namespace SurvivorMode
{
    // Simple client-side UI hook to show current event text
    public class SurvivorEventUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private GameObject panel;

        private void OnEnable()
        {
            SurvivorEventSystem.OnEventStartedClient += HandleStarted;
            SurvivorEventSystem.OnEventEndedClient += HandleEnded;
        }

        private void OnDisable()
        {
            SurvivorEventSystem.OnEventStartedClient -= HandleStarted;
            SurvivorEventSystem.OnEventEndedClient -= HandleEnded;
        }

        private void HandleStarted(string title, string desc)
        {
            if (titleText) titleText.SetText(title);
            if (descriptionText) descriptionText.SetText(desc);
            if (panel) panel.SetActive(true);
        }

        private void HandleEnded()
        {
            if (panel) panel.SetActive(false);
        }
    }
}

