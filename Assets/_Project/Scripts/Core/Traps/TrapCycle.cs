using System;

namespace OneMoreTime.Core.Traps
{
    /// <summary>
    /// A repeating on/off rhythm read from a clock — the laser gate's cycle (GDD 05). The last
    /// <see cref="WarningSeconds"/> of each "off" are the telegraph, so crossing is a read, not a guess.
    /// Time 0 is the start of an "on" period; <see cref="Offset"/> shifts the whole cycle.
    /// </summary>
    public sealed class TrapCycle
    {
        public TrapCycle(float onSeconds, float offSeconds, float offset = 0f, float warningSeconds = 0f)
        {
            OnSeconds = Math.Max(0f, onSeconds);
            OffSeconds = Math.Max(0f, offSeconds);
            Offset = offset;
            WarningSeconds = Math.Min(Math.Max(0f, warningSeconds), OffSeconds);
        }

        public float OnSeconds { get; }
        public float OffSeconds { get; }
        public float Offset { get; }
        public float WarningSeconds { get; }
        public float Period => OnSeconds + OffSeconds;

        /// <summary>Position inside the cycle, in [0, <see cref="Period"/>): "on" first, then "off".</summary>
        public float Phase(float time)
        {
            if (Period <= 0f)
                return 0f;
            float p = (time + Offset) % Period;
            return p < 0f ? p + Period : p;
        }

        /// <summary>Index of the cycle <paramref name="time"/> falls in (0, 1, 2…).</summary>
        public int CycleIndex(float time) => Period > 0f ? (int)Math.Floor((time + Offset) / Period) : 0;

        public bool IsOn(float time) => OffSeconds <= 0f || (OnSeconds > 0f && Phase(time) < OnSeconds);

        /// <summary>Off, but about to turn on: the telegraph window.</summary>
        public bool IsWarning(float time) => !IsOn(time) && Phase(time) >= Period - WarningSeconds;
    }
}
