using System;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Enemies
{
    public enum MovementPattern
    {
        /// <summary>Drone: straight line from the right, never shoots (GDD 04).</summary>
        Straight = 0,
        /// <summary>Ondulante: sine zig-zag, in groups (GDD 04).</summary>
        Sine = 1,
    }

    /// <summary>
    /// A scripted enemy. Spawned by <see cref="WaveSpawner"/>, which injects the playfield.
    /// Leaving through the left edge removes it without penalty (GDD 04).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] MovementPattern pattern = MovementPattern.Straight;
        [Tooltip("World units per second toward the left, on top of the scroll.")]
        [SerializeField, Min(0f)] float speed = 4f;
        [SerializeField, Min(0f)] float sineAmplitude = 1.5f;
        [SerializeField, Min(0f)] float sineFrequency = 0.6f;
        [SerializeField, Min(0)] int scoreValue = 100;

        Health health;
        Playfield playfield;
        float baseY;
        float phase;
        float age;
        bool removed;

        public event Action<Enemy, bool> Removed;   // bool: killed by the player
        public int ScoreValue => scoreValue;
        public Health Health => health;

        void Awake()
        {
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        public void Spawn(Playfield field, MovementPattern? overridePattern, float phaseOffset)
        {
            playfield = field;
            if (overridePattern.HasValue)
                pattern = overridePattern.Value;
            baseY = transform.position.y;
            phase = phaseOffset;
            age = 0f;
        }

        void Update()
        {
            age += Time.deltaTime;
            var p = transform.position;
            p.x -= speed * Time.deltaTime;
            if (pattern == MovementPattern.Sine)
                p.y = baseY + Mathf.Sin((age * sineFrequency + phase) * Mathf.PI * 2f) * sineAmplitude;
            transform.position = p;

            if (playfield != null && playfield.IsPastLeftEdge(p))
                Remove(false);
        }

        void OnDied(Health _, DamageInfo __) => Remove(true);

        void Remove(bool killed)
        {
            if (removed)
                return;
            removed = true;
            Removed?.Invoke(this, killed);
            Removed = null;
            Destroy(gameObject);
        }
    }
}
