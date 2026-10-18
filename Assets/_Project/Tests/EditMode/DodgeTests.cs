using NUnit.Framework;
using OneMoreTime.Core.Ship;
using UnityEngine;

namespace OneMoreTime.Tests
{
    public class DodgeModelTests
    {
        static Vector2 Run(DodgeModel dodge, float seconds, float step = 1f / 60f)
        {
            var total = Vector2.zero;
            for (float t = 0f; t < seconds; t += step)
                total += dodge.Tick(step);
            return total;
        }

        [Test]
        public void Covers_exactly_the_distance_toward_the_direction_and_then_stops()
        {
            var dodge = new DodgeModel(0.4f, 3.6f, 0.8f);
            Assert.That(dodge.TryBegin(new Vector2(0f, 2f)), Is.True);

            Vector2 moved = Run(dodge, 0.6f);

            Assert.That(moved.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(moved.y, Is.EqualTo(3.6f).Within(1e-3f));
            Assert.That(dodge.IsActive, Is.False);
        }

        [Test]
        public void Starts_fast_and_brakes()
        {
            var dodge = new DodgeModel(0.4f, 4f, 0.8f);
            dodge.TryBegin(Vector2.right);
            float first = dodge.Tick(0.1f).x;
            dodge.Tick(0.2f);
            float last = dodge.Tick(0.1f).x;
            Assert.That(first, Is.GreaterThan(last * 3f));
        }

        [Test]
        public void Without_a_direction_it_rolls_in_place()
        {
            var dodge = new DodgeModel(0.4f, 4f, 0.8f);
            Assert.That(dodge.TryBegin(Vector2.zero), Is.True);
            Assert.That(Run(dodge, 0.5f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Waits_for_the_cooldown_after_it_ends()
        {
            var dodge = new DodgeModel(0.4f, 3f, 0.8f);
            dodge.TryBegin(Vector2.right);
            Assert.That(dodge.TryBegin(Vector2.left), Is.False, "busy while rolling");

            Run(dodge, 0.45f);
            Assert.That(dodge.IsReady, Is.False);
            Assert.That(dodge.TryBegin(Vector2.right), Is.False, "cooling down");

            Run(dodge, 0.85f);
            Assert.That(dodge.IsReady, Is.True);
            Assert.That(dodge.TryBegin(Vector2.right), Is.True);
        }

        [Test]
        public void Roll_makes_one_full_turn_eased_at_both_ends()
        {
            Assert.That(DodgeModel.RollAngle(0f), Is.EqualTo(0f));
            Assert.That(DodgeModel.RollAngle(1f), Is.EqualTo(360f).Within(1e-3f));
            Assert.That(DodgeModel.RollAngle(0.5f), Is.EqualTo(180f).Within(1e-3f));
            Assert.That(DodgeModel.RollAngle(0.1f), Is.LessThan(36f), "slower than linear at the start");
            Assert.That(DodgeModel.RollAngle(2f), Is.EqualTo(360f).Within(1e-3f), "progress is clamped");
        }
    }
}
