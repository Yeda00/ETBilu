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
        /// <summary>Kamikaze (Enemy_S model 1): homes in on the ship with a limited turn rate (GDD 04).</summary>
        Chase = 3,
        /// <summary>Gunship that follows the ship's height, locks the line and fires a horizontal laser.</summary>
        TrackerLaser = 4,
    }

    /// <summary>
    /// A scripted enemy. Spawned by <see cref="WaveSpawner"/>, which injects the playfield, the ship and
    /// the projectile pool. Moves in playfield space (screen-relative), with the path rules in
    /// <see cref="SwoopMotion"/>, <see cref="SerpentineMotion"/>, <see cref="HoverMotion"/> and
    /// <see cref="ChaseMotion"/>; arms its <see cref="EnemyGun"/> when the path allows. Leaving through the
    /// left edge removes it without penalty (GDD 04).
    /// <para>
    /// A type may carry one <see cref="EnemyProfile"/> per model (<see cref="ModelVariants"/>): the model that
    /// shows decides the path, toughness, gun, bomb and dodge, so each look means one behaviour. Without
    /// profiles the fields below are used as they are.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] MovementPattern pattern = MovementPattern.Swoop;
        [Tooltip("Tilted while the enemy changes height. Optional.")]
        [SerializeField] Transform visual;
        [SerializeField, Min(0f)] float bankAngle = 25f;
        [SerializeField, Min(0)] int scoreValue = 100;
        [Tooltip("One per model, in the order of ModelVariants. Empty: the fields below are used.")]
        [SerializeField] EnemyProfile[] profiles = Array.Empty<EnemyProfile>();

        [Header("Swoop (small) — starting values")]
        [Tooltip("Units per second toward the left, relative to the screen.")]
        [SerializeField, Min(0f)] float speed = 7.2f;
        [Tooltip("Where the swoop starts, as a fraction of the half width: 1 is the right edge, 0 the middle.")]
        [SerializeField, Range(-1f, 1f)] float swoopAt = 0.3f;
        [SerializeField, Min(0.05f)] float swoopSeconds = 0.75f;
        [Tooltip("Most units the swoop can bend up or down.")]
        [SerializeField, Min(0f)] float maxSwoop = 5f;

        [Header("Serpentine (interceptor) — starting values")]
        [SerializeField, Min(0f)] float waveAmplitude = 1.4f;
        [SerializeField, Min(0f)] float waveFrequency = 0.55f;

        [Header("Hover (medium) — starting values")]
        [Tooltip("Column where it holds, measured from the right edge.")]
        [SerializeField, Min(0f)] float holdFromRightEdge = 4.5f;
        [SerializeField, Min(0.05f)] float enterSeconds = 1.25f;
        [SerializeField, Min(0f)] float holdSeconds = 6.5f;
        [SerializeField, Min(0f)] float bobAmplitude = 0.8f;
        [SerializeField, Min(0f)] float bobFrequency = 0.45f;
        [SerializeField, Min(0f)] float leaveAcceleration = 6f;

        Health health;
        EnemyGun gun;
        Playfield playfield;
        Transform target;
        Health targetHealth;
        ProjectilePool shots;
        SwoopMotion swoop;
        HoverMotion hover;
        SerpentineMotion serpentine;
        ChaseMotion chase;
        BlastFuse fuse;
        BlastTuning blast;
        DodgeTuning dodgeTuning;
        TrackingLaserTuning laserTuning;
        TrackingLaserCycle trackingLaser;
        LineRenderer laserWarning;
        LineRenderer laserBeam;
        Material laserWarningMaterial;
        Material laserBeamMaterial;
        float chaseTurnRate = 100f;
        Vector2 local;
        float trackingY;
        float bank;
        float yaw;
        float roll;
        float dodgeClock = -1f;
        float dodgeFrom, dodgeTo, dodgeOffset, dodgeCooldown, dodgeSettle;
        bool removed;
        bool selfDestructed;

        public event Action<Enemy, bool> Removed;   // bool: killed by the player
        /// <summary>A bomber went off, with the radius of its blast.</summary>
        public event Action<Enemy, float> Detonated;
        public event Action<Enemy> LaserTelegraphed;
        public event Action<Enemy> LaserFired;
        public int ScoreValue => scoreValue;
        public Health Health => health;
        public MovementPattern Pattern => pattern;

        void Awake()
        {
            health = GetComponent<Health>();
            gun = GetComponent<EnemyGun>();
            health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (health != null)
                health.Died -= OnDied;
            if (laserWarningMaterial != null)
                Destroy(laserWarningMaterial);
            if (laserBeamMaterial != null)
                Destroy(laserBeamMaterial);
        }

        /// <param name="queueOffset">How far behind the first enemy of its wave this one entered; a hovering
        /// enemy keeps that gap at its column, so a wave of two does not stack.</param>
        /// <param name="phase">Desynchronises the bob or sine path of enemies in the same wave.</param>
        /// <param name="variant">The model this enemy takes (0-based); a negative number leaves the random pick.</param>
        public void Spawn(Playfield field, Transform ship, ProjectilePool pool, float queueOffset, float phase, int variant = -1)
        {
            playfield = field;
            target = ship;
            targetHealth = ship != null ? ship.GetComponentInParent<Health>() : null;
            shots = pool;
            local = playfield.WorldToLocal(transform.position);
            var h = playfield.HalfExtents;

            ChooseProfile(variant);

            if (pattern == MovementPattern.Swoop)
                swoop = new SwoopMotion(local, speed, swoopAt * h.x, swoopSeconds, maxSwoop);
            else if (pattern == MovementPattern.Hover || pattern == MovementPattern.TrackerLaser)
                hover = new HoverMotion(local, h.x - holdFromRightEdge + queueOffset, enterSeconds, holdSeconds,
                    bobAmplitude, bobFrequency, phase, leaveAcceleration);
            else if (pattern == MovementPattern.Chase)
                chase = new ChaseMotion(local, speed, chaseTurnRate);
            else
                serpentine = new SerpentineMotion(local, speed, waveAmplitude, waveFrequency, phase);
            if (blast.explodes)
                fuse = new BlastFuse(blast.triggerRadius, blast.fuseSeconds, blast.autoSeconds);
            if (laserTuning.enabled)
            {
                trackingY = local.y;
                trackingLaser = new TrackingLaserCycle(laserTuning.trackingSeconds, laserTuning.chargeSeconds,
                    laserTuning.fireSeconds, laserTuning.recoverySeconds, laserTuning.bursts);
                BuildTrackingLaserVisuals();
            }
            if (gun != null)
            {
                gun.Configure(field, ship, pool);
                if (serpentine != null)
                    gun.Arm();
            }
        }

        // The model that shows decides how this enemy behaves (GDD 04).
        void ChooseProfile(int variant)
        {
            var models = GetComponent<ModelVariants>();
            if (profiles.Length == 0 || models == null)
                return;
            if (variant >= 0 && variant < models.Count)
                models.Pick(variant);
            var p = profiles[Mathf.Clamp(models.Current, 0, profiles.Length - 1)];

            pattern = p.pattern;
            scoreValue = p.scoreValue;
            speed = p.speed;
            swoopAt = p.swoopAt;
            swoopSeconds = p.swoopSeconds;
            maxSwoop = p.maxSwoop;
            chaseTurnRate = p.turnRate;
            waveAmplitude = p.waveAmplitude;
            waveFrequency = p.waveFrequency;
            holdFromRightEdge = p.holdFromRightEdge;
            enterSeconds = p.enterSeconds;
            holdSeconds = p.holdSeconds;
            bobAmplitude = p.bobAmplitude;
            bobFrequency = p.bobFrequency;
            leaveAcceleration = p.leaveAcceleration;
            blast = p.blast;
            dodgeTuning = p.dodge;
            laserTuning = p.trackingLaser;
            health.SetMax(p.health);
            gun?.Tune(p.gun);
            // A bomber's attack is its blast, not the touch.
            var contact = GetComponent<ContactDamage>();
            if (contact != null && blast.explodes)
                contact.enabled = false;
        }

        void Update()
        {
            if (playfield == null || removed)
                return;
            float dt = Time.deltaTime;
            var before = local;
            bool shipAlive = target != null && target.gameObject.activeInHierarchy;
            Vector2 shipLocal = shipAlive ? playfield.WorldToLocal(target.position) : local;

            if (swoop != null)
            {
                float targetY = shipAlive ? shipLocal.y : local.y;
                if (swoop.Tick(dt, targetY) && gun != null)
                    gun.Arm();
                local = swoop.Position;
            }
            else if (hover != null)
            {
                hover.Tick(dt);
                if (trackingLaser != null)
                {
                    local.x = hover.Position.x;
                    if (hover.Phase == HoverPhase.Entering)
                        trackingY = hover.Position.y;
                    else if (hover.Phase == HoverPhase.Holding)
                        UpdateTrackingLaser(dt, shipAlive, shipLocal);
                    local.y = trackingY;
                    gun?.Disarm();
                }
                else
                {
                    if (gun != null)
                    {
                        if (hover.Phase == HoverPhase.Holding) gun.Arm();
                        else gun.Disarm();
                    }
                    local = hover.Position;
                }
            }
            else if (chase != null)
            {
                chase.Tick(dt, shipAlive ? shipLocal : local + Vector2.left * 10f);
                local = chase.Position;
                yaw = chase.Heading - 180f;
            }
            else if (serpentine != null)
            {
                serpentine.Tick(dt);
                local = serpentine.Position;
            }

            UpdateDodge(dt);
            local.y += dodgeOffset;
            transform.position = playfield.LocalToWorld(local);
            UpdateTrackingLaserVisuals();
            Bank(before, dt);
            UpdateBlast(dt, shipAlive ? Vector2.Distance(local, shipLocal) : float.MaxValue);
            if (removed)
                return;

            if (playfield.IsPastLeftEdge(transform.position) || (chase != null && playfield.IsFarOutside(transform.position, 6f)))
                Remove(false);
        }

        void UpdateTrackingLaser(float dt, bool shipAlive, Vector2 shipLocal)
        {
            if (trackingLaser.Phase is TrackingLaserPhase.Tracking or TrackingLaserPhase.Recovering)
            {
                float targetY = shipAlive ? shipLocal.y : 0f;
                float limit = playfield.HalfExtents.y - 0.8f;
                trackingY = Mathf.MoveTowards(trackingY, Mathf.Clamp(targetY, -limit, limit), laserTuning.followSpeed * dt);
            }

            TrackingLaserPhase before = trackingLaser.Phase;
            if (!trackingLaser.Tick(dt) || trackingLaser.Phase == before)
                return;

            if (trackingLaser.Phase == TrackingLaserPhase.Charging)
            {
                LaserTelegraphed?.Invoke(this);
                if (gun != null)
                    gun.TelegraphOverride = true;
            }
            else
            {
                if (gun != null)
                    gun.TelegraphOverride = false;
                if (trackingLaser.Phase == TrackingLaserPhase.Firing)
                    FireTrackingLaser(shipAlive, shipLocal);
            }
        }

        void FireTrackingLaser(bool shipAlive, Vector2 shipLocal)
        {
            LaserFired?.Invoke(this);
            if (!shipAlive || targetHealth == null || shipLocal.x > local.x)
                return;
            float hitTolerance = laserTuning.beamWidth * 0.5f + 0.42f;
            if (Mathf.Abs(shipLocal.y - trackingY) <= hitTolerance)
                targetHealth.TakeDamage(new DamageInfo(laserTuning.damage, DamageKind.Direct, ShotSource.Enemy));
        }

        void BuildTrackingLaserVisuals()
        {
            laserWarning = BuildLaserLine("Laser Warning", new Color(1f, 0.72f, 0.12f, 0.85f), 71);
            laserBeam = BuildLaserLine("Tracking Laser", new Color(1f, 0.16f, 0.06f, 1f), 72);
            laserWarningMaterial = laserWarning != null ? laserWarning.sharedMaterial : null;
            laserBeamMaterial = laserBeam != null ? laserBeam.sharedMaterial : null;
        }

        LineRenderer BuildLaserLine(string lineName, Color color, int order)
        {
            var lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = order;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
                line.sharedMaterial = new Material(shader) { name = $"{lineName} (Runtime)", hideFlags = HideFlags.DontSave };
            line.startColor = color;
            line.endColor = color;
            line.enabled = false;
            return line;
        }

        void UpdateTrackingLaserVisuals()
        {
            if (trackingLaser == null || laserWarning == null || laserBeam == null || playfield == null)
                return;
            if (hover == null || hover.Phase != HoverPhase.Holding || trackingLaser.Phase == TrackingLaserPhase.Exhausted)
            {
                if (gun != null)
                    gun.TelegraphOverride = false;
                laserWarning.enabled = false;
                laserBeam.enabled = false;
                return;
            }
            bool warning = trackingLaser.Phase == TrackingLaserPhase.Charging;
            bool firing = trackingLaser.Phase == TrackingLaserPhase.Firing;
            laserWarning.enabled = warning;
            laserBeam.enabled = firing;
            if (!warning && !firing)
                return;

            Vector3 origin = playfield.LocalToWorld(new Vector2(local.x - 0.55f, trackingY));
            Vector3 end = playfield.LocalToWorld(new Vector2(-playfield.HalfExtents.x - 0.6f, trackingY));
            LineRenderer line = firing ? laserBeam : laserWarning;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
            line.widthMultiplier = firing
                ? laserTuning.beamWidth
                : Mathf.Lerp(0.035f, 0.13f, 0.5f + Mathf.Sin(Time.time * 22f) * 0.5f);
        }

        // The shotgunner steps out of the way of a shot it can see coming, with a barrel roll (GDD 04).
        void UpdateDodge(float dt)
        {
            if (!dodgeTuning.dodges)
                return;
            dodgeCooldown -= dt;
            bool holding = hover != null && hover.Phase == HoverPhase.Holding;
            if (dodgeClock < 0f && dodgeCooldown <= 0f && holding && shots != null
                && shots.TryFindThreat(transform.position, dodgeTuning.senseRadius, Faction.Player, dodgeTuning.lookahead, out float side))
            {
                float limit = playfield.HalfExtents.y - 1.2f;
                float away = side != 0f ? side : (local.y > 0f ? -1f : 1f);   // dead on: toward the middle
                float baseY = local.y - dodgeOffset;
                dodgeFrom = dodgeOffset;
                dodgeTo = Mathf.Clamp(baseY + away * dodgeTuning.distance, -limit, limit) - baseY;
                dodgeClock = 0f;
                dodgeCooldown = dodgeTuning.cooldown;
            }

            if (dodgeClock >= 0f)
            {
                dodgeClock += dt;
                float k = Mathf.Clamp01(dodgeClock / dodgeTuning.seconds);
                dodgeOffset = Mathf.Lerp(dodgeFrom, dodgeTo, Mathf.SmoothStep(0f, 1f, k));
                roll = 360f * Mathf.SmoothStep(0f, 1f, k);
                if (k >= 1f)
                {
                    dodgeClock = -1f;
                    roll = 0f;
                    dodgeSettle = 0.9f;
                }
            }
            else
            {
                // After the sidestep it drifts back to its line.
                dodgeSettle -= dt;
                if (dodgeSettle <= 0f)
                    dodgeOffset = Mathf.MoveTowards(dodgeOffset, 0f, 1.6f * dt);
            }
        }

        void UpdateBlast(float dt, float distanceToShip)
        {
            if (fuse == null)
                return;
            bool bang = fuse.Tick(dt, distanceToShip);
            if (gun != null)
                gun.TelegraphOverride = fuse.Warning;
            if (visual != null)
                visual.localScale = Vector3.one * (1f + 0.14f * fuse.Urgency * Mathf.Sin(Time.time * (14f + 26f * fuse.Urgency)));
            if (bang)
                Detonate();
        }

        // The bang hurts the ship if it is inside the radius when the fuse ends; the enemy dies without paying out (GDD 04).
        void Detonate()
        {
            if (removed)
                return;
            bool inside = target != null && target.gameObject.activeInHierarchy
                && Vector2.Distance(local, playfield.WorldToLocal(target.position)) <= blast.blastRadius;
            if (inside && targetHealth != null)
                targetHealth.TakeDamage(new DamageInfo(blast.damage, DamageKind.Contact, ShotSource.Enemy));
            Detonated?.Invoke(this, blast.blastRadius);
            selfDestructed = true;
            if (!health.TakeDamage(new DamageInfo(Mathf.Max(1, health.Current), DamageKind.Direct, ShotSource.Enemy)))
                Remove(false);
        }

        void Bank(Vector2 before, float dt)
        {
            if (visual == null || dt <= 0f)
                return;
            float climb = (local.y - before.y) / dt;
            float goal = Mathf.Clamp(-climb * 8f, -bankAngle, bankAngle);
            bank = Mathf.Lerp(bank, goal, 1f - Mathf.Exp(-10f * dt));
            visual.localRotation = Quaternion.Euler(bank + roll, 0f, yaw);
        }

        /// <summary>Takes it off the field without a kill: its caller was beaten (GDD 06).</summary>
        public void Dismiss() => Remove(false);

        void OnDied(Health _, DamageInfo __) => Remove(!selfDestructed);

        void Remove(bool killed)
        {
            if (removed)
                return;
            removed = true;
            Removed?.Invoke(this, killed);
            Removed = null;
            Detonated = null;
            LaserTelegraphed = null;
            LaserFired = null;
            Destroy(gameObject);
        }
    }
}
