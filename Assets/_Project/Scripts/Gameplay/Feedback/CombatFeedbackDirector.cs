using System.Collections.Generic;
using OneMoreTime.Core.Boss;
using OneMoreTime.Core.Combat;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.Session;
using UnityEngine;

namespace OneMoreTime.Gameplay.Feedback
{
    /// <summary>
    /// Connects combat events to presentation. Damage rules remain in Health/Core; this component only
    /// flashes renderers, reuses VFX and sends restrained impulses to the camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackDirector : MonoBehaviour
    {
        [SerializeField] Health player;
        [SerializeField] WaveSpawner waves;
        [SerializeField] StageDirector stage;
        [SerializeField] ComboTracker combos;
        [SerializeField] CameraImpact cameraImpact;
        [SerializeField] CombatVfxPool effects;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        enum TargetKind { Player, Enemy, BossPart, BossTeeth }

        sealed class FlashState
        {
            public Health Target;
            public Renderer[] Renderers;
            public Color[] BaseColors;
            public MaterialPropertyBlock[] Blocks;
            public Color FlashColor;
            public float Age;
            public float Duration;
            public bool Active;
        }

        readonly Dictionary<Health, TargetKind> targets = new();
        readonly Dictionary<Health, FlashState> flashByTarget = new();
        readonly List<FlashState> activeFlashes = new();
        GuardianBoss boss;

        void OnEnable()
        {
            Track(player, TargetKind.Player);
            if (waves != null)
            {
                waves.EnemySpawned += OnEnemySpawned;
                waves.EnemyRemoved += OnEnemyRemoved;
                waves.EnemyDetonated += OnEnemyDetonated;
                waves.EnemyLaserTelegraphed += OnEnemyLaserTelegraphed;
                waves.EnemyLaserFired += OnEnemyLaserFired;
            }
            if (stage != null)
                stage.Changed += OnStageChanged;
            if (combos != null)
                combos.Triggered += OnCombo;
        }

        void OnDisable()
        {
            if (waves != null)
            {
                waves.EnemySpawned -= OnEnemySpawned;
                waves.EnemyRemoved -= OnEnemyRemoved;
                waves.EnemyDetonated -= OnEnemyDetonated;
                waves.EnemyLaserTelegraphed -= OnEnemyLaserTelegraphed;
                waves.EnemyLaserFired -= OnEnemyLaserFired;
            }
            if (stage != null)
                stage.Changed -= OnStageChanged;
            if (combos != null)
                combos.Triggered -= OnCombo;
            DetachBoss();
            foreach (Health target in new List<Health>(targets.Keys))
                Untrack(target);
            targets.Clear();
            flashByTarget.Clear();
            activeFlashes.Clear();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = activeFlashes.Count - 1; i >= 0; i--)
            {
                FlashState state = activeFlashes[i];
                state.Age += dt;
                float t = Mathf.Clamp01(state.Age / state.Duration);
                float amount = (1f - t) * (1f - t);
                ApplyFlash(state, amount);
                if (t >= 1f || state.Target == null)
                {
                    ApplyFlash(state, 0f);
                    state.Active = false;
                    activeFlashes.RemoveAt(i);
                }
            }
        }

        void OnEnemySpawned(Enemy enemy) => Track(enemy != null ? enemy.Health : null, TargetKind.Enemy);

        // A bomber going off shows how far its blast reaches.
        void OnEnemyDetonated(Enemy enemy, float radius)
        {
            if (enemy == null)
                return;
            effects?.Ring(enemy.transform.position, new Color(1f, 0.45f, 0.1f, 0.9f), radius, 0.3f, 0.12f);
            cameraImpact?.Impulse(0.6f, 0.2f, default, 0.7f);
        }

        void OnEnemyLaserTelegraphed(Enemy enemy)
        {
            if (enemy == null)
                return;
            effects?.EnemyLaserTelegraph(enemy.transform.position);
            cameraImpact?.Impulse(0.08f, 0.12f, new Vector2(-0.08f, 0f), 0.08f);
        }

        void OnEnemyLaserFired(Enemy enemy)
        {
            if (enemy == null)
                return;
            effects?.EnemyLaserFired(enemy.transform.position);
            cameraImpact?.Impulse(0.3f, 0.13f, new Vector2(-0.28f, 0f), 0.3f);
        }

        void OnEnemyRemoved(Enemy enemy, bool _)
        {
            if (enemy != null)
                Untrack(enemy.Health);
        }

        void OnStageChanged()
        {
            GuardianBoss next = stage != null ? stage.Boss : null;
            if (next == null || next == boss)
                return;
            DetachBoss();
            boss = next;
            boss.DashTelegraphed += OnBossDashTelegraphed;
            boss.Dashed += OnBossDashed;
            boss.AttackTelegraphed += OnBossAttackTelegraphed;
            boss.TeethExposed += OnBossTeethExposed;
            boss.PartDestroyed += OnBossPartDestroyed;
            boss.PhaseChanged += OnBossPhaseChanged;
            foreach (PartRef part in boss.Parts)
                Track(part.health, part.part == GuardianPart.TeethUpper || part.part == GuardianPart.TeethLower ? TargetKind.BossTeeth : TargetKind.BossPart);
        }

        void DetachBoss()
        {
            if (boss == null)
                return;
            boss.DashTelegraphed -= OnBossDashTelegraphed;
            boss.Dashed -= OnBossDashed;
            boss.AttackTelegraphed -= OnBossAttackTelegraphed;
            boss.TeethExposed -= OnBossTeethExposed;
            boss.PartDestroyed -= OnBossPartDestroyed;
            boss.PhaseChanged -= OnBossPhaseChanged;
            boss = null;
        }

        void OnBossAttackTelegraphed(Vector3 position) => effects?.BossAttackTelegraph(position);

        void OnBossDashTelegraphed(Vector3 position)
        {
            effects?.BossDashTelegraph(position);
            cameraImpact?.Impulse(0.18f, 0.18f, new Vector2(-0.2f, 0f), 0.15f);
        }

        void OnBossDashed(Vector3 position)
        {
            effects?.BossDash(position);
            cameraImpact?.Impulse(0.42f, 0.16f, new Vector2(-0.55f, 0f), 0.45f);
        }

        void OnBossTeethExposed(Vector3 position)
        {
            effects?.BossCoreOpened(position);
            cameraImpact?.Impulse(0.78f, 0.28f, new Vector2(-0.35f, 0.08f), 1f);
        }

        // A part shot off changes how the Guardian fights: the screen shakes harder the more furious it gets.
        void OnBossPartDestroyed(GuardianBoss source, GuardianPart part, DamageInfo _)
        {
            bool furious = source.Rules != null && source.Rules.Fury >= 2;
            effects?.BossPhaseShift(source.PartPosition(part), furious);
            cameraImpact?.Impulse(furious ? 0.92f : 0.58f, furious ? 0.38f : 0.24f, new Vector2(-0.45f, 0f), furious ? 1.2f : 0.7f);
        }

        void OnBossPhaseChanged(GuardianBoss source, OneMoreTime.Core.Boss.GuardianPhase _, OneMoreTime.Core.Boss.GuardianPhase next)
        {
            if (next != OneMoreTime.Core.Boss.GuardianPhase.Dismantle)
                return;
            effects?.BossPhaseShift(source.CorePosition, false);
            cameraImpact?.Impulse(0.58f, 0.24f, new Vector2(-0.45f, 0f), 0.7f);
        }

        void Track(Health target, TargetKind kind)
        {
            if (target == null || targets.ContainsKey(target))
                return;
            targets.Add(target, kind);
            flashByTarget.Add(target, BuildFlashState(target));
            target.Damaged += OnDamaged;
            target.Blocked += OnBlocked;
            target.Died += OnDied;
        }

        void Untrack(Health target)
        {
            if (target == null || !targets.Remove(target))
                return;
            target.Damaged -= OnDamaged;
            target.Blocked -= OnBlocked;
            target.Died -= OnDied;
            if (flashByTarget.TryGetValue(target, out FlashState state))
            {
                if (state.Active)
                {
                    ApplyFlash(state, 0f);
                    activeFlashes.Remove(state);
                }
                flashByTarget.Remove(target);
            }
        }

        void OnDamaged(Health target, DamageInfo info)
        {
            TargetKind kind = KindOf(target);
            Color sourceColor = SourceColor(info.Source);
            if (kind == TargetKind.Player)
            {
                Flash(target, new Color(1f, 0.2f, 0.18f, 1f), 0.26f);
                effects?.Burst(target.transform.position, new Color(0.25f, 0.9f, 1f, 1f), 1.1f, 16);
                effects?.Ring(target.transform.position, new Color(1f, 0.2f, 0.18f, 0.9f), 1.25f, 0.24f, 0.09f);
                cameraImpact?.Impulse(0.82f, 0.2f, new Vector2(-0.4f, 0.15f), 0.8f);
                return;
            }

            float scale = kind == TargetKind.Enemy ? (target.Max > 5 ? 0.85f : 0.55f) : 1.05f;
            int count = info.Kind == DamageKind.Gadget || info.Kind == DamageKind.Combined ? 12 : 6;
            Flash(target, Color.white, kind == TargetKind.Enemy ? 0.085f : 0.12f);
            effects?.Burst(target.transform.position, sourceColor, scale, count);
            if (info.Kind == DamageKind.Combined)
                effects?.Ring(target.transform.position, new Color(0.68f, 0.35f, 1f, 0.9f), 1.2f, 0.2f, 0.06f);
            cameraImpact?.Impulse(kind == TargetKind.Enemy ? 0.14f : 0.22f, 0.07f);
        }

        void OnBlocked(Health target, DamageInfo info)
        {
            Color sourceColor = SourceColor(info.Source);
            effects?.Burst(target.transform.position, sourceColor, 0.55f, 5);
            effects?.Ring(target.transform.position, new Color(0.3f, 0.75f, 1f, 0.75f), 0.85f, 0.16f, 0.045f);
            cameraImpact?.Impulse(0.1f, 0.05f);
        }

        void OnDied(Health target, DamageInfo info)
        {
            TargetKind kind = KindOf(target);
            Vector3 position = target.transform.position;
            Color color;
            float scale;
            int particles;
            float shake;
            float radius;

            switch (kind)
            {
                case TargetKind.Player:
                    color = new Color(0.2f, 0.95f, 1f, 1f);
                    scale = 2.2f; particles = 52; shake = 1f; radius = 3.2f;
                    break;
                case TargetKind.BossTeeth:
                    color = new Color(1f, 0.28f, 0.12f, 1f);
                    scale = 3.4f; particles = 64; shake = 1f; radius = 4.5f;
                    break;
                case TargetKind.BossPart:
                    color = new Color(1f, 0.55f, 0.12f, 1f);
                    scale = 2f; particles = 44; shake = 0.72f; radius = 2.7f;
                    break;
                default:
                    bool tough = target.Max > 5;
                    color = tough ? new Color(1f, 0.22f, 0.12f, 1f) : new Color(1f, 0.58f, 0.12f, 1f);
                    scale = tough ? 1.6f : 0.95f;
                    particles = tough ? 34 : 20;
                    shake = tough ? 0.52f : 0.32f;
                    radius = tough ? 2f : 1.2f;
                    break;
            }

            effects?.Burst(position, color, scale, particles);
            effects?.Ring(position, color, radius, 0.32f + scale * 0.05f, 0.09f + scale * 0.025f);
            cameraImpact?.Impulse(shake, 0.12f + scale * 0.035f, default, Mathf.Lerp(0.15f, 1.25f, shake));
        }

        void OnCombo(ComboReport combo)
        {
            effects?.TemporalConvergence(combo.Position);
            cameraImpact?.Impulse(0.62f, 0.22f, default, 0.65f);
        }

        TargetKind KindOf(Health target)
        {
            if (target != null && targets.TryGetValue(target, out TargetKind kind))
                return kind;
            return target != null && target.Faction == Faction.Player ? TargetKind.Player : TargetKind.Enemy;
        }

        void Flash(Health target, Color color, float duration)
        {
            if (target == null || !flashByTarget.TryGetValue(target, out FlashState state))
                return;
            state.FlashColor = color;
            state.Age = 0f;
            state.Duration = Mathf.Max(0.02f, duration);
            if (!state.Active)
            {
                state.Active = true;
                activeFlashes.Add(state);
            }
            ApplyFlash(state, 1f);
        }

        static FlashState BuildFlashState(Health target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            var state = new FlashState
            {
                Target = target,
                Renderers = renderers,
                BaseColors = new Color[renderers.Length],
                Blocks = new MaterialPropertyBlock[renderers.Length],
            };
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i] != null ? renderers[i].sharedMaterial : null;
                if (material != null && material.HasProperty(BaseColorId))
                    state.BaseColors[i] = material.GetColor(BaseColorId);
                else if (material != null && material.HasProperty(ColorId))
                    state.BaseColors[i] = material.GetColor(ColorId);
                else
                    state.BaseColors[i] = Color.white;
                state.Blocks[i] = new MaterialPropertyBlock();
            }
            return state;
        }

        static void ApplyFlash(FlashState state, float amount)
        {
            for (int i = 0; i < state.Renderers.Length; i++)
            {
                Renderer renderer = state.Renderers[i];
                if (renderer == null)
                    continue;
                Color color = Color.Lerp(state.BaseColors[i], state.FlashColor, amount);
                renderer.GetPropertyBlock(state.Blocks[i]);
                state.Blocks[i].SetColor(BaseColorId, color);
                state.Blocks[i].SetColor(ColorId, color);
                renderer.SetPropertyBlock(state.Blocks[i]);
            }
        }

        static Color SourceColor(ShotSource source)
        {
            return source switch
            {
                ShotSource.Echo => new Color(0.72f, 0.35f, 1f, 1f),
                ShotSource.Enemy => new Color(1f, 0.2f, 0.12f, 1f),
                _ => new Color(0.2f, 0.95f, 1f, 1f),
            };
        }
    }
}
