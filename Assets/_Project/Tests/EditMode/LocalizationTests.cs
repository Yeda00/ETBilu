using System.Linq;
using NUnit.Framework;
using OneMoreTime.Core.Controls;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace OneMoreTime.Tests
{
    /// <summary>The Game string table (Assets/_Project/Localization) has to be whole before a build: a hole shows in the game.</summary>
    public class LocalizationTests
    {
        const string Table = "Game";
        static readonly string[] Languages = { "en", "pt-BR" };

        static StringTableCollection Collection => LocalizationEditorSettings.GetStringTableCollection(Table);

        [Test]
        public void The_game_table_exists_with_every_language_of_the_game()
        {
            Assert.That(Collection, Is.Not.Null, "run One More Time > Bootstrap > Build Localization");
            foreach (var code in Languages)
                Assert.That(Collection.GetTable(new LocaleIdentifier(code)), Is.Not.Null, code);
        }

        [Test]
        public void Every_key_has_a_text_in_every_language()
        {
            var gaps = (from key in Collection.SharedData.Entries
                        from code in Languages
                        let entry = (Collection.GetTable(new LocaleIdentifier(code)) as StringTable)?.GetEntry(key.Id)
                        where entry == null || string.IsNullOrWhiteSpace(entry.Value)
                        select $"{code}/{key.Key}").ToList();
            Assert.That(gaps, Is.Empty, string.Join("\n", gaps));
        }

        [Test]
        public void Every_binding_token_and_control_line_is_in_the_table()
        {
            var keys = Collection.SharedData.Entries.Select(e => e.Key).ToHashSet();
            foreach (var token in BindingLabels.English.Keys)
                Assert.That(keys, Does.Contain(token));
            foreach (var row in ControlLayout.Rows)
                Assert.That(keys, Does.Contain("control." + row.Key));
        }

        [Test]
        public void The_game_starts_in_english_unless_the_player_picked_another_language()
        {
            var selectors = LocalizationSettings.Instance.GetStartupLocaleSelectors();
            Assert.That(selectors.Last(), Is.TypeOf<SpecificLocaleSelector>(), "the last word is a fixed locale, not the system language");
            Assert.That(((SpecificLocaleSelector)selectors.Last()).LocaleId.Code, Is.EqualTo("en"));
            Assert.That(selectors.OfType<SystemLocaleSelector>(), Is.Empty);
        }
    }
}
