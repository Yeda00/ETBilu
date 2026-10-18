using System.IO;
using System.Linq;
using OneMoreTime.Gameplay.Session;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace OneMoreTime.EditorTools
{
    /// <summary>
    /// Sets up Unity Localization for the game: the Localization Settings (English first, then whatever the
    /// player picked), the locales English and Portuguese (Brazil), the <c>Game</c> string table with every text
    /// of <see cref="LocalizationStrings"/>, and the keys of Mission_01. Idempotent like the other bootstraps:
    /// nothing that exists is overwritten, only what is missing is added. To add a language, create its Locale
    /// in the Localization Tables window; the Language screen of the menu lists every locale of the project.
    /// </summary>
    public static class LocalizationBootstrap
    {
        const string Root = "Assets/_Project/Localization";
        const string SettingsPath = Root + "/Localization Settings.asset";
        const string LocalesDir = Root + "/Locales";
        const string TablesDir = Root + "/Tables";
        const string MissionPath = "Assets/_Project/Data/Missions/Mission_01.asset";

        [MenuItem("One More Time/Bootstrap/Build Localization", priority = 13)]
        public static void BuildAll()
        {
            EnsureFolder(Root);
            EnsureFolder(LocalesDir);
            EnsureFolder(TablesDir);

            var settings = EnsureSettings();
            var english = EnsureLocale("en");
            var portuguese = EnsureLocale("pt-BR");
            EnsureFallback(portuguese, english);
            ConfigureStartup(settings);

            var collection = EnsureCollection();
            int added = Fill(collection);
            LinkMission01();
            BuildAddressablesWithPlayer();

            AssetDatabase.SaveAssets();
            Debug.Log($"[One More Time] Localization ready: locales {string.Join(", ", LocalizationEditorSettings.GetLocales().Select(l => l.Identifier.Code))}, " +
                      $"table '{collection.TableCollectionName}' with {collection.SharedData.Entries.Count} keys ({added} values added).");
        }

        // ---------------------------------------------------------------- settings and locales

        static LocalizationSettings EnsureSettings()
        {
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings != null)
                return settings;

            string[] guids = AssetDatabase.FindAssets("t:LocalizationSettings", new[] { "Assets" });
            settings = guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<LocalizationSettings>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            return settings;
        }

        static Locale EnsureLocale(string code)
        {
            var id = new LocaleIdentifier(code);
            var locale = LocalizationEditorSettings.GetLocale(id);
            if (locale != null)
                return locale;

            locale = Locale.CreateLocale(id);
            // The name Unity's own Locales window gives: "English(en)", "Portuguese (Brazil)(pt-BR)".
            locale.name = $"{id.CultureInfo?.EnglishName ?? code}({code})";
            AssetDatabase.CreateAsset(locale, $"{LocalesDir}/{SafeName(locale.name)}.asset");
            LocalizationEditorSettings.AddLocale(locale, createUndo: false);
            return locale;
        }

        // Anything the Portuguese table lacks falls back to English instead of showing a hole.
        static void EnsureFallback(Locale locale, Locale fallback)
        {
            if (locale.Metadata.GetMetadata<FallbackLocale>() != null)
                return;
            locale.Metadata.AddMetadata(new FallbackLocale(fallback));
            EditorUtility.SetDirty(locale);
        }

        // English by default, whatever the system language is. The player's own pick (saved by L10n) wins when there is one.
        static void ConfigureStartup(LocalizationSettings settings)
        {
            var selectors = settings.GetStartupLocaleSelectors();
            selectors.Clear();
            selectors.Add(new PlayerPrefLocaleSelector());
            selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier("en") });
            EditorUtility.SetDirty(settings);
        }

        // ---------------------------------------------------------------- the table

        static StringTableCollection EnsureCollection()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(OneMoreTime.Gameplay.Language.L10n.TableName);
            if (collection != null)
                return collection;
            return LocalizationEditorSettings.CreateStringTableCollection(OneMoreTime.Gameplay.Language.L10n.TableName, TablesDir, LocalizationEditorSettings.GetLocales().ToList());
        }

        // Adds the keys and the values that are missing; a value that already exists is left as the translator wrote it.
        static int Fill(StringTableCollection collection)
        {
            int added = 0;
            var shared = collection.SharedData;
            foreach (var item in LocalizationStrings.All())
            {
                var key = shared.GetEntry(item.Key) ?? shared.AddKey(item.Key);
                if (!string.IsNullOrEmpty(item.Context) && key.Metadata.GetMetadata<Comment>() == null)
                    key.Metadata.AddMetadata(new Comment { CommentText = item.Context });

                added += Put(collection, "en", key.Id, item.English);
                added += Put(collection, "pt-BR", key.Id, item.Portuguese);
            }

            EditorUtility.SetDirty(shared);
            foreach (var table in collection.StringTables)
                EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
            return added;
        }

        static int Put(StringTableCollection collection, string code, long keyId, string value)
        {
            var table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
            if (table == null)
                return 0;
            var entry = table.GetEntry(keyId);
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Value))
                return 0;
            entry = table.AddEntry(keyId, value);
            entry.IsSmart = false;   // plain text with {0} slots: L10n formats it itself
            return 1;
        }

        // ---------------------------------------------------------------- mission 01

        static void LinkMission01()
        {
            var mission = AssetDatabase.LoadAssetAtPath<MissionData>(MissionPath);
            if (mission == null)
                return;
            if (string.IsNullOrEmpty(mission.placeKey))
                mission.placeKey = "mission.01.place";
            if (string.IsNullOrEmpty(mission.titleKey))
                mission.titleKey = "mission.01.title";
            if (mission.storyKeys == null || mission.storyKeys.Length == 0)
                mission.storyKeys = LocalizationStrings.Mission01Story;
            EditorUtility.SetDirty(mission);
        }

        // The string tables are Addressables: a player build has to build them too.
        static void BuildAddressablesWithPlayer()
        {
            var addressables = AddressableAssetSettingsDefaultObject.Settings;
            if (addressables == null || addressables.BuildAddressablesWithPlayerBuild == AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer)
                return;
            addressables.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
            EditorUtility.SetDirty(addressables);
        }

        // ---------------------------------------------------------------- helpers

        static string SafeName(string name) => string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
