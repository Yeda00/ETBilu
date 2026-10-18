namespace OneMoreTime.Core.Menu
{
    /// <summary>The highlighted entry of a list of buttons (or the slots of the mission map).</summary>
    public sealed class MenuCursor
    {
        public int Count { get; }
        public int Index { get; private set; }

        public MenuCursor(int count, int start = 0)
        {
            Count = count < 0 ? 0 : count;
            Index = Count == 0 ? 0 : Clamp(start);
        }

        /// <summary>Moves by <paramref name="delta"/>. Wraps around when <paramref name="wrap"/>; returns false if it did not move.</summary>
        public bool Move(int delta, bool wrap = true)
        {
            if (Count <= 1 || delta == 0)
                return false;
            int next = Index + delta;
            if (wrap)
                next = ((next % Count) + Count) % Count;
            else
                next = Clamp(next);
            if (next == Index)
                return false;
            Index = next;
            return true;
        }

        public void Set(int index)
        {
            if (Count > 0)
                Index = Clamp(index);
        }

        int Clamp(int value) => value < 0 ? 0 : value >= Count ? Count - 1 : value;
    }
}
