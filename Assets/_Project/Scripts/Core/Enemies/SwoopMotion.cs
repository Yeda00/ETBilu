using UnityEngine;

namespace OneMoreTime.Core.Enemies
{
    /// <summary>
    /// Path of the small enemy (GDD 04): it flies in from the right in a straight line and, once it
    /// crosses <c>swoopX</c>, bends toward a target height — the ship's, taken at that moment — and keeps
    /// flying left until it leaves the screen. The bend is limited, so a fighter never crosses the whole
    /// screen. Positions are local to the playfield.
    /// </summary>
    public sealed class SwoopMotion
    {
        readonly float speed, swoopX, swoopSeconds, maxTurn;
        float fromY, toY, swoopClock;

        public SwoopMotion(Vector2 start, float speed, float swoopX, float swoopSeconds, float maxTurn)
        {
            Position = start;
            this.speed = Mathf.Max(0f, speed);
            this.swoopX = swoopX;
            this.swoopSeconds = Mathf.Max(0.01f, swoopSeconds);
            this.maxTurn = Mathf.Max(0f, maxTurn);
        }

        public Vector2 Position { get; private set; }
        /// <summary>The swoop has started (the gun arms then).</summary>
        public bool HasSwooped { get; private set; }
        public bool SwoopDone => HasSwooped && swoopClock >= swoopSeconds;
        /// <summary>Height the swoop bends to; meaningful once it has started.</summary>
        public float TargetY => toY;

        /// <summary>
        /// Advance by <paramref name="deltaTime"/>. <paramref name="targetY"/> is where to bend to if the
        /// swoop starts on this step. Returns true on the step the swoop begins.
        /// </summary>
        public bool Tick(float deltaTime, float targetY)
        {
            if (deltaTime <= 0f)
                return false;
            var p = Position;
            p.x -= speed * deltaTime;
            bool started = false;
            if (!HasSwooped)
            {
                if (p.x <= swoopX)
                {
                    HasSwooped = true;
                    started = true;
                    fromY = p.y;
                    toY = Mathf.Clamp(targetY, fromY - maxTurn, fromY + maxTurn);
                }
            }
            else if (swoopClock < swoopSeconds)
            {
                swoopClock = Mathf.Min(swoopSeconds, swoopClock + deltaTime);
                p.y = Mathf.Lerp(fromY, toY, Mathf.SmoothStep(0f, 1f, swoopClock / swoopSeconds));
            }
            Position = p;
            return started;
        }
    }
}
