using System;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.Traps;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Session
{
    /// <summary>The encounters of the arena, in order (GDD 06, estrutura das runs).</summary>
    public enum StageSegment
    {
        Waves = 0,
        Traps = 1,
        Boss = 2,
        Done = 3,
    }

    /// <summary>
    /// Runs the arena's encounters in order — the scripted waves, the trap corridor, the Guardian of the
    /// Cycle — and reports victory when the Guardian falls. After a death the scene reloads at the
    /// encounter where it happened (<see cref="RunCheckpoint"/>), not at the first wave (GDD 06, model A).
    /// </summary>
    public sealed class StageDirector : MonoBehaviour
    {
        [SerializeField] Playfield playfield;
        [SerializeField] WaveSpawner waves;
        [SerializeField] Transform ship;
        [SerializeField] EchoAbility echoes;
        [SerializeField] ProjectilePool pool;
        [Tooltip("Parent for what the stage spawns (the World group).")]
        [SerializeField] Transform world;
        [SerializeField] TrapSection trapSection;
        [SerializeField] GuardianBoss boss;
        [Tooltip("Encounter the scene starts at. Set to Traps or Boss to test them straight away.")]
        [SerializeField] StageSegment startAt = StageSegment.Waves;
        [Tooltip("Gap between the right edge and the trap section's first trap when it spawns.")]
        [SerializeField, Min(0f)] float sectionSpawnMargin = 1f;
        [Tooltip("Scroll speed while the cleared trap corridor leaves the screen.")]
        [SerializeField, Min(0f)] float exitScrollSpeed = 6f;

        public event Action Changed;
        public event Action Completed;
        public event Action<int> Scored;

        /// <summary>False until the first encounter begins.</summary>
        public bool HasBegun { get; private set; }
        public StageSegment Segment { get; private set; }
        public TrapSection Section { get; private set; }
        public GuardianBoss Boss { get; private set; }

        void Start()
        {
            if (playfield == null)
            {
                Debug.LogWarning($"{nameof(StageDirector)} needs a playfield.", this);
                enabled = false;
                return;
            }
            Begin(RunCheckpoint.Resume(startAt));
        }

        void OnDisable()
        {
            if (waves != null)
                waves.Cleared -= OnWavesCleared;
        }

        void Begin(StageSegment segment)
        {
            HasBegun = true;
            Segment = segment;
            if (segment < StageSegment.Done)
                RunCheckpoint.Save(segment);

            switch (segment)
            {
                case StageSegment.Waves:
                    if (waves == null)
                    {
                        Begin(StageSegment.Traps);
                        return;
                    }
                    waves.Cleared += OnWavesCleared;
                    waves.Play();
                    break;
                case StageSegment.Traps:
                    if (trapSection == null)
                    {
                        Begin(StageSegment.Boss);
                        return;
                    }
                    SpawnSection();
                    break;
                case StageSegment.Boss:
                    SpawnBoss();
                    break;
            }
            Changed?.Invoke();
            if (segment == StageSegment.Done)
                Completed?.Invoke();
        }

        void Update()
        {
            // The cleared corridor scrolls out fast; the Guardian comes once it is off the screen.
            if (Segment == StageSegment.Traps && Section != null && Section.State == SectionState.Cleared)
            {
                var rightmost = Section.transform.position + Vector3.right * Section.ExtentX.y;
                if (playfield.IsPastLeftEdge(rightmost, 0.5f))
                {
                    Destroy(Section.gameObject);
                    Section = null;
                    Begin(StageSegment.Boss);
                }
            }
        }

        void OnWavesCleared()
        {
            waves.Cleared -= OnWavesCleared;
            if (Segment == StageSegment.Waves)
                Begin(StageSegment.Traps);
        }

        void SpawnSection()
        {
            Section = Instantiate(trapSection, world);
            Section.Begin(playfield, ship, echoes);
            // Place it so its first trap starts just past the right edge.
            float x = playfield.HalfExtents.x + sectionSpawnMargin - Section.ExtentX.x;
            Section.transform.position = playfield.LocalToWorld(new Vector2(x, 0f));
            Section.StateChanged += OnSectionStateChanged;
        }

        void OnSectionStateChanged(TrapSection section)
        {
            if (section.State == SectionState.Cleared)
                playfield.ScrollSpeed = exitScrollSpeed;
            Changed?.Invoke();
        }

        void SpawnBoss()
        {
            playfield.ScrollSpeed = 0f;
            if (boss == null)
            {
                Debug.LogWarning($"{nameof(StageDirector)} has no boss prefab.", this);
                return;
            }
            Boss = Instantiate(boss, world);
            Boss.Begin(playfield, ship, pool);
            Boss.Scored += OnScored;
            Boss.Defeated += OnBossDefeated;
        }

        void OnScored(int points) => Scored?.Invoke(points);

        void OnBossDefeated() => Begin(StageSegment.Done);
    }
}
