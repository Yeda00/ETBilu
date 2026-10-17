using System;
using UnityEngine;

namespace OneMoreTime.Core.Echo
{
    /// <summary>What a recorded event was. Extend as the prototype supports more (GDD 09).</summary>
    public enum EchoEventType
    {
        Shot = 0,
        Gadget = 1,
    }

    /// <summary>
    /// One pose of the ship. Positions are local to the scrolling playfield, so an echo replays
    /// the same screen-relative path even while the camera keeps advancing (GDD 02, 09).
    /// </summary>
    [Serializable]
    public struct EchoSample
    {
        public float time;
        public Vector2 position;
        public float rotation;

        public EchoSample(float time, Vector2 position, float rotation)
        {
            this.time = time;
            this.position = position;
            this.rotation = rotation;
        }
    }

    /// <summary>A discrete action (shot, gadget use) with its instant relative to the recording.</summary>
    [Serializable]
    public struct EchoEvent
    {
        public float time;
        public EchoEventType type;
        public Vector2 position;
        public Vector2 direction;

        public EchoEvent(float time, EchoEventType type, Vector2 position, Vector2 direction)
        {
            this.time = time;
            this.type = type;
            this.position = position;
            this.direction = direction;
        }
    }
}
