using System.Collections.Generic;
using OneMoreTime.Core.Controls;
using UnityEngine.InputSystem;

namespace OneMoreTime.Gameplay.Input
{
    /// <summary>
    /// Finds, in the Gameplay map, the binding that a line of the controls screen stands for, and says what is
    /// bound to it now. The layout of the lines is Core (<see cref="ControlLayout"/>); this is the part that
    /// needs the Input System.
    /// </summary>
    public static class ControlsCatalog
    {
        const string GameplayMap = "Gameplay";

        public static InputAction FindAction(InputActionAsset asset, ControlRow row) =>
            asset != null ? asset.FindActionMap(GameplayMap)?.FindAction(row.Action) : null;

        /// <summary>The index, in the action's bindings, of the keyboard or gamepad binding this line controls; -1 if it has none.</summary>
        public static int FindBindingIndex(InputAction action, ControlRow row, ControlDevice device)
        {
            if (action == null)
                return -1;
            string prefix = device == ControlDevice.Keyboard ? "<Keyboard>" : "<Gamepad>";
            var bindings = action.bindings;

            if (row.Part.Length > 0)
            {
                // The movement keys: the parts of the first composite (WASD). The arrows stay as they are.
                if (device != ControlDevice.Keyboard)
                    return -1;
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (!bindings[i].isComposite)
                        continue;
                    for (int j = i + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
                        if (bindings[j].name == row.Part)
                            return j;
                    return -1;
                }
                return -1;
            }

            for (int i = 0; i < bindings.Count; i++)
                if (!bindings[i].isComposite && !bindings[i].isPartOfComposite && bindings[i].path.StartsWith(prefix))
                    return i;
            return -1;
        }

        public static string EffectivePath(InputActionAsset asset, ControlRow row, ControlDevice device)
        {
            var action = FindAction(asset, row);
            int index = FindBindingIndex(action, row, device);
            return index >= 0 ? action.bindings[index].effectivePath : "";
        }

        /// <summary>The paths now bound to every line the player may change on a device (empty for fixed lines), in layout order.</summary>
        public static List<string> EffectivePaths(InputActionAsset asset, ControlDevice device)
        {
            var paths = new List<string>();
            foreach (var row in ControlLayout.Rows)
                paths.Add(row.IsEditable(device) ? EffectivePath(asset, row, device) : "");
            return paths;
        }

        /// <summary>What the controls screen shows in a cell: the label of the binding, plus the line's note.</summary>
        public static string Describe(InputActionAsset asset, ControlRow row, ControlDevice device)
        {
            string note = row.Note(device);
            if (!row.IsEditable(device))
                return note;
            string label = BindingLabels.Format(EffectivePath(asset, row, device));
            return note.Length > 0 ? $"{label}  {note}" : label;
        }

        /// <summary>The line of hints at the foot of the HUD, with the controls as they are now.</summary>
        public static string HintLine(InputActionAsset asset, ControlDevice device)
        {
            string L(string key)
            {
                foreach (var row in ControlLayout.Rows)
                    if (row.Key == key)
                        return device == ControlDevice.Gamepad && !row.GamepadEditable ? row.GamepadNote : BindingLabels.Format(EffectivePath(asset, row, device));
                return "";
            }

            if (device == ControlDevice.Gamepad)
                return $"{L("Move.up")} mover · {L("Aim")} mirar · {L("Fire")} atirar · {L("Record")} gravar · {L("InvokeEcho")} invocar eco · {L("Gadget")} mina · {L("Pause")} pausar";

            string up = L("Move.up"), left = L("Move.left"), down = L("Move.down"), right = L("Move.right");
            string move = up.Length == 1 && left.Length == 1 && down.Length == 1 && right.Length == 1
                ? up + left + down + right
                : $"{up}/{left}/{down}/{right}";
            return $"{move} mover · MOUSE mirar · {L("Fire")}/CLIQUE atirar · {L("Record")} gravar · {L("InvokeEcho")} invocar eco · {L("Gadget")} mina · {L("Pause")} pausar";
        }
    }
}
