using System.Collections.Generic;
using OneMoreTime.Gameplay.Language;
using UnityEngine;

namespace OneMoreTime.Gameplay.Session
{
    /// <summary>
    /// The texts and picture of one mission: its card at the start of the stage ("Missão Nº 1", the place, the
    /// name), its slot on the mission map, and the story that scrolls before the stage. The words live in the
    /// string table (Localization/Game): the <c>*Key</c> fields name the entries, and the plain text fields are
    /// only the fallback for a mission without keys. Nothing here is read by the combat code. The text of the first mission is a draft
    /// from the premise in the art bible and still needs the owner's approval.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Time/Mission", fileName = "Mission_01")]
    public sealed class MissionData : ScriptableObject
    {
        [Min(1)] public int number = 1;

        [Header("Mission card (shown when the stage starts)")]
        [Tooltip("Where it happens, like the planet under \"Mission No. 2\".")]
        public string place = "Earth · Arid Desert";
        [Tooltip("The mission's name.")]
        public string title = "Overthrow the Empire";
        [Tooltip("Key in the Game string table for the place. Empty: use the plain text above.")]
        public string placeKey = "";
        [Tooltip("Key in the Game string table for the mission's name. Empty: use the plain text above.")]
        public string titleKey = "";

        [Header("Mission map")]
        [Tooltip("Two or three letters inside the node of the map.")]
        public string mapCode = "SE";

        [Header("Story before the stage")]
        [Tooltip("Fallback text, one entry per paragraph, for a mission without keys.")]
        [TextArea(3, 8)] public string[] story = System.Array.Empty<string>();
        [Tooltip("Keys in the Game string table, one per paragraph; they scroll up in order. Empty: use the plain text above.")]
        public string[] storyKeys = System.Array.Empty<string>();
        [Tooltip("The small picture above the scrolling text.")]
        public Sprite storyPicture;

        [Header("Scene")]
        [Tooltip("Name of the scene to load (it must be in the build settings).")]
        public string sceneName = "Arena_01";

        /// <summary>The place in the current language.</summary>
        public string Place => Localized(placeKey, place);

        /// <summary>The mission's name in the current language.</summary>
        public string Title => Localized(titleKey, title);

        /// <summary>The paragraphs of the story in the current language.</summary>
        public IReadOnlyList<string> Story
        {
            get
            {
                if (storyKeys == null || storyKeys.Length == 0)
                    return story ?? System.Array.Empty<string>();
                var paragraphs = new List<string>(storyKeys.Length);
                foreach (var key in storyKeys)
                    paragraphs.Add(L10n.Get(key));
                return paragraphs;
            }
        }

        static string Localized(string key, string fallback) => string.IsNullOrEmpty(key) ? fallback : L10n.Get(key);
    }
}
