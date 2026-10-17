using System;
using System.Collections.Generic;

namespace OneMoreTime.Core.Waves
{
    /// <summary>
    /// Walks a list of instants (seconds, ascending) and reports which became due since the last call.
    /// Used for scripted waves: enemies never spawn at random (GDD 04).
    /// </summary>
    public sealed class TimelineCursor
    {
        readonly IReadOnlyList<float> times;
        int next;

        public TimelineCursor(IReadOnlyList<float> times)
        {
            this.times = times ?? Array.Empty<float>();
            for (int i = 1; i < this.times.Count; i++)
                if (this.times[i] < this.times[i - 1])
                    throw new ArgumentException("Times must be in ascending order.", nameof(times));
        }

        public bool IsDone => next >= times.Count;

        /// <summary>Append to <paramref name="due"/> the index of every entry whose time is &lt;= <paramref name="now"/>.</summary>
        public void Poll(float now, List<int> due)
        {
            while (next < times.Count && times[next] <= now)
            {
                due.Add(next);
                next++;
            }
        }
    }
}
