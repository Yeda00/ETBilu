using System.Collections.Generic;
using UnityEngine;

namespace OneMoreTime.Core.Enemies
{
    /// <summary>
    /// When an enemy gun fires (GDD 04). Once armed it waits <c>firstDelay</c>, fires a burst of
    /// <c>shotsPerBurst</c> shots spaced <c>shotGap</c> apart, then waits <c>interval</c> before the next
    /// burst. The last <c>windup</c> seconds before each burst are the telegraph, so the player can read
    /// the attack coming. <c>maxBursts</c> 0 means no limit. Disarming freezes it where it was.
    /// </summary>
    public sealed class FireCycle
    {
        readonly float firstDelay, interval, shotGap, windup;
        readonly int shotsPerBurst, maxBursts;
        float untilNext;
        int nextShot;     // index within the burst of the next shot; 0 = the next shot opens a burst
        bool started;

        public FireCycle(float firstDelay, float interval, int shotsPerBurst, float shotGap, float windup, int maxBursts)
        {
            this.firstDelay = Mathf.Max(0f, firstDelay);
            this.interval = Mathf.Max(0.01f, interval);
            this.shotsPerBurst = Mathf.Max(1, shotsPerBurst);
            this.shotGap = Mathf.Max(0f, shotGap);
            this.windup = Mathf.Max(0f, windup);
            this.maxBursts = Mathf.Max(0, maxBursts);
        }

        public bool Armed { get; private set; }
        public int BurstsFired { get; private set; }
        public bool Exhausted => maxBursts > 0 && BurstsFired >= maxBursts;
        /// <summary>A burst is about to start: show the telegraph.</summary>
        public bool WindingUp => Armed && !Exhausted && nextShot == 0 && untilNext <= windup;
        public int ShotsPerBurst => shotsPerBurst;

        /// <summary>Start (or resume) the cycle. The first arming waits <c>firstDelay</c>.</summary>
        public void Arm()
        {
            if (Armed)
                return;
            Armed = true;
            if (!started)
            {
                started = true;
                untilNext = firstDelay;
            }
        }

        public void Disarm() => Armed = false;

        /// <summary>
        /// Advance by <paramref name="deltaTime"/> and add, for each shot due, its index within its burst
        /// (0 … shotsPerBurst − 1) to <paramref name="shots"/>. Returns how many shots are due.
        /// </summary>
        public int Tick(float deltaTime, List<int> shots)
        {
            if (!Armed || Exhausted || deltaTime <= 0f)
                return 0;
            untilNext -= deltaTime;
            int count = 0;
            while (untilNext <= 0f && !Exhausted)
            {
                shots?.Add(nextShot);
                count++;
                nextShot++;
                if (nextShot < shotsPerBurst)
                {
                    untilNext += shotGap;
                }
                else
                {
                    nextShot = 0;
                    BurstsFired++;
                    untilNext += interval;
                }
            }
            return count;
        }
    }
}
