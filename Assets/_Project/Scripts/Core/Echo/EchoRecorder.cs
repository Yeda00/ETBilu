using System.Collections.Generic;
using UnityEngine;

namespace OneMoreTime.Core.Echo
{
    /// <summary>
    /// Captures poses at a fixed rate (not every frame) and events at their own instant, for at most
    /// <see cref="MaxDuration"/> seconds. Pure C#: the MonoBehaviour side only feeds it (GDD 09).
    /// </summary>
    public sealed class EchoRecorder
    {
        readonly float maxDuration;
        readonly float sampleInterval;

        List<EchoSample> samples;
        List<EchoEvent> events;
        int sampleIndex;
        float nextSampleTime;
        Vector2 lastPosition;
        float lastRotation;

        public EchoRecorder(float maxDuration, float samplesPerSecond)
        {
            this.maxDuration = Mathf.Max(0.01f, maxDuration);
            sampleInterval = 1f / Mathf.Max(1f, samplesPerSecond);
        }

        public bool IsRecording { get; private set; }
        public float Elapsed { get; private set; }
        public float MaxDuration => maxDuration;
        public bool IsFull => IsRecording && Elapsed >= maxDuration;

        public void Begin(Vector2 position, float rotation)
        {
            samples = new List<EchoSample>(Mathf.CeilToInt(maxDuration / sampleInterval) + 2);
            events = new List<EchoEvent>();
            Elapsed = 0f;
            IsRecording = true;
            lastPosition = position;
            lastRotation = rotation;
            samples.Add(new EchoSample(0f, position, rotation));
            sampleIndex = 1;
            nextSampleTime = sampleInterval;
        }

        /// <summary>Advance by <paramref name="deltaTime"/> with the ship's pose at the end of that step.</summary>
        public void Tick(float deltaTime, Vector2 position, float rotation)
        {
            if (!IsRecording)
                return;

            float start = Elapsed;
            float end = Mathf.Min(Elapsed + Mathf.Max(0f, deltaTime), maxDuration);

            // Emit every sample instant crossed in this step, interpolating the pose within the step.
            while (nextSampleTime <= end + 1e-6f)
            {
                float k = end > start ? (nextSampleTime - start) / (end - start) : 1f;
                samples.Add(new EchoSample(
                    nextSampleTime,
                    Vector2.Lerp(lastPosition, position, k),
                    Mathf.LerpAngle(lastRotation, rotation, k)));
                // From the index, not by accumulation, so long recordings don't drift.
                nextSampleTime = ++sampleIndex * sampleInterval;
            }

            Elapsed = end;
            lastPosition = position;
            lastRotation = rotation;
        }

        public void AddEvent(EchoEventType type, Vector2 position, Vector2 direction)
        {
            if (!IsRecording || Elapsed >= maxDuration)
                return;
            events.Add(new EchoEvent(Elapsed, type, position, direction));
        }

        /// <summary>Stop and hand over the recording. Returns null if nothing usable was captured.</summary>
        public EchoRecording End()
        {
            if (!IsRecording)
                return null;
            IsRecording = false;

            // Close the path with a final pose unless a sample already sits at the end.
            if (Elapsed - samples[samples.Count - 1].time > 1e-4f)
                samples.Add(new EchoSample(Elapsed, lastPosition, lastRotation));

            if (Elapsed <= 0f)
                return null;
            return new EchoRecording(samples, events, Elapsed);
        }

        public void Cancel()
        {
            IsRecording = false;
            Elapsed = 0f;
        }
    }
}
