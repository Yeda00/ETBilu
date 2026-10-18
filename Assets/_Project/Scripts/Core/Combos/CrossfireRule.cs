using System;
using System.Collections.Generic;

namespace OneMoreTime.Core.Combos
{
    /// <summary>
    /// Crossfire (GDD 03): the ship and an echo hit the same target within a short window. A match spends
    /// both hits, and that target cannot cross-fire again until the rearm time has passed — two streams of
    /// fire on one target give a combo every so often, not one per shot.
    /// </summary>
    public sealed class CrossfireRule
    {
        struct Marks
        {
            public float Ship, Echo, LockedUntil;
        }

        static readonly Marks Fresh = new() { Ship = float.NegativeInfinity, Echo = float.NegativeInfinity, LockedUntil = float.NegativeInfinity };

        readonly Dictionary<int, Marks> targets = new();

        public CrossfireRule(float windowSeconds, float rearmSeconds)
        {
            Window = Math.Max(0f, windowSeconds);
            Rearm = Math.Max(0f, rearmSeconds);
        }

        /// <summary>Most seconds between the ship's hit and the echo's hit.</summary>
        public float Window { get; }
        /// <summary>Seconds after a Crossfire before the same target can give another.</summary>
        public float Rearm { get; }
        public int TrackedTargets => targets.Count;

        /// <summary>
        /// <paramref name="target"/> was hit by <paramref name="by"/> at <paramref name="now"/>.
        /// Returns true when this hit completes a Crossfire.
        /// </summary>
        public bool Hit(int target, Attacker by, float now)
        {
            if (!targets.TryGetValue(target, out var m))
                m = Fresh;
            if (now < m.LockedUntil)
                return false;

            float other = by == Attacker.Ship ? m.Echo : m.Ship;
            if (now - other <= Window)
            {
                var spent = Fresh;
                spent.LockedUntil = now + Rearm;
                targets[target] = spent;
                return true;
            }

            if (by == Attacker.Ship)
                m.Ship = now;
            else
                m.Echo = now;
            targets[target] = m;
            return false;
        }

        /// <summary>Drop what is known about a target that died or left.</summary>
        public void Forget(int target) => targets.Remove(target);
    }
}
