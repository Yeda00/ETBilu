using System;
using System.Collections.Generic;
using OneMoreTime.Core.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Boss
{
    /// <summary>One breakable part of the Guardian: its hit points, the shield drawn over it while it cannot be hurt, and its score.</summary>
    [Serializable]
    public struct PartRef
    {
        public GuardianPart part;
        public Health health;
        [Tooltip("Shown while the part is shielded or guarded.")]
        public GameObject shield;
        [Min(0)] public int score;
    }

    /// <summary>
    /// The Guardian fights from readable stations instead of tracking the player continuously. Its parts —
    /// antenna, eyes, cannons — come off in any order and change how it fights; the teeth are the weak points
    /// that finish it (GDD 06). The antenna and the eyes call reinforcements while they last. Rules live in
    /// <see cref="GuardianModel"/>.
    /// </summary>
    public sealed class GuardianBoss : MonoBehaviour
    {
        enum MoveState { Holding, Telegraphing, Dashing, Recovering }

        [Header("Parts")]
        [Tooltip("Antenna, eyes, both cannons and both sets of teeth.")]
        [SerializeField] PartRef[] parts = Array.Empty<PartRef>();
        [SerializeField] Transform muzzle;
        [SerializeField] SweepLaser laser;
        [SerializeField] Projectile shot;
        [Tooltip("Visual model that leans and recoils. Resolved automatically when omitted.")]
        [SerializeField] Transform poseRoot;

        [Header("Phases")]
        [Tooltip("Seconds with every part shielded, before the fight opens up.")]
        [SerializeField, Min(0f)] float survivalSeconds = 10f;

        [Header("Reinforcements (GDD 06) — starting values")]
        [Tooltip("Called by the antenna: small fighters. Optional.")]
        [SerializeField] Enemy fighterPrefab;
        [Tooltip("Called by the eyes: medium gunships. Optional.")]
        [SerializeField] Enemy gunshipPrefab;
        [SerializeField, Min(0f)] float firstFighterCall = 3f;
        [SerializeField, Min(1f)] float fighterCallSeconds = 8f;
        [SerializeField, Min(1)] int fightersPerCall = 2;
        [Tooltip("The antenna waits while this many of its fighters are alive.")]
        [SerializeField, Min(1)] int fighterCap = 4;
        [SerializeField, Min(0f)] float firstGunshipCall = 7f;
        [SerializeField, Min(1f)] float gunshipCallSeconds = 15f;
        [SerializeField, Min(1)] int gunshipCap = 1;

        [Header("Entry")]
        [Tooltip("Playfield-local x where the Guardian holds position, measured from the right edge.")]
        [SerializeField] float holdFromRightEdge = 3f;
        [Tooltip("How far past the right edge the Guardian starts its entry.")]
        [SerializeField, Min(0f)] float entryDistance = 5f;
        [SerializeField, Min(0.1f)] float entrySeconds = 2.5f;

        [Header("Combat movement")]
        [Tooltip("Distance from the central station to the upper and lower stations.")]
        [SerializeField, Min(0.5f)] float laneHeight = 2.35f;
        [Tooltip("Upper and lower stations also lean into the arena by this amount.")]
        [SerializeField, Min(0f)] float laneInset = 1.1f;
        [SerializeField, Min(0.05f)] float moveTelegraphSeconds = 0.52f;
        [SerializeField, Min(0.05f)] float dashSeconds = 0.42f;
        [SerializeField, Min(0f)] float moveRecoverySeconds = 0.32f;
        [SerializeField, Min(0f)] float hoverAmplitude = 0.18f;
        [SerializeField, Min(0f)] float hoverFrequency = 0.9f;
        [Tooltip("Each cannon lost speeds its moves up by this share.")]
        [SerializeField, Min(0f)] float furySpeedUp = 0.4f;

        [Header("Attacks")]
        [SerializeField, Min(0.05f)] float attackTelegraphSeconds = 0.42f;
        [Tooltip("Phase 1: aimed fan.")]
        [SerializeField, Min(0.1f)] float fanInterval = 1.1f;
        [SerializeField, Min(1)] int fanShots = 7;
        [SerializeField, Min(0f)] float fanSpread = 11f;
        [Tooltip("Phase 2: aimed fan and projectile wall.")]
        [SerializeField, Min(0.1f)] float burstInterval = 0.95f;
        [SerializeField, Min(1)] int burstShots = 6;
        [SerializeField, Min(5)] int wallRows = 11;
        [SerializeField, Min(0.5f)] float wallGap = 2.5f;
        [Tooltip("Phase 3: rings with a readable gap, mixed with aimed attacks.")]
        [SerializeField, Min(0.1f)] float ringInterval = 1f;
        [SerializeField, Min(6)] int ringShots = 18;
        [SerializeField, Range(5f, 80f)] float ringGapDegrees = 28f;
        [SerializeField, Min(0.1f)] float lastChanceInterval = 0.78f;
        [Tooltip("The antenna gone: attacks come this much sooner (a share of the usual wait).")]
        [SerializeField, Range(0.3f, 1f)] float frenzyInterval = 0.72f;
        [Tooltip("The eyes gone: every fourth attack is a spinning stream of shots from the eyes.")]
        [SerializeField, Min(0.5f)] float spiralSeconds = 1.8f;
        [SerializeField, Min(0.02f)] float spiralStep = 0.075f;
        [SerializeField, Range(4f, 40f)] float spiralTurn = 13f;

        [Header("Combat pacing")]
        [Tooltip("Quiet window after a part breaks, so the player can read the new state.")]
        [SerializeField, Min(0f)] float partBreakBreather = 1.1f;
        [Tooltip("How long a projectile pattern keeps the laser scheduler back. The laser still has its own warning after this.")]
        [SerializeField, Min(0f)] float laserSuppressionAfterPattern = 1.6f;
        [Tooltip("Recovery after calling reinforcements. Adds replace the boss' projectile pressure while alive.")]
        [SerializeField, Min(0f)] float reinforcementRecovery = 1.35f;

        GuardianModel model;
        Playfield playfield;
        Transform ship;
        ProjectilePool pool;
        WaveSpawner reinforcements;
        readonly List<Enemy> fighters = new();
        readonly List<Enemy> gunships = new();
        float fighterTimer;
        float gunshipTimer;
        int fighterCalls;
        int gunshipCalls;
        float spiralLeft;
        float spiralClock;
        float spiralAngle;
        Vector2 entryFrom;
        Vector2 entryTo;
        Vector2 combatPosition;
        Vector2 moveFrom;
        Vector2 moveTo;
        float entryAge;
        float attackTimer;
        float moveTimer;
        float dashAge;
        float hoverClock;
        float ringTurn;
        float hitRecoil;
        float poseRoll;
        int attackPattern;
        int patternsSinceLaser;
        int moveSequence;
        int laneIndex = 1;
        bool attackTelegraphed;
        float breatherTimer;
        float laserSuppressionTimer;
        MoveState moveState;
        GuardianPhase announcedPhase;
        Vector3 poseNeutralPosition;
        Quaternion poseNeutralRotation;
        LineRenderer[] tethers;
        Material tetherMaterial;
        Action<Health, DamageInfo>[] partHandlers;

        public event Action Defeated;
        public event Action<int> Scored;
        /// <summary>A part was shot off, and by whom (the Synchronized Strike reads the source).</summary>
        public event Action<GuardianBoss, GuardianPart, DamageInfo> PartDestroyed;
        /// <summary>A set of teeth just became reachable, with where it is.</summary>
        public event Action<Vector3> TeethExposed;
        public event Action<Vector3> DashTelegraphed;
        public event Action<Vector3> Dashed;
        public event Action<Vector3> AttackTelegraphed;
        public event Action<Vector3> AttackReleased;
        public event Action<GuardianBoss, GuardianPhase, GuardianPhase> PhaseChanged;

        public bool IsEntering => model == null;
        public GuardianPhase Phase => model?.Phase ?? GuardianPhase.Survival;
        /// <summary>The rules state (parts down, what it calls, how furious it is); null while it is still entering.</summary>
        public GuardianModel Rules => model;
        public IReadOnlyList<PartRef> Parts => parts;
        public bool IsPartDown(GuardianPart part) => model != null && model.IsDestroyed(part);
        public bool IsPartExposed(GuardianPart part) => model != null && model.IsExposed(part);
        public Health PartHealth(GuardianPart part) => Find(part)?.health;
        public Vector3 PartPosition(GuardianPart part)
        {
            Health health = PartHealth(part);
            return health != null ? health.transform.position : transform.position;
        }

        /// <summary>The two sets of teeth together: what is left of the Guardian's life.</summary>
        public float TeethNormalized
        {
            get
            {
                Health upper = PartHealth(GuardianPart.TeethUpper), lower = PartHealth(GuardianPart.TeethLower);
                int max = (upper != null ? upper.Max : 0) + (lower != null ? lower.Max : 0);
                int now = (upper != null ? upper.Current : 0) + (lower != null ? lower.Current : 0);
                return max > 0 ? (float)now / max : 0f;
            }
        }

        public Vector3 CorePosition => PartPosition(GuardianPart.Eyes);

        /// <param name="spawner">Takes the reinforcements the antenna and the eyes call. Optional.</param>
        public void Begin(Playfield field, Transform shipTransform, ProjectilePool projectiles, WaveSpawner spawner = null)
        {
            playfield = field;
            ship = shipTransform;
            pool = projectiles;
            reinforcements = spawner;
            var h = playfield.HalfExtents;
            entryFrom = new Vector2(h.x + entryDistance, 0f);
            entryTo = new Vector2(h.x - holdFromRightEdge, 0f);
            combatPosition = entryTo;
            transform.position = playfield.LocalToWorld(entryFrom);
            ApplyShields();
        }

        void Awake()
        {
            partHandlers = new Action<Health, DamageInfo>[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                GuardianPart part = parts[i].part;
                partHandlers[i] = (_, info) => OnPartDied(part, info);
            }

            if (poseRoot == null)
                poseRoot = FindDescendant(transform, "Enemie_Boss_BlockOut");
            if (poseRoot != null)
            {
                poseNeutralPosition = poseRoot.localPosition;
                poseNeutralRotation = poseRoot.localRotation;
            }
            BuildTethers();
        }

        void OnEnable()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].health == null)
                    continue;
                parts[i].health.Damaged += OnPartDamaged;
                parts[i].health.Died += partHandlers[i];
            }
            if (laser != null)
            {
                laser.Telegraphed += OnLaserTelegraphed;
                laser.Fired += OnLaserFired;
                laser.Completed += OnLaserCompleted;
            }
        }

        void OnDisable()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].health == null)
                    continue;
                parts[i].health.Damaged -= OnPartDamaged;
                parts[i].health.Died -= partHandlers[i];
            }
            if (laser != null)
            {
                laser.Telegraphed -= OnLaserTelegraphed;
                laser.Fired -= OnLaserFired;
                laser.Completed -= OnLaserCompleted;
            }
        }

        void OnDestroy()
        {
            if (tetherMaterial != null)
                Destroy(tetherMaterial);
        }

        void Update()
        {
            if (playfield == null)
                return;
            if (model == null)
            {
                Enter();
                return;
            }
            if (model.IsDefeated)
                return;

            model.Tick(Time.deltaTime);
            breatherTimer = Mathf.Max(0f, breatherTimer - Time.deltaTime);
            laserSuppressionTimer = Mathf.Max(0f, laserSuppressionTimer - Time.deltaTime);
            ReportPhaseChange();
            Move();
            ApplyShields();
            UpdateTethers();
            if (laser != null)
                laser.Active = WantsLaser() && !AddsAlive && spiralLeft <= 0f && breatherTimer <= 0f
                    && laserSuppressionTimer <= 0f && moveState == MoveState.Holding;
            CallReinforcements();
            Attack();
            Spiral();
            UpdatePose();
        }

        void Enter()
        {
            entryAge += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(entryAge / entrySeconds));
            transform.position = playfield.LocalToWorld(Vector2.Lerp(entryFrom, entryTo, k));
            UpdateTethers();
            if (k < 1f)
                return;

            model = new GuardianModel(survivalSeconds);
            announcedPhase = model.Phase;
            fighterTimer = firstFighterCall;
            gunshipTimer = firstGunshipCall;
            combatPosition = entryTo;
            moveState = MoveState.Holding;
            moveTimer = 1.15f;
            attackTimer = 0.8f;
        }

        void ReportPhaseChange()
        {
            GuardianPhase next = model.Phase;
            if (next == announcedPhase)
                return;
            GuardianPhase previous = announcedPhase;
            announcedPhase = next;
            attackPattern = 0;
            patternsSinceLaser = 0;
            attackTimer = Mathf.Max(attackTimer, 0.72f);
            attackTelegraphed = false;
            moveTimer = Mathf.Min(moveTimer, 0.55f);
            PhaseChanged?.Invoke(this, previous, next);
        }

        void Move()
        {
            hoverClock += Time.deltaTime;
            float speedUp = 1f + furySpeedUp * model.Fury;
            switch (moveState)
            {
                case MoveState.Holding:
                    // Once a laser turn is due, keep the station long enough for its rest + warning to complete.
                    // Otherwise a last-moment dash would disable the laser and restart its clock forever.
                    bool laserTurnDue = WantsLaser() && laserSuppressionTimer <= 0f;
                    if (!PrimaryThreatActive && !laserTurnDue)
                        moveTimer -= Time.deltaTime;
                    if (moveTimer <= 0f)
                        BeginMove();
                    break;
                case MoveState.Telegraphing:
                    moveTimer -= Time.deltaTime;
                    if (moveTimer <= 0f)
                    {
                        moveState = MoveState.Dashing;
                        dashAge = 0f;
                    }
                    break;
                case MoveState.Dashing:
                    dashAge += Time.deltaTime;
                    float t = Mathf.Clamp01(dashAge / (dashSeconds / speedUp));
                    float eased = t * t * (3f - 2f * t);
                    combatPosition = Vector2.Lerp(moveFrom, moveTo, eased);
                    if (t >= 1f)
                    {
                        combatPosition = moveTo;
                        moveState = MoveState.Recovering;
                        moveTimer = moveRecoverySeconds;
                        Dashed?.Invoke(playfield.LocalToWorld(moveTo));
                    }
                    break;
                case MoveState.Recovering:
                    moveTimer -= Time.deltaTime;
                    if (moveTimer <= 0f)
                    {
                        moveState = MoveState.Holding;
                        moveTimer = HoldSeconds() / speedUp;
                    }
                    break;
            }

            float bob = moveState == MoveState.Dashing ? 0f
                : Mathf.Sin(hoverClock * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
            transform.position = playfield.LocalToWorld(combatPosition + Vector2.up * bob);
        }

        void BeginMove()
        {
            int step = moveSequence++ % 2 == 0 ? 1 : 2;
            laneIndex = (laneIndex + step) % 3;
            float y = (1 - laneIndex) * laneHeight;
            float x = laneIndex == 1 ? 0f : -laneInset;
            if (model.Fury >= 2)
                x -= 0.35f;
            moveFrom = combatPosition;
            moveTo = entryTo + new Vector2(x, y);
            moveState = MoveState.Telegraphing;
            moveTimer = moveTelegraphSeconds;
            DashTelegraphed?.Invoke(transform.position);
        }

        float HoldSeconds() => model.Phase == GuardianPhase.Survival ? 3.2f : 2.7f;

        bool AddsAlive => fighters.Count > 0 || gunships.Count > 0;
        // Laser turns are earned by the normal patterns instead of stacking on top of them:
        // one teaching pattern in Survival, two patterns in the final fury state, then one sweep.
        bool WantsLaser() => model.Phase == GuardianPhase.Survival
            ? patternsSinceLaser >= 1
            : model.Fury >= 2 && patternsSinceLaser >= 2;
        bool PrimaryThreatActive => breatherTimer > 0f || spiralLeft > 0f || AddsAlive
            || (laser != null && laser.IsThreatening);

        void UpdatePose()
        {
            if (poseRoot == null)
                return;
            float targetRoll = moveState == MoveState.Dashing
                ? Mathf.Clamp(-(moveTo.y - moveFrom.y) * 2.4f, -8f, 8f)
                : 0f;
            poseRoll = Mathf.Lerp(poseRoll, targetRoll, 1f - Mathf.Exp(-10f * Time.deltaTime));
            hitRecoil = Mathf.MoveTowards(hitRecoil, 0f, Time.deltaTime * 1.8f);
            float charge = moveState == MoveState.Telegraphing
                ? Mathf.Sin((moveTelegraphSeconds - moveTimer) * 34f) * 0.08f
                : 0f;
            poseRoot.localPosition = poseNeutralPosition + Vector3.right * (hitRecoil + charge);
            poseRoot.localRotation = poseNeutralRotation * Quaternion.Euler(0f, 0f, poseRoll);
        }

        PartRef? Find(GuardianPart part)
        {
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].part == part)
                    return parts[i];
            return null;
        }

        // A part is shielded while the fight has not opened up, or while what guards it still stands.
        void ApplyShields()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                GuardianPart part = parts[i].part;
                bool hittable = model != null && model.IsExposed(part);
                bool showShield = !hittable && !(model != null && model.IsDestroyed(part));
                if (parts[i].health != null)
                    parts[i].health.Shielded = !hittable;
                if (parts[i].shield != null && parts[i].shield.activeSelf != showShield)
                    parts[i].shield.SetActive(showShield);
            }
        }

        void OnPartDied(GuardianPart part, DamageInfo info)
        {
            PartRef? found = Find(part);
            if (found == null)
                return;
            PartRef data = found.Value;
            if (model == null)
            {
                data.health.ResetHealth();
                return;
            }

            bool upperBefore = model.IsExposed(GuardianPart.TeethUpper);
            bool lowerBefore = model.IsExposed(GuardianPart.TeethLower);
            if (!model.Destroy(part))
            {
                data.health.ResetHealth();
                return;
            }

            data.health.gameObject.SetActive(false);
            Scored?.Invoke(data.score);
            // A destroyed part is a reward beat, not an invitation for the next three hazards to land together.
            breatherTimer = partBreakBreather;
            laserSuppressionTimer = Mathf.Max(laserSuppressionTimer, partBreakBreather);
            attackTimer = Mathf.Max(attackTimer, partBreakBreather + attackTelegraphSeconds);
            attackTelegraphed = false;
            spiralLeft = 0f;
            if (laser != null)
                laser.Active = false;
            ApplyShields();
            PartDestroyed?.Invoke(this, part, info);

            if (!upperBefore && model.IsExposed(GuardianPart.TeethUpper))
                TeethExposed?.Invoke(PartPosition(GuardianPart.TeethUpper));
            if (!lowerBefore && model.IsExposed(GuardianPart.TeethLower))
                TeethExposed?.Invoke(PartPosition(GuardianPart.TeethLower));
            if (model.IsDefeated)
                Finish();
        }

        // Both teeth are gone: the Guardian is beaten, and what it called goes with it (GDD 06).
        void Finish()
        {
            if (laser != null)
                laser.Active = false;
            spiralLeft = 0f;
            foreach (Enemy enemy in fighters.ToArray())
                if (enemy != null)
                    enemy.Dismiss();
            foreach (Enemy enemy in gunships.ToArray())
                if (enemy != null)
                    enemy.Dismiss();
            fighters.Clear();
            gunships.Clear();
            Defeated?.Invoke();
        }

        void OnPartDamaged(Health _, DamageInfo info)
        {
            hitRecoil = Mathf.Max(hitRecoil,
                info.Kind == DamageKind.Combined || info.Kind == DamageKind.Gadget ? 0.28f : 0.14f);
        }

        void Attack()
        {
            // Reinforcements, a laser sweep, a dash and the spiral are complete threats on their own.
            // Holding the normal projectile clock here preserves intensity without unreadable stacking.
            if (PrimaryThreatActive || moveState != MoveState.Holding || (laser != null && laser.Active))
            {
                attackTimer = Mathf.Max(attackTimer, attackTelegraphSeconds + 0.12f);
                attackTelegraphed = false;
                return;
            }
            attackTimer -= Time.deltaTime;
            if (!attackTelegraphed && attackTimer > 0f && attackTimer <= attackTelegraphSeconds)
            {
                attackTelegraphed = true;
                AttackTelegraphed?.Invoke(muzzle != null ? muzzle.position : transform.position);
            }
            if (attackTimer > 0f || pool == null || shot == null || muzzle == null)
                return;

            FirePhasePattern();
            patternsSinceLaser++;
            attackTimer = AttackInterval();
            laserSuppressionTimer = Mathf.Max(laserSuppressionTimer, laserSuppressionAfterPattern);
            attackTelegraphed = false;
            AttackReleased?.Invoke(muzzle.position);
        }

        void FirePhasePattern()
        {
            int pattern = attackPattern++;
            // The eyes gone: a new mechanic every fourth attack, a spinning stream from where they were.
            if (model.EyeAttack && pattern % 4 == 3)
            {
                spiralLeft = spiralSeconds;
                spiralClock = 0f;
                spiralAngle = 180f;
                return;
            }

            if (model.Phase == GuardianPhase.Survival)
            {
                if (pattern % 2 == 0) Fan(fanShots, fanSpread);
                else ProjectileWall(wallRows, wallGap + 0.5f);
                return;
            }

            // Each cannon lost makes the patterns denser (GDD 06).
            switch (model.Fury)
            {
                case 0:
                    if (pattern % 2 == 0) Fan(burstShots, fanSpread * 0.8f);
                    else ProjectileWall(wallRows, wallGap);
                    break;
                case 1:
                    switch (pattern % 3)
                    {
                        case 0: RingWithGap(ringShots, ringTurn, ringGapDegrees); break;
                        case 1: Fan(fanShots + 2, fanSpread * 0.82f); break;
                        default: ProjectileWall(wallRows, wallGap * 0.9f); break;
                    }
                    ringTurn += 180f / ringShots;
                    break;
                default:
                    switch (pattern % 3)
                    {
                        case 0: RingWithGap(ringShots + 2, ringTurn, ringGapDegrees * 0.9f); break;
                        case 1: Fan(fanShots + 2, fanSpread * 0.8f); break;
                        default: ProjectileWall(wallRows, wallGap * 0.9f); break;
                    }
                    ringTurn += 240f / Mathf.Max(1, ringShots);
                    break;
            }
        }

        float AttackInterval()
        {
            float interval = model.Phase == GuardianPhase.Survival ? fanInterval : model.Fury switch
            {
                0 => burstInterval,
                1 => ringInterval,
                _ => lastChanceInterval,
            };
            if (model.Frenzied)
                interval *= frenzyInterval;
            if (spiralLeft > 0f)
                interval += spiralLeft;   // the next attack waits for the stream to end
            return interval;
        }

        // Two arms of shots turning around the eyes' socket.
        void Spiral()
        {
            if (spiralLeft <= 0f || pool == null || shot == null)
                return;
            spiralLeft -= Time.deltaTime;
            spiralClock += Time.deltaTime;
            Vector3 origin = PartPosition(GuardianPart.Eyes);
            while (spiralClock >= spiralStep)
            {
                spiralClock -= spiralStep;
                for (int arm = 0; arm < 2; arm++)
                    Fire(origin, Quaternion.Euler(0f, 0f, spiralAngle + arm * 180f) * Vector3.right);
                spiralAngle += spiralTurn;
            }
        }

        // The antenna calls small fighters and the eyes call medium gunships, for as long as they last (GDD 06).
        void CallReinforcements()
        {
            if (reinforcements == null)
                return;
            if (!AddsAlive)
            {
                if (fighterPrefab != null && model.CallsFighters)
                    fighterTimer -= Time.deltaTime;
                if (gunshipPrefab != null && model.CallsGunships)
                    gunshipTimer -= Time.deltaTime;
            }

            if (AddsAlive || PrimaryThreatActive || moveState != MoveState.Holding || laserSuppressionTimer > 0f)
                return;
            if (fighterPrefab != null && model.CallsFighters && fighterTimer <= 0f)
            {
                fighterTimer = fighterCallSeconds;
                CallFighters();
                AfterReinforcementCall();
                return;
            }
            if (gunshipPrefab != null && model.CallsGunships && gunshipTimer <= 0f)
            {
                gunshipTimer = gunshipCallSeconds;
                CallGunship();
                AfterReinforcementCall();
            }
        }

        void AfterReinforcementCall()
        {
            attackTimer = Mathf.Max(attackTimer, reinforcementRecovery + attackTelegraphSeconds);
            laserSuppressionTimer = Mathf.Max(laserSuppressionTimer, reinforcementRecovery);
            attackTelegraphed = false;
            moveTimer = Mathf.Max(moveTimer, reinforcementRecovery);
        }

        // Fighters take models S_2 and S_3 in turn, with a kamikaze (S_1) every third call; heights follow a fixed table.
        static readonly float[] CallHeights = { 0.55f, -0.5f, 0.1f, -0.75f, 0.7f };
        static readonly int[] FighterModels = { 1, 2, 0 };

        void CallFighters()
        {
            int call = fighterCalls++;
            int modelIndex = FighterModels[call % FighterModels.Length];
            int room = fighterCap - fighters.Count;
            for (int i = 0; i < Mathf.Min(fightersPerCall, room); i++)
                Track(fighters, reinforcements.SpawnExtra(fighterPrefab, CallHeights[(call + i) % CallHeights.Length], modelIndex, i * 1.5f, i * 0.9f));
        }

        // A gunship holds the open left half of the screen, clear of the Guardian's body; M_2 and M_3 alternate.
        void CallGunship()
        {
            if (gunships.Count >= gunshipCap)
                return;
            int call = gunshipCalls++;
            float clearOfBody = -9.5f;   // moves its hold column from 4.5 u off the right edge to about mid-screen
            Track(gunships, reinforcements.SpawnExtra(gunshipPrefab, CallHeights[(call + 2) % CallHeights.Length], 1 + call % 2, clearOfBody, call * 1.7f));
        }

        void Track(List<Enemy> list, Enemy enemy)
        {
            if (enemy == null)
                return;
            list.Add(enemy);
            enemy.Removed += (e, _) => list.Remove(e);
        }

        void Fan(int count, float spread)
        {
            Vector3 aim = ship != null && ship.gameObject.activeInHierarchy ? ship.position - muzzle.position : Vector3.left;
            aim.z = 0f;
            if (aim.sqrMagnitude < 1e-4f)
                aim = Vector3.left;
            for (int i = 0; i < count; i++)
            {
                float angle = (i - (count - 1) * 0.5f) * spread;
                Fire(muzzle.position, Quaternion.Euler(0f, 0f, angle) * aim.normalized);
            }
        }

        void ProjectileWall(int rows, float gapSize)
        {
            float halfHeight = playfield.HalfExtents.y;
            float gapY = ship != null ? playfield.WorldToLocal(ship.position).y : 0f;
            gapY = Mathf.Clamp(gapY, -halfHeight + gapSize, halfHeight - gapSize);
            float x = playfield.HalfExtents.x + 0.25f;
            for (int i = 0; i < rows; i++)
            {
                float y = Mathf.Lerp(-halfHeight + 0.45f, halfHeight - 0.45f, rows <= 1 ? 0.5f : (float)i / (rows - 1));
                if (Mathf.Abs(y - gapY) < gapSize * 0.5f)
                    continue;
                Fire(playfield.LocalToWorld(new Vector2(x, y)), Vector3.left);
            }
        }

        void RingWithGap(int count, float turn, float gapDegrees)
        {
            const float safeDirection = 180f;
            for (int i = 0; i < count; i++)
            {
                float angle = turn + i * 360f / count;
                if (Mathf.Abs(Mathf.DeltaAngle(angle, safeDirection)) < gapDegrees)
                    continue;
                Fire(muzzle.position, Quaternion.Euler(0f, 0f, angle) * Vector3.right);
            }
        }

        void Fire(Vector3 origin, Vector3 direction) =>
            pool.Spawn(shot, origin, direction, Faction.Enemy, ShotSource.Enemy);

        void OnLaserTelegraphed(Vector3 position) => AttackTelegraphed?.Invoke(position);
        void OnLaserFired(Vector3 position) => AttackReleased?.Invoke(position);
        void OnLaserCompleted()
        {
            patternsSinceLaser = 0;
            attackTimer = Mathf.Max(attackTimer, attackTelegraphSeconds + 0.25f);
            attackTelegraphed = false;
        }

        void BuildTethers()
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                return;
            tetherMaterial = new Material(shader) { name = "Guardian Energy Tethers (Runtime)", hideFlags = HideFlags.DontSave };
            tethers = new LineRenderer[2];
            for (int i = 0; i < tethers.Length; i++)
            {
                var go = new GameObject($"Energy Tether {i + 1}");
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.numCapVertices = 2;
                line.widthMultiplier = 0.075f;
                line.sharedMaterial = tetherMaterial;
                line.sortingOrder = 66;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                tethers[i] = line;
            }
        }

        // The cables from the cannons to the eyes: they glow redder as the cannons are lost, and go with the parts.
        void UpdateTethers()
        {
            if (tethers == null)
                return;
            Health eyes = PartHealth(GuardianPart.Eyes);
            GuardianPart[] cannons = { GuardianPart.CannonUpper, GuardianPart.CannonLower };
            for (int i = 0; i < tethers.Length; i++)
            {
                LineRenderer line = tethers[i];
                Health cannon = PartHealth(cannons[i]);
                bool show = eyes != null && cannon != null && eyes.gameObject.activeInHierarchy && cannon.gameObject.activeInHierarchy;
                line.enabled = show;
                if (!show)
                    continue;
                line.SetPosition(0, cannon.transform.position);
                line.SetPosition(1, eyes.transform.position);
                int fury = model != null ? model.Fury : 0;
                Color color = fury >= 2
                    ? new Color(1f, 0.12f, 0.05f, 0.95f)
                    : fury == 1 ? new Color(1f, 0.78f, 0.12f, 1f) : new Color(1f, 0.42f, 0.08f, 0.72f);
                float pulse = fury > 0 ? 0.5f + Mathf.Sin(Time.time * (fury >= 2 ? 15f : 10f)) * 0.5f : 0f;
                line.startColor = color;
                line.endColor = color;
                line.widthMultiplier = Mathf.Lerp(0.065f, fury >= 2 ? 0.17f : 0.13f, pulse);
            }
        }

        static Transform FindDescendant(Transform root, string objectName)
        {
            foreach (Transform child in root)
            {
                if (child.name == objectName)
                    return child;
                Transform found = FindDescendant(child, objectName);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
