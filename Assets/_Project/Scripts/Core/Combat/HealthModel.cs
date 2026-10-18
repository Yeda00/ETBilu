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
        /// <summary>Armour or an energy shield: every hit is absorbed while on (GDD 06, núcleo blindado).</summary>
        public bool Shielded { get; set; }

        public bool IsInvulnerable(float now) => now < invulnerableUntil;

        /// <summary>Extends, but never shortens, the current invulnerability window.</summary>
        public void GrantInvulnerability(float seconds, float now)
        {
            if (seconds > 0f && !IsDead)
                invulnerableUntil = Math.Max(invulnerableUntil, now + seconds);
        }

        /// <summary>Apply damage at time <paramref name="now"/>. Returns true if it landed.</summary>
        public bool TryDamage(int amount, float now)
        {
            if (amount <= 0 || IsDead || Shielded || IsInvulnerable(now))
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
