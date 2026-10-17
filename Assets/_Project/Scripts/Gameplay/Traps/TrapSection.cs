using System;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Traps
{
    public enum SectionState
    {
        /// <summary>Scrolling in with the world; its gates cross the ship as the screen advances.</summary>
        Approaching = 0,
        /// <summary>The scroll stopped: the switch puzzle holds still on screen.</summary>
        Locked = 1,
        /// <summary>The ship got past the exit; the traps are shut down.</summary>
        Cleared = 2,
    }

    /// <summary>
    /// A stretch of traps that scrolls in with the world (GDD 05, encounter 2 of GDD 06). The gates at
    /// its front cross the ship while the screen advances. When the section's origin reaches the lock
    /// point the scroll stops, so the switch sits still on screen and an echo replays over the very spot
    /// it was recorded on — recordings are screen-relative (GDD 02). The section is cleared when the
    /// ship flies past <see cref="exitX"/>.
    /// </summary>
    public sealed class TrapSection : MonoBehaviour
    {
        [Tooltip("Playfield-local x at which the section's origin stops the scroll.")]
        [SerializeField] float lockAtLocalX;
        [Tooltip("Section-local x the ship must pass to clear the section.")]
        [SerializeField] float exitX = 7f;

        Playfield playfield;
        Transform ship;
        EchoAbility echoes;
        LaserGate[] gates;
        MovingBarrier[] barriers;

        public event Action<TrapSection> StateChanged;

        public SectionState State { get; private set; }
        /// <summary>Section-local x range covered by its visuals, measured when it begins.</summary>
        public Vector2 ExtentX { get; private set; }

        /// <summary>Hand the section what it needs from the scene. Called right after spawning.</summary>
        public void Begin(Playfield field, Transform shipTransform, EchoAbility echoAbility)
        {
            playfield = field;
            ship = shipTransform;
            echoes = echoAbility;
            gates = GetComponentsInChildren<LaserGate>(true);
            barriers = GetComponentsInChildren<MovingBarrier>(true);
            foreach (var pad in GetComponentsInChildren<SwitchPad>(true))
                pad.Bind(this);
            ExtentX = MeasureExtent();
        }

        /// <summary>How many of the ship and the active echoes are inside the box around <paramref name="center"/>.</summary>
        public int CountOccupants(Vector3 center, Vector2 halfSize, out bool echoInside)
        {
            int count = 0;
            echoInside = false;
            if (ship != null && ship.gameObject.activeInHierarchy && Inside(ship.position, center, halfSize))
                count++;
            if (echoes != null)
            {
                foreach (var echo in echoes.ActiveEchoes)
                {
                    if (echo != null && Inside(echo.transform.position, center, halfSize))
                    {
                        count++;
                        echoInside = true;
                    }
                }
            }
            return count;
        }

        static bool Inside(Vector3 p, Vector3 center, Vector2 halfSize) =>
            Mathf.Abs(p.x - center.x) <= halfSize.x && Mathf.Abs(p.y - center.y) <= halfSize.y;

        void Update()
        {
            if (playfield == null)
                return;

            switch (State)
            {
                case SectionState.Approaching:
                    if (playfield.WorldToLocal(transform.position).x <= lockAtLocalX)
                    {
                        playfield.ScrollSpeed = 0f;
                        SetState(SectionState.Locked);
                    }
                    break;
                case SectionState.Locked:
                    if (ship != null && ship.gameObject.activeInHierarchy && ship.position.x > transform.position.x + exitX)
                    {
                        foreach (var g in gates) g.Powered = false;
                        foreach (var b in barriers) b.Powered = false;
                        SetState(SectionState.Cleared);
                    }
                    break;
            }
        }

        void SetState(SectionState state)
        {
            State = state;
            StateChanged?.Invoke(this);
        }

        Vector2 MeasureExtent()
        {
            float min = 0f, max = 0f;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                min = Mathf.Min(min, r.bounds.min.x - transform.position.x);
                max = Mathf.Max(max, r.bounds.max.x - transform.position.x);
            }
            return new Vector2(min, max);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.21f, 0.89f, 1f, 0.8f);
            var exit = transform.position + Vector3.right * exitX;
            Gizmos.DrawLine(exit + Vector3.down * 7f, exit + Vector3.up * 7f);
        }
    }
}
