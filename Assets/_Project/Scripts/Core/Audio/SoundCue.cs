namespace OneMoreTime.Core.Audio
{
    /// <summary>
    /// Every sound the game asks for, by what happened and not by which clip plays (GDD 08). The
    /// <c>AudioLibrary</c> maps each cue to clips, so the sound design changes without touching gameplay.
    /// </summary>
    public enum SoundCue
    {
        None = 0,

        // Shots
        ShipShot,
        EchoShot,
        EnemyShot,

        // Hits and deaths
        EnemyHit,
        ShieldBlock,
        PlayerHit,
        EnemyDeathSmall,
        EnemyDeathBig,
        GeneratorDeath,
        CoreDeath,
        PlayerDeath,

        // Gadget
        MineDrop,
        MineBlast,

        // The echo
        RecordStart,
        RecordStop,
        EchoInvoke,
        EchoArrive,
        Combo,

        // Boss and session
        BossVulnerable,
        BossPhase,
        // 21-23 were the switch and corridor sounds, gone with the traps. The numbers below stay as they were,
        // because the AudioLibrary asset stores cues by number.
        Pause = 24,
        Resume,
        Victory,

        // Menus (appended: the library stores cues by number)
        MenuMove,
        MenuSelect,
        MenuBack,
        MenuStart,
        MenuDenied,
        MissionCard,
    }
}
