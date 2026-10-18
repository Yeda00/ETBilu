using System;
using OneMoreTime.Core.Enemies;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Enemies
{
    public enum MovementPattern
    {
        /// <summary>Small enemy (Enemy_S): straight in, bends toward the ship's height, flies out (GDD 04).</summary>
        Swoop = 0,
        /// <summary>Medium enemy (Enemy_M): slides in, holds a column bobbing while it shoots, leaves (GDD 04).</summary>
        Hover = 1,
        /// <summary>Interceptor (Enemy_W): crosses the arena on a serpentine path while firing (GDD 04).</summary>
        Serpentine = 2,
    }

    /// <summary>
    /// A scripted enemy. Spawned by <see cref="WaveSpawner"/>, which injects the playfield, the ship and
    /// the projectile pool. Moves in playfield space (screen-relative), with the path rules in
    /// <see cref="SwoopMotion"/> and <see cref="HoverMotion"/>; arms its <see cref="EnemyGun"/> when the
    /// path allows. Leaving through the left edge removes it without penalty (GDD 04).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] MovementPattern pattern = MovementPattern.Swoop;
        [Tooltip("Tilted while the enemy changes height. Optional.")]
        [SerializeField] Transform visual;
        [SerializeField, Min(0f)] float bankAngle = 25f;
        [SerializeField, Min(0)] int scoreValue = 100;

        [Header("Swoop (small) — starting values")]
        [Tooltip("Units per second toward the left, relative to the screen.")]
        [SerializeField, Min(0f)] float speed = 6.5f;
        [Tooltip("Where the swoop starts, as a fraction of the half width: 1 is the right edge, 0 the middle.")]
        [SerializeField, Range(-1f, 1f)] float swoopAt = 0.3f;
        [SerializeField, Min(0.05f)] float swoopSeconds = 1f;
        [Tooltip("Most units the swoop can bend up or down.")]
        [SerializeField, Min(0f)] float maxSwoop = 4.5f;

        [Header("Serpentine (interceptor) — starting values")]
        [SerializeField, Min(0f)] float waveAmplitude = 1.35f;
        [SerializeField, Min(0f)] float waveFrequency = 0.55f;

        [Header("Hover (medium) — starting values")]
        [Tooltip("Column where it holds, measured from the right edge.")]
        [SerializeField, Min(0f)] float holdFromRightEdge = 4.5f;
        [SerializeField, Min(0.05f)] float enterSeconds = 1.6f;
        [SerializeField, Min(0f)] float holdSeconds = 7f;
        [SerializeField, Min(0f)] float bobAmplitude = 0.5f;
        [SerializeField, Min(0f)] float bobFrequency = 0.3f;
        [SerializeField, Min(0f)] float leaveAcceleration = 5f;

        Health health;
        EnemyGun gun;
        Playfield playfield;
        Transform target;
        SwoopMotion swoop;
        HoverMotion hover;
        SerpentineMotion serpentine;
        Vector2 local;
        float bank;
        bool removed;

        public event Action<Enemy, bool> Removed;   // bool: killed by the player
        public int ScoreValue => scoreValue;
        public Health Health => health;
        public MovementPattern Pattern => pattern;

        void Awake()
        {
            health = GetComponent<Health>();
            gun = GetComponent<EnemyGun>();
            health.Died += OnDied;
        }

        /// <param name="queueOffset">How far behind the first enemy of its wave this one entered; a hovering
        /// enemy keeps that gap at its column, so a wave of two does not stack.</param>
        /// <param name="phase">Desynchronises the bob of enemies in the same wave.</param>
        public void Spawn(Playfield field, Transform ship, ProjectilePool pool, float queueOffset, float phase)
        {
            playfield = field;
            target = ship;
            local = playfield.WorldToLocal(transform.position);
            var h = playfield.HalfExtents;
            if (pattern == MovementPattern.Swoop)
                swoop = new SwoopMotion(local, speed, swoopAt * h.x, swoopSeconds, maxSwoop);
            else if (pattern == MovementPattern.Hover)
                hover = new HoverMotion(local, h.x - holdFromRightEdge + queueOffset, enterSeconds, holdSeconds,
                    bobAmplitude, bobFrequency, phase, leaveAcceleration);
            else
                serpentine = new SerpentineMotion(local, speed, waveAmplitude, waveFrequency, phase);
            if (gun != null)
            {
                gun.Configure(field, ship, pool);
                if (serpentine != null)
                    gun.Arm();
            }
        }

        void Update()
        {
            if (playfield == null || removed)
                return;
            float dt = Time.deltaTime;
            var before = local;

            if (swoop != null)
            {
                float targetY = target != null && target.gameObject.activeInHierarchy ? playfield.WorldToLocal(target.position).y : local.y;
                if (swoop.Tick(dt, targetY) && gun != null)
                    gun.Arm();
                local = swoop.Position;
            }
            else if (hover != null)
            {
                hover.Tick(dt);
                if (gun != null)
                {
                    if (hover.Phase == HoverPhase.Holding) gun.Arm();
                    else gun.Disarm();
                }
                local = hover.Position;
            }
            else if (serpentine != null)
            {
                serpentine.Tick(dt);
                local = serpentine.Position;
            }
            transform.position = playfield.LocalToWorld(local);
            Bank(before, dt);

            if (playfield.IsPastLeftEdge(transform.position))
                Remove(false);
        }

        void Bank(Vector2 before, float dt)
        {
            if (visual == null || dt <= 0f)
                return;
            float climb = (local.y - before.y) / dt;
            float goal = Mathf.Clamp(-climb * 8f, -bankAngle, bankAngle);
            bank = Mathf.Lerp(bank, goal, 1f - Mathf.Exp(-10f * dt));
            visual.localRotation = Quaternion.Euler(bank, 0f, 0f);
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
