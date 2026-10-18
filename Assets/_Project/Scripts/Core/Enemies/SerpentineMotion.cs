using UnityEngine;

namespace OneMoreTime.Core.Enemies
{
    /// <summary>
    /// A readable sine-wave path for the interceptor: constant horizontal speed and a vertical rhythm
    /// around its entry lane. The phase lets formations interleave instead of stacking on one curve.
    /// Positions are local to the playfield.
    /// </summary>
    public sealed class SerpentineMotion
    {
        readonly float startX, centreY, speed, amplitude, angularFrequency, phase;

        public SerpentineMotion(Vector2 start, float speed, float amplitude, float frequency, float phase)
        {
            startX = start.x;
            centreY = start.y;
            this.speed = Mathf.Max(0f, speed);
            this.amplitude = Mathf.Max(0f, amplitude);
            angularFrequency = 2f * Mathf.PI * Mathf.Max(0f, frequency);
            this.phase = phase;
            Position = start;
        }

        public float Time { get; private set; }
        public Vector2 Position { get; private set; }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;
            Time += deltaTime;
            Position = Evaluate(Time);
        }

        public Vector2 Evaluate(float time)
        {
            float t = Mathf.Max(0f, time);
            // Subtract the initial sine so every variant joins its authored entry height without a pop.
            // The half factor keeps the full phase-shifted displacement inside the authored amplitude.
            float y = centreY + amplitude * 0.5f * (Mathf.Sin(angularFrequency * t + phase) - Mathf.Sin(phase));
            return new Vector2(startX - speed * t, y);
        }
    }
}
