using OneMoreTime.Gameplay.Audio;
using OneMoreTime.Core.Audio;
using OneMoreTime.Gameplay.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneMoreTime.UI
{
    /// <summary>
    /// The card at the start of a stage: "Missão Nº 1", the place and the mission's name, on black, like the
    /// stage intros of Star Fox 64. The stage waits behind it (it is the <see cref="StageGate"/>) and starts as
    /// the card fades out. It does not show again after a death, when the scene reloads in the middle of a run.
    /// The mission is the one picked on the menu, or <c>fallbackMission</c> when the scene is opened directly.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MissionCardController : StageGate
    {
        [SerializeField] MenuTexts texts;
        [SerializeField] MissionData fallbackMission;
        [SerializeField] SfxPlayer sfx;
        [Header("Timing (seconds)")]
        [SerializeField, Min(0f)] float lineDelay = 0.5f;
        [SerializeField, Min(0.05f)] float lineFade = 0.35f;
        [SerializeField, Min(0f)] float holdSeconds = 1.6f;
        [SerializeField, Min(0.05f)] float fadeOutSeconds = 0.6f;

        VisualElement card;
        Label number, place, title;
        float age;
        bool showing;
        bool open;

        public override bool IsOpen => open;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            card = root.Q("card");
            number = root.Q<Label>("card-number");
            place = root.Q<Label>("card-place");
            title = root.Q<Label>("card-title");

            var mission = MissionSelection.Current != null ? MissionSelection.Current : fallbackMission;
            if (card == null || mission == null || RunCheckpoint.HasSaved)
            {
                // A fresh start without a mission, or a reload after a death: no card.
                if (card != null)
                    card.AddToClassList("hidden");
                open = true;
                return;
            }

            string format = texts != null ? texts.missionNumberFormat : "MISSÃO Nº {0}";
            number.text = string.Format(format, mission.number);
            place.text = mission.place;
            title.text = mission.title;
            card.RemoveFromClassList("hidden");
            card.style.opacity = 1f;
            age = 0f;
            showing = true;
            open = false;
        }

        void Start()
        {
            if (showing && sfx != null)
                sfx.Play(SoundCue.MissionCard, Vector3.zero);
        }

        void Update()
        {
            if (!showing)
                return;

            age += Time.unscaledDeltaTime;
            number.style.opacity = Fade(age, 0.2f);
            place.style.opacity = Fade(age, 0.2f + lineDelay);
            title.style.opacity = Fade(age, 0.2f + lineDelay * 2f);

            float fadeStart = 0.2f + lineDelay * 2f + lineFade + holdSeconds;
            if (age >= fadeStart)
            {
                open = true;   // the stage starts as the card fades
                float k = Mathf.Clamp01((age - fadeStart) / fadeOutSeconds);
                card.style.opacity = 1f - k;
                if (k >= 1f)
                {
                    card.AddToClassList("hidden");
                    showing = false;
                }
            }
        }

        float Fade(float time, float start) => Mathf.Clamp01((time - start) / lineFade);
    }
}
