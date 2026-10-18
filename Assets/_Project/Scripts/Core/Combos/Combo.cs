namespace OneMoreTime.Core.Combos
{
    /// <summary>The temporal combos of GDD 03 that the prototype recognises.</summary>
    public enum ComboType
    {
        Crossfire = 0,
        SynchronizedStrike = 1,
    }

    /// <summary>Who landed a hit, as the combos see it: the ship being flown now, or one of its echoes.</summary>
    public enum Attacker
    {
        Ship = 0,
        Echo = 1,
    }
}
