using NUnit.Framework;
using OneMoreTime.Core.Menu;

namespace OneMoreTime.Tests
{
    public class ScreenFlowTests
    {
        [Test]
        public void Back_returns_through_the_screens_in_reverse_and_stops_at_the_first()
        {
            var flow = new ScreenFlow(MenuScreen.Title);
            flow.Go(MenuScreen.Main);
            flow.Go(MenuScreen.Sound);
            Assert.That(flow.Current, Is.EqualTo(MenuScreen.Sound));
            Assert.That(flow.Back(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(MenuScreen.Main));
            Assert.That(flow.Back(), Is.True);
            Assert.That(flow.Back(), Is.False, "nothing before the title");
            Assert.That(flow.Current, Is.EqualTo(MenuScreen.Title));
        }

        [Test]
        public void Going_to_the_current_screen_does_not_add_history_and_reset_clears_it()
        {
            var flow = new ScreenFlow(MenuScreen.Main);
            flow.Go(MenuScreen.Main);
            Assert.That(flow.Back(), Is.False);
            flow.Go(MenuScreen.Missions);
            flow.Reset(MenuScreen.Main);
            Assert.That(flow.Back(), Is.False);
        }
    }

    public class MenuCursorTests
    {
        [Test]
        public void Wraps_around_both_ends()
        {
            var cursor = new MenuCursor(4);
            Assert.That(cursor.Move(-1), Is.True);
            Assert.That(cursor.Index, Is.EqualTo(3));
            Assert.That(cursor.Move(1), Is.True);
            Assert.That(cursor.Index, Is.Zero);
        }

        [Test]
        public void Without_wrap_it_stops_at_the_ends_and_reports_no_move()
        {
            var cursor = new MenuCursor(3, 2);
            Assert.That(cursor.Move(1, wrap: false), Is.False);
            Assert.That(cursor.Index, Is.EqualTo(2));
            Assert.That(cursor.Move(-1, wrap: false), Is.True);
        }

        [Test]
        public void A_list_of_one_or_none_never_moves()
        {
            Assert.That(new MenuCursor(1).Move(1), Is.False);
            var empty = new MenuCursor(0);
            Assert.That(empty.Move(1), Is.False);
            empty.Set(5);
            Assert.That(empty.Index, Is.Zero);
        }

        [Test]
        public void Set_clamps_to_the_list()
        {
            var cursor = new MenuCursor(5);
            cursor.Set(99);
            Assert.That(cursor.Index, Is.EqualTo(4));
            cursor.Set(-3);
            Assert.That(cursor.Index, Is.Zero);
        }
    }

    public class CrawlScrollTests
    {
        [Test]
        public void Starts_at_the_bottom_edge_and_finishes_once_the_text_left_through_the_top()
        {
            var crawl = new CrawlScroll(viewportHeight: 300f, contentHeight: 500f, speed: 100f);
            Assert.That(crawl.TopY, Is.EqualTo(300f));
            Assert.That(crawl.IsFinished, Is.False);
            crawl.Advance(7.9f, false);
            Assert.That(crawl.IsFinished, Is.False);
            crawl.Advance(0.2f, false);   // 800 px in all: (300 + 500) / 100 s
            Assert.That(crawl.IsFinished, Is.True);
            Assert.That(crawl.TopY, Is.EqualTo(-500f));
            Assert.That(crawl.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void Fast_mode_multiplies_the_speed()
        {
            var normal = new CrawlScroll(100f, 100f, 50f, 4f);
            var fast = new CrawlScroll(100f, 100f, 50f, 4f);
            normal.Advance(1f, false);
            fast.Advance(1f, true);
            Assert.That(fast.Offset, Is.EqualTo(normal.Offset * 4f).Within(1e-3f));
        }

        [Test]
        public void Never_overshoots_the_end()
        {
            var crawl = new CrawlScroll(10f, 10f, 100f);
            crawl.Advance(60f, true);
            Assert.That(crawl.Offset, Is.EqualTo(crawl.TotalDistance));
        }
    }
}
