using System;

namespace OneMoreTime.Core.Audio
{
    /// <summary>
    /// Keeps a bullet-hell from turning into noise: a cue may not repeat faster than its minimum interval
    /// and may not have more than a set number of voices alive at once. The caller says how long the
    /// sound lasts so the voice frees itself.
    /// </summary>
    public sealed class CueThrottle
    {
        const int MaxTrackedVoices = 16;

        readonly float[] lastStart;
        readonly float[][] ends;

        public CueThrottle(int cueCount)
        {
            lastStart = new float[cueCount];
            ends = new float[cueCount][];
            for (int i = 0; i < cueCount; i++)
            {
                lastStart[i] = float.NegativeInfinity;
                ends[i] = new float[MaxTrackedVoices];
                Array.Fill(ends[i], float.NegativeInfinity);
            }
        }

        /// <summary>True, and the voice is recorded, when the cue may play now. <paramref name="maxVoices"/> of 0 means no limit.</summary>
        public bool TryAcquire(int cue, float now, float minInterval, int maxVoices, float duration)
        {
            if (cue < 0 || cue >= lastStart.Length)
                return false;
            if (now - lastStart[cue] < minInterval)
                return false;

            float[] slots = ends[cue];
            int alive = 0;
            int free = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] > now)
                    alive++;
                else if (free < 0)
                    free = i;
            }

            if (maxVoices > 0 && alive >= maxVoices)
                return false;
            if (free >= 0)
                slots[free] = now + Math.Max(0f, duration);
            lastStart[cue] = now;
            return true;
        }
    }
}
