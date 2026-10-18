using System;

namespace OneMoreTime.Core.Menu
{
    /// <summary>
    /// The scrolling story text: it enters below the viewport, climbs at a steady speed and is done when its last
    /// line has left through the top. Positions are in pixels, from the top of the viewport.
    /// </summary>
    public sealed class CrawlScroll
    {
        readonly float viewportHeight;
        readonly float contentHeight;
        readonly float speed;
        readonly float fastMultiplier;

        public CrawlScroll(float viewportHeight, float contentHeight, float speed = 48f, float fastMultiplier = 4f)
        {
            this.viewportHeight = Math.Max(0f, viewportHeight);
            this.contentHeight = Math.Max(0f, contentHeight);
            this.speed = Math.Max(1f, speed);
            this.fastMultiplier = Math.Max(1f, fastMultiplier);
        }

        /// <summary>How far the text has climbed since it started, in pixels.</summary>
        public float Offset { get; private set; }

        /// <summary>Distance the text must climb in all: from fully below the viewport to fully above it.</summary>
        public float TotalDistance => viewportHeight + contentHeight;

        /// <summary>Y of the text's top edge, from the viewport's top. Starts at the bottom edge.</summary>
        public float TopY => viewportHeight - Offset;

        public float Progress => TotalDistance <= 0f ? 1f : Math.Min(1f, Offset / TotalDistance);

        public bool IsFinished => Offset >= TotalDistance;

        public void Advance(float dt, bool fast)
        {
            if (dt <= 0f || IsFinished)
                return;
            Offset = Math.Min(TotalDistance, Offset + dt * speed * (fast ? fastMultiplier : 1f));
        }
    }
}
