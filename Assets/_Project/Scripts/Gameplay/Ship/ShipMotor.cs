using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Ship
{
    /// <summary>
    /// Eight-direction movement on the playfield plane, clamped to the visible area (GDD 03).
    /// The ship moves in the playfield's local space, so it keeps pace with the scroll.
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

        float bank;

        /// <summary>Position on the playfield plane (local units).</summary>
        public Vector2 LocalPosition => transform.localPosition;
        /// <summary>Current bank, recorded so the echo leans the same way.</summary>
        public float Bank => bank;
        public PlayerInputReader Input => input;
        public Playfield Playfield => playfield;
        public Transform Visual => visual;

        public void Configure(PlayerInputReader reader, Playfield field)
        {
            input = reader;
            playfield = field;
        }

        void Update()
        {
            if (input == null || playfield == null)
                return;

            var move = input.Move;
            var p = (Vector2)transform.localPosition + move * (speed * Time.deltaTime);
            var limit = playfield.HalfExtents - edgePadding;
            p.x = Mathf.Clamp(p.x, -limit.x, limit.x);
            p.y = Mathf.Clamp(p.y, -limit.y, limit.y);
            transform.localPosition = new Vector3(p.x, p.y, 0f);

            bank = Mathf.Lerp(bank, -move.y * bankAngle, 1f - Mathf.Exp(-bankSharpness * Time.deltaTime));
        }
    }
}
