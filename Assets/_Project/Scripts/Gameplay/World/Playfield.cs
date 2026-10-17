using UnityEngine;

namespace OneMoreTime.Gameplay.World
{
    /// <summary>
    /// The scrolling frame of the 2.5D side-scroller. The camera, the ship and the echoes are its
    /// children, so their local XY is screen-relative while the world is 3D (GDD 01, 08).
    /// Gameplay happens on the local plane z = 0; depth is only for the world around it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class Playfield : MonoBehaviour
    {
        [SerializeField] Camera view;
        [Tooltip("Units per second the frame advances to the right. 0 locks the screen (boss, puzzle).")]
        [SerializeField] float scrollSpeed = 2f;

        public Camera View => view;
        public float ScrollSpeed { get => scrollSpeed; set => scrollSpeed = value; }

        /// <summary>Half extents of the visible area on the gameplay plane, in local units.</summary>
        public Vector2 HalfExtents
        {
            get
            {
                if (view == null)
                    return new Vector2(16f, 9f);
                float halfHeight;
                if (view.orthographic)
                {
                    halfHeight = view.orthographicSize;
                }
                else
                {
                    float distance = Mathf.Abs(transform.InverseTransformPoint(view.transform.position).z);
                    halfHeight = distance * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
                }
                return new Vector2(halfHeight * view.aspect, halfHeight);
            }
        }

        public Vector3 LocalToWorld(Vector2 local) => transform.TransformPoint(new Vector3(local.x, local.y, 0f));
        public Vector2 WorldToLocal(Vector3 world) => transform.InverseTransformPoint(world);
        public Vector3 DirectionToWorld(Vector2 local) => transform.TransformDirection(new Vector3(local.x, local.y, 0f));

        /// <summary>True once a world point has scrolled out past the left edge (plus a margin).</summary>
        public bool IsPastLeftEdge(Vector3 world, float margin = 2f) => WorldToLocal(world).x < -HalfExtents.x - margin;

        /// <summary>True when a world point is far outside the visible area in any direction.</summary>
        public bool IsFarOutside(Vector3 world, float margin = 4f)
        {
            var p = WorldToLocal(world);
            var h = HalfExtents;
            return Mathf.Abs(p.x) > h.x + margin || Mathf.Abs(p.y) > h.y + margin;
        }

        void Update()
        {
            if (scrollSpeed != 0f)
                transform.position += Vector3.right * (scrollSpeed * Time.deltaTime);
        }

        void OnDrawGizmos()
        {
            var h = HalfExtents;
            Gizmos.color = new Color(0.21f, 0.89f, 1f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(h.x * 2f, h.y * 2f, 0f));
        }
    }
}
