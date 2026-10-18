using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime.Gameplay.Input
{
    /// <summary>Saves the player's changed controls between runs, as the Input System's binding overrides in PlayerPrefs.</summary>
    public static class ControlsStore
    {
        const string Key = "omt.controls.overrides";

        /// <summary>Applies the saved overrides to the asset. Both input readers call this before they bind their actions.</summary>
        public static void Load(InputActionAsset asset)
        {
            if (asset == null)
                return;
            string json = PlayerPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(json))
                asset.LoadBindingOverridesFromJson(json);
        }

        public static void Save(InputActionAsset asset)
        {
            if (asset == null)
                return;
            PlayerPrefs.SetString(Key, asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public static void ResetAll(InputActionAsset asset)
        {
            asset?.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
