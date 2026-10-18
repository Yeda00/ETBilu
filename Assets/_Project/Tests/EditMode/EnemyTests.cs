using System.Collections.Generic;
using NUnit.Framework;
using OneMoreTime.Core.Enemies;
using UnityEngine;

namespace OneMoreTime.Tests
{
    public class FireCycleTests
    {
        readonly List<int> shots = new();

        [Test]
        public void Stays_silent_until_armed()
        {
            var gun = new FireCycle(0f, 1f, 1, 0f, 0f, 0);
            Assert.That(gun.Tick(5f, shots), Is.EqualTo(0));
        }

        [Test]
        public void Fires_after_the_first_delay_then_every_interval()
        {
            var gun = new FireCycle(firstDelay: 1f, interval: 2f, shotsPerBurst: 1, shotGap: 0f, windup: 0.3f, maxBursts: 0);
            gun.Arm();
            Assert.That(gun.Tick(0.9f, shots), Is.EqualTo(0));
            Assert.That(gun.Tick(0.2f, shots), Is.EqualTo(1));
            Assert.That(gun.Tick(1.8f, shots), Is.EqualTo(0));
            Assert.That(gun.Tick(0.2f, shots), Is.EqualTo(1));
            Assert.That(gun.BurstsFired, Is.EqualTo(2));
        }

        [Test]
        public void A_burst_spaces_its_shots_and_numbers_them()
        {
            var spaced = new FireCycle(0f, 5f, 3, 0.1f, 0f, 0);
            spaced.Arm();
            spaced.Tick(0.01f, shots);
            spaced.Tick(0.1f, shots);
            spaced.Tick(0.1f, shots);
            Assert.That(shots, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(spaced.Tick(1f, shots), Is.EqualTo(0), "next burst only after the interval");

            shots.Clear();
            var fan = new FireCycle(0f, 5f, 3, 0f, 0f, 0);
            fan.Arm();
            Assert.That(fan.Tick(0.01f, shots), Is.EqualTo(3), "no gap: the whole burst at once");
            Assert.That(shots, Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void Stops_after_max_bursts()
        {
            var gun = new FireCycle(0.5f, 1f, 1, 0f, 0f, maxBursts: 1);
            gun.Arm();
            Assert.That(gun.Tick(0.6f, shots), Is.EqualTo(1));
            Assert.That(gun.Tick(10f, shots), Is.EqualTo(0));
            Assert.That(gun.Exhausted, Is.True);
            Assert.That(gun.WindingUp, Is.False);
        }

        [Test]
        public void Winds_up_just_before_each_burst()
        {
            var gun = new FireCycle(1f, 2f, 1, 0f, windup: 0.3f, maxBursts: 0);
            gun.Arm();
            gun.Tick(0.5f, shots);
            Assert.That(gun.WindingUp, Is.False);
            gun.Tick(0.25f, shots);
            Assert.That(gun.WindingUp, Is.True);
            gun.Tick(0.3f, shots);
            Assert.That(gun.WindingUp, Is.False, "just fired: the next burst is far");
        }

        [Test]
        public void Disarming_freezes_the_cycle()
        {
            var gun = new FireCycle(1f, 2f, 1, 0f, 0f, 0);
            gun.Arm();
            gun.Tick(0.5f, shots);
            gun.Disarm();
            Assert.That(gun.Tick(5f, shots), Is.EqualTo(0));
            gun.Arm();
            Assert.That(gun.Tick(0.4f, shots), Is.EqualTo(0), "resumes where it stopped, without a new first delay");
            Assert.That(gun.Tick(0.2f, shots), Is.EqualTo(1));
        }
    }

    public class SwoopMotionTests
    {
        [Test]
        public void Flies_straight_until_the_swoop_point()
        {
            var m = new SwoopMotion(new Vector2(10f, 2f), speed: 5f, swoopX: 4f, swoopSeconds: 1f, maxTurn: 10f);
            Assert.That(m.Tick(0.5f, -3f), Is.False);
            Assert.That(m.Position.x, Is.EqualTo(7.5f).Within(1e-4f));
            Assert.That(m.Position.y, Is.EqualTo(2f));
            Assert.That(m.HasSwooped, Is.False);
        }

        [Test]
        public void Bends_to_the_height_taken_when_the_swoop_starts()
        {
            var m = new SwoopMotion(new Vector2(10f, 2f), 5f, 4f, 1f, 10f);
            m.Tick(1f, -3f);
            Assert.That(m.Tick(0.25f, -3f), Is.True, "crossed x = 4");
            m.Tick(0.5f, 99f);
            Assert.That(m.Position.y, Is.LessThan(2f).And.GreaterThan(-3f), "on its way, still bending to -3");
            Assert.That(m.Tick(0.6f, 99f), Is.False, "the swoop starts only once");
            Assert.That(m.Position.y, Is.EqualTo(-3f).Within(1e-4f));
            Assert.That(m.SwoopDone, Is.True);
        }

        [Test]
        public void The_bend_is_limited()
        {
            var m = new SwoopMotion(new Vector2(4.5f, 2f), 5f, 4f, 1f, maxTurn: 2f);
            m.Tick(0.2f, -10f);
            Assert.That(m.TargetY, Is.EqualTo(0f).Within(1e-4f));
        }
    }

    public class HoverMotionTests
    {
        const float Enter = 1.5f, Hold = 6f, Bob = 0.5f;

        static HoverMotion Make(float phase = 0f) =>
            new(new Vector2(14f, 1f), holdX: 8f, enterSeconds: Enter, holdSeconds: Hold,
                bobAmplitude: Bob, bobFrequency: 0.3f, bobPhase: phase, leaveAcceleration: 4f);

        [Test]
        public void Brakes_at_the_hold_column()
        {
            var m = Make();
            Assert.That(m.PhaseAt(0.5f), Is.EqualTo(HoverPhase.Entering));
            Assert.That(m.Evaluate(Enter * 0.5f).x, Is.EqualTo(9.5f).Within(1e-4f), "past the midpoint: fast in, slow at the end");
            Assert.That(m.Evaluate(Enter).x, Is.EqualTo(8f).Within(1e-4f));
            Assert.That(m.PhaseAt(Enter), Is.EqualTo(HoverPhase.Holding));
        }

        [Test]
        public void Holds_the_column_and_bobs_without_a_jump()
        {
            var m = Make(phase: 1.3f);
            Assert.That(m.Evaluate(Enter).y, Is.EqualTo(1f).Within(1e-4f), "the bob starts at zero");
            for (float t = Enter; t < Enter + Hold; t += 0.1f)
            {
                var p = m.Evaluate(t);
                Assert.That(p.x, Is.EqualTo(8f).Within(1e-4f));
                Assert.That(Mathf.Abs(p.y - 1f), Is.LessThanOrEqualTo(2f * Bob + 1e-4f));
            }
        }

        [Test]
        public void Leaves_to_the_left_speeding_up()
        {
            var m = Make();
            float t = Enter + Hold;
            float x0 = m.Evaluate(t).x, x1 = m.Evaluate(t + 1f).x, x2 = m.Evaluate(t + 2f).x;
            Assert.That(m.PhaseAt(t + 0.1f), Is.EqualTo(HoverPhase.Leaving));
            Assert.That(x1, Is.LessThan(x0));
            Assert.That(x1 - x2, Is.GreaterThan(x0 - x1));
        }

        [Test]
        public void Tick_moves_along_the_path()
        {
            var m = Make();
            m.Tick(2f);
            Assert.That(m.Time, Is.EqualTo(2f));
            Assert.That(m.Position, Is.EqualTo(m.Evaluate(2f)));
        }
    }
}
