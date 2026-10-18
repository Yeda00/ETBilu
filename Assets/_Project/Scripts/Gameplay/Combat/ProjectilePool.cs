using System;
using System.Collections.Generic;
using OneMoreTime.Core.Audio;
using UnityEngine;
using UnityEngine.Pool;

namespace OneMoreTime.Gameplay.Combat
{
    /// <summary>
    /// One pool per projectile prefab, so bullet-hell density doesn't churn the GC. Shots and mines already
    /// reach everything through this pool, so it also relays their sound cues to the audio director.
    /// </summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField, Min(0)] int defaultCapacity = 64;
        [SerializeField, Min(1)] int maxSize = 512;

        readonly Dictionary<Projectile, ObjectPool<Projectile>> pools = new();

        /// <summary>Raised when a shot leaves or a mine is dropped or goes off, with where it happened.</summary>
        public event Action<SoundCue, Vector3> CueRaised;

        public void Raise(SoundCue cue, Vector3 position) => CueRaised?.Invoke(cue, position);

        public Projectile Spawn(Projectile prefab, Vector3 position, Vector3 direction, Faction faction, ShotSource source)
        {
            if (prefab == null)
                return null;
            var pool = GetPool(prefab);
            var p = pool.Get();
            p.Launch(position, direction, faction, source, pool.Release);
            CueRaised?.Invoke(source switch
            {
                ShotSource.Echo => SoundCue.EchoShot,
                ShotSource.Enemy => SoundCue.EnemyShot,
                _ => SoundCue.ShipShot,
            }, position);
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
