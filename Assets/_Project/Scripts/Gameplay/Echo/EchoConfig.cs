using UnityEngine;

namespace OneMoreTime.Gameplay.Echo
{
    /// <summary>Tuning of the temporal echo. Starting values from GDD 01/02 — for testing, not final.</summary>
    [CreateAssetMenu(menuName = "One More Time/Echo Config", fileName = "EchoConfig")]
    public sealed class EchoConfig : ScriptableObject
    {
        [Tooltip("Maximum time between planting the anchor and returning to it.")]
        [Min(0.1f)] public float maxRecordSeconds = 3.5f;

        [Tooltip("Letting go of Record sooner than this cancels the recording: no echo, no cooldown.")]
        [Min(0f)] public float minRecordSeconds = 0.25f;

        [Tooltip("Pose samples per second; playback interpolates between them (GDD 09: 20).")]
        [Min(1f)] public float samplesPerSecond = 20f;

        [Tooltip("Echoes alive at once. One keeps the first version readable and intentional.")]
        [Min(1)] public int maxActiveEchoes = 1;

        [Tooltip("If on, a recording is used up when invoked: record again for the next echo.")]
        public bool consumeRecordingOnInvoke = true;

        [Tooltip("Seconds the echo fades in and out at the ends of its playback.")]
        [Min(0f)] public float fadeSeconds = 0.2f;

        [Tooltip("Delay before another anchor may be planted after returning.")]
        [Min(0f)] public float cooldownSeconds = 2f;

        [Tooltip("Safety window after snapping back, with the same spin as the dodge (match ShipMotor's dodge time). This does not restore health.")]
        [Min(0f)] public float returnInvulnerabilitySeconds = 0.4f;

        [Tooltip("Enemy bullets inside this radius are erased at the return point.")]
        [Min(0f)] public float returnBulletClearRadius = 1.6f;

        [Tooltip("How far behind the player the harmless preview echo follows while recording.")]
        [Min(0f)] public float previewDelaySeconds = 0.18f;
    }
}
