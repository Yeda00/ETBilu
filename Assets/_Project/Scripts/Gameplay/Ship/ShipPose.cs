using UnityEngine;

namespace OneMoreTime.Gameplay.Ship
{
    /// <summary>The rotation of a ship's visual, shared by the ship and its echo so the two lean and roll the same way.</summary>
    public static class ShipPose
    {
        /// <summary>
        /// Tilts by <paramref name="bank"/>, turns the nose to <paramref name="aimAngle"/> and spins a barrel roll of
        /// <paramref name="roll"/> degrees around the nose (the dodge, GDD 03).
        /// </summary>
        public static Quaternion Rotation(float bank, float aimAngle, float roll)
        {
            Quaternion pose = Quaternion.Euler(bank, 0f, aimAngle);
            if (Mathf.Abs(roll) < 0.01f)
                return pose;
            float a = aimAngle * Mathf.Deg2Rad;
            return Quaternion.AngleAxis(roll, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f)) * pose;
        }
    }
}
