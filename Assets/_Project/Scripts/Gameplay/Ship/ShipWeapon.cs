using System;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Ship
{
    /// <summary>Continuous main fire while the button is held (GDD 03). Reports each shot for recording.</summary>
    [DefaultExecutionOrder(10)]
    public sealed class ShipWeapon : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Playfield playfield;
        [SerializeField] ProjectilePool pool;
        [SerializeField] Projectile projectile;
        [SerializeField] ShipAim aim;
        [SerializeField, Min(0.1f)] float shotsPerSecond = 10f;
        [Tooltip("Where shots leave the ship, in local units.")]
        [SerializeField] Vector2 muzzle = new(1.1f, 0f);

        float cooldown;

        /// <summary>Raised per shot with the playfield-local origin and direction.</summary>
        public event Action<Vector2, Vector2> Fired;
        public Vector2 AimDirection => aim != null ? aim.Direction : Vector2.right;

        public void Configure(PlayerInputReader reader, Playfield field, ProjectilePool projectiles)
        {
            input = reader;
            playfield = field;
            pool = projectiles;
        }

        void Update()
        {
            cooldown -= Time.deltaTime;
            if (input == null || pool == null || playfield == null || !input.FireHeld || cooldown > 0f)
                return;

            cooldown = 1f / shotsPerSecond;
            var dir = AimDirection;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var origin = (Vector2)transform.localPosition + (Vector2)(Quaternion.Euler(0f, 0f, angle) * muzzle);
            pool.Spawn(projectile, playfield.LocalToWorld(origin), playfield.DirectionToWorld(dir), Faction.Player, ShotSource.Ship);
            aim?.Pulse();
            Fired?.Invoke(origin, dir);
        }
    }
}
