using UnityEngine;

namespace OneMoreTime.Gameplay.Session
{
    /// <summary>
    /// Something the stage waits for before it starts, like the mission card. The stage lives in Gameplay and
    /// the card in UI, so this is the one thing they share.
    /// </summary>
    public abstract class StageGate : MonoBehaviour
    {
        public abstract bool IsOpen { get; }
    }
}
