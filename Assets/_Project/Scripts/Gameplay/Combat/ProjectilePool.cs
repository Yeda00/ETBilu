using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace OneMoreTime.Gameplay.Combat
{
    /// <summary>One pool per projectile prefab, so bullet-hell density doesn't churn the GC.</summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField, Min(0)] int defaultCapacity = 64;
        [SerializeField, Min(1)] int maxSize = 512;

        readonly Dictionary<Projectile, ObjectPool<Projectile>> pools = new();

        public Projectile Spawn(Projectile prefab, Vector3 position, Vector3 direction, Faction faction, ShotSource source)
        {
            if (prefab == null)
                return null;
            var pool = GetPool(prefab);
            var p = pool.Get();
            p.Launch(position, direction, faction, source, pool.Release);
            return p;
        }

        ObjectPool<Projectile> GetPool(Projectile prefab)
        {
            if (pools.TryGetValue(prefab, out var pool))
                return pool;

            pool = new ObjectPool<Projectile>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
            pools.Add(prefab, pool);
            return pool;
        }

        void OnDestroy()
        {
            foreach (var pool in pools.Values)
                pool.Dispose();
            pools.Clear();
        }
    }
}
