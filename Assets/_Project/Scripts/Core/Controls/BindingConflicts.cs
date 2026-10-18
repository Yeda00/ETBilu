using System;
using System.Collections.Generic;

namespace OneMoreTime.Core.Controls
{
    /// <summary>Finds another action that already uses the control the player just picked.</summary>
    public static class BindingConflicts
    {
        /// <summary>Index of another entry of <paramref name="paths"/> equal to <paramref name="newPath"/>, or -1.</summary>
        public static int Find(IReadOnlyList<string> paths, int exceptIndex, string newPath)
        {
            if (string.IsNullOrEmpty(newPath))
                return -1;
            for (int i = 0; i < paths.Count; i++)
                if (i != exceptIndex && string.Equals(paths[i], newPath, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }
    }
}
