using System;
using OneMoreTime.Core.Combat;
using UnityEngine;

namespace OneMoreTime.Gameplay.Combat
{
    /// <summary>Hit points of a ship or enemy. Rules live in <see cref="HealthModel"/>.</summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] Faction faction = Faction.Enemy;
        [SerializeField, Min(1)] int maxHealth = 1;
        [Tooltip("Seconds of invulnerability after taking damage (GDD 03).")]
        [SerializeField, Min(0f)] float invulnerabilitySeconds;
        [Tooltip("Absorbs every hit while on: armour, or a boss part under its shield (GDD 06).")]
        [SerializeField] bool shielded;

        HealthModel model;

        public event Action<Health, DamageInfo> Damaged;
        /// <summary>A shield absorbed this hit. Invulnerability frames stay silent to avoid feedback spam.</summary>
        public event Action<Health, DamageInfo> Blocked;
        public event Action<Health, DamageInfo> Died;

        public Faction Faction => faction;
        public int Current => Model.Current;
        public int Max => Model.Max;
        public float Normalized => Model.Normalized;
        public bool IsDead => Model.IsDead;
        public bool IsInvulnerable => Model.IsInvulnerable(Time.time);
        public bool Shielded
        {
            get => Model.Shielded;
            set => Model.Shielded = value;
        }

        HealthModel Model => model ??= new HealthModel(maxHealth, invulnerabilitySeconds) { Shielded = shielded };

        public bool TakeDamage(in DamageInfo info)
        {
            bool shieldedBeforeHit = Model.Shielded;
            if (!Model.TryDamage(info.Amount, Time.time))
            {
                if (shieldedBeforeHit)
                    Blocked?.Invoke(this, info);
                return false;
            }
            // Read before Damaged: a listener may deal more damage (a combo bonus), and a nested hit
            // that kills reports the death itself.
            bool killed = Model.IsDead;
            Damaged?.Invoke(this, info);
            if (killed)
                Died?.Invoke(this, info);
            return true;
        }

        public void ResetHealth() => Model.Reset();

        void OnValidate() => model = null;
    }
}
