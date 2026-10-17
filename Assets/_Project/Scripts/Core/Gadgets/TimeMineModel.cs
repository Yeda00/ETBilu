using System;

namespace OneMoreTime.Core.Gadgets
{
    /// <summary>
    /// The time mine's fuse (GDD 05): harmless for a moment after it is dropped, then it goes off on the
    /// first target in range — or by itself when the fuse runs out. It goes off exactly once.
    /// </summary>
    public sealed class TimeMineModel
    {
        readonly float armSeconds;
        readonly float fuseSeconds;

        public TimeMineModel(float armSeconds, float fuseSeconds)
        {
            this.armSeconds = Math.Max(0f, armSeconds);
            this.fuseSeconds = Math.Max(0f, fuseSeconds);
        }

        public float Age { get; private set; }
        public bool IsArmed => Age >= armSeconds;
        public bool HasExploded { get; private set; }

        /// <summary>Advance the fuse. Returns true on the one step the mine goes off.</summary>
        public bool Tick(float deltaTime, bool targetInRange)
        {
            if (HasExploded)
                return false;
            Age += Math.Max(0f, deltaTime);
            if ((IsArmed && targetInRange) || Age >= fuseSeconds)
            {
                HasExploded = true;
                return true;
            }
            return false;
        }
    }
}
