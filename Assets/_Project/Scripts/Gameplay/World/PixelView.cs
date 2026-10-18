using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OneMoreTime.Gameplay.World
{
    /// <summary>
    /// Low-resolution look, as in the SNES Star Fox (GDD 08, experiment): the 3D world is drawn at a small
    /// fixed height and enlarged without smoothing, so every pixel is a visible block. The HUD is a UI
    /// Toolkit overlay, drawn afterwards at full resolution.
    /// Works through the active URP asset's render scale and Point upscaling, and puts both back when the
    /// component is disabled. To see the game without it, untick this component (or delete it).
    /// </summary>
    public sealed class PixelView : MonoBehaviour
    {
        [Tooltip("Height of the 3D world in pixels. The SNES Star Fox is 224; larger means finer pixels. " +
                 "Rounded so each pixel is a whole number of screen pixels. Can be changed while playing.")]
        [SerializeField, Range(90, 1080)] int targetHeight = 270;

        UniversalRenderPipelineAsset asset;
        float savedScale;
        UpscalingFilterSelection savedFilter;

        void OnEnable()
        {
            asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null)
            {
                Debug.LogWarning($"{nameof(PixelView)} needs the Universal Render Pipeline.", this);
                return;
            }
            savedScale = asset.renderScale;
            savedFilter = asset.upscalingFilter;
            asset.upscalingFilter = UpscalingFilterSelection.Point;
            Apply();
        }

        void OnDisable()
        {
            if (asset == null)
                return;
            asset.renderScale = savedScale;
            asset.upscalingFilter = savedFilter;
            asset = null;
        }

        // Follows window resizes and edits of the target height.
        void Update()
        {
            if (asset != null)
                Apply();
        }

        void Apply()
        {
            int block = Mathf.Max(1, Mathf.RoundToInt(Screen.height / (float)targetHeight));
            float scale = 1f / block;
            if (!Mathf.Approximately(asset.renderScale, scale))
                asset.renderScale = scale;
        }
    }
}
