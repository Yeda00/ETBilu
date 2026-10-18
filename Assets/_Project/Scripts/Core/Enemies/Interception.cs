using UnityEngine;

namespace OneMoreTime.Core.Enemies
{
    /// <summary>Is a shot going to hit me? Lets an enemy dodge before it does (GDD 04, shotgunner).</summary>
    public static class Interception
    {
        /// <summary>
        /// If the shot, flying straight with <paramref name="velocity"/> from <paramref name="shotPosition"/>, passes within
        /// <paramref name="radius"/> of <paramref name="position"/> in the next <paramref name="lookahead"/> seconds, returns
        /// its time to arrival, with <paramref name="side"/> = +1 when I sit above the shot's line (so up is the way out),
        /// −1 when below, 0 when dead on; otherwise a negative number.
        /// </summary>
        public static float TimeToHit(Vector2 position, float radius, Vector2 shotPosition, Vector2 velocity, float lookahead, out float side)
        {
            side = 0f;
            float speedSq = velocity.sqrMagnitude;
            if (speedSq < 1e-6f)
                return -1f;
            Vector2 toMe = position - shotPosition;
            float t = Vector2.Dot(toMe, velocity) / speedSq;   // closest approach
            if (t < 0f || t > lookahead)
                return -1f;
            Vector2 closest = shotPosition + velocity * t;
            Vector2 miss = position - closest;               // from the shot's line to me
            if (miss.sqrMagnitude > radius * radius)
                return -1f;
            side = Mathf.Abs(miss.y) < 0.05f ? 0f : Mathf.Sign(miss.y);
            return t;
        }
    }
}
