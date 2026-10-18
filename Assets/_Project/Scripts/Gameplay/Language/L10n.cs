using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace OneMoreTime.Gameplay.Language
{
    /// <summary>One language the player can pick: the code of its Unity Locale ("en", "pt-BR") and its name in its own language.</summary>
    public readonly struct LanguageOption
    {
        public readonly string Code;
        public readonly string Name;

        public LanguageOption(string code, string name)
        {
            Code = code;
            Name = name;
        }
    }

    /// <summary>
    /// The game's words, from the Unity Localization string table <see cref="TableName"/> (Assets/_Project/Localization).
    /// The language is the Localization package's own <c>SelectedLocale</c>: this class only reads the table, keeps
    /// the choice between runs and tells the screens when it changes. English is the default; the player picks
    /// another language on the menu's Language screen.
    /// </summary>
    public static class L10n
    {
        public const string TableName = "Game";
        public const string DefaultLanguage = "en";
        // The key the package's PlayerPrefLocaleSelector reads at startup, so the choice survives a restart.
        const string SavedKey = "selected-locale";

        static readonly Dictionary<string, string> cache = new();
        static readonly HashSet<string> reported = new();
        static bool hooked;

        /// <summary>Raised after the language changed; screens that already drew words redraw them.</summary>
        public static event Action Changed;

        /// <summary>The text for a key in the current language; the key itself, once reported, when the table has none.</summary>
        public static string Get(string key)
        {
            string text = Find(key);
            if (text != null)
                return text;
            if (reported.Add(key))
                Debug.LogWarning($"[L10n] No text for '{key}' in table '{TableName}'.");
            return key;
        }

        /// <summary>Like <see cref="Get"/> with <c>string.Format</c> arguments; numbers follow the language's culture.</summary>
        public static string Format(string key, params object[] args) => string.Format(Culture, Get(key), args);

        /// <summary>The text for a key, or null when there is none. This is the shape <c>BindingLabels</c> takes as its lookup.</summary>
        public static string Find(string key)
        {
            Hook();
            if (cache.TryGetValue(key, out var cached))
                return cached;
            string text = Read(key);
            cache[key] = text;
            return text;
        }

        /// <summary>The culture of the current language, for numbers.</summary>
        public static CultureInfo Culture
        {
            get
            {
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null && locale.Formatter is CultureInfo culture ? culture : CultureInfo.GetCultureInfo("en-US");
            }
        }

        /// <summary>The code of the language in use ("en", "pt-BR").</summary>
        public static string CurrentCode
        {
            get
            {
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : DefaultLanguage;
            }
        }

        /// <summary>The languages of the project, in the order of the Localization settings.</summary>
        public static List<LanguageOption> Languages()
        {
            var list = new List<LanguageOption>();
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
                list.Add(new LanguageOption(locale.Identifier.Code, NativeName(locale)));
            return list;
        }

        /// <summary>Switches the language and keeps it for the next run. False when the project has no such language.</summary>
        public static bool SetLanguage(string code)
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale == null)
                return false;
            LocalizationSettings.SelectedLocale = locale;
            PlayerPrefs.SetString(SavedKey, locale.Identifier.Code);
            PlayerPrefs.Save();
            return true;
        }

        static string Read(string key)
        {
            var entry = LocalizationSettings.StringDatabase.GetTableEntry(TableName, key).Entry;
            return entry != null && !string.IsNullOrEmpty(entry.Value) ? entry.Value : null;
        }

        static void Hook()
        {
            if (hooked)
                return;
            hooked = true;
            LocalizationSettings.SelectedLocaleChanged += _ =>
            {
                cache.Clear();
                Changed?.Invoke();
            };
        }

        static string NativeName(Locale locale)
        {
            // Each language is shown in its own words, whatever the language in use.
            string name = locale.Identifier.CultureInfo != null ? locale.Identifier.CultureInfo.NativeName : locale.LocaleName;
            return name.ToUpperInvariant();
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Domain reload may be off in the Editor: start every Play with a clean cache.
            cache.Clear();
            reported.Clear();
            hooked = false;
            Changed = null;
        }
#endif
    }
}
