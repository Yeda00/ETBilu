using UnityEngine;

namespace OneMoreTime.Core.Enemies
{
    public enum TrackingLaserPhase
    {
        Tracking = 0,
        Charging = 1,
        Firing = 2,
        Recovering = 3,
        Exhausted = 4,
    }

    /// <summary>
    /// Readable rhythm for a horizontal enemy laser: follow the ship, lock the line while charging,
    /// fire briefly, then give the player a recovery window. Pure timing keeps it deterministic and testable.
    /// </summary>
    public sealed class TrackingLaserCycle
    {
        readonly float trackingSeconds, chargeSeconds, firingSeconds, recoverySeconds;
        readonly int maxBursts;
        float remaining;

        public TrackingLaserCycle(float trackingSeconds, float chargeSeconds, float firingSeconds,
            float recoverySeconds, int maxBursts)
        {
            this.trackingSeconds = Mathf.Max(0.05f, trackingSeconds);
            this.chargeSeconds = Mathf.Max(0.05f, chargeSeconds);
            this.firingSeconds = Mathf.Max(0.05f, firingSeconds);
            this.recoverySeconds = Mathf.Max(0.05f, recoverySeconds);
            this.maxBursts = Mathf.Max(1, maxBursts);
            Phase = TrackingLaserPhase.Tracking;
            remaining = this.trackingSeconds;
        }

        public TrackingLaserPhase Phase { get; private set; }
        public int BurstsFired { get; private set; }
        public bool LocksAim => Phase is TrackingLaserPhase.Charging or TrackingLaserPhase.Firing;
        public bool IsThreatening => Phase is TrackingLaserPhase.Charging or TrackingLaserPhase.Firing;
        public float PhaseProgress => Phase == TrackingLaserPhase.Exhausted
            ? 1f
            : 1f - Mathf.Clamp01(remaining / Duration(Phase));

        /// <summary>Advances the cycle and returns true when it enters a different phase.</summary>
        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f || Phase == TrackingLaserPhase.Exhausted)
                return false;

            remaining -= deltaTime;
            if (remaining > 0f)
                return false;

            float overflow = -remaining;
            Advance();
            if (Phase != TrackingLaserPhase.Exhausted)
                remaining = Mathf.Max(0.001f, Duration(Phase) - overflow);
            return true;
        }

        void Advance()
        {
            switch (Phase)
            {
                case TrackingLaserPhase.Tracking:
                    Phase = TrackingLaserPhase.Charging;
                    break;
                case TrackingLaserPhase.Charging:
                    Phase = TrackingLaserPhase.Firing;
                    BurstsFired++;
                    break;
                case TrackingLaserPhase.Firing:
                    Phase = TrackingLaserPhase.Recovering;
                    break;
                case TrackingLaserPhase.Recovering:
                    Phase = BurstsFired >= maxBursts ? TrackingLaserPhase.Exhausted : TrackingLaserPhase.Tracking;
                    break;
            }
        }

        float Duration(TrackingLaserPhase phase) => phase switch
        {
            TrackingLaserPhase.Tracking => trackingSeconds,
            TrackingLaserPhase.Charging => chargeSeconds,
            TrackingLaserPhase.Firing => firingSeconds,
            TrackingLaserPhase.Recovering => recoverySeconds,
            _ => 1f,
        };
    }
}
