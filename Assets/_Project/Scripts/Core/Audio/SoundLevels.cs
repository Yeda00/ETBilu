using System;

namespace OneMoreTime.Core.Audio
{
    public enum SoundChannel
    {
        Music = 0,
        Effects = 1,
    }

    /// <summary>The player's volume sliders of the sound screen: whole steps from 0 to <see cref="MaxStep"/>.</summary>
    public sealed class SoundLevels
    {
        public const int MaxStep = 10;
        public const int DefaultMusic = 7;
        public const int DefaultEffects = 8;

        int music;
        int effects;

        public SoundLevels(int music = DefaultMusic, int effects = DefaultEffects)
        {
            this.music = Clamp(music);
            this.effects = Clamp(effects);
        }

        public int Get(SoundChannel channel) => channel == SoundChannel.Music ? music : effects;

        public void Set(SoundChannel channel, int step)
        {
            step = Clamp(step);
            if (channel == SoundChannel.Music)
                music = step;
            else
                effects = step;
        }

        /// <summary>Moves one slider by whole steps; false when it was already at the end.</summary>
        public bool Adjust(SoundChannel channel, int delta)
        {
            int before = Get(channel);
            Set(channel, before + delta);
            return Get(channel) != before;
        }

        /// <summary>Volume multiplier from 0 to 1.</summary>
        public float Gain(SoundChannel channel) => Get(channel) / (float)MaxStep;

        static int Clamp(int step) => Math.Max(0, Math.Min(MaxStep, step));
    }
}
