namespace OneMoreTime.Core.Traps
{
    public enum GateState
    {
        Off = 0,
        /// <summary>Still harmless, about to turn on.</summary>
        Warning = 1,
        On = 2,
    }

    /// <summary>
    /// The laser gate's rule (GDD 05): it follows its cycle — or stays on, if it has none — unless a
    /// linked switch is held, which keeps it off for as long as something sits on the switch.
    /// </summary>
    public sealed class LaserGateModel
    {
        readonly TrapCycle cycle;

        /// <param name="cycle">The on/off rhythm, or null for a gate that is always on.</param>
        public LaserGateModel(TrapCycle cycle)
        {
            this.cycle = cycle;
        }

        public bool Cycles => cycle != null;

        public GateState Evaluate(float time, bool heldOpen)
        {
            if (heldOpen)
                return GateState.Off;
            if (cycle == null || cycle.IsOn(time))
                return GateState.On;
            return cycle.IsWarning(time) ? GateState.Warning : GateState.Off;
        }
    }
}
