using System;

namespace OneMoreTime.Core.Gadgets
{
    /// <summary>A recharge timer advanced by the game clock, so pausing also pauses it.</summary>
    public sealed class Cooldown
    {
        public Cooldown(float seconds)
        {
            Seconds = Math.Max(0f, seconds);
        }

        public float Seconds { get; }
        public float Remaining { get; private set; }
        public bool IsReady => Remaining <= 0f;
        /// <summary>1 right after use, 0 when ready.</summary>
        public float Normalized => Seconds > 0f ? Remaining / Seconds : 0f;

        public void Tick(float deltaTime)
        {
            if (deltaTime > 0f)
                Remaining = Math.Max(0f, Remaining - deltaTime);
        }

        /// <summary>Start the recharge if ready. Returns false (and changes nothing) otherwise.</summary>
        public bool TryUse()
        {
            if (!IsReady)
                return false;
            Remaining = Seconds;
            return true;
        }
    }
}
