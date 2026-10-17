using System;
using OneMoreTime.Core.Gadgets;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Gadgets;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Ship
{
    /// <summary>
    /// The one gadget the ship carries (GDD 05): Gadget drops a time mine where the ship is. Reports
    /// each use so the echo can record it and drop the same mine on replay.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class ShipGadget : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Playfield playfield;
        [Tooltip("Parent for dropped mines; they stay in the world, not on the scrolling screen.")]
        [SerializeField] ProjectilePool pool;
        [SerializeField] TimeMine mine;
        [SerializeField, Min(0f)] float cooldownSeconds = 1.5f;
        [Tooltip("Where the mine is dropped, in local units from the ship.")]
        [SerializeField] Vector2 dropOffset = new(-0.3f, 0f);

        Cooldown cooldown;

        /// <summary>Raised per use with the playfield-local drop point and the ship's facing.</summary>
        public event Action<Vector2, Vector2> Used;

        public bool IsReady => Cooldown.IsReady;
        /// <summary>1 right after a drop, 0 when ready.</summary>
        public float CooldownNormalized => Cooldown.Normalized;
        public float CooldownRemaining => Cooldown.Remaining;

        Cooldown Cooldown => cooldown ??= new Cooldown(cooldownSeconds);

        public void Configure(PlayerInputReader reader, Playfield field, ProjectilePool projectiles)
        {
            input = reader;
            playfield = field;
            pool = projectiles;
        }

        void Update()
        {
            Cooldown.Tick(Time.deltaTime);
            if (input == null || playfield == null || mine == null || !input.GadgetPressed || !Cooldown.TryUse())
                return;

            var origin = (Vector2)transform.localPosition + dropOffset;
            var dropped = Instantiate(mine, playfield.LocalToWorld(origin), Quaternion.identity, pool != null ? pool.transform : null);
            dropped.Arm(Faction.Player, ShotSource.Ship);
            Used?.Invoke(origin, Vector2.right);
        }

        void OnValidate() => cooldown = null;
    }
}
