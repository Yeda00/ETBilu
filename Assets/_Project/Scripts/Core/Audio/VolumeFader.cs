using System;

namespace OneMoreTime.Core.Audio
{
    /// <summary>A volume that glides toward a target at a fixed speed. Used for music fades and the pause duck.</summary>
    public sealed class VolumeFader
    {
        public float Value { get; private set; }
        public float Target { get; set; }

        public VolumeFader(float value = 0f)
        {
            Value = value;
            Target = value;
        }

        /// <summary>Moves toward the target; a full 0-to-1 swing takes <paramref name="secondsForFullRange"/>.</summary>
        public void Step(float dt, float secondsForFullRange)
        {
            if (dt <= 0f)
                return;
            float delta = secondsForFullRange > 0f ? dt / secondsForFullRange : 1f;
            Value = Value < Target ? Math.Min(Target, Value + delta) : Math.Max(Target, Value - delta);
        }

        public void Snap(float value)
        {
            Value = value;
            Target = value;
        }
    }
}
