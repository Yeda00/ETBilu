using System;

namespace OneMoreTime.Core.Combat
{
    /// <summary>Hit points with a brief invulnerability window after each hit (GDD 03).</summary>
    public sealed class HealthModel
    {
        readonly float invulnerabilitySeconds;
        float invulnerableUntil = float.NegativeInfinity;

        public HealthModel(int max, float invulnerabilitySeconds)
        {
            Max = Math.Max(1, max);
            Current = Max;
            this.invulnerabilitySeconds = Math.Max(0f, invulnerabilitySeconds);
        }

        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;
        public float Normalized => (float)Current / Max;

        public bool IsInvulnerable(float now) => now < invulnerableUntil;

        /// <summary>Apply damage at time <paramref name="now"/>. Returns true if it landed.</summary>
        public bool TryDamage(int amount, float now)
        {
            if (amount <= 0 || IsDead || IsInvulnerable(now))
                return false;
            Current = Math.Max(0, Current - amount);
            if (invulnerabilitySeconds > 0f)
                invulnerableUntil = now + invulnerabilitySeconds;
            return true;
        }

        public void Reset()
        {
            Current = Max;
            invulnerableUntil = float.NegativeInfinity;
        }
    }
}
