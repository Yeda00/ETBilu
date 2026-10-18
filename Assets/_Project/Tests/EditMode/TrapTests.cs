using NUnit.Framework;
using OneMoreTime.Core.Traps;

namespace OneMoreTime.Tests
{
    /// <summary>The on/off cycle with a warning, used by the Guardian's sweeping laser.</summary>
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
}
