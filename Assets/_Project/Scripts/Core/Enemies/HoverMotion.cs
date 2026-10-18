using UnityEngine;

namespace OneMoreTime.Core.Enemies
{
    public enum HoverPhase
    {
        Entering = 0,
        Holding = 1,
        Leaving = 2,
    }

    /// <summary>
    /// Path of the medium enemy (GDD 04): it slides in from the right and brakes at a hold column, bobs
    /// there for a while — the gun only fires in this phase — then accelerates out to the left.
    /// A function of time alone, so the same wave always moves the same way. Positions are local to the
    /// playfield.
    /// </summary>
    public sealed class HoverMotion
    {
        readonly Vector2 start;
        readonly float holdX, enterSeconds, holdSeconds, bobAmplitude, bobFrequency, bobPhase, leaveAcceleration;

        public HoverMotion(Vector2 start, float holdX, float enterSeconds, float holdSeconds,
            float bobAmplitude, float bobFrequency, float bobPhase, float leaveAcceleration)
        {
            this.start = start;
            this.holdX = holdX;
            this.enterSeconds = Mathf.Max(0.01f, enterSeconds);
            this.holdSeconds = Mathf.Max(0f, holdSeconds);
            this.bobAmplitude = Mathf.Max(0f, bobAmplitude);
            this.bobFrequency = Mathf.Max(0f, bobFrequency);
            this.bobPhase = bobPhase;
            this.leaveAcceleration = Mathf.Max(0f, leaveAcceleration);
        }

        /// <summary>Seconds since the enemy appeared.</summary>
        public float Time { get; private set; }
        public HoverPhase Phase => PhaseAt(Time);
        public Vector2 Position => Evaluate(Time);

        public void Tick(float deltaTime)
        {
            if (deltaTime > 0f)
                Time += deltaTime;
        }

        public HoverPhase PhaseAt(float t) =>
            t < enterSeconds ? HoverPhase.Entering : t < enterSeconds + holdSeconds ? HoverPhase.Holding : HoverPhase.Leaving;

        public Vector2 Evaluate(float t)
        {
            if (t < enterSeconds)
            {
                float k = Mathf.Clamp01(t / enterSeconds);
                float ease = 1f - (1f - k) * (1f - k);   // fast in, brakes at the column
                return new Vector2(Mathf.Lerp(start.x, holdX, ease), start.y);
            }
            float held = Mathf.Min(t - enterSeconds, holdSeconds);
            float y = start.y + Bob(held);
            if (t < enterSeconds + holdSeconds)
                return new Vector2(holdX, y);
            float leaving = t - enterSeconds - holdSeconds;
            return new Vector2(holdX - 0.5f * leaveAcceleration * leaving * leaving, y);
        }

        // Starts at zero, so the bob joins the entry without a jump.
        float Bob(float held)
        {
            float w = 2f * Mathf.PI * bobFrequency;
            return bobAmplitude * (Mathf.Sin(w * held + bobPhase) - Mathf.Sin(bobPhase));
        }
    }
}
