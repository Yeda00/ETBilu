using System.Collections.Generic;

namespace OneMoreTime.Core.Echo
{
    /// <summary>
    /// Plays a recording on the echo's own clock. Each event fires exactly once, in order,
    /// even if a single frame crosses several of them (GDD 09).
    /// </summary>
    public sealed class EchoPlayback
    {
        readonly EchoRecording recording;
        int nextEvent;

        public EchoPlayback(EchoRecording recording)
        {
            this.recording = recording;
        }

        public float Time { get; private set; }
        public float Duration => recording.Duration;
        public bool IsFinished => Time >= recording.Duration;
        public float Progress => recording.Duration > 0f ? Time / recording.Duration : 1f;
        public EchoSample Pose => recording.Evaluate(Time);

        /// <summary>Advance the clock and append every event now due to <paramref name="due"/>.</summary>
        public void Advance(float deltaTime, List<EchoEvent> due)
        {
            if (deltaTime > 0f)
                Time = System.Math.Min(Time + deltaTime, recording.Duration);

            var events = recording.Events;
            while (nextEvent < events.Count && events[nextEvent].time <= Time)
            {
                due?.Add(events[nextEvent]);
                nextEvent++;
            }
        }
    }
}
