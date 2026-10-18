using NUnit.Framework;
using OneMoreTime.Core.Boss;

namespace OneMoreTime.Tests
{
    public class GuardianModelTests
    {
        const float Survival = 10f;

        static GuardianModel Fighting()
        {
            var g = new GuardianModel(Survival);
            g.Tick(Survival);
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Dismantle));
            return g;
        }

        [Test]
        public void Survival_shields_every_part_until_it_ends()
        {
            var g = new GuardianModel(Survival);
            foreach (GuardianPart part in System.Enum.GetValues(typeof(GuardianPart)))
            {
                Assert.That(g.IsExposed(part), Is.False, part.ToString());
                Assert.That(g.Destroy(part), Is.False, part.ToString());
            }

            g.Tick(Survival - 0.1f);
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Survival));
            g.Tick(0.2f);
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Dismantle));
            Assert.That(g.IsExposed(GuardianPart.Antenna), Is.True);
        }

        [Test]
        public void Antenna_eyes_and_cannons_are_open_from_the_start_but_the_teeth_are_guarded()
        {
            var g = Fighting();
            Assert.That(g.IsExposed(GuardianPart.Antenna), Is.True);
            Assert.That(g.IsExposed(GuardianPart.Eyes), Is.True);
            Assert.That(g.IsExposed(GuardianPart.CannonUpper), Is.True);
            Assert.That(g.IsExposed(GuardianPart.CannonLower), Is.True);
            Assert.That(g.IsExposed(GuardianPart.TeethUpper), Is.False);
            Assert.That(g.IsExposed(GuardianPart.TeethLower), Is.False);
            Assert.That(g.Destroy(GuardianPart.TeethUpper), Is.False);
        }

        [Test]
        public void The_eyes_open_the_upper_teeth_and_both_cannons_open_the_lower_ones()
        {
            var g = Fighting();
            g.Destroy(GuardianPart.Eyes);
            Assert.That(g.IsExposed(GuardianPart.TeethUpper), Is.True);
            Assert.That(g.IsExposed(GuardianPart.TeethLower), Is.False);

            g.Destroy(GuardianPart.CannonUpper);
            Assert.That(g.IsExposed(GuardianPart.TeethLower), Is.False, "one cannon is not enough");
            g.Destroy(GuardianPart.CannonLower);
            Assert.That(g.IsExposed(GuardianPart.TeethLower), Is.True);
        }

        [Test]
        public void A_part_falls_once_and_stays_down()
        {
            var g = Fighting();
            Assert.That(g.Destroy(GuardianPart.Antenna), Is.True);
            Assert.That(g.Destroy(GuardianPart.Antenna), Is.False);
            Assert.That(g.IsDestroyed(GuardianPart.Antenna), Is.True);
            Assert.That(g.IsExposed(GuardianPart.Antenna), Is.False);
            g.Tick(100f);
            Assert.That(g.IsDestroyed(GuardianPart.Antenna), Is.True, "nothing grows back");
        }

        [Test]
        public void The_antenna_calls_fighters_and_the_eyes_call_gunships_until_they_fall()
        {
            var g = new GuardianModel(Survival);
            Assert.That(g.CallsFighters, Is.False, "not while everything is shielded");
            Assert.That(g.CallsGunships, Is.False);

            g = Fighting();
            Assert.That(g.CallsFighters, Is.True);
            Assert.That(g.CallsGunships, Is.True);
            Assert.That(g.Frenzied, Is.False);

            g.Destroy(GuardianPart.Antenna);
            Assert.That(g.CallsFighters, Is.False);
            Assert.That(g.CallsGunships, Is.True);
            Assert.That(g.Frenzied, Is.True, "without the antenna it fires faster");

            Assert.That(g.EyeAttack, Is.False);
            g.Destroy(GuardianPart.Eyes);
            Assert.That(g.CallsGunships, Is.False);
            Assert.That(g.EyeAttack, Is.True, "without the eyes it gains a new attack");
        }

        [Test]
        public void Each_cannon_lost_raises_the_fury()
        {
            var g = Fighting();
            Assert.That(g.Fury, Is.Zero);
            g.Destroy(GuardianPart.CannonLower);
            Assert.That(g.Fury, Is.EqualTo(1));
            g.Destroy(GuardianPart.CannonUpper);
            Assert.That(g.Fury, Is.EqualTo(2));
        }

        [Test]
        public void Only_both_teeth_down_defeats_it()
        {
            var g = Fighting();
            g.Destroy(GuardianPart.Eyes);
            g.Destroy(GuardianPart.CannonUpper);
            g.Destroy(GuardianPart.CannonLower);

            Assert.That(g.Destroy(GuardianPart.TeethUpper), Is.True);
            Assert.That(g.IsDefeated, Is.False, "one set of teeth is not the end");
            Assert.That(g.Destroy(GuardianPart.TeethLower), Is.True);
            Assert.That(g.IsDefeated, Is.True);

            g.Tick(100f);
            Assert.That(g.Phase, Is.EqualTo(GuardianPhase.Defeated));
            Assert.That(g.CallsFighters, Is.False, "it calls nobody once it is beaten");
        }
    }
}
