using System.Collections.Generic;
using OneMoreTime.Core.Enemies;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Enemies
{
    /// <summary>
    /// An enemy's gun (GDD 04): bursts aimed at the ship (or straight ahead), fanned out when a burst has
    /// several shots, with a blinking telegraph just before each burst. Timing lives in
    /// <see cref="FireCycle"/>; the <see cref="Enemy"/> arms it when its path allows. It only fires while
    /// its muzzle is on screen, so nothing shoots from outside the player's view.
    /// </summary>
    public sealed class EnemyGun : MonoBehaviour
    {
        [SerializeField] Projectile shot;
        [SerializeField] Transform muzzle;
        [Tooltip("Shown during the windup before each burst. Optional.")]
        [SerializeField] GameObject telegraph;
        [SerializeField] bool aimAtShip = true;
        [Tooltip("Degrees between neighbouring shots of a burst; 0 keeps them on one line.")]
        [SerializeField, Min(0f)] float spread;

        [Header("Timing (GDD 04) — starting values")]
        [SerializeField, Min(0f)] float firstDelay = 0.6f;
        [SerializeField, Min(0.05f)] float interval = 1.8f;
        [SerializeField, Min(1)] int shotsPerBurst = 1;
        [SerializeField, Min(0f)] float shotGap;
        [SerializeField, Min(0f)] float windup = 0.4f;
        [Tooltip("0 = no limit.")]
        [SerializeField, Min(0)] int maxBursts;

        readonly List<int> due = new();
        FireCycle cycle;
        Playfield playfield;
        Transform target;
        ProjectilePool pool;

        void Awake()
        {
            cycle = new FireCycle(firstDelay, interval, shotsPerBurst, shotGap, windup, maxBursts);
            if (telegraph != null)
                telegraph.SetActive(false);
        }

        public void Configure(Playfield field, Transform ship, ProjectilePool projectiles)
        {
            playfield = field;
            target = ship;
            pool = projectiles;
        }

        public void Arm() => cycle.Arm();
        public void Disarm() => cycle.Disarm();

        void Update()
        {
            if (playfield == null || muzzle == null)
                return;
            bool visible = OnScreen();
            if (telegraph != null)
            {
                bool show = visible && cycle.WindingUp;
                if (telegraph.activeSelf != show)
                    telegraph.SetActive(show);
            }

            due.Clear();
            cycle.Tick(Time.deltaTime, due);
            if (!visible || pool == null || shot == null)
                return;
            foreach (int index in due)
                Fire(index);
        }

        void Fire(int index)
        {
            Vector3 aim = Vector3.left;
            if (aimAtShip && target != null && target.gameObject.activeInHierarchy)
            {
                aim = target.position - muzzle.position;
                aim.z = 0f;
                if (aim.sqrMagnitude < 1e-4f)
                    aim = Vector3.left;
            }
            float angle = (index - (cycle.ShotsPerBurst - 1) * 0.5f) * spread;
            pool.Spawn(shot, muzzle.position, Quaternion.Euler(0f, 0f, angle) * aim.normalized, Faction.Enemy, ShotSource.Enemy);
        }

        bool OnScreen()
        {
            var p = playfield.WorldToLocal(muzzle.position);
            var h = playfield.HalfExtents;
            return Mathf.Abs(p.x) < h.x - 0.3f && Mathf.Abs(p.y) < h.y - 0.2f;
        }
    }
}
