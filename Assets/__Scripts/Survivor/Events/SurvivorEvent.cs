using System;
using UnityEngine;

namespace SurvivorMode.Events
{
    // Base abstract class for a one-round event
    public abstract class SurvivorEvent : ScriptableObject
    {
        [TextArea] public string title;
        [TextArea] public string description;

        [Header("Localization Keys (0 = none)")]
        public int titleKey = 0;
        public int descriptionKey = 0;

        [Header("Announcement Media Keys")]
        [Tooltip("Sprite path under Resources/ to display with the announcement")]
        public string imageResourceKey = string.Empty;
        [Tooltip("Audio key for SoundManager to play on announcement")]
        public string audioKey = string.Empty;

        [Header("Outcome (Success)")]
        public int successTitleKey = 0;
        public int successDescriptionKey = 0;
        public string successImageResourceKey = string.Empty;
        public string successAudioKey = string.Empty;

        [Header("Outcome (Failure)")]
        public int failureTitleKey = 0;
        public int failureDescriptionKey = 0;
        public string failureImageResourceKey = string.Empty;
        public string failureAudioKey = string.Empty;

        // Called on server at wave start when the event is selected
        public abstract void Apply();

        // Called on server at wave end to revert any changes
        public abstract void Revert();

        // Optional validation hook if some events require assets in scene
        public virtual bool IsAvailable() => true;

        // Optional: challenge lifecycle (server-side)
        public virtual void OnChallengeFailed() { }
        public virtual void OnChallengeSucceeded() { }
    }
}
