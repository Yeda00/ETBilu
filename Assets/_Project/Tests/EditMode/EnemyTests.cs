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

    public class TrackingLaserCycleTests
    {
        [Test]
        public void Tracks_then_locks_warns_fires_and_recovers()
        {
            var laser = new TrackingLaserCycle(1f, 0.8f, 0.3f, 1.2f, maxBursts: 2);
            Assert.That(laser.Phase, Is.EqualTo(TrackingLaserPhase.Tracking));
            Assert.That(laser.LocksAim, Is.False);

            laser.Tick(1f);
            Assert.That(laser.Phase, Is.EqualTo(TrackingLaserPhase.Charging));
            Assert.That(laser.LocksAim, Is.True);
            Assert.That(laser.IsThreatening, Is.True);

            laser.Tick(0.8f);
            Assert.That(laser.Phase, Is.EqualTo(TrackingLaserPhase.Firing));
            Assert.That(laser.BurstsFired, Is.EqualTo(1));

            laser.Tick(0.3f);
            Assert.That(laser.Phase, Is.EqualTo(TrackingLaserPhase.Recovering));
            Assert.That(laser.LocksAim, Is.False);
        }

        [Test]
        public void Stops_after_the_authored_number_of_beams()
        {
            var laser = new TrackingLaserCycle(0.1f, 0.1f, 0.1f, 0.1f, maxBursts: 2);
            for (int i = 0; i < 8; i++)
                laser.Tick(0.1f);
            Assert.That(laser.BurstsFired, Is.EqualTo(2));
            Assert.That(laser.Phase, Is.EqualTo(TrackingLaserPhase.Exhausted));
            Assert.That(laser.Tick(10f), Is.False);
        }

        [Test]
        public void Non_positive_time_does_not_advance()
        {
            var laser = new TrackingLaserCycle(1f, 1f, 1f, 1f, 1);
            Assert.That(laser.Tick(0f), Is.False);
            Assert.That(laser.Tick(-1f), Is.False);
            Assert.That(laser.Phase, Is.EqualTo(TrackingLaserPhase.Tracking));
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

    public class SerpentineMotionTests
    {
        [Test]
        public void Moves_left_at_constant_speed_and_joins_without_a_vertical_pop()
        {
            var m = new SerpentineMotion(new Vector2(10f, 2f), speed: 5f, amplitude: 1.5f, frequency: 0.5f, phase: 1.2f);
            Assert.That(m.Evaluate(0f), Is.EqualTo(new Vector2(10f, 2f)));
            m.Tick(0.5f);
            Assert.That(m.Position.x, Is.EqualTo(7.5f).Within(1e-4f));
        }

        [Test]
        public void Phase_desynchronises_members_of_the_same_formation()
        {
            var a = new SerpentineMotion(Vector2.zero, 5f, 1f, 0.5f, 0f);
            var b = new SerpentineMotion(Vector2.zero, 5f, 1f, 0.5f, 1.2f);
            a.Tick(0.4f);
            b.Tick(0.4f);
            Assert.That(a.Position.x, Is.EqualTo(b.Position.x).Within(1e-4f));
            Assert.That(a.Position.y, Is.Not.EqualTo(b.Position.y).Within(1e-3f));
        }

        [Test]
        public void Non_positive_delta_does_not_move()
        {
            var m = new SerpentineMotion(new Vector2(3f, -2f), 5f, 1f, 1f, 0f);
            m.Tick(-1f);
            Assert.That(m.Position, Is.EqualTo(new Vector2(3f, -2f)));
            Assert.That(m.Time, Is.Zero);
        }

        [Test]
        public void Vertical_displacement_stays_inside_the_authored_amplitude()
        {
            const float amplitude = 1.4f;
            var m = new SerpentineMotion(new Vector2(0f, 2f), 5f, amplitude, 0.55f, 1.7f);
            for (float t = 0f; t < 5f; t += 0.05f)
                Assert.That(Mathf.Abs(m.Evaluate(t).y - 2f), Is.LessThanOrEqualTo(amplitude + 1e-4f));
        }
    }
}

namespace OneMoreTime.Tests
{
    public class ChaseMotionTests
    {
        [Test]
        public void Homes_in_on_a_target_it_can_reach()
        {
            var motion = new ChaseMotion(new Vector2(10f, 0f), 6f, 120f);
            var target = new Vector2(-2f, 3f);
            float closest = float.MaxValue;
            for (int i = 0; i < 360; i++)
            {
                motion.Tick(1f / 60f, target);
                closest = Mathf.Min(closest, Vector2.Distance(motion.Position, target));
            }
            Assert.That(closest, Is.LessThan(0.5f));
        }

        [Test]
        public void Turns_only_so_fast()
        {
            var motion = new ChaseMotion(Vector2.zero, 5f, 90f);   // flying left
            motion.Tick(0.5f, new Vector2(0f, 10f));               // target straight up: wants a 90° turn

            Assert.That(motion.Heading, Is.EqualTo(135f).Within(0.01f), "45° in half a second at 90°/s");
        }

        [Test]
        public void A_target_that_keeps_moving_can_make_it_overshoot()
        {
            var motion = new ChaseMotion(new Vector2(8f, 0f), 6f, 70f);
            float closest = float.MaxValue;
            for (int i = 0; i < 240; i++)
            {
                var target = new Vector2(0f, 3f * Mathf.Sin(i / 60f * 6f));
                motion.Tick(1f / 60f, target);
                closest = Mathf.Min(closest, Vector2.Distance(motion.Position, target));
            }
            Assert.That(closest, Is.GreaterThan(0.2f), "it never quite lands on a ship that weaves");
        }
    }

    public class BlastFuseTests
    {
        [Test]
        public void Waits_quietly_while_nothing_is_near()
        {
            var fuse = new BlastFuse(triggerRadius: 3f, fuseSeconds: 0.8f);
            Assert.That(fuse.Tick(5f, 10f), Is.False);
            Assert.That(fuse.Triggered, Is.False);
            Assert.That(fuse.Warning, Is.False);
        }

        [Test]
        public void A_target_in_range_lights_the_fuse_and_it_goes_off_once()
        {
            var fuse = new BlastFuse(3f, 0.8f);
            Assert.That(fuse.Tick(0.1f, 2f), Is.False);
            Assert.That(fuse.Triggered, Is.True);
            Assert.That(fuse.Warning, Is.True);

            Assert.That(fuse.Tick(0.5f, 9f), Is.False, "the fuse burns even if the target runs");
            Assert.That(fuse.Tick(0.3f, 9f), Is.True);
            Assert.That(fuse.Tick(1f, 0f), Is.False, "only once");
        }

        [Test]
        public void The_timer_sets_it_off_without_a_target_and_warns_before()
        {
            var fuse = new BlastFuse(1f, 0.1f, autoSeconds: 4f, warnSeconds: 1f);
            fuse.Tick(2.9f, 20f);
            Assert.That(fuse.Warning, Is.False);
            fuse.Tick(0.2f, 20f);
            Assert.That(fuse.Warning, Is.True, "the last second before the timer ends");
            Assert.That(fuse.Tick(1f, 20f), Is.True, "the timer ran out, and the short fuse with it");
        }

        [Test]
        public void Urgency_rises_as_the_fuse_burns()
        {
            var fuse = new BlastFuse(3f, 1f);
            fuse.Tick(0.01f, 1f);
            float early = fuse.Urgency;
            fuse.Tick(0.6f, 1f);
            Assert.That(fuse.Urgency, Is.GreaterThan(early));
        }
    }

    public class InterceptionTests
    {
        [Test]
        public void A_shot_flying_at_me_is_seen_with_its_arrival_time()
        {
            float t = Interception.TimeToHit(new Vector2(10f, 0f), 0.6f, Vector2.zero, new Vector2(20f, 0f), 1f, out float side);
            Assert.That(t, Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(side, Is.EqualTo(0f));
        }

        [Test]
        public void Says_which_way_is_out_for_a_shot_passing_close()
        {
            // I am a little above the shot's line: up is the way out.
            Interception.TimeToHit(new Vector2(10f, 0.3f), 0.6f, Vector2.zero, new Vector2(20f, 0f), 1f, out float above);
            Interception.TimeToHit(new Vector2(10f, -0.3f), 0.6f, Vector2.zero, new Vector2(20f, 0f), 1f, out float below);
            Assert.That(above, Is.EqualTo(1f));
            Assert.That(below, Is.EqualTo(-1f));
        }

        [Test]
        public void Shots_that_miss_or_leave_are_ignored()
        {
            Assert.That(Interception.TimeToHit(new Vector2(10f, 3f), 0.6f, Vector2.zero, new Vector2(20f, 0f), 1f, out _), Is.Negative, "passes wide");
            Assert.That(Interception.TimeToHit(new Vector2(-5f, 0f), 0.6f, Vector2.zero, new Vector2(20f, 0f), 1f, out _), Is.Negative, "already gone");
            Assert.That(Interception.TimeToHit(new Vector2(40f, 0f), 0.6f, Vector2.zero, new Vector2(20f, 0f), 1f, out _), Is.Negative, "too far to matter yet");
        }
    }
}
