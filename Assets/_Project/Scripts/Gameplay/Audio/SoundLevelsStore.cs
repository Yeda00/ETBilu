using OneMoreTime.Core.Audio;
using UnityEngine;

namespace OneMoreTime.Gameplay.Audio
{
    /// <summary>Saves the sound screen's sliders between runs. A plain value in PlayerPrefs, read by the audio players when they wake.</summary>
    public static class SoundLevelsStore
    {
        const string MusicKey = "omt.sound.music";
        const string EffectsKey = "omt.sound.effects";

        public static SoundLevels Load()
        {
            return new SoundLevels(
                PlayerPrefs.GetInt(MusicKey, SoundLevels.DefaultMusic),
                PlayerPrefs.GetInt(EffectsKey, SoundLevels.DefaultEffects));
        }

        public static void Save(SoundLevels levels)
        {
            PlayerPrefs.SetInt(MusicKey, levels.Get(SoundChannel.Music));
            PlayerPrefs.SetInt(EffectsKey, levels.Get(SoundChannel.Effects));
            PlayerPrefs.Save();
        }
    }
}
