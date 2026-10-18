using System.Collections.Generic;

namespace OneMoreTime.Core.Menu
{
    /// <summary>The screens of the menu scene, in the order the player meets them.</summary>
    public enum MenuScreen
    {
        Title = 0,
        Main = 1,
        Sound = 2,
        Controls = 3,
        Credits = 4,
        Missions = 5,
        Intro = 6,
    }

    /// <summary>Which screen is showing and how to go back to the one before it.</summary>
    public sealed class ScreenFlow
    {
        readonly Stack<MenuScreen> history = new();

        public MenuScreen Current { get; private set; }

        public ScreenFlow(MenuScreen start) => Current = start;

        public void Go(MenuScreen next)
        {
            if (next == Current)
                return;
            history.Push(Current);
            Current = next;
        }

        /// <summary>Returns to the previous screen; false when there is none (the title screen).</summary>
        public bool Back()
        {
            if (history.Count == 0)
                return false;
            Current = history.Pop();
            return true;
        }

        public void Reset(MenuScreen screen)
        {
            history.Clear();
            Current = screen;
        }
    }
}
