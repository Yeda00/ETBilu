using System;

namespace OneMoreTime.Core.Boss
{
    /// <summary>Phases of the Guardian of the Cycle implemented in the prototype (GDD 06, phases 1–3).</summary>
    public enum GuardianPhase
    {
        Survival = 0,
        Preparation = 1,
        Synchronization = 2,
        Defeated = 3,
    }

    /// <summary>
    /// Rules of the Guardian of the Cycle (GDD 06), without the scene. The core is armoured until both
    /// generators fall inside one window; then it is vulnerable for a while and the generators come back.
    /// <list type="bullet">
    /// <item><b>Survival</b>: everything is shielded for a fixed time.</item>
    /// <item><b>Preparation</b>: generators are exposed and the window is generous. The first opening of
    /// the core ends the phase.</item>
    /// <item><b>Synchronization</b>: same rule with a short window, until the core falls.</item>
    /// </list>
    /// A generator left standing when the window closes brings the fallen one back.
    /// </summary>
    public sealed class GuardianModel
    {
        public const int GeneratorCount = 2;

        readonly float survivalSeconds, preparationWindow, syncWindow, vulnerableSeconds;
        readonly bool[] up = new bool[GeneratorCount];

        public GuardianModel(float survivalSeconds, float preparationWindow, float syncWindow, float vulnerableSeconds)
        {
            this.survivalSeconds = Math.Max(0f, survivalSeconds);
            this.preparationWindow = Math.Max(0.01f, preparationWindow);
            this.syncWindow = Math.Max(0.01f, syncWindow);
            this.vulnerableSeconds = Math.Max(0.01f, vulnerableSeconds);
            RebuildGenerators();
        }

        public GuardianPhase Phase { get; private set; } = GuardianPhase.Survival;
        /// <summary>Seconds since the current phase began.</summary>
        public float PhaseTime { get; private set; }
        /// <summary>Time left for the remaining generator to fall; 0 when no window is open.</summary>
        public float WindowRemaining { get; private set; }
        public float VulnerableRemaining { get; private set; }

        public bool IsDefeated => Phase == GuardianPhase.Defeated;
        public bool CoreVulnerable => VulnerableRemaining > 0f;
        public bool GeneratorsShielded => Phase == GuardianPhase.Survival;
        /// <summary>Length of the window in the current phase.</summary>
        public float Window => Phase == GuardianPhase.Preparation ? preparationWindow : syncWindow;

        public bool IsGeneratorUp(int index) => up[index];

        public int GeneratorsUp
        {
            get
            {
                int n = 0;
                foreach (var g in up)
                    if (g) n++;
                return n;
            }
        }

        public void Tick(float deltaTime)
        {
            if (IsDefeated || deltaTime <= 0f)
                return;
            PhaseTime += deltaTime;

            if (Phase == GuardianPhase.Survival)
            {
                if (PhaseTime >= survivalSeconds)
                    Enter(GuardianPhase.Preparation);
                return;
            }

            if (CoreVulnerable)
            {
                VulnerableRemaining = Math.Max(0f, VulnerableRemaining - deltaTime);
                if (!CoreVulnerable)
                {
                    RebuildGenerators();
                    if (Phase == GuardianPhase.Preparation)
                        Enter(GuardianPhase.Synchronization);
                }
            }
            else if (WindowRemaining > 0f)
            {
                WindowRemaining = Math.Max(0f, WindowRemaining - deltaTime);
                if (WindowRemaining <= 0f)
                    RebuildGenerators();   // the window closed with a generator still standing
            }
        }

        /// <summary>
        /// A generator was shot down. Returns false when it does not count: shielded, already down,
        /// the core already open, or the fight over.
        /// </summary>
        public bool DestroyGenerator(int index)
        {
            if (index < 0 || index >= GeneratorCount || !up[index])
                return false;
            if (IsDefeated || GeneratorsShielded || CoreVulnerable)
                return false;

            up[index] = false;
            if (GeneratorsUp > 0)
            {
                if (WindowRemaining <= 0f)
                    WindowRemaining = Window;
                return true;
            }

            WindowRemaining = 0f;
            VulnerableRemaining = vulnerableSeconds;
            return true;
        }

        /// <summary>The core's health ran out. Only counts while it is vulnerable.</summary>
        public bool DefeatCore()
        {
            if (!CoreVulnerable)
                return false;
            VulnerableRemaining = 0f;
            WindowRemaining = 0f;
            Enter(GuardianPhase.Defeated);
            return true;
        }

        void Enter(GuardianPhase phase)
        {
            Phase = phase;
            PhaseTime = 0f;
        }

        void RebuildGenerators()
        {
            for (int i = 0; i < up.Length; i++)
                up[i] = true;
            WindowRemaining = 0f;
        }
    }
}
