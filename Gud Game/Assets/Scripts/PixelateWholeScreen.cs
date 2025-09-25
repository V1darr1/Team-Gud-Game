using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

public class PixelateWholeScreen : MonoBehaviour
{
    [Header("Hook up these")]
    public Camera targetCamera;
    public RawImage fullscreenImg;

    [Header("Pixel settings")]
    [Tooltip("Target vertical resolution")]
    public int targetVerticalPixels = 180;

    RenderTexture rt;
    int lastW, lastH;

    private void OnEnable()
    {
        if (!targetCamera) targetCamera = Camera.main;
        RebuildRT();
    }

    private void OnDisable()
    {
        if (targetCamera) targetCamera.targetTexture = null;
        if (fullscreenImg) fullscreenImg.texture = null;
        if (rt)
        {
            rt.Release();
            Destroy(rt);
            rt = null;
        }
    }

    private void Update()
    {
        int screenW = Mathf.Max(1, Screen.width);
        int screenH = Mathf.Max(1, Screen.height);
        if (screenW != lastW || screenH != lastH)
            RebuildRT();
    }

    void RebuildRT()
    {
        if (!targetCamera || !fullscreenImg) return;

        int screenW = Mathf.Max(1, Screen.width);
        int screenH = Mathf.Max(1, Screen.height);

        float aspect = (float)screenW / screenH;
        int lowH = Mathf.Max(32, targetVerticalPixels);
        int lowW = Mathf.Max(32, Mathf.RoundToInt(lowH * aspect));

        // If size unchanged, nothing to do
        if (rt && rt.width == lowW && rt.height == lowH) return;

        // Detach the current RT from the camera before destroying it
        if (targetCamera.targetTexture == rt)
            targetCamera.targetTexture = null;

        if (rt)
        {
            rt.Release();
            Destroy(rt);
            rt = null;
        }

        // URP requires a depth buffer for a camera output texture.
        var desc = new RenderTextureDescriptor(lowW, lowH)
        {
            // Color
#if UNITY_2021_2_OR_NEWER
            graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
#else
        colorFormat = RenderTextureFormat.ARGB32,
#endif
            // Depth/Stencil (required)
#if UNITY_2021_2_OR_NEWER
            depthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt,
#else
        depthBufferBits = 24,
#endif
            msaaSamples = 1,           // no MSAA (keeps pixels crisp)
            mipCount = 1,
            sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
            useMipMap = false,
            autoGenerateMips = false
        };

        rt = new RenderTexture(desc)
        {
            filterMode = FilterMode.Point,       // <- pixelation
            wrapMode = TextureWrapMode.Clamp,
            name = "PixelateRT"
        };
        rt.Create();

        // Reattach safely
        targetCamera.targetTexture = rt;
        fullscreenImg.texture = rt;

        lastW = screenW;
        lastH = screenH;
    }
}
