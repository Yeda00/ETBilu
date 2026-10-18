using System.Collections.Generic;
using OneMoreTime.Core.Audio;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Audio
{
    /// <summary>
    /// Plays cues from the <see cref="AudioLibrary"/> on a fixed pool of sources, so a bullet-hell never
    /// creates or destroys audio objects. <see cref="CueThrottle"/> decides whether a cue may play now;
    /// this component only picks the clip, pitch, pan and muffling.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField] AudioLibrary library;
        [Tooltip("Pans sounds with their place on the screen. Optional.")]
        [SerializeField] Playfield playfield;
        [SerializeField, Range(0f, 1f)] float masterVolume = 0.8f;
        [SerializeField, Min(4)] int voices = 24;
        [Tooltip("How far a sound at the screen edge pans (0 = centre, 1 = hard left/right).")]
        [SerializeField, Range(0f, 1f)] float panWidth = 0.6f;

        const float OpenFilterHz = 22000f;

        sealed class Voice
        {
            public AudioSource Source;
            public AudioLowPassFilter Filter;
            public float BaseVolume;
            public float StopAt;       // unscaled time; 0 = play to the end
            public float FadeSeconds;
        }

        readonly List<Voice> pool = new();
        readonly Dictionary<SoundCue, int> lastClip = new();
        CueThrottle throttle;
        float level = 1f;   // the player's effects slider, 0 to 1

        public AudioLibrary Library => library;

        /// <summary>The player's effects slider as a 0-to-1 gain.</summary>
        public void SetLevel(float gain) => level = Mathf.Clamp01(gain);

        void Awake()
        {
            level = SoundLevelsStore.Load().Gain(SoundChannel.Effects);
            throttle = new CueThrottle(System.Enum.GetValues(typeof(SoundCue)).Length);
            for (int i = 0; i < voices; i++)
            {
                var go = new GameObject($"Voice {i:00}");
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                var filter = go.AddComponent<AudioLowPassFilter>();
                filter.cutoffFrequency = OpenFilterHz;
                pool.Add(new Voice { Source = source, Filter = filter });
            }
        }

        void Update()
        {
            float now = Time.unscaledTime;
            foreach (var voice in pool)
            {
                if (voice.StopAt <= 0f || !voice.Source.isPlaying)
                    continue;
                float remaining = voice.StopAt - now;
                if (remaining <= 0f)
                {
                    voice.Source.Stop();
                    voice.StopAt = 0f;
                }
                else if (voice.FadeSeconds > 0f && remaining < voice.FadeSeconds)
                    voice.Source.volume = voice.BaseVolume * (remaining / voice.FadeSeconds);
            }
        }

        /// <summary>Plays a cue at a world position (used only for panning). Returns false when it was throttled or has no clip.</summary>
        public bool Play(SoundCue cue, Vector3 worldPosition, float delaySeconds = 0f)
        {
            var entry = library != null ? library.Find(cue) : null;
            if (entry == null || entry.clips == null || entry.clips.Length == 0)
                return false;

            var clip = PickClip(cue, entry);
            if (clip == null)
                return false;

            float pitch = Random.Range(entry.pitchMin, Mathf.Max(entry.pitchMin, entry.pitchMax));
            float duration = clip.length / Mathf.Max(0.1f, pitch);
            if (entry.maxSeconds > 0f)
                duration = Mathf.Min(duration, entry.maxSeconds);

            float now = Time.unscaledTime;
            if (!throttle.TryAcquire((int)cue, now + delaySeconds, entry.minInterval, entry.maxVoices, duration))
                return false;

            var voice = FreeVoice();
            if (voice == null)
                return false;

            float volume = entry.volume * masterVolume * level;
            Begin(voice, clip, volume, pitch, entry.panned ? Pan(worldPosition) : 0f, entry.lowPassHz, delaySeconds);
            voice.StopAt = entry.maxSeconds > 0f ? now + delaySeconds + entry.maxSeconds : 0f;
            voice.FadeSeconds = entry.fadeOutSeconds;
            return true;
        }

        /// <summary>Plays a clip that is not in the library (the recording pulse).</summary>
        public void PlayClip(AudioClip clip, float volume, float pitch, float delaySeconds = 0f, float pan = 0f, float lowPassHz = 0f)
        {
            var voice = clip != null ? FreeVoice() : null;
            if (voice == null)
                return;
            Begin(voice, clip, volume * masterVolume * level, pitch, pan, lowPassHz, delaySeconds);
            voice.StopAt = 0f;
        }

        void Begin(Voice voice, AudioClip clip, float volume, float pitch, float pan, float lowPassHz, float delay)
        {
            var source = voice.Source;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.panStereo = pan;
            voice.BaseVolume = volume;
            voice.Filter.cutoffFrequency = lowPassHz > 0f ? lowPassHz : OpenFilterHz;
            if (delay > 0f)
                source.PlayDelayed(delay);
            else
                source.Play();
        }

        AudioClip PickClip(SoundCue cue, AudioLibrary.CueEntry entry)
        {
            int count = entry.clips.Length;
            int index = Random.Range(0, count);
            if (count > 1 && lastClip.TryGetValue(cue, out int last) && index == last)
                index = (index + 1) % count;
            lastClip[cue] = index;
            return entry.clips[index];
        }

        Voice FreeVoice()
        {
            foreach (var voice in pool)
                if (!voice.Source.isPlaying)
                    return voice;
            return null;
        }

        float Pan(Vector3 worldPosition)
        {
            if (playfield == null)
                return 0f;
            float half = playfield.HalfExtents.x;
            if (half <= 0.01f)
                return 0f;
            return Mathf.Clamp(playfield.WorldToLocal(worldPosition).x / half, -1f, 1f) * panWidth;
        }
    }
}
