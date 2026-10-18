using System.Collections.Generic;
using OneMoreTime.Core.Audio;
using OneMoreTime.Core.Boss;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.Session;
using UnityEngine;

namespace OneMoreTime.Gameplay.Audio
{
    /// <summary>
    /// Connects game events to sound, the way <c>CombatFeedbackDirector</c> connects them to visuals. Rules
    /// stay in Health, the echo and the stage; this only asks <see cref="SfxPlayer"/> and
    /// <see cref="MusicPlayer"/> for the right cue (GDD 08).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] SfxPlayer sfx;
        [SerializeField] MusicPlayer music;
        [SerializeField] Health player;
        [SerializeField] WaveSpawner waves;
        [SerializeField] StageDirector stage;
        [SerializeField] ComboTracker combos;
        [SerializeField] EchoAbility echoes;
        [SerializeField] GameSession session;
        [SerializeField] ProjectilePool pool;

        [Header("Recording pulse (GDD 08)")]
        [SerializeField, Range(0f, 1f)] float pulseVolume = 0.22f;
        [Tooltip("Pitch of the first beat; it rises to the second value as the 5 s fill up.")]
        [SerializeField, Min(40f)] float pulseFrequency = 660f;

        enum TargetKind { Player, Enemy, Part, Teeth }

        readonly Dictionary<Health, TargetKind> targets = new();
        readonly RecordPulse pulse = new();
        AudioClip pulseClip;
        AudioClip anchorClip;
        AudioClip returnClip;
        AudioClip arrivalClip;
        AudioClip bossChargeClip;
        AudioClip bossDashClip;
        AudioClip bossRuptureClip;
        AudioClip enemyLaserChargeClip;
        AudioClip enemyLaserFireClip;
        GuardianBoss boss;
        int bossPhase;
        SessionState lastState = SessionState.Playing;

        bool pulsePanRight;

        void Awake()
        {
            pulseClip = ProceduralClips.Blip(pulseFrequency);
            anchorClip = ProceduralClips.TemporalAnchor();
            returnClip = ProceduralClips.TemporalReturn();
            arrivalClip = ProceduralClips.TemporalArrival();
            bossChargeClip = ProceduralClips.BossCharge();
            bossDashClip = ProceduralClips.BossDash();
            bossRuptureClip = ProceduralClips.BossRupture();
            enemyLaserChargeClip = ProceduralClips.EnemyLaserCharge();
            enemyLaserFireClip = ProceduralClips.EnemyLaserFire();
        }

        void OnEnable()
        {
            Track(player, TargetKind.Player);
            if (pool != null)
                pool.CueRaised += OnCueRaised;
            if (waves != null)
            {
                waves.EnemySpawned += OnEnemySpawned;
                waves.EnemyRemoved += OnEnemyRemoved;
                waves.EnemyLaserTelegraphed += OnEnemyLaserTelegraphed;
                waves.EnemyLaserFired += OnEnemyLaserFired;
            }
            if (stage != null)
                stage.Changed += OnStageChanged;
            if (combos != null)
                combos.Triggered += OnCombo;
            if (echoes != null)
            {
                echoes.AnchorPlanted += OnAnchorPlanted;
                echoes.Returned += OnTemporalReturned;
            }
            if (session != null)
                session.Changed += OnSessionChanged;
        }

        void Start()
        {
            if (stage != null && stage.HasBegun)
                OnStageChanged();   // the stage may have started before this was enabled
        }

        void OnDisable()
        {
            if (pool != null)
                pool.CueRaised -= OnCueRaised;
            if (waves != null)
            {
                waves.EnemySpawned -= OnEnemySpawned;
                waves.EnemyRemoved -= OnEnemyRemoved;
                waves.EnemyLaserTelegraphed -= OnEnemyLaserTelegraphed;
                waves.EnemyLaserFired -= OnEnemyLaserFired;
            }
            if (stage != null)
                stage.Changed -= OnStageChanged;
            if (combos != null)
                combos.Triggered -= OnCombo;
            if (echoes != null)
            {
                echoes.AnchorPlanted -= OnAnchorPlanted;
                echoes.Returned -= OnTemporalReturned;
            }
            if (session != null)
                session.Changed -= OnSessionChanged;
            DetachBoss();
            foreach (var target in new List<Health>(targets.Keys))
                Untrack(target);
            targets.Clear();
        }

        void OnDestroy()
        {
            if (pulseClip != null)
                Destroy(pulseClip);
            if (anchorClip != null)
                Destroy(anchorClip);
            if (returnClip != null)
                Destroy(returnClip);
            if (arrivalClip != null)
                Destroy(arrivalClip);
            if (bossChargeClip != null)
                Destroy(bossChargeClip);
            if (bossDashClip != null)
                Destroy(bossDashClip);
            if (bossRuptureClip != null)
                Destroy(bossRuptureClip);
            if (enemyLaserChargeClip != null)
                Destroy(enemyLaserChargeClip);
            if (enemyLaserFireClip != null)
                Destroy(enemyLaserFireClip);
        }

        void Update()
        {
            UpdateRecordingPulse();
            UpdateBoss();
        }

        // ---------------------------------------------------------------- shots, mines

        void OnCueRaised(SoundCue cue, Vector3 position) => Play(cue, position);

        // ---------------------------------------------------------------- health

        void OnEnemySpawned(Enemy enemy) => Track(enemy != null ? enemy.Health : null, TargetKind.Enemy);

        void OnEnemyRemoved(Enemy enemy, bool _)
        {
            if (enemy != null)
                Untrack(enemy.Health);
        }

        void OnEnemyLaserTelegraphed(Enemy enemy)
        {
            if (enemy != null)
                sfx?.PlayClip(enemyLaserChargeClip, 0.55f, 1f, 0f, Pan(enemy.transform.position), 5600f);
        }

        void OnEnemyLaserFired(Enemy enemy)
        {
            if (enemy == null)
                return;
            sfx?.PlayClip(enemyLaserFireClip, 0.78f, 1f, 0f, Pan(enemy.transform.position), 9200f);
            music?.PulseDuck(0.68f, 0.04f);
        }

        void Track(Health target, TargetKind kind)
        {
            if (target == null || targets.ContainsKey(target))
                return;
            targets.Add(target, kind);
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
        }

        void OnDamaged(Health target, DamageInfo info)
        {
            if (target == null || target.IsDead)
                return;   // the death cue covers it
            Play(KindOf(target) == TargetKind.Player ? SoundCue.PlayerHit : SoundCue.EnemyHit, target.transform.position);
        }

        void OnBlocked(Health target, DamageInfo info) => Play(SoundCue.ShieldBlock, target.transform.position);

        void OnDied(Health target, DamageInfo info)
        {
            SoundCue cue = KindOf(target) switch
            {
                TargetKind.Player => SoundCue.PlayerDeath,
                TargetKind.Teeth => SoundCue.CoreDeath,
                TargetKind.Part => SoundCue.GeneratorDeath,
                _ => target.Max > 5 ? SoundCue.EnemyDeathBig : SoundCue.EnemyDeathSmall,
            };
            Play(cue, target.transform.position);
        }

        TargetKind KindOf(Health target)
        {
            if (target != null && targets.TryGetValue(target, out TargetKind kind))
                return kind;
            return target != null && target.Faction == Faction.Player ? TargetKind.Player : TargetKind.Enemy;
        }

        // ---------------------------------------------------------------- echo and combos

        void OnCombo(ComboReport combo) => Play(SoundCue.Combo, combo.Position);

        void OnAnchorPlanted(Vector3 position)
        {
            pulse.Begin();
            pulsePanRight = false;
            Play(SoundCue.RecordStart, position);
            sfx?.PlayClip(anchorClip, 0.7f, 1f);
        }

        void OnTemporalReturned(Vector3 position, int bulletsDispelled)
        {
            Play(SoundCue.RecordStop, position);
            Play(SoundCue.EchoInvoke, position);
            Play(SoundCue.EchoArrive, position, 0.14f);
            float weight = Mathf.Clamp01(bulletsDispelled / 8f);
            sfx?.PlayClip(returnClip, Mathf.Lerp(0.72f, 0.9f, weight), Mathf.Lerp(1f, 0.9f, weight));
            sfx?.PlayClip(arrivalClip, 0.5f, 1f, 0.09f, 0f, 2800f);
            music?.PulseDuck(Mathf.Lerp(0.42f, 0.28f, weight), 0.08f);
        }

        void UpdateRecordingPulse()
        {
            if (echoes == null || sfx == null || !echoes.IsRecording)
                return;
            float progress = echoes.MaxRecordSeconds > 0f ? echoes.RecordedSeconds / echoes.MaxRecordSeconds : 0f;
            if (pulse.Tick(Time.deltaTime, progress))
            {
                pulsePanRight = !pulsePanRight;
                float pan = (pulsePanRight ? 1f : -1f) * Mathf.Lerp(0.05f, 0.26f, progress);
                sfx.PlayClip(pulseClip, pulseVolume, pulse.PitchAt(progress), 0f, pan, Mathf.Lerp(4200f, 12000f, progress));
            }
        }

        // ---------------------------------------------------------------- stage and boss

        void OnStageChanged()
        {
            if (stage == null)
                return;

            if (music != null)
            {
                switch (stage.Segment)
                {
                    case StageSegment.Waves: music.Play(MusicTrack.Waves); break;
                    case StageSegment.Boss: music.Play(MusicTrack.Boss); break;
                    default: music.Stop(); break;
                }
            }

            if (stage.Boss != null && stage.Boss != boss)
            {
                DetachBoss();
                boss = stage.Boss;
                bossPhase = (int)boss.Phase;
                boss.AttackTelegraphed += OnBossAttackTelegraphed;
                boss.DashTelegraphed += OnBossDashTelegraphed;
                boss.Dashed += OnBossDashed;
                boss.TeethExposed += OnBossTeethExposed;
                boss.PartDestroyed += OnBossPartDestroyed;
                boss.PhaseChanged += OnBossPhaseChanged;
                foreach (PartRef part in boss.Parts)
                    Track(part.health, part.part == GuardianPart.TeethUpper || part.part == GuardianPart.TeethLower ? TargetKind.Teeth : TargetKind.Part);
            }
        }

        void DetachBoss()
        {
            if (boss == null)
                return;
            boss.AttackTelegraphed -= OnBossAttackTelegraphed;
            boss.DashTelegraphed -= OnBossDashTelegraphed;
            boss.Dashed -= OnBossDashed;
            boss.TeethExposed -= OnBossTeethExposed;
            boss.PartDestroyed -= OnBossPartDestroyed;
            boss.PhaseChanged -= OnBossPhaseChanged;
            boss = null;
        }

        void OnBossAttackTelegraphed(Vector3 position)
        {
            float pan = Pan(position);
            sfx?.PlayClip(bossChargeClip, 0.42f, 1f, 0f, pan, 5200f);
        }

        void OnBossDashTelegraphed(Vector3 position)
        {
            float pan = Pan(position);
            sfx?.PlayClip(bossChargeClip, 0.48f, 0.68f, 0f, pan, 3100f);
        }

        void OnBossDashed(Vector3 position)
        {
            sfx?.PlayClip(bossDashClip, 0.76f, 0.92f, 0f, Pan(position), 7200f);
            music?.PulseDuck(0.62f, 0.045f);
        }

        // A set of teeth became reachable: the weak point opens.
        void OnBossTeethExposed(Vector3 position)
        {
            Play(SoundCue.BossVulnerable, position);
            sfx?.PlayClip(bossRuptureClip, 0.9f, 1f, 0f, Pan(position));
            music?.PulseDuck(0.26f, 0.12f);
        }

        // Each part lost is a change in the fight: the lower the Guardian's rumble, the more furious it is.
        void OnBossPartDestroyed(GuardianBoss source, GuardianPart part, DamageInfo _)
        {
            bool furious = source.Rules != null && source.Rules.Fury >= 2;
            sfx?.PlayClip(bossRuptureClip, furious ? 0.88f : 0.58f, furious ? 0.72f : 0.9f, 0f, Pan(source.PartPosition(part)));
            music?.PulseDuck(furious ? 0.2f : 0.38f, 0.12f);
        }

        void OnBossPhaseChanged(GuardianBoss source, OneMoreTime.Core.Boss.GuardianPhase _, OneMoreTime.Core.Boss.GuardianPhase next)
        {
            bossPhase = (int)next;
            Play(SoundCue.BossPhase, source.CorePosition);
            sfx?.PlayClip(bossRuptureClip, 0.58f, 0.9f, 0f, Pan(source.CorePosition));
            music?.PulseDuck(0.38f, 0.12f);
        }

        void UpdateBoss()
        {
            if (boss == null)
                return;

            int phase = (int)boss.Phase;
            if (phase > bossPhase)
                Play(SoundCue.BossPhase, boss.CorePosition);
            bossPhase = phase;
        }

        // ---------------------------------------------------------------- session

        void OnSessionChanged()
        {
            if (session == null || session.State == lastState)
                return;

            SessionState previous = lastState;
            lastState = session.State;
            switch (session.State)
            {
                case SessionState.Paused:
                    Play(SoundCue.Pause, ShipPosition());
                    music?.SetPaused(true);
                    break;
                case SessionState.Playing:
                    if (previous == SessionState.Paused)
                        Play(SoundCue.Resume, ShipPosition());
                    music?.SetPaused(false);
                    break;
                case SessionState.Defeated:
                    music?.Stop();
                    break;
                case SessionState.Cleared:
                    music?.Stop();
                    Play(SoundCue.Victory, ShipPosition());
                    break;
            }
        }

        // ---------------------------------------------------------------- helpers

        void Play(SoundCue cue, Vector3 position, float delay = 0f)
        {
            if (sfx != null)
                sfx.Play(cue, position, delay);
        }

        Vector3 ShipPosition() => player != null ? player.transform.position : Vector3.zero;

        float Pan(Vector3 worldPosition)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return 0f;
            return Mathf.Clamp((camera.WorldToViewportPoint(worldPosition).x - 0.5f) * 1.2f, -0.65f, 0.65f);
        }
    }
}
