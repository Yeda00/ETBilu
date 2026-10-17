using System.Collections.Generic;
using UnityEngine;

namespace OneMoreTime.Core.Echo
{
    /// <summary>
    /// A finished recording: poses at a fixed rate plus a separate, time-ordered list of events.
    /// It is data, not a video replay — the world is never rewound (GDD 02, regra fundamental).
    /// </summary>
    public sealed class EchoRecording
    {
        readonly List<EchoSample> samples;
        readonly List<EchoEvent> events;

        public EchoRecording(List<EchoSample> samples, List<EchoEvent> events, float duration)
        {
            this.samples = samples;
            this.events = events;
            Duration = duration;
        }

        public float Duration { get; }
        public IReadOnlyList<EchoSample> Samples => samples;
        public IReadOnlyList<EchoEvent> Events => events;

        /// <summary>Pose at time <paramref name="t"/>, interpolated between the two nearest samples.</summary>
        public EchoSample Evaluate(float t)
        {
            if (samples.Count == 0)
                return default;
            if (t <= samples[0].time)
                return samples[0];
            var last = samples[samples.Count - 1];
            if (t >= last.time)
                return last;

            int hi = FindFirstSampleAfter(t);
            var a = samples[hi - 1];
            var b = samples[hi];
            float span = b.time - a.time;
            float k = span > 0f ? (t - a.time) / span : 1f;
            return new EchoSample(
                t,
                Vector2.LerpUnclamped(a.position, b.position, k),
                Mathf.LerpAngle(a.rotation, b.rotation, k));
        }

        // Binary search: index of the first sample whose time is strictly greater than t.
        int FindFirstSampleAfter(float t)
        {
            int lo = 0, hi = samples.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (samples[mid].time > t) hi = mid;
                else lo = mid + 1;
            }
            return lo;
        }
    }
}
