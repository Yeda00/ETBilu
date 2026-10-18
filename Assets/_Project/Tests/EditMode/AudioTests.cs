using NUnit.Framework;
using OneMoreTime.Core.Audio;

namespace OneMoreTime.Tests
{
    public class CueThrottleTests
    {
        const int Cue = (int)SoundCue.EnemyHit;

        [Test]
        public void Blocks_a_repeat_inside_the_minimum_interval()
        {
            var throttle = new CueThrottle(8);
            Assert.That(throttle.TryAcquire(Cue, 0f, 0.05f, 0, 0.1f), Is.True);
            Assert.That(throttle.TryAcquire(Cue, 0.03f, 0.05f, 0, 0.1f), Is.False);
            Assert.That(throttle.TryAcquire(Cue, 0.06f, 0.05f, 0, 0.1f), Is.True);
        }

        [Test]
        public void Caps_the_voices_alive_at_once_and_frees_them_when_they_end()
        {
            var throttle = new CueThrottle(8);
            Assert.That(throttle.TryAcquire(Cue, 0f, 0f, 2, 1f), Is.True);
            Assert.That(throttle.TryAcquire(Cue, 0.1f, 0f, 2, 1f), Is.True);
            Assert.That(throttle.TryAcquire(Cue, 0.2f, 0f, 2, 1f), Is.False, "two voices are still playing");
            Assert.That(throttle.TryAcquire(Cue, 1.05f, 0f, 2, 1f), Is.True, "the first one ended at 1.0");
        }

        [Test]
        public void Zero_voices_means_no_limit_and_cues_do_not_share_state()
        {
            var throttle = new CueThrottle(8);
            for (int i = 0; i < 20; i++)
                Assert.That(throttle.TryAcquire(Cue, i * 0.01f, 0f, 0, 5f), Is.True);
            Assert.That(throttle.TryAcquire((int)SoundCue.PlayerHit, 0f, 0f, 1, 1f), Is.True);
        }

        [Test]
        public void Unknown_cue_is_refused()
        {
            var throttle = new CueThrottle(4);
            Assert.That(throttle.TryAcquire(-1, 0f, 0f, 0, 1f), Is.False);
            Assert.That(throttle.TryAcquire(4, 0f, 0f, 0, 1f), Is.False);
        }
    }

    public class RecordPulseTests
    {
        [Test]
        public void Beats_come_faster_as_the_recording_fills()
        {
            Assert.That(CountBeats(0f), Is.LessThan(CountBeats(1f)));
        }

        [Test]
        public void First_beat_is_immediate_and_a_long_frame_gives_only_one()
        {
            var pulse = new RecordPulse();
            pulse.Begin();
            Assert.That(pulse.Tick(0f, 0f), Is.True);
            Assert.That(pulse.Tick(5f, 0f), Is.True);
            Assert.That(pulse.Tick(0f, 0f), Is.False);
        }

        [Test]
        public void Pitch_rises_with_progress_and_is_clamped()
        {
            var pulse = new RecordPulse(lowPitch: 0.8f, highPitch: 1.9f);
            Assert.That(pulse.PitchAt(0f), Is.EqualTo(0.8f).Within(1e-4f));
            Assert.That(pulse.PitchAt(1f), Is.EqualTo(1.9f).Within(1e-4f));
            Assert.That(pulse.PitchAt(2f), Is.EqualTo(1.9f).Within(1e-4f));
            Assert.That(pulse.PitchAt(0.5f), Is.GreaterThan(pulse.PitchAt(0.1f)));
        }

        static int CountBeats(float progress)
        {
            var pulse = new RecordPulse();
            pulse.Begin();
            int beats = 0;
            for (int i = 0; i < 100; i++)   // one second at 100 Hz
                if (pulse.Tick(0.01f, progress))
                    beats++;
            return beats;
        }
    }

    public class VolumeFaderTests
    {
        [Test]
        public void Moves_toward_the_target_at_a_fixed_speed_and_stops_on_it()
        {
            var fader = new VolumeFader(0f) { Target = 1f };
            fader.Step(0.25f, 1f);
            Assert.That(fader.Value, Is.EqualTo(0.25f).Within(1e-4f));
            fader.Step(5f, 1f);
            Assert.That(fader.Value, Is.EqualTo(1f));
            fader.Target = 0.4f;
            fader.Step(0.3f, 1f);
            Assert.That(fader.Value, Is.EqualTo(0.7f).Within(1e-4f));
        }

        [Test]
        public void Zero_duration_jumps_straight_to_the_target()
        {
            var fader = new VolumeFader(1f) { Target = 0f };
            fader.Step(0.01f, 0f);
            Assert.That(fader.Value, Is.Zero);
        }
    }
}

namespace OneMoreTime.Tests
{
    public class SoundLevelsTests
    {
        [Test]
        public void Steps_are_clamped_and_gain_is_a_fraction_of_the_maximum()
        {
            var levels = new SoundLevels(7, 8);
            Assert.That(levels.Gain(SoundChannel.Music), Is.EqualTo(0.7f).Within(1e-4f));
            Assert.That(levels.Adjust(SoundChannel.Music, 5), Is.True);
            Assert.That(levels.Get(SoundChannel.Music), Is.EqualTo(SoundLevels.MaxStep));
            Assert.That(levels.Adjust(SoundChannel.Music, 1), Is.False, "already at the end");
            levels.Set(SoundChannel.Effects, -4);
            Assert.That(levels.Gain(SoundChannel.Effects), Is.Zero);
        }

        [Test]
        public void Channels_are_independent()
        {
            var levels = new SoundLevels(3, 3);
            levels.Adjust(SoundChannel.Effects, 2);
            Assert.That(levels.Get(SoundChannel.Music), Is.EqualTo(3));
            Assert.That(levels.Get(SoundChannel.Effects), Is.EqualTo(5));
        }
    }
}
