using UnityEngine;

namespace OneMoreTime.Core.Enemies
{
    /// <summary>
    /// When a bomber goes off (GDD 04): once its target comes within <c>triggerRadius</c> — or when
    /// <c>autoSeconds</c> have passed since it appeared, if set — a fuse of <c>fuseSeconds</c> starts, and
    /// the telegraph shows while it burns. The bang itself comes when the fuse runs out, and only once.
    /// The kamikaze uses a short fuse and a timer; the tanker a longer fuse and no timer.
    /// </summary>
    public sealed class BlastFuse
    {
        readonly float triggerRadius, fuseSeconds, autoSeconds, warnSeconds;
        float age;
        float fuseLeft;

        public BlastFuse(float triggerRadius, float fuseSeconds, float autoSeconds = 0f, float warnSeconds = 0.8f)
        {
            this.triggerRadius = Mathf.Max(0f, triggerRadius);
            this.fuseSeconds = Mathf.Max(0f, fuseSeconds);
            this.autoSeconds = Mathf.Max(0f, autoSeconds);
            this.warnSeconds = Mathf.Max(0f, warnSeconds);
        }

        public bool Triggered { get; private set; }
        public bool Exploded { get; private set; }
        /// <summary>The fuse is burning, or the timer is about to run it: show the telegraph.</summary>
        public bool Warning => !Exploded && (Triggered || (autoSeconds > 0f && age >= autoSeconds - warnSeconds));
        /// <summary>0 while quiet, up to 1 as the bang gets near.</summary>
        public float Urgency
        {
            get
            {
                if (Exploded)
                    return 1f;
                if (Triggered)
                    return fuseSeconds > 0f ? 1f - Mathf.Clamp01(fuseLeft / fuseSeconds) : 1f;
                return autoSeconds > 0f ? Mathf.Clamp01((age - (autoSeconds - warnSeconds)) / Mathf.Max(0.01f, warnSeconds)) : 0f;
            }
        }

        /// <summary>Advances by <paramref name="deltaTime"/> with the current <paramref name="distanceToTarget"/>. True on the step it goes off.</summary>
        public bool Tick(float deltaTime, float distanceToTarget)
        {
            if (Exploded || deltaTime <= 0f)
                return false;
            age += deltaTime;
            if (!Triggered && (distanceToTarget <= triggerRadius || (autoSeconds > 0f && age >= autoSeconds)))
            {
                Triggered = true;
                fuseLeft = fuseSeconds;
            }
            if (!Triggered)
                return false;
            fuseLeft -= deltaTime;
            if (fuseLeft > 0f)
                return false;
            Exploded = true;
            return true;
        }
    }
}
