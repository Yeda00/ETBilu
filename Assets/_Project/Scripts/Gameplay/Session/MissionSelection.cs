namespace OneMoreTime.Gameplay.Session
{
    /// <summary>
    /// The mission picked on the menu, carried into the stage scene. A plain value that outlives the scene
    /// load; empty when the stage scene is opened directly, and then the scene uses its own default mission.
    /// </summary>
    public static class MissionSelection
    {
        public static MissionData Current { get; private set; }

        public static void Select(MissionData mission) => Current = mission;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear() => Current = null;
    }
}
