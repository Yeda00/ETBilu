using NUnit.Framework;
using OneMoreTime.Core.Gadgets;

namespace OneMoreTime.Tests
{
    public class CooldownTests
    {
        [Test]
        public void Blocks_reuse_until_recharged()
        {
            var cd = new Cooldown(1.5f);
            Assert.That(cd.TryUse(), Is.True);
            Assert.That(cd.TryUse(), Is.False);
            cd.Tick(1f);
            Assert.That(cd.Normalized, Is.EqualTo(1f / 3f).Within(1e-4f));
            Assert.That(cd.TryUse(), Is.False);
            cd.Tick(0.5f);
            Assert.That(cd.IsReady, Is.True);
            Assert.That(cd.TryUse(), Is.True);
        }
    }

    public class TimeMineModelTests
    {
        [Test]
        public void Ignores_targets_until_armed()
        {
            var mine = new TimeMineModel(armSeconds: 0.4f, fuseSeconds: 4f);
            Assert.That(mine.Tick(0.2f, targetInRange: true), Is.False);
            Assert.That(mine.Tick(0.3f, targetInRange: true), Is.True);
            Assert.That(mine.HasExploded, Is.True);
        }

        [Test]
        public void Goes_off_by_itself_when_the_fuse_runs_out()
        {
            var mine = new TimeMineModel(0.4f, 4f);
            for (int i = 0; i < 39; i++)
                Assert.That(mine.Tick(0.1f, false), Is.False);
            Assert.That(mine.Tick(0.11f, false), Is.True);
        }

        [Test]
        public void Explodes_only_once()
        {
            var mine = new TimeMineModel(0f, 4f);
            Assert.That(mine.Tick(0.1f, true), Is.True);
            Assert.That(mine.Tick(0.1f, true), Is.False);
            Assert.That(mine.Tick(10f, false), Is.False);
        }
    }
}
