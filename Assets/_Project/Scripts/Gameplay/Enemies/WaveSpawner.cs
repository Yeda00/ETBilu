using System;
using System.Collections.Generic;
using OneMoreTime.Core.Waves;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Enemies
{
    /// <summary>
    /// Plays a <see cref="WaveSchedule"/>: shows a short warning at the right edge, then spawns the
    /// wave just outside it. Killing is never required — the scroll goes on (GDD 04).
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        [SerializeField] WaveSchedule schedule;
        [SerializeField] Playfield playfield;
        [Tooltip("Marker shown at the right edge before a wave enters. Optional.")]
        [SerializeField] GameObject warningMarker;
        [SerializeField, Min(0f)] float spawnMarginX = 1.5f;

        readonly List<int> due = new();
        readonly List<Enemy> alive = new();
        TimelineCursor warnings, spawns;
        float clock;
        bool cleared;

        public event Action<Enemy> EnemySpawned;
        public event Action<Enemy, bool> EnemyRemoved;
        public event Action Cleared;

        public float Clock => clock;
        public int AliveCount => alive.Count;

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
                    enemy.Spawn(playfield, wave.pattern, n * 0.15f + row * 0.5f);
                    enemy.Removed += OnEnemyRemoved;
                    alive.Add(enemy);
                    EnemySpawned?.Invoke(enemy);
                }
                row++;
            }
        }

        static IEnumerable<float> Rows(WaveSchedule.Wave wave)
        {
            yield return wave.height;
            if (wave.secondRow)
                yield return wave.secondRowHeight;
        }

        void OnEnemyRemoved(Enemy enemy, bool killed)
        {
            alive.Remove(enemy);
            EnemyRemoved?.Invoke(enemy, killed);
        }
    }
}
