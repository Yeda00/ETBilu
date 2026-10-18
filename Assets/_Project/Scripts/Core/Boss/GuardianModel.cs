using System;

namespace OneMoreTime.Core.Boss
{
    /// <summary>The Guardian's breakable parts and weak points (GDD 06).</summary>
    public enum GuardianPart
    {
        /// <summary>Calls reinforcement fighters (S). Without it the Guardian fires faster.</summary>
        Antenna = 0,
        /// <summary>Calls reinforcement gunships (M). Without them the Guardian gains a new attack and its upper teeth open.</summary>
        Eyes = 1,
        /// <summary>The upper arm cannon. Without both cannons the Guardian moves faster and hits harder, and its lower teeth open.</summary>
        CannonUpper = 2,
        /// <summary>The lower arm cannon.</summary>
        CannonLower = 3,
        /// <summary>A weak point: only reachable once the eyes are gone.</summary>
        TeethUpper = 4,
        /// <summary>A weak point: only reachable once both cannons are gone.</summary>
        TeethLower = 5,
    }

    /// <summary>Phases of the Guardian of the Cycle (GDD 06).</summary>
    public enum GuardianPhase
    {
        /// <summary>Everything is shielded while the player learns the patterns.</summary>
        Survival = 0,
        /// <summary>The parts are exposed and come off one by one; the teeth finish it.</summary>
        Dismantle = 1,
        Defeated = 2,
    }

    /// <summary>
    /// Rules of the Guardian of the Cycle (GDD 06), without the scene. After the opening Survival phase its parts
    /// can be shot off in any order and never come back. What each one does when it goes:
    /// <list type="bullet">
    /// <item><b>Antenna</b>: no more fighter reinforcements; the Guardian fires faster (<see cref="Frenzied"/>).</item>
    /// <item><b>Eyes</b>: no more gunship reinforcements; a new attack (<see cref="EyeAttack"/>); the upper teeth open.</item>
    /// <item><b>Cannons</b>: each one makes it faster and more aggressive (<see cref="Fury"/>); both open the lower teeth.</item>
    /// <item><b>Teeth</b>: the weak points. Both gone: the Guardian is defeated.</item>
    /// </list>
    /// </summary>
    public sealed class GuardianModel
    {
        public const int PartCount = 6;

        readonly float survivalSeconds;
        readonly bool[] down = new bool[PartCount];

        public GuardianModel(float survivalSeconds)
        {
            this.survivalSeconds = Math.Max(0f, survivalSeconds);
        }

        public GuardianPhase Phase { get; private set; } = GuardianPhase.Survival;
        /// <summary>Seconds since the current phase began.</summary>
        public float PhaseTime { get; private set; }

        public bool IsDefeated => Phase == GuardianPhase.Defeated;
        public bool AntennaDown => down[(int)GuardianPart.Antenna];
        public bool EyesDown => down[(int)GuardianPart.Eyes];
        /// <summary>How many of the two arm cannons are gone (0–2).</summary>
        public int CannonsDown => (down[(int)GuardianPart.CannonUpper] ? 1 : 0) + (down[(int)GuardianPart.CannonLower] ? 1 : 0);

        /// <summary>The antenna still calls fighters (S), once the fight is on.</summary>
        public bool CallsFighters => Phase == GuardianPhase.Dismantle && !AntennaDown;
        /// <summary>The eyes still call gunships (M), once the fight is on.</summary>
        public bool CallsGunships => Phase == GuardianPhase.Dismantle && !EyesDown;
        /// <summary>The antenna is gone: it fires more often.</summary>
        public bool Frenzied => AntennaDown;
        /// <summary>The eyes are gone: it has a new attack.</summary>
        public bool EyeAttack => Phase == GuardianPhase.Dismantle && EyesDown;
        /// <summary>0–2: the more cannons are gone, the faster it moves and the denser its patterns.</summary>
        public int Fury => CannonsDown;

        public bool IsDestroyed(GuardianPart part) => down[(int)part];

        /// <summary>Whether a shot can hurt the part now: the fight is on, it still stands, and what guards it is gone.</summary>
        public bool IsExposed(GuardianPart part)
        {
            if (Phase != GuardianPhase.Dismantle || down[(int)part])
                return false;
            return part switch
            {
                GuardianPart.TeethUpper => EyesDown,
                GuardianPart.TeethLower => CannonsDown == 2,
                _ => true,
            };
        }

        public void Tick(float deltaTime)
        {
            if (IsDefeated || deltaTime <= 0f)
                return;
            PhaseTime += deltaTime;
            if (Phase == GuardianPhase.Survival && PhaseTime >= survivalSeconds)
                Enter(GuardianPhase.Dismantle);
        }

        /// <summary>
        /// A part was shot off. Returns false when it does not count: shielded, guarded, already down, or the fight
        /// is over. Both teeth gone defeats the Guardian.
        /// </summary>
        public bool Destroy(GuardianPart part)
        {
            if (!IsExposed(part))
                return false;
            down[(int)part] = true;
            if (down[(int)GuardianPart.TeethUpper] && down[(int)GuardianPart.TeethLower])
                Enter(GuardianPhase.Defeated);
            return true;
        }

        void Enter(GuardianPhase phase)
        {
            Phase = phase;
            PhaseTime = 0f;
        }
    }
}
