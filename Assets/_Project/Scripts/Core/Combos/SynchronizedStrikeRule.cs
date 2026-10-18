using System;
using System.Collections.Generic;

namespace OneMoreTime.Core.Combos
{
    /// <summary>
    /// Synchronized Strike (GDD 03): two different weak points are broken almost at the same time, one by
    /// the ship and the other by an echo. In the prototype the weak points are the Guardian's two
    /// generators. Only the latest break of each point counts, and a match spends both breaks.
    /// </summary>
    public sealed class SynchronizedStrikeRule
    {
        readonly struct Break
        {
            public readonly int Point;
            public readonly Attacker By;
            public readonly float Time;

            public Break(int point, Attacker by, float time)
            {
                Point = point;
                By = by;
                Time = time;
            }
        }

        readonly List<Break> breaks = new();

        public SynchronizedStrikeRule(float windowSeconds)
        {
            Window = Math.Max(0f, windowSeconds);
        }

        /// <summary>Most seconds between the two breaks.</summary>
        public float Window { get; }

        /// <summary>
        /// Weak point <paramref name="point"/> was broken by <paramref name="by"/> at <paramref name="now"/>.
        /// Returns true when this break completes a Synchronized Strike.
        /// </summary>
        public bool Broken(int point, Attacker by, float now)
        {
            for (int i = breaks.Count - 1; i >= 0; i--)
                if (breaks[i].Point == point || now - breaks[i].Time > Window)
                    breaks.RemoveAt(i);

            for (int i = 0; i < breaks.Count; i++)
            {
                if (breaks[i].By == by)
                    continue;
                breaks.RemoveAt(i);
                return true;
            }

            breaks.Add(new Break(point, by, now));
            return false;
        }

        public void Reset() => breaks.Clear();
    }
}
