using OneMoreTime.Core.Traps;
using UnityEngine;

namespace OneMoreTime.Gameplay.Traps
{
    /// <summary>
    /// A laser barrier across the screen (GDD 05). It switches on and off in cycles, blinking a thin
    /// warning line before each "on" — or, without a cycle, stays on. A linked <see cref="SwitchPad"/>
    /// keeps it off while something holds the switch. The live beam hurts the ship on contact.
    /// Rules live in <see cref="LaserGateModel"/>.
    /// </summary>
    [DefaultExecutionOrder(210)]
    public sealed class LaserGate : MonoBehaviour
    {
        [Tooltip("On: follows the cycle below. Off: always on unless a switch holds it open.")]
        [SerializeField] bool cycles = true;
        [SerializeField, Min(0f)] float onSeconds = 1.5f;
        [SerializeField, Min(0f)] float offSeconds = 1.5f;
        [Tooltip("Seconds added to the gate's clock, to put neighbouring gates out of step.")]
        [SerializeField] float offset;
        [Tooltip("Last seconds of each \"off\" spent blinking the warning line.")]
        [SerializeField, Min(0f)] float warningSeconds = 0.5f;
        [Tooltip("Switch that keeps this gate off while held. Optional.")]
        [SerializeField] SwitchPad holdOpenBy;
        [Tooltip("The live beam: its collider and visual. Enabled only while the gate is on.")]
        [SerializeField] GameObject beam;
        [Tooltip("Thin blinking line shown before the gate turns on.")]
        [SerializeField] GameObject warning;

        LaserGateModel model;
        float clock;

        public GateState State { get; private set; } = GateState.On;
        /// <summary>Unpowered gates stay off: the section shuts them down once it is cleared.</summary>
        public bool Powered { get; set; } = true;
        public SwitchPad HoldOpenBy => holdOpenBy;

        void Awake()
        {
            model = new LaserGateModel(cycles ? new TrapCycle(onSeconds, offSeconds, offset, warningSeconds) : null);
            Apply(Evaluate());
        }

        void Update()
        {
            clock += Time.deltaTime;
            var state = Evaluate();
            if (state != State)
                Apply(state);
        }

        GateState Evaluate()
        {
            if (!Powered)
                return GateState.Off;
            return model.Evaluate(clock, holdOpenBy != null && holdOpenBy.IsHeld);
        }

        void Apply(GateState state)
        {
            State = state;
            if (beam != null)
                beam.SetActive(state == GateState.On);
            if (warning != null)
                warning.SetActive(state == GateState.Warning);
        }
    }
}
