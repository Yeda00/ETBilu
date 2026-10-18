using System.Collections.Generic;
using NUnit.Framework;
using OneMoreTime.Core.Echo;
using UnityEngine;

namespace OneMoreTime.Tests
{
    public class EchoRecorderTests
    {
        const float Rate = 20f;

        [Test]
        public void Samples_at_fixed_rate_not_per_frame()
        {
            var rec = new EchoRecorder(5f, Rate);
            rec.Begin(Vector2.zero, 0f);
            for (int i = 0; i < 60; i++)            // 1 s at 60 fps
                rec.Tick(1f / 60f, new Vector2(i, 0f), 0f);
            var r = rec.End();

            // t = 0 plus one sample every 0.05 s up to 1 s → 21 samples.
            Assert.That(r.Samples.Count, Is.EqualTo(21));
            Assert.That(r.Duration, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Stops_growing_at_max_duration()
        {
            var rec = new EchoRecorder(5f, Rate);
            rec.Begin(Vector2.zero, 0f);
            for (int i = 0; i < 70; i++)
                rec.Tick(0.1f, Vector2.zero, 0f);    // 7 s of input

            Assert.That(rec.IsFull, Is.True);
            Assert.That(rec.Elapsed, Is.EqualTo(5f).Within(1e-4f));
            rec.AddEvent(EchoEventType.Shot, Vector2.zero, Vector2.right);
            Assert.That(rec.End().Events.Count, Is.Zero, "events after the 5 s window are dropped");
        }

        [Test]
        public void Events_keep_their_instant()
        {
            var rec = new EchoRecorder(5f, Rate);
            rec.Begin(Vector2.zero, 0f);
            rec.Tick(0.6f, Vector2.zero, 0f);
            rec.AddEvent(EchoEventType.Shot, new Vector2(1f, 2f), Vector2.right);
            rec.Tick(1f, Vector2.zero, 0f);
            var r = rec.End();

            Assert.That(r.Events.Count, Is.EqualTo(1));
            Assert.That(r.Events[0].time, Is.EqualTo(0.6f).Within(1e-4f));
            Assert.That(r.Events[0].position, Is.EqualTo(new Vector2(1f, 2f)));
        }

        [Test]
        public void Empty_recording_is_discarded()
        {
            var rec = new EchoRecorder(5f, Rate);
            rec.Begin(Vector2.zero, 0f);
            Assert.That(rec.End(), Is.Null);
        }

        [Test]
        public void Aim_direction_is_sampled_and_interpolated()
        {
            var rec = new EchoRecorder(5f, Rate);
            rec.Begin(Vector2.zero, 0f, Vector2.right);
            rec.Tick(1f, Vector2.zero, 0f, Vector2.up);
            var recording = rec.End();

            Vector2 halfway = recording.Evaluate(0.5f).aimDirection;
            Assert.That(halfway.x, Is.EqualTo(0.7071f).Within(1e-3f));
            Assert.That(halfway.y, Is.EqualTo(0.7071f).Within(1e-3f));
        }
    }

    public class EchoPlaybackTests
    {
        static EchoRecording Line(float duration, params float[] eventTimes)
        {
            var samples = new List<EchoSample>
            {
                new EchoSample(0f, Vector2.zero, 0f),
                new EchoSample(duration, new Vector2(10f, 0f), 90f),
            };
            var events = new List<EchoEvent>();
            foreach (var t in eventTimes)
                events.Add(new EchoEvent(t, EchoEventType.Shot, Vector2.zero, Vector2.right));
            return new EchoRecording(samples, events, duration);
        }

        [Test]
        public void Interpolates_between_samples()
        {
            var pose = Line(2f).Evaluate(1f);
            Assert.That(pose.position.x, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(pose.rotation, Is.EqualTo(45f).Within(1e-3f));
        }

        [Test]
        public void Each_event_fires_exactly_once_in_order()
        {
            var play = new EchoPlayback(Line(1f, 0.1f, 0.2f, 0.9f));
            var due = new List<EchoEvent>();

            play.Advance(0.25f, due);               // one long frame crosses two events
            Assert.That(due.Count, Is.EqualTo(2));
            play.Advance(0.25f, due);
            Assert.That(due.Count, Is.EqualTo(2), "nothing new between 0.25 and 0.5");
            play.Advance(10f, due);
            Assert.That(due.Count, Is.EqualTo(3));
            play.Advance(1f, due);
            Assert.That(due.Count, Is.EqualTo(3), "already fired events never repeat");
            Assert.That(play.IsFinished, Is.True);
        }

        [Test]
        public void Event_at_time_zero_fires_on_first_advance()
        {
            var play = new EchoPlayback(Line(1f, 0f));
            var due = new List<EchoEvent>();
            play.Advance(0f, due);
            Assert.That(due.Count, Is.EqualTo(1));
        }
    }
}
