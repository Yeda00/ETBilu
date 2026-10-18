using System.Collections.Generic;

namespace OneMoreTime.Core.Controls
{
    public enum ControlDevice
    {
        Keyboard = 0,
        Gamepad = 1,
    }

    /// <summary>
    /// One line of the controls screen: an action of the Gameplay map, and which of its two bindings (keyboard
    /// and gamepad) the player may change. <see cref="Part"/> names the direction of the movement keys.
    /// </summary>
    public sealed class ControlRow
    {
        /// <summary>Stable key of the row; the words for it live in the string table (control.<Key>).</summary>
        public string Key { get; }
        public string Action { get; }
        /// <summary>For the movement keys: "up", "down", "left" or "right". Empty for the other actions.</summary>
        public string Part { get; }
        public bool KeyboardEditable { get; }
        public bool GamepadEditable { get; }
        /// <summary>Shown beside the binding when it is fixed or has a fixed alternative (a <c>bind.*</c> token, see <see cref="BindingLabels"/>, or text that is the same in every language, like "+ ↑").</summary>
        public string KeyboardNote { get; }
        public string GamepadNote { get; }

        public ControlRow(string key, string action, string part, bool keyboardEditable, bool gamepadEditable, string keyboardNote = "", string gamepadNote = "")
        {
            Key = key;
            Action = action;
            Part = part ?? "";
            KeyboardEditable = keyboardEditable;
            GamepadEditable = gamepadEditable;
            KeyboardNote = keyboardNote ?? "";
            GamepadNote = gamepadNote ?? "";
        }

        public bool IsEditable(ControlDevice device) => device == ControlDevice.Keyboard ? KeyboardEditable : GamepadEditable;
        public string Note(ControlDevice device) => device == ControlDevice.Keyboard ? KeyboardNote : GamepadNote;
    }

    /// <summary>The lines of the controls screen, top to bottom (GDD 07).</summary>
    public static class ControlLayout
    {
        public static readonly IReadOnlyList<ControlRow> Rows = new[]
        {
            new ControlRow("Move.up", "Move", "up", true, false, "+ ↑", "bind.stickLeft"),
            new ControlRow("Move.down", "Move", "down", true, false, "+ ↓", "bind.stickLeft"),
            new ControlRow("Move.left", "Move", "left", true, false, "+ ←", "bind.stickLeft"),
            new ControlRow("Move.right", "Move", "right", true, false, "+ →", "bind.stickLeft"),
            new ControlRow("Aim", "Aim", "", false, false, "bind.mouse", "bind.stickRight"),
            new ControlRow("Fire", "Fire", "", true, true, "bind.plusClick"),
            new ControlRow("Record", "Record", "", true, true),
            new ControlRow("Gadget", "Gadget", "", true, true),
            new ControlRow("Dodge", "Dodge", "", true, true, "bind.plusClickRight"),
            new ControlRow("Pause", "Pause", "", true, true),
        };
    }
}
