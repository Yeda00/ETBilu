using OneMoreTime.Core.Traps;
using UnityEngine;

namespace OneMoreTime.Gameplay.Traps
{
    /// <summary>
    /// Two blocks that slide apart and back together in cycles, opening and closing a gap in the middle
    /// of the screen (GDD 05, barreiras móveis). Touching a block hurts the ship. Each block's pivot is
    /// its inner edge. Rules live in <see cref="ShutterCycle"/>.
    /// </summary>
    public sealed class MovingBarrier : MonoBehaviour
    {
        [SerializeField, Min(0f)] float closedSeconds = 1.2f;
        [SerializeField, Min(0f)] float openSeconds = 1.6f;
        [SerializeField, Min(0f)] float travelSeconds = 0.5f;
        [SerializeField] float offset;
        [Tooltip("Gap between the blocks when fully open, in units.")]
        [SerializeField, Min(0f)] float openGap = 4.5f;
        [Tooltip("Height of the middle of the gap, in local units.")]
        [SerializeField] float gapCenter;
        [SerializeField] Transform upper;
        [SerializeField] Transform lower;

        ShutterCycle cycle;
        float clock;

        /// <summary>0 = closed, 1 = fully open.</summary>
        public float Openness { get; private set; }
        /// <summary>Unpowered barriers stay open: the section shuts them down once it is cleared.</summary>
        public bool Powered { get; set; } = true;

        void Awake()
        {
            cycle = new ShutterCycle(closedSeconds, openSeconds, travelSeconds, offset);
            Apply();
        }

        void Update()
        {
            clock += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            Openness = Powered ? cycle.Openness(clock) : 1f;
            float half = openGap * 0.5f * Openness;
            if (upper != null)
                upper.localPosition = new Vector3(0f, gapCenter + half, 0f);
            if (lower != null)
                lower.localPosition = new Vector3(0f, gapCenter - half, 0f);
        }
    }
}
