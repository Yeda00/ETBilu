using NUnit.Framework;
using OneMoreTime.Core.Combos;

namespace OneMoreTime.Tests
{
    public class CrossfireRuleTests
    {
        const float Window = 1f, Rearm = 1f;
        const int Target = 7;

        [Test]
        public void Ship_and_echo_on_the_same_target_inside_the_window_match()
        {
            var rule = new CrossfireRule(Window, Rearm);
            Assert.That(rule.Hit(Target, Attacker.Ship, 0f), Is.False);
            Assert.That(rule.Hit(Target, Attacker.Echo, 0.9f), Is.True);

            var reverse = new CrossfireRule(Window, Rearm);
            Assert.That(reverse.Hit(Target, Attacker.Echo, 0f), Is.False);
            Assert.That(reverse.Hit(Target, Attacker.Ship, 1f), Is.True, "the window edge still counts");
        }

        [Test]
        public void No_match_outside_the_window()
        {
            var rule = new CrossfireRule(Window, Rearm);
            rule.Hit(Target, Attacker.Ship, 0f);
            Assert.That(rule.Hit(Target, Attacker.Echo, Window + 0.05f), Is.False);
        }

        [Test]
        public void The_same_source_twice_does_not_count()
        {
            var rule = new CrossfireRule(Window, Rearm);
            rule.Hit(Target, Attacker.Ship, 0f);
            Assert.That(rule.Hit(Target, Attacker.Ship, 0.1f), Is.False);
            rule.Hit(Target, Attacker.Echo, 5f);
            Assert.That(rule.Hit(Target, Attacker.Echo, 5.1f), Is.False);
        }

        [Test]
        public void Hits_on_different_targets_do_not_match()
        {
            var rule = new CrossfireRule(Window, Rearm);
            rule.Hit(1, Attacker.Ship, 0f);
            Assert.That(rule.Hit(2, Attacker.Echo, 0.1f), Is.False);
        }

        [Test]
        public void Fires_once_per_pair_and_rearms_after_a_while()
        {
            var rule = new CrossfireRule(Window, Rearm);
            rule.Hit(Target, Attacker.Ship, 0f);
            Assert.That(rule.Hit(Target, Attacker.Echo, 0.2f), Is.True);
            Assert.That(rule.Hit(Target, Attacker.Ship, 0.3f), Is.False, "both hits were spent");
            Assert.That(rule.Hit(Target, Attacker.Echo, 0.4f), Is.False, "locked until the rearm time");

            Assert.That(rule.Hit(Target, Attacker.Ship, 0.2f + Rearm), Is.False, "a fresh pair is needed");
            Assert.That(rule.Hit(Target, Attacker.Echo, 0.3f + Rearm), Is.True);
        }

        [Test]
        public void Forgetting_a_target_drops_its_hits()
        {
            var rule = new CrossfireRule(Window, Rearm);
            rule.Hit(Target, Attacker.Ship, 0f);
            rule.Forget(Target);
            Assert.That(rule.TrackedTargets, Is.EqualTo(0));
            Assert.That(rule.Hit(Target, Attacker.Echo, 0.1f), Is.False);
        }
    }

    public class SynchronizedStrikeRuleTests
    {
        const float Window = 2.5f;
        const int Upper = 0, Lower = 1;

        [Test]
        public void Two_points_broken_by_ship_and_echo_inside_the_window_match()
        {
            var rule = new SynchronizedStrikeRule(Window);
            Assert.That(rule.Broken(Upper, Attacker.Echo, 0f), Is.False);
            Assert.That(rule.Broken(Lower, Attacker.Ship, 2f), Is.True);
        }

        [Test]
        public void No_match_outside_the_window()
        {
            var rule = new SynchronizedStrikeRule(Window);
            rule.Broken(Upper, Attacker.Echo, 0f);
            Assert.That(rule.Broken(Lower, Attacker.Ship, Window + 0.1f), Is.False);
        }

        [Test]
        public void The_same_source_on_both_points_does_not_count()
        {
            var rule = new SynchronizedStrikeRule(Window);
            rule.Broken(Upper, Attacker.Ship, 0f);
            Assert.That(rule.Broken(Lower, Attacker.Ship, 0.5f), Is.False);
        }

        [Test]
        public void The_same_point_twice_does_not_count()
        {
            var rule = new SynchronizedStrikeRule(Window);
            rule.Broken(Upper, Attacker.Ship, 0f);
            Assert.That(rule.Broken(Upper, Attacker.Echo, 0.5f), Is.False);
        }

        [Test]
        public void Fires_once_per_pair()
        {
            var rule = new SynchronizedStrikeRule(Window);
            rule.Broken(Upper, Attacker.Ship, 0f);
            Assert.That(rule.Broken(Lower, Attacker.Echo, 0.5f), Is.True);
            Assert.That(rule.Broken(Upper, Attacker.Ship, 0.6f), Is.False, "both breaks were spent");
        }

        [Test]
        public void Only_the_latest_break_of_a_point_counts()
        {
            var rule = new SynchronizedStrikeRule(Window);
            rule.Broken(Upper, Attacker.Echo, 0f);
            rule.Broken(Upper, Attacker.Ship, 1f);   // rebuilt and broken again, now by the ship
            Assert.That(rule.Broken(Lower, Attacker.Ship, 1.5f), Is.False, "the echo's break was replaced");
            Assert.That(rule.Broken(Lower, Attacker.Echo, 2f), Is.True, "the lower point's new break replaces its own");
        }
    }
}
