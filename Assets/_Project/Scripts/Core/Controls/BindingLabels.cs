using System.Text;
using System.Text.RegularExpressions;

namespace OneMoreTime.Core.Controls
{
    /// <summary>Turns an Input System control path ("&lt;Keyboard&gt;/space") into the short Portuguese label the screens show.</summary>
    public static class BindingLabels
    {
        static readonly Regex LayoutKey = new(@"^#\((.+)\)$");

        public static string Format(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "—";

            int close = path.IndexOf('>');
            if (!path.StartsWith("<") || close < 0 || close + 2 > path.Length)
                return path.ToUpperInvariant();

            string device = path.Substring(1, close - 1);
            string control = path.Substring(close + 2);
            var layout = LayoutKey.Match(control);
            if (layout.Success)
                return layout.Groups[1].Value.ToUpperInvariant();

            return device switch
            {
                "Keyboard" => Keyboard(control),
                "Mouse" => Mouse(control),
                "Gamepad" => Gamepad(control),
                _ => Words(control),
            };
        }

        static string Keyboard(string key)
        {
            switch (key)
            {
                case "space": return "ESPAÇO";
                case "leftShift": case "rightShift": case "shift": return "SHIFT";
                case "leftCtrl": case "rightCtrl": case "ctrl": return "CTRL";
                case "leftAlt": case "rightAlt": case "alt": return "ALT";
                case "escape": return "ESC";
                case "enter": return "ENTER";
                case "numpadEnter": return "ENTER (NUM)";
                case "backspace": return "BACKSPACE";
                case "tab": return "TAB";
                case "upArrow": return "↑";
                case "downArrow": return "↓";
                case "leftArrow": return "←";
                case "rightArrow": return "→";
                case "capsLock": return "CAPS LOCK";
                case "semicolon": return ";";
                case "comma": return ",";
                case "period": return ".";
                case "slash": return "/";
            }
            if (key.Length == 1)
                return key.ToUpperInvariant();
            if (key.StartsWith("digit") && key.Length == 6)
                return key.Substring(5);
            if (key.StartsWith("numpad") && key.Length > 6)
                return "NUM " + Words(key.Substring(6));
            return Words(key);
        }

        static string Mouse(string control)
        {
            switch (control)
            {
                case "leftButton": return "CLIQUE ESQ.";
                case "rightButton": return "CLIQUE DIR.";
                case "middleButton": return "CLIQUE MEIO";
                default: return "MOUSE " + Words(control);
            }
        }

        static string Gamepad(string control)
        {
            switch (control)
            {
                case "leftTrigger": return "LT";
                case "rightTrigger": return "RT";
                case "leftShoulder": return "LB";
                case "rightShoulder": return "RB";
                case "buttonSouth": return "A";
                case "buttonEast": return "B";
                case "buttonWest": return "X";
                case "buttonNorth": return "Y";
                case "start": return "START";
                case "select": return "SELECT";
                case "leftStick": return "STICK ESQ.";
                case "rightStick": return "STICK DIR.";
                case "leftStickPress": return "L3";
                case "rightStickPress": return "R3";
                case "dpad": return "D-PAD";
                case "dpad/up": return "D-PAD ↑";
                case "dpad/down": return "D-PAD ↓";
                case "dpad/left": return "D-PAD ←";
                case "dpad/right": return "D-PAD →";
                case "leftStick/up": return "STICK ESQ. ↑";
                case "leftStick/down": return "STICK ESQ. ↓";
                case "leftStick/left": return "STICK ESQ. ←";
                case "leftStick/right": return "STICK ESQ. →";
                case "rightStick/up": return "STICK DIR. ↑";
                case "rightStick/down": return "STICK DIR. ↓";
                case "rightStick/left": return "STICK DIR. ←";
                case "rightStick/right": return "STICK DIR. →";
                default: return Words(control);
            }
        }

        /// <summary>"leftBracket" becomes "LEFT BRACKET".</summary>
        static string Words(string camel)
        {
            var sb = new StringBuilder();
            foreach (char c in camel)
            {
                if (char.IsUpper(c) && sb.Length > 0)
                    sb.Append(' ');
                sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString().Replace("/", " ");
        }
    }
}
