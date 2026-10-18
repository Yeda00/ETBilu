using System.Linq;
using NUnit.Framework;
using OneMoreTime.Core.Controls;

namespace OneMoreTime.Tests
{
    public class BindingLabelsTests
    {
        [TestCase("<Keyboard>/space", "ESPAÇO")]
        [TestCase("<Keyboard>/leftShift", "SHIFT")]
        [TestCase("<Keyboard>/escape", "ESC")]
        [TestCase("<Keyboard>/e", "E")]
        [TestCase("<Keyboard>/upArrow", "↑")]
        [TestCase("<Keyboard>/digit5", "5")]
        [TestCase("<Keyboard>/numpad7", "NUM 7")]
        [TestCase("<Keyboard>/leftBracket", "LEFT BRACKET")]
        [TestCase("<Keyboard>/#(q)", "Q")]
        [TestCase("<Mouse>/leftButton", "CLIQUE ESQ.")]
        [TestCase("<Gamepad>/rightTrigger", "RT")]
        [TestCase("<Gamepad>/leftShoulder", "LB")]
        [TestCase("<Gamepad>/buttonSouth", "A")]
        [TestCase("<Gamepad>/start", "START")]
        [TestCase("<Gamepad>/dpad/up", "D-PAD ↑")]
        [TestCase("<Gamepad>/leftStick", "STICK ESQ.")]
        public void Paths_read_as_short_portuguese_labels(string path, string expected)
        {
            Assert.That(BindingLabels.Format(path), Is.EqualTo(expected));
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
