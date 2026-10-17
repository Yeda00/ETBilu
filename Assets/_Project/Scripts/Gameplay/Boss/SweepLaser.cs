using OneMoreTime.Core.Traps;
using UnityEngine;

namespace OneMoreTime.Gameplay.Boss
{
    /// <summary>
    /// The Guardian's sweeping laser (GDD 06, phase 1): a horizontal beam that blinks a warning at one
    /// edge of the screen, then sweeps from that edge toward the middle. Sweeps alternate between the
    /// top and the bottom half, so the answer is to change altitude. Timing comes from
    /// <see cref="TrapCycle"/>: the "on" stretch is the sweep.
    /// </summary>
    public sealed class SweepLaser : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float sweepSeconds = 2.2f;
        [SerializeField, Min(0f)] float restSeconds = 1.6f;
        [SerializeField, Min(0f)] float warningSeconds = 0.9f;
        [Tooltip("Height the sweep starts from (the edge) and ends at (near the middle), local to the boss.")]
        [SerializeField] float edgeY = 6.6f;
        [SerializeField] float innerY = 0.8f;
        [SerializeField] GameObject beam;
        [SerializeField] GameObject warning;

        TrapCycle cycle;
        float clock;
        bool active;

        /// <summary>The boss turns the laser on for the phase that uses it.</summary>
        public bool Active
        {
            get => active;
            set
            {
                if (value == active)
                    return;
                active = value;
                clock = 0f;
                Apply();
            }
        }

        void Awake()
        {
            // Offset so a fresh cycle starts with the rest, then the warning, then the sweep.
            cycle = new TrapCycle(sweepSeconds, restSeconds + warningSeconds, sweepSeconds, warningSeconds);
            Apply();
        }

        void Update()
        {
            if (!active)
                return;
            clock += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            bool on = active && cycle.IsOn(clock);
            bool warn = active && cycle.IsWarning(clock);
            if (beam != null && beam.activeSelf != on)
                beam.SetActive(on);
            if (warning != null && warning.activeSelf != warn)
                warning.SetActive(warn);
            if (!active)
                return;

            // The warning belongs to the sweep about to start, so it already shows that sweep's side.
            int sweep = cycle.CycleIndex(clock) + (on ? 0 : 1);
            float side = sweep % 2 == 0 ? 1f : -1f;
            float progress = on ? cycle.Phase(clock) / sweepSeconds : 0f;
            transform.localPosition = new Vector3(transform.localPosition.x, side * Mathf.Lerp(edgeY, innerY, progress), 0f);
        }
    }
}
