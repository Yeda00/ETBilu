using System.Collections.Generic;
using OneMoreTime.Core.Audio;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.Session;
using OneMoreTime.Gameplay.Traps;
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

        enum TargetKind { Player, Enemy, Generator, Core }

        readonly Dictionary<Health, TargetKind> targets = new();
        readonly RecordPulse pulse = new();
        readonly List<SwitchPad> switches = new();
        readonly List<bool> switchHeld = new();
        AudioClip pulseClip;
        AudioClip anchorClip;
        AudioClip returnClip;
        AudioClip arrivalClip;
        GuardianBoss boss;
        TrapSection section;
        bool bossVulnerable;
        int bossPhase;
        bool sectionCleared;
        SessionState lastState = SessionState.Playing;

        bool pulsePanRight;

        void Awake()
        {
            pulseClip = ProceduralClips.Blip(pulseFrequency);
            anchorClip = ProceduralClips.TemporalAnchor();
            returnClip = ProceduralClips.TemporalReturn();
            arrivalClip = ProceduralClips.TemporalArrival();
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
            foreach (var target in new List<Health>(targets.Keys))
                Untrack(target);
            targets.Clear();
            switches.Clear();
            switchHeld.Clear();
            boss = null;
            section = null;
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
        }

        void Update()
        {
            UpdateRecordingPulse();
            UpdateBoss();
            UpdateSwitches();
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
                TargetKind.Core => SoundCue.CoreDeath,
                TargetKind.Generator => SoundCue.GeneratorDeath,
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

        // ---------------------------------------------------------------- stage, boss, traps

        void OnStageChanged()
        {
            if (stage == null)
                return;

            if (music != null)
            {
                switch (stage.Segment)
                {
                    case StageSegment.Waves: music.Play(MusicTrack.Waves); break;
                    case StageSegment.Traps: music.Play(MusicTrack.Traps); break;
                    case StageSegment.Boss: music.Play(MusicTrack.Boss); break;
                    default: music.Stop(); break;
                }
            }

            if (stage.Boss != null && stage.Boss != boss)
            {
                boss = stage.Boss;
                bossVulnerable = boss.CoreVulnerable;
                bossPhase = (int)boss.Phase;
                Track(boss.CoreHealth, TargetKind.Core);
                foreach (Health generator in boss.Generators)
                    Track(generator, TargetKind.Generator);
            }

            if (stage.Section != null && stage.Section != section)
            {
                section = stage.Section;
                sectionCleared = false;
                switches.Clear();
                switchHeld.Clear();
                foreach (SwitchPad pad in section.GetComponentsInChildren<SwitchPad>(true))
                {
                    switches.Add(pad);
                    switchHeld.Add(false);
                }
            }

            if (section != null && !sectionCleared && section.State == SectionState.Cleared)
            {
                sectionCleared = true;
                Play(SoundCue.SectionCleared, ShipPosition());
            }
        }

        void UpdateBoss()
        {
            if (boss == null)
                return;

            bool vulnerable = boss.CoreVulnerable;
            if (vulnerable && !bossVulnerable)
                Play(SoundCue.BossVulnerable, boss.CorePosition);
            bossVulnerable = vulnerable;

            int phase = (int)boss.Phase;
            if (phase > bossPhase)
                Play(SoundCue.BossPhase, boss.CorePosition);
            bossPhase = phase;
        }

        void UpdateSwitches()
        {
            for (int i = 0; i < switches.Count; i++)
            {
                SwitchPad pad = switches[i];
                if (pad == null)
                    continue;
                bool held = pad.IsHeld;
                if (held != switchHeld[i])
                    Play(held ? SoundCue.SwitchOn : SoundCue.SwitchOff, pad.transform.position);
                switchHeld[i] = held;
            }
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
    }
}
