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

        /// <summary>A bright two-stage lock-on sound for planting the temporal anchor.</summary>
        public static AudioClip TemporalAnchor(int sampleRate = 44100)
        {
            const float seconds = 0.22f;
            int count = Mathf.RoundToInt(seconds * sampleRate);
            var samples = new float[count];
            uint noise = 0xA341316Cu;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float p = t / seconds;
                float sweep = Mathf.Lerp(330f, 1180f, p * p);
                float tone = Mathf.Sin(t * sweep * Mathf.PI * 2f) * 0.52f;
                float harmonic = Mathf.Sin(t * sweep * Mathf.PI * 4f) * 0.18f;
                noise = noise * 1664525u + 1013904223u;
                float grit = ((noise >> 9) / 8388607f * 2f - 1f) * Mathf.Max(0f, 1f - p * 8f) * 0.18f;
                float attack = Mathf.Clamp01(t / 0.012f);
                float envelope = attack * (1f - p) * (1f - p);
                samples[i] = Mathf.Clamp((tone + harmonic + grit) * envelope, -1f, 1f);
            }
            return Make("Temporal Anchor (Runtime)", samples, sampleRate);
        }

        /// <summary>A reverse sweep ending in a low transient: the ship snapping back through time.</summary>
        public static AudioClip TemporalReturn(int sampleRate = 44100)
        {
            const float seconds = 0.44f;
            int count = Mathf.RoundToInt(seconds * sampleRate);
            var samples = new float[count];
            uint noise = 0xC8013EA4u;
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float p = t / seconds;
                float sweep = Mathf.Lerp(1650f, 120f, p);
                phase += sweep / sampleRate;
                noise = noise * 1103515245u + 12345u;
                float hiss = ((noise >> 8) / 16777215f * 2f - 1f) * Mathf.Sin(p * Mathf.PI);
                float reverse = Mathf.Sin(phase * Mathf.PI * 2f) * Mathf.Sin(p * Mathf.PI) * 0.38f + hiss * 0.14f;
                float impactTime = Mathf.Max(0f, t - 0.29f);
                float impact = Mathf.Sin(impactTime * 92f * Mathf.PI * 2f) * Mathf.Exp(-impactTime * 18f) * (t >= 0.29f ? 0.72f : 0f);
                samples[i] = Mathf.Clamp(reverse + impact, -1f, 1f);
            }
            return Make("Temporal Return (Runtime)", samples, sampleRate);
        }

        /// <summary>A short violet shimmer that leaves the combat echo sounding like a second timeline.</summary>
        public static AudioClip TemporalArrival(int sampleRate = 44100)
        {
            const float seconds = 0.52f;
            int count = Mathf.RoundToInt(seconds * sampleRate);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float p = t / seconds;
                float chord = Mathf.Sin(t * 520f * Mathf.PI * 2f) * 0.34f
                    + Mathf.Sin(t * 780f * Mathf.PI * 2f) * 0.22f
                    + Mathf.Sin(t * 1040f * Mathf.PI * 2f) * 0.12f;
                float tremolo = 0.72f + Mathf.Sin(t * 26f * Mathf.PI * 2f) * 0.28f;
                float envelope = Mathf.Clamp01(t / 0.018f) * (1f - p) * (1f - p);
                samples[i] = chord * tremolo * envelope;
            }
            return Make("Temporal Echo Arrival (Runtime)", samples, sampleRate);
        }

        static AudioClip Make(string name, float[] samples, int sampleRate)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
