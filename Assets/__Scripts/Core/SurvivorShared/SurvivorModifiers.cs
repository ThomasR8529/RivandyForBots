using UnityEngine;

namespace SurvivorMode
{
    // Central, lightweight flags/multipliers other systems can query.
    // Keep static for easy access anywhere without wiring.
    public static class SurvivorModifiers
    {
        private const int WaveScalingCapWave = 50;
        private const float WaveScalingMaxMultiplier = 100f;

        // Transforms
        public static bool humanTransformBlocked = false;
        public static bool spiritTransformBlocked = false;

        // Multipliers
        public static float spiritDamageMultiplier = 1f;
        public static float humanDamageMultiplier = 1f;
        public static float monsterDamageMultiplier = 1f;
        public static float playerMaxHealthMultiplier = 1f; // Applied at wave start; systems should read and clamp.

        // Spawns/Challenges
        public static int extraBossCountThisWave = 0;
        public static bool defendStoneHeartThisWave = false;
        public static bool pacifist30sChallengeThisWave = false;
        public static bool dangerZoneActiveThisWave = false; // Stay in the circle

        // Persistent progression within the run (carry-over after wave end)
        public static int permanentMonsterUpgradeLevel = 0; // Other systems can scale stats based on this
        public static int failedEventCount = 0; // Increases monster scaling by +50% per failed event

        // Utility: Reset all per-wave modifiers
        public static void ResetPerWave()
        {
            humanTransformBlocked = false;
            spiritTransformBlocked = false;
            spiritDamageMultiplier = 1f;
            humanDamageMultiplier = 1f;
            monsterDamageMultiplier = 1f;
            playerMaxHealthMultiplier = 1f;
            extraBossCountThisWave = 0;
            defendStoneHeartThisWave = false;
            pacifist30sChallengeThisWave = false;
            dangerZoneActiveThisWave = false;
        }

        // Unified monster scaling: applies to both HP and damage in Survivor/Streamer.
        // Waves ramp smoothly from x1 (wave 1) to at most x100 (wave 50 and beyond).
        // Each failed event doubles the current factor but the final value is still capped at x100.
        public static float GetMonsterScalingFactor(int currentWave)
        {
            float waveFactor = ComputeWaveFactor(currentWave);
            float failureFactor = Mathf.Pow(2.0f, Mathf.Max(0, failedEventCount));
            float total = waveFactor * failureFactor;
            // Clamp to avoid runaway exponentials at very high waves or repeated failures.
            total = Mathf.Min(WaveScalingMaxMultiplier, total);
            return Mathf.Max(1f, total);
        }

        private static float ComputeWaveFactor(int currentWave)
        {
            if (currentWave <= 1) return 1f;

            float normalizedWave = Mathf.Clamp01((currentWave - 1f) / (WaveScalingCapWave - 1f));
            // Smooth growth so early waves ramp up slowly and approach the cap around wave 50.
            float eased = normalizedWave * normalizedWave * (3f - 2f * normalizedWave); // SmoothStep
            return Mathf.Lerp(1f, WaveScalingMaxMultiplier, eased);
        }
    }
}
