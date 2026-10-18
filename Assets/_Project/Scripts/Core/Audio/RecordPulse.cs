using System;

namespace OneMoreTime.Core.Audio
{
    /// <summary>
    /// The recording sound (GDD 08): an electronic pulse that rises in frequency as the route fills up. Beats
    /// come faster and higher as <c>progress</c> goes from 0 to 1.
    /// </summary>
    public sealed class RecordPulse
    {
        readonly float slowInterval;
        readonly float fastInterval;
        readonly float lowPitch;
        readonly float highPitch;
        float timer;

        public RecordPulse(float slowInterval = 0.32f, float fastInterval = 0.07f, float lowPitch = 0.8f, float highPitch = 1.9f)
        {
            this.slowInterval = slowInterval;
            this.fastInterval = fastInterval;
            this.lowPitch = lowPitch;
            this.highPitch = highPitch;
        }

        /// <summary>Restarts the pulse; the first beat comes on the next tick.</summary>
        public void Begin() => timer = 0f;

        /// <summary>True when a beat is due. At most one beat per tick, so a long frame never bursts.</summary>
        public bool Tick(float dt, float progress)
        {
            timer -= dt;
            if (timer > 0f)
                return false;
            timer = Lerp(slowInterval, fastInterval, Clamp01(progress));
            return true;
        }

        public float PitchAt(float progress) => Lerp(lowPitch, highPitch, Clamp01(progress));

        static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));
        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
