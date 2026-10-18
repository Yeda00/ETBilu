using OneMoreTime.Core.Audio;
using UnityEngine;

namespace OneMoreTime.Gameplay.Audio
{
    /// <summary>
    /// The music of each encounter: an optional intro followed by a seamless loop (scheduled on the audio
    /// clock, so there is no gap). A change fades the old track out and the new one in; pausing ducks it.
    /// Runs on unscaled time, so it keeps working while the game is paused.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        [SerializeField] AudioLibrary library;
        [SerializeField, Range(0f, 1f)] float masterVolume = 0.7f;
        [SerializeField, Min(0.05f)] float fadeOutSeconds = 0.5f;
        [SerializeField, Min(0.05f)] float fadeInSeconds = 0.6f;
        [Tooltip("Level of the music while the game is paused.")]
        [SerializeField, Range(0f, 1f)] float pausedLevel = 0.25f;

        readonly VolumeFader fade = new(0f);
        float level = 1f;   // the player's music slider, 0 to 1
        readonly VolumeFader duck = new(1f);
        AudioSource introSource;
        AudioSource loopSource;
        AudioLibrary.MusicEntry current;
        AudioLibrary.MusicEntry pending;
        float impactDuck = 1f;
        float impactDuckUntil;

        public AudioLibrary.MusicEntry Current => current;

        void Awake()
        {
            level = SoundLevelsStore.Load().Gain(SoundChannel.Music);
            introSource = CreateSource("Intro");
            loopSource = CreateSource("Loop");
            loopSource.loop = true;
        }

        AudioSource CreateSource(string objectName)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        /// <summary>Starts a track. The same track keeps playing; no entry means silence.</summary>
        public void Play(MusicTrack track) => pending = library != null ? library.FindMusic(track) : null;

        /// <summary>The player's music slider as a 0-to-1 gain; applies at once, so the sound screen can be heard.</summary>
        public void SetLevel(float gain) => level = Mathf.Clamp01(gain);

        public void Stop() => pending = null;

        public void SetPaused(bool paused) => duck.Target = paused ? pausedLevel : 1f;

        /// <summary>Makes a major SFX cut through the mix, then quickly restores the music.</summary>
        public void PulseDuck(float level = 0.34f, float holdSeconds = 0.08f)
        {
            impactDuck = Mathf.Min(impactDuck, Mathf.Clamp01(level));
            impactDuckUntil = Mathf.Max(impactDuckUntil, Time.unscaledTime + Mathf.Max(0f, holdSeconds));
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool switching = !ReferenceEquals(pending, current);
            fade.Target = switching || current == null ? 0f : 1f;
            fade.Step(dt, fade.Target > fade.Value ? fadeInSeconds : fadeOutSeconds);

            if (switching && fade.Value <= 0.001f)
            {
                StopSources();
                current = pending;
                if (current != null)
                    StartSources(current);
            }

            duck.Step(dt, 0.15f);
            if (Time.unscaledTime >= impactDuckUntil)
                impactDuck = Mathf.MoveTowards(impactDuck, 1f, dt / 0.22f);
            float volume = current != null ? masterVolume * level * current.volume * fade.Value * duck.Value * impactDuck : 0f;
            introSource.volume = volume;
            loopSource.volume = volume;
        }

        void StartSources(AudioLibrary.MusicEntry entry)
        {
            double start = AudioSettings.dspTime + 0.05;
            loopSource.clip = entry.loop;
            if (entry.intro != null)
            {
                introSource.clip = entry.intro;
                introSource.loop = false;
                introSource.PlayScheduled(start);
                loopSource.PlayScheduled(start + (double)entry.intro.samples / entry.intro.frequency);
            }
            else
                loopSource.PlayScheduled(start);
            fade.Target = 1f;
        }

        void StopSources()
        {
            introSource.Stop();
            loopSource.Stop();
        }
    }
}
