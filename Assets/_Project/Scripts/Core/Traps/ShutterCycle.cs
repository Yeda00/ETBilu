using System;

namespace OneMoreTime.Core.Traps
{
    /// <summary>
    /// How open a moving barrier is over time (GDD 05, barreiras móveis): closed, opening, open,
    /// closing, and again. Time 0 is the start of the closed stretch.
    /// </summary>
    public sealed class ShutterCycle
    {
        readonly float closed, open, travel, offset;

        public ShutterCycle(float closedSeconds, float openSeconds, float travelSeconds, float offset = 0f)
        {
            closed = Math.Max(0f, closedSeconds);
            open = Math.Max(0f, openSeconds);
            travel = Math.Max(0f, travelSeconds);
            this.offset = offset;
        }

        public float Period => closed + open + 2f * travel;

        /// <summary>0 = closed, 1 = fully open.</summary>
        public float Openness(float time)
        {
            if (Period <= 0f)
                return 0f;
            float p = (time + offset) % Period;
            if (p < 0f)
                p += Period;

            if (p < closed)
                return 0f;
            p -= closed;
            if (p < travel)
                return p / travel;
            p -= travel;
            if (p < open)
                return 1f;
            p -= open;
            return travel > 0f ? Math.Max(0f, 1f - p / travel) : 0f;
        }
    }
}
