using UnityEngine;

namespace OneMoreTime.Gameplay.Echo
{
    /// <summary>Tuning of the temporal echo. Starting values from GDD 01/02 — for testing, not final.</summary>
    [CreateAssetMenu(menuName = "One More Time/Echo Config", fileName = "EchoConfig")]
    public sealed class EchoConfig : ScriptableObject
    {
        [Tooltip("Seconds per recording (GDD: 5).")]
        [Min(0.1f)] public float maxRecordSeconds = 5f;

        [Tooltip("Pose samples per second; playback interpolates between them (GDD 09: 20).")]
        [Min(1f)] public float samplesPerSecond = 20f;

        [Tooltip("Echoes alive at once (GDD: up to 3 in the prototype).")]
        [Min(1)] public int maxActiveEchoes = 3;

        [Tooltip("If on, a recording is used up when invoked: record again for the next echo.")]
        public bool consumeRecordingOnInvoke = true;

        [Tooltip("Seconds the echo fades in and out at the ends of its playback.")]
        [Min(0f)] public float fadeSeconds = 0.2f;
    }
}
