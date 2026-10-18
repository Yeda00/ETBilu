using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneMoreTime.Core.Combat;
using OneMoreTime.Core.Waves;

namespace OneMoreTime.Tests
{
    public class HealthModelTests
    {
        [Test]
        public void Invulnerability_blocks_hits_for_its_window()
        {
            var hp = new HealthModel(3, 1f);
            Assert.That(hp.TryDamage(1, 0f), Is.True);
            Assert.That(hp.TryDamage(1, 0.5f), Is.False);
            Assert.That(hp.TryDamage(1, 1.01f), Is.True);
            Assert.That(hp.Current, Is.EqualTo(1));
        }

        [Test]
        public void Dies_at_zero_and_ignores_further_damage()
        {
            var hp = new HealthModel(2, 0f);
            hp.TryDamage(5, 0f);
            Assert.That(hp.IsDead, Is.True);
            Assert.That(hp.Current, Is.Zero);
            Assert.That(hp.TryDamage(1, 10f), Is.False);
        }

        [Test]
        public void Shield_absorbs_hits_without_starting_invulnerability()
        {
            var hp = new HealthModel(3, 1f) { Shielded = true };
            Assert.That(hp.TryDamage(1, 0f), Is.False);
            Assert.That(hp.Current, Is.EqualTo(3));
            hp.Shielded = false;
            Assert.That(hp.TryDamage(1, 0.1f), Is.True, "no invulnerability left over from the absorbed hit");
        }

        [Test]
        public void Granted_invulnerability_extends_the_window_without_changing_health()
        {
            var hp = new HealthModel(3, 0f);
            hp.GrantInvulnerability(0.2f, 10f);
            Assert.That(hp.Current, Is.EqualTo(3));
            Assert.That(hp.TryDamage(1, 10.19f), Is.False);
            Assert.That(hp.TryDamage(1, 10.21f), Is.True);
        }
    }

    public class TimelineCursorTests
    {
        [Test]
        public void Reports_each_entry_once_when_due()
        {
            var cursor = new TimelineCursor(new[] { 10f, 25f, 25f, 40f });
            var due = new List<int>();

            cursor.Poll(9.9f, due);
            Assert.That(due, Is.Empty);
            cursor.Poll(25f, due);
            Assert.That(due, Is.EqualTo(new[] { 0, 1, 2 }));
            cursor.Poll(100f, due);
            Assert.That(due, Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(cursor.IsDone, Is.True);
        }

        [Test]
        public void Rejects_unsorted_times()
        {
            Assert.Throws<ArgumentException>(() => new TimelineCursor(new[] { 5f, 1f }));
        }
    }
}
