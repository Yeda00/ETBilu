using System;
using System.Collections.Generic;
using OneMoreTime.Core.Waves;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Enemies
{
    /// <summary>
    /// Plays a <see cref="WaveSchedule"/>: shows a short warning at the right edge, then spawns the
    /// wave just outside it. Killing is never required — the scroll goes on (GDD 04).
    /// Waits for <see cref="Play"/>: the stage decides when the encounter starts.
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        [SerializeField] WaveSchedule schedule;
        [SerializeField] Playfield playfield;
        [Tooltip("What the enemies aim at.")]
        [SerializeField] Transform ship;
        [Tooltip("Where enemy shots come from.")]
        [SerializeField] ProjectilePool pool;
        [Tooltip("Marker shown at the right edge before a wave enters. Optional.")]
        [SerializeField] GameObject warningMarker;
        [SerializeField, Min(0f)] float spawnMarginX = 1.5f;

        readonly List<int> due = new();
        readonly List<Enemy> alive = new();
        TimelineCursor warnings, spawns;
        float clock;
        bool playing;
        bool cleared;

        public event Action<Enemy> EnemySpawned;
        public event Action<Enemy, bool> EnemyRemoved;
        /// <summary>A bomber went off: which one, and the radius of the blast.</summary>
        public event Action<Enemy, float> EnemyDetonated;
        public event Action<Enemy> EnemyLaserTelegraphed;
        public event Action<Enemy> EnemyLaserFired;
        public event Action Cleared;

        public float Clock => clock;
        public int AliveCount => alive.Count;
        public bool IsPlaying => playing;

        /// <summary>Start the schedule from its first wave.</summary>
        public void Play() => playing = true;

        void Start()
        {
            if (schedule == null || playfield == null)
            {
                Debug.LogWarning($"{nameof(WaveSpawner)} needs a schedule and a playfield.", this);
                enabled = false;
                return;
            }

            var times = new List<float>(schedule.waves.Count);
            var warnTimes = new List<float>(schedule.waves.Count);
            foreach (var w in schedule.waves)
            {
                times.Add(w.time);
                warnTimes.Add(Mathf.Max(0f, w.time - schedule.warningSeconds));
            }
            spawns = new TimelineCursor(times);
            warnings = new TimelineCursor(warnTimes);
        }

        void Update()
        {
            if (!playing)
                return;
            clock += Time.deltaTime;

            due.Clear();
            warnings.Poll(clock, due);
            foreach (var i in due)
                ShowWarning(schedule.waves[i]);

            due.Clear();
            spawns.Poll(clock, due);
            foreach (var i in due)
                SpawnWave(schedule.waves[i]);

            if (!cleared && spawns.IsDone && alive.Count == 0 && clock >= LastWaveTime + schedule.clearDelay)
            {
                cleared = true;
                Cleared?.Invoke();
            }
        }

        float LastWaveTime => schedule.waves.Count > 0 ? schedule.waves[schedule.waves.Count - 1].time : 0f;

        void ShowWarning(in WaveSchedule.Wave wave)
        {
            if (warningMarker == null || schedule.warningSeconds <= 0f)
                return;
            var h = playfield.HalfExtents;
            foreach (var height in Rows(wave))
            {
                var marker = Instantiate(warningMarker, playfield.transform);
                marker.transform.localPosition = new Vector3(h.x - 0.6f, height * (h.y - 1f), 0f);
                Destroy(marker, schedule.warningSeconds);
            }
        }

        void SpawnWave(in WaveSchedule.Wave wave)
        {
            if (wave.enemy == null)
                return;
            var h = playfield.HalfExtents;
            int row = 0;
            foreach (var height in Rows(wave))
            {
                for (int n = 0; n < wave.count; n++)
                {
                    var local = new Vector2(h.x + spawnMarginX + n * wave.spacing, height * (h.y - 1f));
                    var enemy = Instantiate(wave.enemy, playfield.LocalToWorld(local), Quaternion.identity, transform);
                    enemy.Spawn(playfield, ship, pool, n * wave.spacing, n * 0.9f + row * 1.7f, wave.variant - 1);
                    Register(enemy);
                }
                row++;
            }
        }

        /// <summary>
        /// Brings in one enemy outside the schedule — a reinforcement the Guardian called (GDD 06) — and treats it like any
        /// other: it is counted, scored and shown by the same systems.
        /// </summary>
        /// <param name="height">Entry height on screen: −1 bottom, 0 centre, 1 top.</param>
        /// <param name="variant">The model it takes (0-based); negative leaves the random pick.</param>
        /// <param name="queueOffset">Delay in units behind the right edge; a hovering enemy also shifts its column by it.</param>
        public Enemy SpawnExtra(Enemy prefab, float height, int variant, float queueOffset, float phase)
        {
            if (prefab == null || playfield == null)
                return null;
            var h = playfield.HalfExtents;
            var local = new Vector2(h.x + spawnMarginX + Mathf.Max(0f, queueOffset), height * (h.y - 1f));
            var enemy = Instantiate(prefab, playfield.LocalToWorld(local), Quaternion.identity, transform);
            enemy.Spawn(playfield, ship, pool, queueOffset, phase, variant);
            Register(enemy);
            return enemy;
        }

        void Register(Enemy enemy)
        {
            enemy.Removed += OnEnemyRemoved;
            enemy.Detonated += OnEnemyDetonated;
            enemy.LaserTelegraphed += OnEnemyLaserTelegraphed;
            enemy.LaserFired += OnEnemyLaserFired;
            alive.Add(enemy);
            EnemySpawned?.Invoke(enemy);
        }

        static IEnumerable<float> Rows(WaveSchedule.Wave wave)
        {
            yield return wave.height;
            if (wave.secondRow)
                yield return wave.secondRowHeight;
        }

        void OnEnemyDetonated(Enemy enemy, float radius) => EnemyDetonated?.Invoke(enemy, radius);
        void OnEnemyLaserTelegraphed(Enemy enemy) => EnemyLaserTelegraphed?.Invoke(enemy);
        void OnEnemyLaserFired(Enemy enemy) => EnemyLaserFired?.Invoke(enemy);

        void OnEnemyRemoved(Enemy enemy, bool killed)
        {
            enemy.Detonated -= OnEnemyDetonated;
            enemy.LaserTelegraphed -= OnEnemyLaserTelegraphed;
            enemy.LaserFired -= OnEnemyLaserFired;
            alive.Remove(enemy);
            EnemyRemoved?.Invoke(enemy, killed);
        }
    }
}
