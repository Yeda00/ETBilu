using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OneMoreTime.UI
{
    /// <summary>
    /// Renders the showcase ship into a texture of its own, which the menu draws as a UI layer above the logo, so
    /// the ship can pass in front of a logo that the pixel filter does not touch. The ship keeps the low-resolution
    /// look of the rest of the 3D scene: it is drawn small (<see cref="size"/>) and enlarged with Point sampling
    /// into a screen-sized texture, so the pixels stay square. The camera is drawn by hand here, with the URP
    /// render scale (which the PixelView lowers for the main view) put back to 1 for that one render, so the
    /// small picture is sharp. The ship and this camera sit far from the main camera, so the main view never sees them.
    /// </summary>
    public sealed class ShowcaseView : MonoBehaviour
    {
        [SerializeField] Camera view;
        [Tooltip("Pixels of the ship's picture: the size of a ship pixel on screen is the screen height divided by the height here (the main view uses 270).")]
        [SerializeField] Vector2Int size = new(480, 270);
        [Tooltip("Size of the texture the UI draws: the size of the menu (1080p).")]
        [SerializeField] Vector2Int output = new(1920, 1080);

        RenderTexture small, big;

        /// <summary>The ship's picture, transparent where there is no ship.</summary>
        public RenderTexture Texture
        {
            get
            {
                if (big == null && view != null)
                {
                    small = new RenderTexture(size.x, size.y, 16) { name = "ShowcaseShipSmall", filterMode = FilterMode.Point };
                    small.Create();
                    big = new RenderTexture(output.x, output.y, 0) { name = "ShowcaseShip", filterMode = FilterMode.Point };
                    big.Create();
                    view.targetTexture = small;
                    view.enabled = false;   // drawn by hand in LateUpdate
                }
                return big;
            }
        }

        void Awake() => _ = Texture;

        void LateUpdate()
        {
            if (view == null || small == null || big == null)
                return;

            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float scale = asset != null ? asset.renderScale : 1f;
            if (asset != null)
                asset.renderScale = 1f;
            view.Render();
            if (asset != null)
                asset.renderScale = scale;

            Graphics.Blit(small, big);   // small is Point-sampled: the pixels stay square
        }

        void OnDestroy()
        {
            if (view != null)
                view.targetTexture = null;
            foreach (var texture in new[] { small, big })
            {
                if (texture == null)
                    continue;
                texture.Release();
                Destroy(texture);
            }
        }
    }
}
