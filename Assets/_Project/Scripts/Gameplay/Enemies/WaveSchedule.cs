using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneMoreTime.Gameplay.Enemies
{
    /// <summary>
    /// The scripted waves of one encounter: entry time, entry point, type and count (GDD 04).
    /// Times are starting values for testing.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Time/Wave Schedule", fileName = "WaveSchedule")]
    public sealed class WaveSchedule : ScriptableObject
    {
        [Serializable]
        public struct Wave
        {
            [Tooltip("Note for designers, e.g. \"3 drones em fila\".")]
            public string label;
            [Tooltip("Seconds from the start of the encounter.")]
            [Min(0f)] public float time;
            public Enemy enemy;
            public MovementPattern pattern;
            [Min(1)] public int count;
            [Tooltip("Entry height on screen: -1 bottom, 0 centre, 1 top.")]
            [Range(-1f, 1f)] public float height;
            [Tooltip("Distance between enemies of the wave, in units (along x).")]
            [Min(0f)] public float spacing;
            [Tooltip("Spawn a second row of the same wave (\"drones em dois níveis de altura\").")]
            public bool secondRow;
            [Range(-1f, 1f)] public float secondRowHeight;
        }

        [Tooltip("Seconds of on-screen warning before each wave enters (GDD 04).")]
        [Min(0f)] public float warningSeconds = 0.8f;
        [Tooltip("Seconds after the last wave before the encounter counts as cleared.")]
        [Min(0f)] public float clearDelay = 3f;
        public List<Wave> waves = new();

        void OnValidate() => waves.Sort((a, b) => a.time.CompareTo(b.time));
    }
}
