using System;
using System.Collections.Generic;
using OneMoreTime.Core.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Boss
{
    /// <summary>
    /// The Guardian of the Cycle, minimal version (GDD 06, phases 1–3): an armoured core flanked by two
    /// generators. Both generators must fall inside one window to open the core for a few seconds; a lone
    /// fallen generator comes back when the window closes. Rules live in <see cref="GuardianModel"/>;
    /// this component flies in, shows the shields, rebuilds generators and fires the attack patterns.
    /// </summary>
    public sealed class GuardianBoss : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] Health core;
        [Tooltip("Upper and lower generator.")]
        [SerializeField] Health[] generators = new Health[GuardianModel.GeneratorCount];
        [SerializeField] GameObject coreShield;
        [SerializeField] GameObject[] generatorShields = new GameObject[GuardianModel.GeneratorCount];
        [SerializeField] Transform muzzle;
        [SerializeField] SweepLaser laser;
        [SerializeField] Projectile shot;

        [Header("Phases (GDD 06) — starting values")]
        [SerializeField, Min(0f)] float survivalSeconds = 10f;
        [Tooltip("Window for the second generator in phase 2 (generous).")]
        [SerializeField, Min(0.1f)] float preparationWindow = 5f;
        [Tooltip("Window for the second generator in phase 3 (short).")]
        [SerializeField, Min(0.1f)] float syncWindow = 2.5f;
        [SerializeField, Min(0.1f)] float vulnerableSeconds = 4f;

        [Header("Entry")]
        [Tooltip("Playfield-local x where the Guardian holds position, measured from the right edge.")]
        [SerializeField] float holdFromRightEdge = 3f;
        [Tooltip("How far past the right edge the Guardian starts its entry: enough for the whole model to be off screen.")]
        [SerializeField, Min(0f)] float entryDistance = 5f;
        [SerializeField, Min(0.1f)] float entrySeconds = 2.5f;

        [Header("Combat movement")]
        [Tooltip("Vertical drift keeps the boss alive on screen without making its weak points unreadable.")]
        [SerializeField, Min(0f)] float driftAmplitude = 0.8f;
        [SerializeField, Min(0f)] float driftFrequency = 0.18f;
        [Tooltip("Small horizontal counter-motion, in playfield units.")]
        [SerializeField, Min(0f)] float driftHorizontal = 0.25f;

        [Header("Attacks")]
        [Tooltip("Phase 1: aimed fan.")]
        [SerializeField, Min(0.1f)] float fanInterval = 1.1f;
        [SerializeField, Min(1)] int fanShots = 7;
        [SerializeField, Min(0f)] float fanSpread = 11f;
        [Tooltip("Phase 2: small aimed burst.")]
        [SerializeField, Min(0.1f)] float burstInterval = 1f;
        [SerializeField, Min(1)] int burstShots = 5;
        [Tooltip("Phase 3: ring of shots, turning a little each time.")]
        [SerializeField, Min(0.1f)] float ringInterval = 1.1f;
        [SerializeField, Min(3)] int ringShots = 18;

        [Header("Score")]
        [SerializeField, Min(0)] int generatorScore = 250;
        [SerializeField, Min(0)] int coreScore = 5000;

        GuardianModel model;
        Playfield playfield;
        Transform ship;
        ProjectilePool pool;
        Vector2 entryFrom, entryTo;
        float entryAge;
        float combatAge;
        float attackTimer;
        float ringTurn;
        readonly bool[] shownUp = new bool[GuardianModel.GeneratorCount];
        Action<Health, DamageInfo>[] generatorHandlers;

        public event Action Defeated;
        public event Action<int> Scored;
        /// <summary>A generator fell and counted: its index and the hit that brought it down.</summary>
        public event Action<GuardianBoss, int, DamageInfo> GeneratorDestroyed;

        public bool IsEntering => model == null;
        public GuardianPhase Phase => model?.Phase ?? GuardianPhase.Survival;
        public bool CoreVulnerable => model != null && model.CoreVulnerable;
        public float VulnerableRemaining => model?.VulnerableRemaining ?? 0f;
        public float WindowRemaining => model?.WindowRemaining ?? 0f;
        public float CoreNormalized => core != null ? core.Normalized : 0f;
        public bool IsGeneratorUp(int i) => model == null || model.IsGeneratorUp(i);
        public Health CoreHealth => core;
        public IReadOnlyList<Health> Generators => generators;
        public Vector3 CorePosition => core != null ? core.transform.position : transform.position;

        /// <summary>Fly in from the right edge. Called by the stage right after spawning.</summary>
        public void Begin(Playfield field, Transform shipTransform, ProjectilePool projectiles)
        {
            playfield = field;
            ship = shipTransform;
            pool = projectiles;
            var h = playfield.HalfExtents;
            entryFrom = new Vector2(h.x + entryDistance, 0f);
            entryTo = new Vector2(h.x - holdFromRightEdge, 0f);
            transform.position = playfield.LocalToWorld(entryFrom);
            ApplyShields();
        }

        void Awake()
        {
            for (int i = 0; i < shownUp.Length; i++)
                shownUp[i] = true;
            generatorHandlers = new Action<Health, DamageInfo>[generators.Length];
            for (int i = 0; i < generators.Length; i++)
            {
                int index = i;
                generatorHandlers[i] = (_, info) => OnGeneratorDied(index, info);
            }
        }

        void OnEnable()
        {
            if (core != null)
                core.Died += OnCoreDied;
            for (int i = 0; i < generators.Length; i++)
                if (generators[i] != null)
                    generators[i].Died += generatorHandlers[i];
        }

        void OnDisable()
        {
            if (core != null)
                core.Died -= OnCoreDied;
            for (int i = 0; i < generators.Length; i++)
                if (generators[i] != null)
                    generators[i].Died -= generatorHandlers[i];
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
            Drift();
            ApplyShields();
            RebuildGenerators();
            if (laser != null)
                laser.Active = model.Phase == GuardianPhase.Survival;
            Attack();
        }

        void Enter()
        {
            entryAge += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(entryAge / entrySeconds));
            transform.position = playfield.LocalToWorld(Vector2.Lerp(entryFrom, entryTo, k));
            if (k < 1f)
                return;
            model = new GuardianModel(survivalSeconds, preparationWindow, syncWindow, vulnerableSeconds);
            attackTimer = 0.8f;
        }

        void Drift()
        {
            combatAge += Time.deltaTime;
            float angle = combatAge * driftFrequency * Mathf.PI * 2f;
            var offset = new Vector2(Mathf.Sin(angle * 0.5f) * driftHorizontal, Mathf.Sin(angle) * driftAmplitude);
            transform.position = playfield.LocalToWorld(entryTo + offset);
        }

        void ApplyShields()
        {
            bool generatorsShielded = model == null || model.GeneratorsShielded;
            bool coreShielded = model == null || !model.CoreVulnerable;
            for (int i = 0; i < generators.Length; i++)
            {
                if (generators[i] != null)
                    generators[i].Shielded = generatorsShielded;
                if (i < generatorShields.Length && generatorShields[i] != null && generatorShields[i].activeSelf != generatorsShielded)
                    generatorShields[i].SetActive(generatorsShielded);
            }
            if (core != null)
                core.Shielded = coreShielded;
            if (coreShield != null && coreShield.activeSelf != coreShielded)
                coreShield.SetActive(coreShielded);
        }

        // The model brings generators back (window closed, core closed); bring their objects back too.
        void RebuildGenerators()
        {
            for (int i = 0; i < generators.Length; i++)
            {
                bool up = model.IsGeneratorUp(i);
                if (up && !shownUp[i] && generators[i] != null)
                {
                    generators[i].ResetHealth();
                    generators[i].gameObject.SetActive(true);
                }
                shownUp[i] = up;
            }
        }

        void OnGeneratorDied(int index, DamageInfo info)
        {
            if (model != null && model.DestroyGenerator(index))
            {
                generators[index].gameObject.SetActive(false);
                shownUp[index] = false;
                Scored?.Invoke(generatorScore);
                ApplyShields();
                GeneratorDestroyed?.Invoke(this, index, info);
            }
            else
            {
                generators[index].ResetHealth();   // a hit that doesn't count leaves it standing
            }
        }

        void OnCoreDied(Health _, DamageInfo __)
        {
            if (model == null || !model.DefeatCore())
            {
                core.ResetHealth();
                return;
            }
            if (laser != null)
                laser.Active = false;
            core.gameObject.SetActive(false);
            Scored?.Invoke(coreScore);
            Defeated?.Invoke();
        }

        // ---------- attacks ----------

        void Attack()
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f || pool == null || shot == null || muzzle == null)
                return;

            switch (model.Phase)
            {
                case GuardianPhase.Survival:
                    Fan(fanShots, fanSpread);
                    attackTimer = fanInterval;
                    break;
                case GuardianPhase.Preparation:
                    Fan(burstShots, fanSpread * 0.6f);
                    attackTimer = burstInterval;
                    break;
                case GuardianPhase.Synchronization:
                    Ring(ringShots, ringTurn);
                    ringTurn += 180f / ringShots;   // the next ring fills the gaps of this one
                    attackTimer = ringInterval;
                    break;
            }
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
                Fire(Quaternion.Euler(0f, 0f, angle) * aim.normalized);
            }
        }

        void Ring(int count, float turn)
        {
            for (int i = 0; i < count; i++)
                Fire(Quaternion.Euler(0f, 0f, turn + i * 360f / count) * Vector3.left);
        }

        void Fire(Vector3 direction) => pool.Spawn(shot, muzzle.position, direction, Faction.Enemy, ShotSource.Enemy);
    }
}
