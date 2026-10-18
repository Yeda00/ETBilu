using UnityEngine;

namespace OneMoreTime.Core.Ship
{
    /// <summary>
    /// The ship's dodge (GDD 03): a short burst of speed in the direction of the stick, with a barrel roll,
    /// during which the ship takes no damage. The burst starts fast and brakes, covering exactly
    /// <c>distance</c>; a cooldown starts when it ends. The roll curve is shared with the echo's return,
    /// which ends with the same spin (GDD 02).
    /// </summary>
    public sealed class DodgeModel
    {
        readonly float duration, distance, cooldown;
        float clock;
        float cooldownLeft;

        public DodgeModel(float duration, float distance, float cooldown)
        {
            this.duration = Mathf.Max(0.05f, duration);
            this.distance = Mathf.Max(0f, distance);
            this.cooldown = Mathf.Max(0f, cooldown);
        }

        public bool IsActive { get; private set; }
        public Vector2 Direction { get; private set; }
        public float Duration => duration;
        public float Progress => IsActive ? Mathf.Clamp01(clock / duration) : 0f;
        public float CooldownRemaining => cooldownLeft;
        public float CooldownNormalized => cooldown > 0f ? Mathf.Clamp01(cooldownLeft / cooldown) : 0f;
        public bool IsReady => !IsActive && cooldownLeft <= 0f;

        /// <summary>Starts a dodge toward <paramref name="direction"/>; with no direction the ship rolls in place. False while busy or cooling down.</summary>
        public bool TryBegin(Vector2 direction)
        {
            if (!IsReady)
                return false;
            IsActive = true;
            clock = 0f;
            Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector2.zero;
            return true;
        }

        /// <summary>Advances by <paramref name="deltaTime"/> and returns how far the dodge moved the ship in that step.</summary>
        public Vector2 Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return Vector2.zero;
            if (!IsActive)
            {
                cooldownLeft = Mathf.Max(0f, cooldownLeft - deltaTime);
                return Vector2.zero;
            }

            float before = Mathf.Clamp01(clock / duration);
            clock += deltaTime;
            float after = Mathf.Clamp01(clock / duration);
            if (clock >= duration)
            {
                IsActive = false;
                cooldownLeft = cooldown;
            }
            return Direction * (distance * (Burst(after) - Burst(before)));
        }

        /// <summary>Fraction of the distance covered at <paramref name="progress"/> (0–1): the speed starts high and falls linearly to zero.</summary>
        public static float Burst(float progress)
        {
            float p = Mathf.Clamp01(progress);
            return 2f * p - p * p;
        }

        /// <summary>Barrel roll angle in degrees at <paramref name="progress"/> (0–1): a full turn, eased at both ends.</summary>
        public static float RollAngle(float progress) => 360f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
    }
}
