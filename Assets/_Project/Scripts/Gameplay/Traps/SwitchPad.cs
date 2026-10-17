using OneMoreTime.Core.Traps;
using UnityEngine;

namespace OneMoreTime.Gameplay.Traps
{
    /// <summary>
    /// A switch held by presence (GDD 02, 05): pressed while the ship or an echo is over the pad.
    /// Presence is a position check against the section's occupants, not physics — the echo has no
    /// collider and still holds it. Rules live in <see cref="PresenceSwitch"/>. The indicators light up
    /// cyan for the ship and violet for an echo, so the player sees who is holding it.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class SwitchPad : MonoBehaviour
    {
        [Tooltip("Half size of the pad on the gameplay plane.")]
        [SerializeField] Vector2 halfSize = new(0.9f, 0.7f);
        [Tooltip("Seconds it stays pressed after the last occupant leaves.")]
        [SerializeField, Min(0f)] float releaseDelay = 0.15f;
        [Tooltip("Renderers tinted with the switch state: the pad, its lamp, the conduit to the gate.")]
        [SerializeField] Renderer[] indicators;
        [SerializeField] Color idleColor = new(0.3f, 0.36f, 0.52f);
        [SerializeField] Color shipColor = new(0.21f, 0.89f, 1f);
        [SerializeField] Color echoColor = new(0.69f, 0.55f, 1f);

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        TrapSection section;
        PresenceSwitch model;
        MaterialPropertyBlock block;
        Color shown;

        public bool IsHeld => model != null && model.IsHeld;
        public bool HeldByEcho { get; private set; }
        public Vector2 HalfSize => halfSize;

        public void Bind(TrapSection owner) => section = owner;

        void Awake()
        {
            model = new PresenceSwitch(releaseDelay);
            Tint(idleColor);
        }

        void Update()
        {
            int occupants = 0;
            bool echo = false;
            if (section != null)
                occupants = section.CountOccupants(transform.position, halfSize, out echo);
            if (occupants > 0)
                HeldByEcho = echo;
            model.Update(occupants, Time.deltaTime);

            Tint(!IsHeld ? idleColor : HeldByEcho ? echoColor : shipColor);
        }

        void Tint(Color c)
        {
            if (indicators == null || c == shown)
                return;
            shown = c;
            block ??= new MaterialPropertyBlock();
            foreach (var r in indicators)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(block);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.69f, 0.55f, 1f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(halfSize.x * 2f, halfSize.y * 2f, 0.1f));
        }
    }
}
