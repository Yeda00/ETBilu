using System;
using OneMoreTime.Core.Ship;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Ship
{
    /// <summary>
    /// Eight-direction movement on the playfield plane, clamped to the visible area (GDD 03).
    /// The ship moves in the playfield's local space, so it keeps pace with the scroll.
    /// The dodge (<see cref="DodgeModel"/>) is a burst of speed with a barrel roll and no damage while it lasts.
    /// </summary>
    [DefaultExecutionOrder(0)]
    public sealed class ShipMotor : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Playfield playfield;
        [SerializeField, Min(0f)] float speed = 9f;
        [Tooltip("Distance kept from the screen edges, in units.")]
        [SerializeField] Vector2 edgePadding = new(1f, 0.8f);
        [Tooltip("Visual child that banks with vertical movement — 3D feel on a 2D plane.")]
        [SerializeField] Transform visual;
        [SerializeField] float bankAngle = 25f;
        [SerializeField, Min(0f)] float bankSharpness = 10f;

        [Header("Dodge (GDD 03) — starting values")]
        [Tooltip("Seconds of the roll; the ship takes no damage meanwhile. The echo's return ends with the same spin.")]
        [SerializeField, Min(0.1f)] float dodgeSeconds = 0.4f;
        [Tooltip("Units covered by the burst.")]
        [SerializeField, Min(0f)] float dodgeDistance = 3.6f;
        [SerializeField, Min(0f)] float dodgeCooldown = 0.8f;
        [Tooltip("Share of the stick still steering during the roll.")]
        [SerializeField, Range(0f, 1f)] float dodgeSteer = 0.3f;

        DodgeModel dodge;
        Health health;
        float bank;
        float rollClock;
        float rollDuration;

        /// <summary>The dodge began at this local position and direction (zero direction: in place). The echo records it.</summary>
        public event Action<Vector2, Vector2> Dodged;

        /// <summary>Position on the playfield plane (local units).</summary>
        public Vector2 LocalPosition => transform.localPosition;
        /// <summary>Current bank, recorded so the echo leans the same way.</summary>
        public float Bank => bank;
        /// <summary>Barrel roll of the visual around its nose, in degrees; 0 when not rolling.</summary>
        public float Roll => rollDuration > 0f ? DodgeModel.RollAngle(rollClock / rollDuration) : 0f;
        public float DodgeSeconds => dodgeSeconds;
        public bool IsDodging => dodge != null && dodge.IsActive;
        public bool DodgeReady => dodge == null || dodge.IsReady;
        public float DodgeCooldownNormalized => dodge?.CooldownNormalized ?? 0f;
        public PlayerInputReader Input => input;
        public Playfield Playfield => playfield;
        public Transform Visual => visual;

        public void Configure(PlayerInputReader reader, Playfield field)
        {
            input = reader;
            playfield = field;
        }

        /// <summary>Spins the visual once over <paramref name="seconds"/> without moving the ship (the echo's return).</summary>
        public void PlayRoll(float seconds)
        {
            rollClock = 0f;
            rollDuration = Mathf.Max(0.05f, seconds);
        }

        void Awake()
        {
            dodge = new DodgeModel(dodgeSeconds, dodgeDistance, dodgeCooldown);
            health = GetComponent<Health>();
        }

        void Update()
        {
            if (input == null || playfield == null)
                return;

            float dt = Time.deltaTime;
            var move = input.Move;
            TryDodge(move);

            Vector2 step = dodge.IsActive
                ? move * (speed * dodgeSteer * dt) + dodge.Tick(dt)
                : move * (speed * dt);
            if (!dodge.IsActive)
                dodge.Tick(dt);   // runs the cooldown down

            var p = (Vector2)transform.localPosition + step;
            var limit = playfield.HalfExtents - edgePadding;
            p.x = Mathf.Clamp(p.x, -limit.x, limit.x);
            p.y = Mathf.Clamp(p.y, -limit.y, limit.y);
            transform.localPosition = new Vector3(p.x, p.y, 0f);

            bank = Mathf.Lerp(bank, -move.y * bankAngle, 1f - Mathf.Exp(-bankSharpness * dt));

            if (rollDuration > 0f)
            {
                rollClock += dt;
                if (rollClock >= rollDuration)
                    rollDuration = 0f;
            }
        }

        void TryDodge(Vector2 move)
        {
            if (!input.DodgePressed || !dodge.TryBegin(move))
                return;
            health?.GrantInvulnerability(dodgeSeconds);
            PlayRoll(dodgeSeconds);
            Dodged?.Invoke(LocalPosition, dodge.Direction);
        }
    }
}
