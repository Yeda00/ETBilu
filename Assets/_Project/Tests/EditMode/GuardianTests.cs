using NUnit.Framework;
using OneMoreTime.Core.Boss;

namespace OneMoreTime.Tests
{
    public class GuardianModelTests
    {
        const float Survival = 10f, PrepWindow = 6f, SyncWindow = 2f, Vulnerable = 4f;

        static GuardianModel InPreparation()
        {
            var g = new GuardianModel(Survival, PrepWindow, SyncWindow, Vulnerable);
            g.Tick(Survival);
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Preparation));
            return g;
        }

        static GuardianModel InSynchronization()
        {
            var g = InPreparation();
            g.DestroyGenerator(0);
            g.DestroyGenerator(1);
            g.Tick(Vulnerable);
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Synchronization));
            return g;
        }

        [Test]
        public void Survival_shields_the_generators_until_it_ends()
        {
            var g = new GuardianModel(Survival, PrepWindow, SyncWindow, Vulnerable);
            Assert.That(g.GeneratorsShielded, Is.True);
            Assert.That(g.DestroyGenerator(0), Is.False);
            g.Tick(Survival - 0.1f);
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Survival));
            g.Tick(0.2f);
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Preparation));
            Assert.That(g.DestroyGenerator(0), Is.True);
        }

        [Test]
        public void A_lone_fallen_generator_comes_back_when_the_window_closes()
        {
            var g = InPreparation();
            g.DestroyGenerator(0);
            Assert.That(g.WindowRemaining, Is.EqualTo(PrepWindow));
            g.Tick(PrepWindow + 0.01f);
            Assert.That(g.IsGeneratorUp(0), Is.True);
            Assert.That(g.CoreVulnerable, Is.False);
        }

        [Test]
        public void Both_generators_inside_the_window_open_the_core()
        {
            var g = InPreparation();
            g.DestroyGenerator(1);
            g.Tick(PrepWindow - 0.5f);
            Assert.That(g.DestroyGenerator(0), Is.True);
            Assert.That(g.CoreVulnerable, Is.True);
            Assert.That(g.VulnerableRemaining, Is.EqualTo(Vulnerable));
            Assert.That(g.DestroyGenerator(0), Is.False, "already down");
        }

        [Test]
        public void First_opening_leads_to_synchronization_with_the_short_window()
        {
            var g = InSynchronization();
            Assert.That(g.GeneratorsUp, Is.EqualTo(2), "generators rebuilt after the core closes");
            Assert.That(g.Window, Is.EqualTo(SyncWindow));

            g.DestroyGenerator(0);
            g.Tick(SyncWindow + 0.01f);
            Assert.That(g.IsGeneratorUp(0), Is.True, "too slow for the short window");
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Synchronization));
        }

        [Test]
        public void Core_only_falls_while_vulnerable()
        {
            var g = InSynchronization();
            Assert.That(g.DefeatCore(), Is.False);

            g.DestroyGenerator(0);
            g.DestroyGenerator(1);
            Assert.That(g.DefeatCore(), Is.True);
            Assert.That(g.IsDefeated, Is.True);

            g.Tick(100f);
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Defeated));
        }
    }
}
