using System;
using System.Collections.Generic;
using OneMoreTime.Core.Audio;
using UnityEngine;
using UnityEngine.Serialization;

namespace OneMoreTime.Gameplay.Audio
{
    /// <summary>
    /// Which clips play for each <see cref="SoundCue"/>, and which music plays on each screen and encounter. Swap a
    /// clip here to change the sound without touching the code. Values are starting points to tune by ear.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Time/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class CueEntry
        {
            public SoundCue cue;
            [Tooltip("One is picked at random each time, never the same twice in a row.")]
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 0.6f;
            [Tooltip("Pitch is picked between these two values, so repeats do not sound identical.")]
            public float pitchMin = 0.96f;
            public float pitchMax = 1.04f;
            [Tooltip("Most voices of this cue alive at once; 0 = no limit.")]
            [Min(0)] public int maxVoices = 4;
            [Tooltip("Shortest time between two plays of this cue.")]
            [Min(0f)] public float minInterval;
            [Tooltip("Pan with the position on screen.")]
            public bool panned = true;
            [Tooltip("Muffles the sound (Hz); 0 = off. The echo's shots use it to sound like another time layer.")]
            [Min(0f)] public float lowPassHz;
            [Tooltip("Cuts a long clip after this many seconds; 0 = play it all.")]
            [Min(0f)] public float maxSeconds;
            [Min(0f)] public float fadeOutSeconds = 0.1f;
        }

        [Serializable]
        public sealed class MusicEntry
        {
            [FormerlySerializedAs("segment")] public MusicTrack track;
            [Tooltip("Played once before the loop. Optional.")]
            public AudioClip intro;
            public AudioClip loop;
            [Range(0f, 1f)] public float volume = 0.6f;
        }

        public CueEntry[] cues = Array.Empty<CueEntry>();
        public MusicEntry[] music = Array.Empty<MusicEntry>();

        Dictionary<SoundCue, CueEntry> cueLookup;

        public CueEntry Find(SoundCue cue)
        {
            if (cueLookup == null)
            {
                cueLookup = new Dictionary<SoundCue, CueEntry>();
                foreach (var entry in cues)
                    if (entry != null && entry.cue != SoundCue.None)
                        cueLookup[entry.cue] = entry;
            }
            return cueLookup.TryGetValue(cue, out var found) ? found : null;
        }

        public MusicEntry FindMusic(MusicTrack track)
        {
            foreach (var entry in music)
                if (entry != null && entry.track == track && entry.loop != null)
                    return entry;
            return null;
        }

        void OnValidate() => cueLookup = null;
    }
}
