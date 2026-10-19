using UnityEngine;

namespace OneMoreTime.UI
{
    /// <summary>
    /// The menu's starfield: small square stars in three depths drifting left at different speeds, so it reads
    /// as depth. Drawn in the 3D scene far behind the showcase ship (a UI layer would cover the ship). Each star
    /// takes the colour of a real star, as often as that kind shows up in the sky: mostly white and blue-white,
    /// some yellow and orange, a few red. Sizes are pixels of the 1080p menu, so stars stay crisp squares.
    /// </summary>
    public sealed class StarfieldView : MonoBehaviour
    {
        [SerializeField] Camera view;
        [Tooltip("Unlit material that shows the vertex colours (URP Particles/Unlit).")]
        [SerializeField] Material material;
        [SerializeField, Min(10)] int stars = 110;
        [SerializeField] Vector2 speeds = new(14f, 70f);
        [SerializeField] int seed = 11;
        [Tooltip("Distance from the camera: behind the ship and inside the far clip plane.")]
        [SerializeField, Min(1f)] float distance = 45f;

        // Star colours by temperature, with how often each kind is picked.
        static readonly (Color32 color, float weight)[] Kinds =
        {
            (new Color32(155, 176, 255, 255), 0.12f),   // blue
            (new Color32(202, 216, 255, 255), 0.30f),   // blue-white
            (new Color32(248, 247, 255, 255), 0.28f),   // white
            (new Color32(255, 244, 232, 255), 0.14f),   // yellow-white
            (new Color32(255, 220, 160, 255), 0.09f),   // yellow
            (new Color32(255, 184, 110, 255), 0.05f),   // orange
            (new Color32(255, 132, 100, 255), 0.02f),   // red
        };

        const float Width = 1920f;
        const float Height = 1080f;

        Mesh mesh;
        Vector3[] vertices;
        float[] startX;
        float[] top;
        float[] size;
        float[] speed;

        void OnEnable()
        {
            if (view == null || material == null)
                return;

            var random = new System.Random(seed);
            startX = new float[stars];
            top = new float[stars];
            size = new float[stars];
            speed = new float[stars];
            vertices = new Vector3[stars * 4];
            var colors = new Color32[stars * 4];
            var triangles = new int[stars * 6];
            for (int i = 0; i < stars; i++)
            {
                float depth = (float)random.NextDouble();   // 0 = far, 1 = near
                size[i] = depth > 0.85f ? 4f : depth > 0.5f ? 3f : 2f;
                top[i] = (float)random.NextDouble() * Height;
                startX[i] = (float)random.NextDouble() * Width;
                speed[i] = Mathf.Lerp(speeds.x, speeds.y, depth);

                // Far stars are dimmer (the background is near black, so dimming reads as distance).
                Color32 color = Color32.Lerp(new Color32(0, 0, 0, 255), Pick(random), Mathf.Lerp(0.35f, 1f, depth));
                for (int v = 0; v < 4; v++)
                    colors[i * 4 + v] = color;
                int b = i * 4;
                triangles[i * 6 + 0] = b;
                triangles[i * 6 + 1] = b + 1;
                triangles[i * 6 + 2] = b + 2;
                triangles[i * 6 + 3] = b;
                triangles[i * 6 + 4] = b + 2;
                triangles[i * 6 + 5] = b + 3;
            }

            mesh = new Mesh { name = "Starfield" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
        }

        void OnDisable()
        {
            if (mesh != null)
                Destroy(mesh);
            mesh = null;
        }

        void LateUpdate()
        {
            if (mesh == null)
                return;

            // Size of one 1080p menu pixel at the starfield's distance.
            float viewHeight = 2f * distance * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float pixel = viewHeight / Height;
            float viewWidth = viewHeight * view.aspect;
            float t = Time.unscaledTime;
            for (int i = 0; i < size.Length; i++)
            {
                float x = (startX[i] - speed[i] * t) % Width;
                if (x < 0f)
                    x += Width;
                float cx = (x / Width - 0.5f) * viewWidth;
                float cy = (0.5f - top[i] / Height) * viewHeight;
                float h = size[i] * pixel * 0.5f;
                int b = i * 4;
                vertices[b + 0] = new Vector3(cx - h, cy - h, 0f);
                vertices[b + 1] = new Vector3(cx - h, cy + h, 0f);
                vertices[b + 2] = new Vector3(cx + h, cy + h, 0f);
                vertices[b + 3] = new Vector3(cx + h, cy - h, 0f);
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            var camera = view.transform;
            var matrix = Matrix4x4.TRS(camera.position + camera.forward * distance, camera.rotation, Vector3.one);
            Graphics.RenderMesh(new RenderParams(material), mesh, 0, matrix);
        }

        static Color32 Pick(System.Random random)
        {
            float roll = (float)random.NextDouble();
            foreach (var (color, weight) in Kinds)
            {
                roll -= weight;
                if (roll <= 0f)
                    return color;
            }
            return Kinds[^1].color;
        }
    }
}
