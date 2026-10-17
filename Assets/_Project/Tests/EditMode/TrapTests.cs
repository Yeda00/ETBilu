using NUnit.Framework;
using OneMoreTime.Core.Traps;

namespace OneMoreTime.Tests
{
    public class TrapCycleTests
    {
        [Test]
        public void Alternates_on_and_off_with_a_warning_before_on()
        {
            var cycle = new TrapCycle(onSeconds: 1f, offSeconds: 2f, warningSeconds: 0.5f);

            Assert.That(cycle.IsOn(0.5f), Is.True);
            Assert.That(cycle.IsOn(1.5f), Is.False);
            Assert.That(cycle.IsWarning(1.5f), Is.False, "early in the off stretch: no warning yet");
            Assert.That(cycle.IsWarning(2.6f), Is.True, "last 0.5 s before turning on");
            Assert.That(cycle.IsOn(3.2f), Is.True, "next cycle");
            Assert.That(cycle.CycleIndex(3.2f), Is.EqualTo(1));
        }

        [Test]
        public void Offset_shifts_the_cycle()
        {
            var cycle = new TrapCycle(1f, 1f, offset: 1f);
            Assert.That(cycle.IsOn(0.5f), Is.False);
            Assert.That(cycle.IsOn(1.5f), Is.True);
        }

        [Test]
        public void No_off_time_means_always_on()
        {
            var cycle = new TrapCycle(1f, 0f, warningSeconds: 0.5f);
            Assert.That(cycle.IsOn(123.4f), Is.True);
            Assert.That(cycle.IsWarning(123.4f), Is.False);
        }
    }

    public class ShutterCycleTests
    {
        [Test]
        public void Closes_opens_and_closes_again()
        {
            var s = new ShutterCycle(closedSeconds: 1f, openSeconds: 2f, travelSeconds: 0.5f);

            Assert.That(s.Period, Is.EqualTo(4f));
            Assert.That(s.Openness(0.5f), Is.Zero);
            Assert.That(s.Openness(1.25f), Is.EqualTo(0.5f).Within(1e-4f), "halfway through opening");
            Assert.That(s.Openness(2f), Is.EqualTo(1f));
            Assert.That(s.Openness(3.75f), Is.EqualTo(0.5f).Within(1e-4f), "halfway through closing");
            Assert.That(s.Openness(4.2f), Is.Zero, "next cycle");
        }
    }

    public class PresenceSwitchTests
    {
        [Test]
        public void Held_only_while_occupied_plus_the_release_delay()
        {
            var sw = new PresenceSwitch(releaseDelay: 0.2f);

            Assert.That(sw.Update(1, 0.1f), Is.True, "pressed");
            Assert.That(sw.IsHeld, Is.True);
            sw.Update(0, 0.1f);
            Assert.That(sw.IsHeld, Is.True, "still inside the release delay");
            sw.Update(0, 0.15f);
            Assert.That(sw.IsHeld, Is.False);
        }

        [Test]
        public void Coming_back_inside_the_delay_keeps_it_held()
        {
            var sw = new PresenceSwitch(0.2f);
            sw.Update(1, 0.1f);
            sw.Update(0, 0.15f);
            sw.Update(1, 0.1f);
            sw.Update(0, 0.15f);
            Assert.That(sw.IsHeld, Is.True, "the delay restarts after each visit");
        }

        [Test]
        public void Nobody_on_it_never_presses_it()
        {
            var sw = new PresenceSwitch(0f);
            Assert.That(sw.Update(0, 1f), Is.False);
            Assert.That(sw.IsHeld, Is.False);
        }
    }

    public class LaserGateModelTests
    {
        [Test]
        public void Cycling_gate_follows_its_cycle()
        {
            var gate = new LaserGateModel(new TrapCycle(1f, 1f, warningSeconds: 0.3f));
            Assert.That(gate.Evaluate(0.5f, false), Is.EqualTo(GateState.On));
            Assert.That(gate.Evaluate(1.2f, false), Is.EqualTo(GateState.Off));
            Assert.That(gate.Evaluate(1.8f, false), Is.EqualTo(GateState.Warning));
        }

        [Test]
        public void Held_switch_keeps_any_gate_off()
        {
            var always = new LaserGateModel(null);
            Assert.That(always.Evaluate(5f, false), Is.EqualTo(GateState.On));
            Assert.That(always.Evaluate(5f, true), Is.EqualTo(GateState.Off));

            var cycling = new LaserGateModel(new TrapCycle(1f, 1f));
            Assert.That(cycling.Evaluate(0.5f, true), Is.EqualTo(GateState.Off));
        }
    }
}
