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

        HealthModel model;

        public event Action<Health, DamageInfo> Damaged;
        public event Action<Health, DamageInfo> Died;

        public Faction Faction => faction;
        public int Current => Model.Current;
        public int Max => Model.Max;
        public float Normalized => Model.Normalized;
        public bool IsDead => Model.IsDead;
        public bool IsInvulnerable => Model.IsInvulnerable(Time.time);

        HealthModel Model => model ??= new HealthModel(maxHealth, invulnerabilitySeconds);

        public bool TakeDamage(in DamageInfo info)
        {
            if (!Model.TryDamage(info.Amount, Time.time))
                return false;
            Damaged?.Invoke(this, info);
            if (Model.IsDead)
                Died?.Invoke(this, info);
            return true;
        }

        public void ResetHealth() => Model.Reset();

        void OnValidate() => model = null;
    }
}
