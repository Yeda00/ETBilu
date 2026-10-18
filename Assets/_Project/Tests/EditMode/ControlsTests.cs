using System.Linq;
using NUnit.Framework;
using OneMoreTime.Core.Controls;

namespace OneMoreTime.Tests
{
    public class BindingLabelsTests
    {
        [TestCase("<Keyboard>/space", "SPACE")]
        [TestCase("<Keyboard>/leftShift", "SHIFT")]
        [TestCase("<Keyboard>/escape", "ESC")]
        [TestCase("<Keyboard>/e", "E")]
        [TestCase("<Keyboard>/upArrow", "↑")]
        [TestCase("<Keyboard>/digit5", "5")]
        [TestCase("<Keyboard>/numpad7", "NUM 7")]
        [TestCase("<Keyboard>/leftBracket", "LEFT BRACKET")]
        [TestCase("<Keyboard>/#(q)", "Q")]
        [TestCase("<Mouse>/leftButton", "LEFT CLICK")]
        [TestCase("<Gamepad>/rightTrigger", "RT")]
        [TestCase("<Gamepad>/leftShoulder", "LB")]
        [TestCase("<Gamepad>/buttonSouth", "A")]
        [TestCase("<Gamepad>/start", "START")]
        [TestCase("<Gamepad>/dpad/up", "D-PAD ↑")]
        [TestCase("<Gamepad>/leftStick", "LEFT STICK")]
        [TestCase("<Gamepad>/rightStick/up", "RIGHT STICK ↑")]
        public void Paths_read_as_short_english_labels_by_default(string path, string expected)
        {
            Assert.That(BindingLabels.Format(path), Is.EqualTo(expected));
        }

        [Test]
        public void A_lookup_replaces_the_words_that_change_with_the_language()
        {
            string Portuguese(string key) => key switch
            {
                "bind.space" => "ESPAÇO",
                "bind.mouseLeft" => "CLIQUE ESQ.",
                "bind.stickLeft" => "STICK ESQ.",
                _ => null,   // not translated: the English default stays
            };

            Assert.That(BindingLabels.Format("<Keyboard>/space", Portuguese), Is.EqualTo("ESPAÇO"));
            Assert.That(BindingLabels.Format("<Mouse>/leftButton", Portuguese), Is.EqualTo("CLIQUE ESQ."));
            Assert.That(BindingLabels.Format("<Gamepad>/leftStick/up", Portuguese), Is.EqualTo("STICK ESQ. ↑"));
            Assert.That(BindingLabels.Format("<Mouse>/rightButton", Portuguese), Is.EqualTo("RIGHT CLICK"));
            Assert.That(BindingLabels.Format("<Keyboard>/escape", Portuguese), Is.EqualTo("ESC"), "keys that read the same stay as they are");
        }

        [Test]
        public void Notes_are_tokens_or_text_that_is_the_same_in_every_language()
        {
            Assert.That(BindingLabels.Token("bind.mouse"), Is.EqualTo("MOUSE"));
            Assert.That(BindingLabels.Token("+ ↑"), Is.EqualTo("+ ↑"));
            Assert.That(BindingLabels.Token("bind.plusClick", _ => "+ CLIQUE"), Is.EqualTo("+ CLIQUE"));
            foreach (var row in ControlLayout.Rows)
                foreach (var note in new[] { row.KeyboardNote, row.GamepadNote })
                    if (note.StartsWith(BindingLabels.TokenPrefix))
                        Assert.That(BindingLabels.English.ContainsKey(note), Is.True, $"{row.Key}: {note} has no English default");
        }

        [Test]
        public void Empty_and_odd_paths_do_not_throw()
        {
            Assert.That(BindingLabels.Format(null), Is.EqualTo("—"));
            Assert.That(BindingLabels.Format(""), Is.EqualTo("—"));
            Assert.That(BindingLabels.Format("something"), Is.EqualTo("SOMETHING"));
        }
    }

    public class BindingConflictsTests
    {
        [Test]
        public void Finds_the_other_row_that_uses_the_same_control_and_ignores_the_row_itself()
        {
            var paths = new[] { "<Keyboard>/w", "<Keyboard>/space", "<Keyboard>/e" };
            Assert.That(BindingConflicts.Find(paths, 2, "<Keyboard>/space"), Is.EqualTo(1));
            Assert.That(BindingConflicts.Find(paths, 1, "<Keyboard>/space"), Is.EqualTo(-1), "its own binding is not a conflict");
            Assert.That(BindingConflicts.Find(paths, 0, "<Keyboard>/E"), Is.EqualTo(2), "paths compare without case");
            Assert.That(BindingConflicts.Find(paths, 0, "<Keyboard>/z"), Is.EqualTo(-1));
            Assert.That(BindingConflicts.Find(paths, 0, null), Is.EqualTo(-1));
        }
    }

    public class ControlLayoutTests
    {
        [Test]
        public void Every_row_has_a_unique_key_and_an_action_and_moves_name_their_direction()
        {
            var rows = ControlLayout.Rows;
            Assert.That(rows.Select(r => r.Key).Distinct().Count(), Is.EqualTo(rows.Count));
            foreach (var row in rows)
            {
                Assert.That(row.Action, Is.Not.Empty, row.Key);
                if (row.Action == "Move")
                    Assert.That(new[] { "up", "down", "left", "right" }, Does.Contain(row.Part), row.Key);
                else
                    Assert.That(row.Part, Is.Empty, row.Key);
            }
        }

        [Test]
        public void Fixed_bindings_carry_a_note_to_show_instead()
        {
            foreach (var row in ControlLayout.Rows)
                foreach (ControlDevice device in new[] { ControlDevice.Keyboard, ControlDevice.Gamepad })
                    if (!row.IsEditable(device))
                        Assert.That(row.Note(device), Is.Not.Empty, $"{row.Key} / {device}");
        }
    }
}
