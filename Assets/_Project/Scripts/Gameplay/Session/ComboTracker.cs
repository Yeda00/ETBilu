using System;
using System.Collections.Generic;
using OneMoreTime.Core.Combos;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Enemies;
using UnityEngine;

namespace OneMoreTime.Gameplay.Session
{
    /// <summary>A combo that landed, for feedback and score.</summary>
    public readonly struct ComboReport
    {
        public readonly ComboType Type;
        public readonly Vector3 Position;
        public readonly int Points;

        public ComboReport(ComboType type, Vector3 position, int points)
        {
            Type = type;
            Position = position;
            Points = points;
        }
    }

    /// <summary>
    /// Recognises the temporal combos (GDD 03) from real hits and applies their result.
    /// <list type="bullet">
    /// <item><b>Crossfire</b>: the ship and an echo hit the same target inside a short window → bonus
    /// combined damage.</item>
    /// <item><b>Synchronized Strike</b>: the Guardian's two generators fall almost together, one to the
    /// ship and the other to an echo → the core's shield drops (the Guardian's own rule) and the combo
    /// is reported.</item>
    /// </list>
    /// Rules live in <see cref="CrossfireRule"/> and <see cref="SynchronizedStrikeRule"/>. Watches the
    /// enemies the waves spawn and the Guardian the stage brings in.
    /// </summary>
    public sealed class ComboTracker : MonoBehaviour
    {
        [SerializeField] WaveSpawner waves;
        [SerializeField] StageDirector stage;

        [Header("Crossfire (GDD 03) — starting values")]
        [Tooltip("Most seconds between the ship's hit and the echo's hit on one target. Generous on purpose (GDD 10).")]
        [SerializeField, Min(0.05f)] float crossfireWindow = 1f;
        [Tooltip("Seconds before the same target can give another Crossfire.")]
        [SerializeField, Min(0f)] float crossfireRearm = 1f;
        [SerializeField, Min(0)] int crossfireDamage = 3;
        [SerializeField, Min(0)] int crossfireScore = 100;

        [Header("Synchronized Strike (GDD 03) — starting values")]
        [Tooltip("Most seconds between the two generators falling. Generous on purpose (GDD 10).")]
        [SerializeField, Min(0.05f)] float strikeWindow = 2.5f;
        [SerializeField, Min(0)] int strikeScore = 1000;

        readonly Dictionary<Health, int> ids = new();
        CrossfireRule crossfire;
        SynchronizedStrikeRule strike;
        GuardianBoss boss;
        int nextId;

        public event Action<ComboReport> Triggered;

        void Awake()
        {
            crossfire = new CrossfireRule(crossfireWindow, crossfireRearm);
            strike = new SynchronizedStrikeRule(strikeWindow);
        }

        void OnEnable()
        {
            if (waves != null)
            {
                waves.EnemySpawned += OnEnemySpawned;
                waves.EnemyRemoved += OnEnemyRemoved;
            }
            if (stage != null)
                stage.Changed += OnStageChanged;
        }

        void OnDisable()
        {
            if (waves != null)
            {
                waves.EnemySpawned -= OnEnemySpawned;
                waves.EnemyRemoved -= OnEnemyRemoved;
            }
            if (stage != null)
                stage.Changed -= OnStageChanged;
            if (boss != null)
                boss.GeneratorDestroyed -= OnGeneratorDestroyed;
            boss = null;
            foreach (var target in ids.Keys)
            {
                if (target == null)
                    continue;
                target.Damaged -= OnDamaged;
                target.Died -= OnDied;
            }
            ids.Clear();
        }

        void OnEnemySpawned(Enemy enemy) => Track(enemy.Health);

        void OnEnemyRemoved(Enemy enemy, bool _) => Untrack(enemy.Health);

        void OnStageChanged()
        {
            var next = stage.Boss;
            if (next == null || next == boss)
                return;
            if (boss != null)
                boss.GeneratorDestroyed -= OnGeneratorDestroyed;
            boss = next;
            boss.GeneratorDestroyed += OnGeneratorDestroyed;
            strike.Reset();
            Track(boss.CoreHealth);
            foreach (var generator in boss.Generators)
                Track(generator);
        }

        void Track(Health target)
        {
            if (target == null || ids.ContainsKey(target))
                return;
            ids.Add(target, nextId++);
            target.Damaged += OnDamaged;
            target.Died += OnDied;
        }

        void Untrack(Health target)
        {
            if (target == null || !ids.TryGetValue(target, out int id))
                return;
            target.Damaged -= OnDamaged;
            target.Died -= OnDied;
            ids.Remove(target);
            crossfire.Forget(id);
        }

        void OnDamaged(Health target, DamageInfo info)
        {
            // The bonus itself is Combined damage and must not feed the rule again.
            if (info.Kind == DamageKind.Combined || info.Kind == DamageKind.Contact)
                return;
            if (!TryGetAttacker(info.Source, out var by) || !ids.TryGetValue(target, out int id))
                return;
            if (!crossfire.Hit(id, by, Time.time))
                return;

            // Report before the bonus: if the bonus brings down a generator, the Synchronized Strike it
            // may complete is the last notice shown.
            Report(ComboType.Crossfire, target.transform.position, crossfireScore);
            if (crossfireDamage > 0 && !target.IsDead)   // the hit that completed the combo may have killed it
                target.TakeDamage(new DamageInfo(crossfireDamage, DamageKind.Combined, info.Source));
        }

        // Generators come back after falling, so they stay tracked; only their Crossfire marks reset.
        void OnDied(Health target, DamageInfo _)
        {
            if (ids.TryGetValue(target, out int id))
                crossfire.Forget(id);
        }

        void OnGeneratorDestroyed(GuardianBoss source, int index, DamageInfo info)
        {
            if (!TryGetAttacker(info.Source, out var by))
                return;
            // The core only opens under the Guardian's own window; the combo is the ship and an echo
            // doing it together, so it counts only when the core did open.
            if (strike.Broken(index, by, Time.time) && source.CoreVulnerable)
                Report(ComboType.SynchronizedStrike, source.CorePosition, strikeScore);
        }

        void Report(ComboType type, Vector3 position, int points) => Triggered?.Invoke(new ComboReport(type, position, points));

        static bool TryGetAttacker(ShotSource source, out Attacker by)
        {
            by = source == ShotSource.Echo ? Attacker.Echo : Attacker.Ship;
            return source == ShotSource.Ship || source == ShotSource.Echo;
        }
    }
}
