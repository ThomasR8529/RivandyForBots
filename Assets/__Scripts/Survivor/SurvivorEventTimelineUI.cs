using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Playables;
using System.Collections;

namespace SurvivorMode
{
    // Timeline-driven UI announcer for Survivor events with localization + audio.
    public class SurvivorEventTimelineUI : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private LocalizedText titleLocalized;
        [SerializeField] private LocalizedText descriptionLocalized;
        [SerializeField] private TextMeshProUGUI titleTMPFallback;
        [SerializeField] private TextMeshProUGUI descriptionTMPFallback;
        [SerializeField] private Image eventImage;

        [Header("Timelines")]
        [SerializeField] private PlayableDirector startDirector;
        [SerializeField] private PlayableAsset startTimeline;

        [SerializeField] private PlayableAsset closeTimeline;
        [SerializeField] private PlayableAsset successTimeline;
        [SerializeField] private PlayableAsset failureTimeline;

        private bool _announcementActive;

        private void OnEnable()
        {
            SurvivorEventSystem.OnEventStartedClient += HandleStartText;
            SurvivorEventSystem.OnEventStartedClientKeys += HandleStartKeys;
            SurvivorEventSystem.OnChallengeOutcomeClient += HandleOutcome;
            SurvivorEventSystem.OnEventEndedClient += HandleEnded;
        }

        private void OnDisable()
        {
            _announcementActive = false;
            SurvivorEventSystem.OnEventStartedClient -= HandleStartText;
            SurvivorEventSystem.OnEventStartedClientKeys -= HandleStartKeys;
            SurvivorEventSystem.OnChallengeOutcomeClient -= HandleOutcome;
            SurvivorEventSystem.OnEventEndedClient -= HandleEnded;
        }

        private void HandleStartKeys(int titleKey, int descriptionKey, string imageKey, string audioKey)
        {
            // Localized texts
            if (titleLocalized && titleKey > 0) titleLocalized.SetText(titleKey);
            if (descriptionLocalized && descriptionKey > 0) descriptionLocalized.SetText(descriptionKey);

            // Image
            if (!string.IsNullOrEmpty(imageKey) && eventImage)
            {
                var sprite = Resources.Load<Sprite>(imageKey);
                if (sprite) eventImage.sprite = sprite;
            }

            // Audio
            if (!string.IsNullOrEmpty(audioKey) && SoundManager.Instance != null)
            {
                StartCoroutine(PlayAudioInSeconds(2f, audioKey));
            }

            // Play timeline
            _announcementActive = true;
            PlayTimeline(startTimeline);
        }

        private IEnumerator PlayAudioInSeconds(float seconds, string audioKey)
        {
            yield return new WaitForSeconds(seconds);
            SoundManager.Instance.Play2D(audioKey);
        }

        private void HandleStartText(string title, string description)
        {
            // Use only if no localization is provided; otherwise keys handler will already have run
            if (titleLocalized == null && titleTMPFallback)
                titleTMPFallback.text = title;
            if (descriptionLocalized == null && descriptionTMPFallback)
                descriptionTMPFallback.text = description;

            _announcementActive = true;
            PlayTimeline(startTimeline);
        }

        private void HandleOutcome(bool success, int titleKey, int descriptionKey, string imageKey, string audioKey)
        {
            if (titleLocalized && titleKey > 0) titleLocalized.SetText(titleKey);
            if (descriptionLocalized && descriptionKey > 0) descriptionLocalized.SetText(descriptionKey);

            if (!string.IsNullOrEmpty(imageKey) && eventImage)
            {
                var sprite = Resources.Load<Sprite>(imageKey);
                if (sprite) eventImage.sprite = sprite;
            }

            if (!string.IsNullOrEmpty(audioKey) && SoundManager.Instance != null)
            {
                SoundManager.Instance.Play2D(audioKey);
            }

            _announcementActive = true;
            PlayTimeline(success ? successTimeline : failureTimeline);
        }

        private void HandleEnded()
        {
            _announcementActive = false;
            Debug.Log("Play end");
            PlayTimeline(closeTimeline, stopIfMissing: true);
        }

        private void PlayTimeline(PlayableAsset timeline, bool stopIfMissing = false)
        {
            Debug.Log("PlayTIMELINE" + timeline.name);
            if (!startDirector)
                return;

            if (timeline != null)
            {
                startDirector.playableAsset = timeline;
                startDirector.time = 0;
                startDirector.Play();
                return;
            }

            if (stopIfMissing)
            {
                startDirector.Stop();
                return;
            }

            if (startDirector.playableAsset != null)
            {
                startDirector.time = 0;
                startDirector.Play();
            }
        }
    }
}
