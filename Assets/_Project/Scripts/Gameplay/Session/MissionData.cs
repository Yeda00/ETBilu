using UnityEngine;

namespace OneMoreTime.Gameplay.Session
{
    /// <summary>
    /// The texts and picture of one mission: its card at the start of the stage ("Missão Nº 1", the place, the
    /// name), its slot on the mission map, and the story that scrolls before the stage. Edit the asset to
    /// change the words; nothing here is read by the combat code. The text of the first mission is a draft
    /// from the premise in the art bible and still needs the owner's approval.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Time/Mission", fileName = "Mission_01")]
    public sealed class MissionData : ScriptableObject
    {
        [Min(1)] public int number = 1;

        [Header("Mission card (shown when the stage starts)")]
        [Tooltip("Where it happens, like the planet under \"Mission No. 2\".")]
        public string place = "Terra · Sertão Seco";
        [Tooltip("The mission's name.")]
        public string title = "Derrubar o império";

        [Header("Mission map")]
        [Tooltip("Two or three letters inside the node of the map.")]
        public string mapCode = "SE";

        [Header("Story before the stage")]
        [Tooltip("One entry per paragraph; they scroll up in order.")]
        [TextArea(3, 8)] public string[] story = System.Array.Empty<string>();
        [Tooltip("The small picture above the scrolling text.")]
        public Sprite storyPicture;

        [Header("Scene")]
        [Tooltip("Name of the scene to load (it must be in the build settings).")]
        public string sceneName = "Arena_01";
    }
}
