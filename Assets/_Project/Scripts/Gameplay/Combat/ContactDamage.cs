using UnityEngine;

namespace OneMoreTime.Gameplay.Combat
{
    /// <summary>Hurts the other faction on touch (an enemy ramming the ship).</summary>
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] Faction faction = Faction.Enemy;
        [SerializeField, Min(1)] int damage = 1;

        void OnTriggerEnter(Collider other) => TryHit(other);
        void OnTriggerStay(Collider other) => TryHit(other);   // invulnerability frames throttle repeats

        void TryHit(Collider other)
        {
            var target = other.GetComponentInParent<Health>();
            if (target != null && target.Faction != faction)
                target.TakeDamage(new DamageInfo(damage, DamageKind.Contact, ShotSource.Enemy));
        }
    }
}
