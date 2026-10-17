namespace OneMoreTime.Gameplay.Combat
{
    public enum Faction
    {
        Player = 0,
        Enemy = 1,
    }

    /// <summary>Who fired a shot. Echo shots count as the player's, but combos need to tell them apart (GDD 03).</summary>
    public enum ShotSource
    {
        Ship = 0,
        Echo = 1,
        Enemy = 2,
    }

    /// <summary>Damage categories of the prototype (GDD 03).</summary>
    public enum DamageKind
    {
        Direct = 0,
        Gadget = 1,
        Combined = 2,
        Contact = 3,
    }

    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly DamageKind Kind;
        public readonly ShotSource Source;

        public DamageInfo(int amount, DamageKind kind, ShotSource source)
        {
            Amount = amount;
            Kind = kind;
            Source = source;
        }
    }
}
