using UnityEngine;

namespace OneMoreTime.Core.Enemies
{
    /// <summary>
    /// Path of the kamikaze (GDD 04): it flies in from the right and homes in on a target, but can only turn
    /// so fast, so a ship that keeps moving can make it overshoot. Positions are local to the playfield.
    /// </summary>
    public sealed class ChaseMotion
    {
        readonly float speed, turnRate;
        float heading;   // degrees, 180 = flying left

        public ChaseMotion(Vector2 start, float speed, float turnRateDegreesPerSecond, float headingDegrees = 180f)
        {
            Position = start;
            this.speed = Mathf.Max(0f, speed);
            turnRate = Mathf.Max(0f, turnRateDegreesPerSecond);
            heading = headingDegrees;
        }

        public Vector2 Position { get; private set; }
        /// <summary>Direction of flight in degrees (0 = right, 180 = left).</summary>
        public float Heading => heading;

        public void Tick(float deltaTime, Vector2 target)
        {
            if (deltaTime <= 0f)
                return;
            Vector2 toTarget = target - Position;
            if (toTarget.sqrMagnitude > 1e-4f)
            {
                float wanted = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                heading = Mathf.MoveTowardsAngle(heading, wanted, turnRate * deltaTime);
            }
            float rad = heading * Mathf.Deg2Rad;
            Position += new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (speed * deltaTime);
        }
    }
}
