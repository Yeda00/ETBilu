using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OneMoreTime.Core.Controls
{
    /// <summary>
    /// Turns an Input System control path ("&lt;Keyboard&gt;/space") into the short label the screens show.
    /// The words that change with the language ("SPACE", "LEFT CLICK") are tokens named <c>bind.*</c>; the
    /// English text below is the default, and a <c>lookup</c> (the string table, in the game) replaces it.
    /// </summary>
    public static class BindingLabels
    {
        public const string TokenPrefix = "bind.";

        /// <summary>The English text of every token. The game's string table carries the same keys for each language.</summary>
        public static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
        {
            ["bind.space"] = "SPACE",
            ["bind.numpadEnter"] = "NUMPAD ENTER",
            ["bind.num"] = "NUM",
            ["bind.mouse"] = "MOUSE",
            ["bind.mouseLeft"] = "LEFT CLICK",
            ["bind.mouseRight"] = "RIGHT CLICK",
            ["bind.mouseMiddle"] = "MIDDLE CLICK",
            ["bind.stickLeft"] = "LEFT STICK",
            ["bind.stickRight"] = "RIGHT STICK",
            ["bind.click"] = "CLICK",
            ["bind.plusClick"] = "+ CLICK",
            ["bind.plusClickRight"] = "+ RIGHT CLICK",
        };

        static readonly Regex LayoutKey = new(@"^#\((.+)\)$");

        /// <summary>The text of a token, or the string itself when it is not a token (a note like "+ ↑" is the same in every language).</summary>
        public static string Token(string key, Func<string, string> lookup = null)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith(TokenPrefix, StringComparison.Ordinal))
                return key ?? "";
            string text = lookup?.Invoke(key);
            if (!string.IsNullOrEmpty(text))
                return text;
            return English.TryGetValue(key, out var fallback) ? fallback : key;
        }

        public static string Format(string path, Func<string, string> lookup = null)
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
                "Keyboard" => Keyboard(control, lookup),
                "Mouse" => Mouse(control, lookup),
                "Gamepad" => Gamepad(control, lookup),
                _ => Words(control),
            };
        }

        static string Keyboard(string key, Func<string, string> lookup)
        {
            switch (key)
            {
                case "space": return Token("bind.space", lookup);
                case "leftShift": case "rightShift": case "shift": return "SHIFT";
                case "leftCtrl": case "rightCtrl": case "ctrl": return "CTRL";
                case "leftAlt": case "rightAlt": case "alt": return "ALT";
                case "escape": return "ESC";
                case "enter": return "ENTER";
                case "numpadEnter": return Token("bind.numpadEnter", lookup);
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
                return Token("bind.num", lookup) + " " + Words(key.Substring(6));
            return Words(key);
        }

        static string Mouse(string control, Func<string, string> lookup)
        {
            switch (control)
            {
                case "leftButton": return Token("bind.mouseLeft", lookup);
                case "rightButton": return Token("bind.mouseRight", lookup);
                case "middleButton": return Token("bind.mouseMiddle", lookup);
                default: return Token("bind.mouse", lookup) + " " + Words(control);
            }
        }

        static string Gamepad(string control, Func<string, string> lookup)
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
                case "leftStick": return Token("bind.stickLeft", lookup);
                case "rightStick": return Token("bind.stickRight", lookup);
                case "leftStickPress": return "L3";
                case "rightStickPress": return "R3";
                case "dpad": return "D-PAD";
                case "dpad/up": return "D-PAD ↑";
                case "dpad/down": return "D-PAD ↓";
                case "dpad/left": return "D-PAD ←";
                case "dpad/right": return "D-PAD →";
                case "leftStick/up": return Token("bind.stickLeft", lookup) + " ↑";
                case "leftStick/down": return Token("bind.stickLeft", lookup) + " ↓";
                case "leftStick/left": return Token("bind.stickLeft", lookup) + " ←";
                case "leftStick/right": return Token("bind.stickLeft", lookup) + " →";
                case "rightStick/up": return Token("bind.stickRight", lookup) + " ↑";
                case "rightStick/down": return Token("bind.stickRight", lookup) + " ↓";
                case "rightStick/left": return Token("bind.stickRight", lookup) + " ←";
                case "rightStick/right": return Token("bind.stickRight", lookup) + " →";
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
