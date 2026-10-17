using System;

namespace OneMoreTime.Core.Traps
{
    /// <summary>
    /// A switch held by presence: pressed while the ship or an echo sits on it (GDD 02, dispositivos de
    /// presença contínua). Passing near is not enough — only being over the pad counts. A short release
    /// delay keeps it from flickering when an occupant brushes its edge.
    /// </summary>
    public sealed class PresenceSwitch
    {
        readonly float releaseDelay;
        float emptyFor;

        public PresenceSwitch(float releaseDelay)
        {
            this.releaseDelay = Math.Max(0f, releaseDelay);
        }

        public bool IsHeld { get; private set; }

        /// <summary>Advance by <paramref name="deltaTime"/> with the occupants now on the pad. True when <see cref="IsHeld"/> changed.</summary>
        public bool Update(int occupants, float deltaTime)
        {
            bool was = IsHeld;
            if (occupants > 0)
            {
                emptyFor = 0f;
                IsHeld = true;
            }
            else if (IsHeld)
            {
                emptyFor += Math.Max(0f, deltaTime);
                if (emptyFor >= releaseDelay)
                    IsHeld = false;
            }
            return IsHeld != was;
        }
    }
}
