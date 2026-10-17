using System.Collections.Generic;
using UnityEngine;

namespace OneMoreTime.Gameplay.World
{
    /// <summary>
    /// Placeholder 3D scenery: props at different depths behind the gameplay plane, recycled ahead of
    /// the camera as it scrolls. The perspective camera gives the 2.5D parallax for free (GDD 08).
    /// Kept small, dark and far back so it never competes with the action for the eye.
    /// Swap the prop prefabs for the artist's set later; the gameplay doesn't depend on them.
    /// </summary>
    public sealed class BackdropScroller : MonoBehaviour
    {
        [SerializeField] Playfield playfield;
        [SerializeField] GameObject[] props;
        [SerializeField, Min(1)] int count = 36;
        [Tooltip("Depth range behind the gameplay plane (world z).")]
        [SerializeField] Vector2 depth = new(16f, 70f);
        [SerializeField] Vector2 heightRange = new(-10f, 6f);
        [SerializeField] Vector2 scaleRange = new(0.6f, 2.6f);
        [SerializeField, Min(1f)] float spread = 110f;
        [SerializeField] int seed = 7;

        readonly List<Transform> live = new();
        System.Random random;

        void Start()
        {
            if (playfield == null || props == null || props.Length == 0)
            {
                enabled = false;
                return;
            }
            random = new System.Random(seed);
            float x = playfield.transform.position.x - spread * 0.3f;
            for (int i = 0; i < count; i++)
            {
                var prop = Instantiate(props[i % props.Length], transform).transform;
                Place(prop, x + (float)random.NextDouble() * spread);
                live.Add(prop);
            }
        }

        void LateUpdate()
        {
            float left = playfield.transform.position.x - spread * 0.35f;
            foreach (var prop in live)
                if (prop.position.x < left)
                    Place(prop, prop.position.x + spread);
        }

        void Place(Transform prop, float x)
        {
            float z = Mathf.Lerp(depth.x, depth.y, (float)random.NextDouble());
            float s = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)random.NextDouble());
            prop.SetPositionAndRotation(
                new Vector3(x, Mathf.Lerp(heightRange.x, heightRange.y, (float)random.NextDouble()), z),
                Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
            prop.localScale = new Vector3(s, s * Mathf.Lerp(1f, 3f, (float)random.NextDouble()), s);
        }
    }
}
