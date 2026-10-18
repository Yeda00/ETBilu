using System;
using System.Collections;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Session
{
    /// <summary>The encounters of the arena, in order (GDD 06, estrutura das runs).</summary>
    public enum StageSegment
    {
        Waves = 0,
        Boss = 1,
        Done = 2,
    }

    /// <summary>
    /// Runs the arena's encounters in order — the scripted waves, then the Guardian of the Cycle — and reports
    /// victory when the Guardian falls. After a death the scene reloads at the encounter where it happened
    /// (<see cref="RunCheckpoint"/>), not at the first wave (GDD 06, model A).
    /// </summary>
    public sealed class StageDirector : MonoBehaviour
    {
        [SerializeField] Playfield playfield;
        [SerializeField] WaveSpawner waves;
        [SerializeField] Transform ship;
        [SerializeField] ProjectilePool pool;
        [Tooltip("Parent for what the stage spawns (the World group).")]
        [SerializeField] Transform world;
        [SerializeField] GuardianBoss boss;
        [Tooltip("Encounter the scene starts at. Set to Boss to test the Guardian straight away.")]
        [SerializeField] StageSegment startAt = StageSegment.Waves;
        [Tooltip("Held until it opens before the first encounter starts (the mission card). Optional.")]
        [SerializeField] StageGate gate;
        [Tooltip("Breather between the last wave and the Guardian. Starting value.")]
        [SerializeField, Min(0f)] float bossDelaySeconds = 2f;

        public event Action Changed;
        public event Action Completed;
        public event Action<int> Scored;

        /// <summary>False while the stage waits for its gate.</summary>
        public bool HasBegun { get; private set; }
        public StageSegment Segment { get; private set; }
        public GuardianBoss Boss { get; private set; }

        IEnumerator Start()
        {
            if (playfield == null)
            {
                Debug.LogWarning($"{nameof(StageDirector)} needs a playfield.", this);
                enabled = false;
                yield break;
            }
            if (gate != null)
                while (!gate.IsOpen)
                    yield return null;
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
                        Begin(StageSegment.Boss);
                        return;
                    }
                    waves.Cleared += OnWavesCleared;
                    waves.Play();
                    break;
                case StageSegment.Boss:
                    SpawnBoss();
                    break;
            }
            Changed?.Invoke();
            if (segment == StageSegment.Done)
                Completed?.Invoke();
        }

        void OnWavesCleared()
        {
            waves.Cleared -= OnWavesCleared;
            if (Segment == StageSegment.Waves)
                StartCoroutine(BossAfterBreather());
        }

        IEnumerator BossAfterBreather()
        {
            yield return new WaitForSeconds(bossDelaySeconds);
            if (Segment == StageSegment.Waves)
                Begin(StageSegment.Boss);
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
