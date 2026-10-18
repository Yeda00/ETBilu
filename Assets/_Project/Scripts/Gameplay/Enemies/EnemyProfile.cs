using System;

namespace OneMoreTime.Gameplay.Enemies
{
    /// <summary>How an enemy's gun is set up for one model (GDD 04). Fire rate, burst and spread are what tell the models apart.</summary>
    [Serializable]
    public struct GunTuning
    {
        public bool fires;
        public bool aimAtShip;
        [UnityEngine.Min(0f)] public float firstDelay;
        [UnityEngine.Min(0.05f)] public float interval;
        [UnityEngine.Min(1)] public int shotsPerBurst;
        [UnityEngine.Min(0f)] public float shotGap;
        [UnityEngine.Min(0f)] public float spread;
        [UnityEngine.Min(0f)] public float windup;
        [UnityEngine.Min(0)] public int maxBursts;
        [UnityEngine.Tooltip("0 uses the gun's own shot; k uses its k-th alternative.")]
        [UnityEngine.Min(0)] public int shotVariant;
    }

    /// <summary>A bomber's settings: the kamikaze and the tanker (GDD 04).</summary>
    [Serializable]
    public struct BlastTuning
    {
        public bool explodes;
        [UnityEngine.Tooltip("The ship this close lights the fuse.")]
        [UnityEngine.Min(0f)] public float triggerRadius;
        [UnityEngine.Min(0f)] public float fuseSeconds;
        [UnityEngine.Tooltip("Goes off by itself this long after appearing; 0 = never.")]
        [UnityEngine.Min(0f)] public float autoSeconds;
        [UnityEngine.Min(0.1f)] public float blastRadius;
        [UnityEngine.Min(1)] public int damage;
    }

    /// <summary>Dodging of incoming shots: the shotgunner (GDD 04).</summary>
    [Serializable]
    public struct DodgeTuning
    {
        public bool dodges;
        [UnityEngine.Tooltip("A shot that will pass this close, soon, is dodged.")]
        [UnityEngine.Min(0.1f)] public float senseRadius;
        [UnityEngine.Min(0.1f)] public float lookahead;
        [UnityEngine.Min(0.1f)] public float distance;
        [UnityEngine.Min(0.05f)] public float seconds;
        [UnityEngine.Min(0f)] public float cooldown;
    }

    /// <summary>A gunship that follows the ship vertically, locks that line, then fires a horizontal beam.</summary>
    [Serializable]
    public struct TrackingLaserTuning
    {
        public bool enabled;
        [UnityEngine.Min(0.1f)] public float followSpeed;
        [UnityEngine.Min(0.05f)] public float trackingSeconds;
        [UnityEngine.Min(0.05f)] public float chargeSeconds;
        [UnityEngine.Min(0.05f)] public float fireSeconds;
        [UnityEngine.Min(0.05f)] public float recoverySeconds;
        [UnityEngine.Min(0.1f)] public float beamWidth;
        [UnityEngine.Min(1)] public int damage;
        [UnityEngine.Min(1)] public int bursts;
    }

    /// <summary>
    /// What one model of an enemy type does (GDD 04): its path, toughness, gun and any bomb or dodge. The enemy
    /// picks the profile whose index is the model's, so the player learns each model by its look.
    /// Starting values for testing, set by the Bootstrap › Enemy Variants menu.
    /// </summary>
    [Serializable]
    public sealed class EnemyProfile
    {
        public string name;
        public MovementPattern pattern;
        [UnityEngine.Min(1)] public int health = 1;
        [UnityEngine.Min(0)] public int scoreValue = 100;

        [UnityEngine.Header("Swoop / Chase / Serpentine")]
        [UnityEngine.Min(0f)] public float speed = 7f;
        [UnityEngine.Range(-1f, 1f)] public float swoopAt = 0.3f;
        [UnityEngine.Min(0.05f)] public float swoopSeconds = 0.75f;
        [UnityEngine.Min(0f)] public float maxSwoop = 5f;
        [UnityEngine.Tooltip("Chase: degrees per second it can turn.")]
        [UnityEngine.Min(0f)] public float turnRate = 100f;

        [UnityEngine.Header("Serpentine")]
        [UnityEngine.Min(0f)] public float waveAmplitude = 1.4f;
        [UnityEngine.Min(0f)] public float waveFrequency = 0.55f;

        [UnityEngine.Header("Hover")]
        [UnityEngine.Min(0f)] public float holdFromRightEdge = 4.5f;
        [UnityEngine.Min(0.05f)] public float enterSeconds = 1.25f;
        [UnityEngine.Min(0f)] public float holdSeconds = 6.5f;
        [UnityEngine.Min(0f)] public float bobAmplitude = 0.8f;
        [UnityEngine.Min(0f)] public float bobFrequency = 0.45f;
        [UnityEngine.Min(0f)] public float leaveAcceleration = 6f;

        [UnityEngine.Header("Weapons and tricks")]
        public GunTuning gun;
        public BlastTuning blast;
        public DodgeTuning dodge;
        public TrackingLaserTuning trackingLaser;
    }
}
