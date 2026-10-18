using UnityEngine;

namespace OneMoreTime.Gameplay.Session
{
    /// <summary>
    /// The encounter to resume after a death reloads the scene (GDD 06, model A: "a morte reinicia o
    /// encontro"). A plain value that outlives the reload; cleared whenever Play mode starts.
    /// </summary>
    public static class RunCheckpoint
    {
        static StageSegment? saved;

        public static void Save(StageSegment segment) => saved = segment;

        /// <summary>True after an encounter began in this run: a reload after a death, not a fresh start.</summary>
        public static bool HasSaved => saved.HasValue;

        /// <summary>The saved encounter, or <paramref name="fallback"/> if it comes later.</summary>
        public static StageSegment Resume(StageSegment fallback) =>
            saved.HasValue && saved.Value > fallback ? saved.Value : fallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => saved = null;
    }
}
