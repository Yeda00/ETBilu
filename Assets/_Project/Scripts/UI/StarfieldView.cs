using UnityEngine;
using UnityEngine.UIElements;

namespace OneMoreTime.UI
{
    /// <summary>
    /// A scrolling starfield made of small UI squares behind the menu screens: three layers drifting left at
    /// different speeds, so it reads as depth. Crisp pixels on purpose.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StarfieldView : MonoBehaviour
    {
        [SerializeField, Min(10)] int stars = 110;
        [SerializeField] Vector2 speeds = new(14f, 70f);
        [SerializeField] int seed = 11;

        const float Width = 1920f;
        const float Height = 1080f;

        VisualElement[] elements;
        float[] startX;
        float[] speed;

        void OnEnable()
        {
            var layer = GetComponent<UIDocument>().rootVisualElement.Q("stars");
            if (layer == null)
                return;
            layer.Clear();

            var random = new System.Random(seed);
            elements = new VisualElement[stars];
            startX = new float[stars];
            speed = new float[stars];
            for (int i = 0; i < stars; i++)
            {
                float depth = (float)random.NextDouble();   // 0 = far, 1 = near
                int size = depth > 0.85f ? 4 : depth > 0.5f ? 3 : 2;
                var star = new VisualElement();
                star.AddToClassList("star");
                star.pickingMode = PickingMode.Ignore;
                star.style.width = size;
                star.style.height = size;
                star.style.top = (float)random.NextDouble() * Height;
                star.style.opacity = Mathf.Lerp(0.35f, 1f, depth);
                layer.Add(star);
                elements[i] = star;
                startX[i] = (float)random.NextDouble() * Width;
                speed[i] = Mathf.Lerp(speeds.x, speeds.y, depth);
            }
        }

        void Update()
        {
            if (elements == null)
                return;
            float t = Time.unscaledTime;
            for (int i = 0; i < elements.Length; i++)
            {
                float x = (startX[i] - speed[i] * t) % Width;
                if (x < 0f)
                    x += Width;
                elements[i].style.left = x;
            }
        }
    }
}
