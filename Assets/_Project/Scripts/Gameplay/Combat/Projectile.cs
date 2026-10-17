using System;
using UnityEngine;

namespace OneMoreTime.Gameplay.Combat
{
    /// <summary>
    /// A straight shot. Needs a trigger collider and a kinematic Rigidbody. Collisions are judged
    /// against the world as it is now — an echo's shot hits what is there today (GDD 02).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float speed = 24f;
        [SerializeField, Min(1)] int damage = 1;
        [SerializeField, Min(0.1f)] float lifetime = 2.5f;
        [SerializeField] DamageKind kind = DamageKind.Direct;

        Vector3 direction = Vector3.right;
        float age;
        bool live;
        Action<Projectile> release;

        public Faction Faction { get; private set; }
        public ShotSource Source { get; private set; }

        public void Launch(Vector3 position, Vector3 dir, Faction faction, ShotSource source, Action<Projectile> onRelease)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, dir.normalized)));
            direction = dir.sqrMagnitude > 0f ? dir.normalized : Vector3.right;
            Faction = faction;
            Source = source;
            release = onRelease;
            age = 0f;
            live = true;
        }

        void Update()
        {
            transform.position += direction * (speed * Time.deltaTime);
            age += Time.deltaTime;
            if (age >= lifetime)
                Release();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!live)
                return;
            var target = other.GetComponentInParent<Health>();
            if (target == null || target.Faction == Faction)
                return;
            target.TakeDamage(new DamageInfo(damage, kind, Source));
            Release();
        }

        // Guarded: one projectile can touch two colliders in the same physics step.
        void Release()
        {
            if (!live)
                return;
            live = false;
            var r = release;
            release = null;
            if (r != null)
                r(this);
            else
                Destroy(gameObject);
        }
    }
}
