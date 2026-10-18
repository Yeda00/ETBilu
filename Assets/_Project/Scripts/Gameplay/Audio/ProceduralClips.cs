using UnityEngine;

namespace OneMoreTime.Gameplay.Audio
{
    /// <summary>Tiny clips made in code, for sounds that are just a tone (the recording pulse).</summary>
    public static class ProceduralClips
    {
        /// <summary>A square-wave blip with a quick decay, like an NES pulse channel.</summary>
        public static AudioClip Blip(float frequency = 660f, float seconds = 0.045f, int sampleRate = 44100)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(seconds * sampleRate));
            var samples = new float[count];
            float period = sampleRate / Mathf.Max(1f, frequency);
            for (int i = 0; i < count; i++)
            {
                float square = (i % period) < period * 0.5f ? 1f : -1f;
                float envelope = 1f - (float)i / count;
                samples[i] = square * envelope * 0.5f;
            }
            var clip = AudioClip.Create("Blip (Runtime)", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
