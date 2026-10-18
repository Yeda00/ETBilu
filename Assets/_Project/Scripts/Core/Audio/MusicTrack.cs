namespace OneMoreTime.Core.Audio
{
    /// <summary>
    /// Every piece of music the game asks for. The first three keep the values of the old per-encounter entries
    /// (waves, traps, boss) so an existing <c>AudioLibrary</c> keeps working.
    /// </summary>
    public enum MusicTrack
    {
        None = -1,
        Waves = 0,
        Traps = 1,
        Boss = 2,
        Title = 3,
        Missions = 4,
        Intro = 5,
    }
}
