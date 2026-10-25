using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[DefaultExecutionOrder(10000)]
public class StandaloneResolutionSetter : MonoBehaviour
{
    private static StandaloneResolutionSetter instance;
    private readonly Dictionary<Canvas, RectTransform> viewports = new Dictionary<Canvas, RectTransform>();
    private readonly Dictionary<Camera, Rect> cameraRects = new Dictionary<Camera, Rect>();
    private Canvas barsCanvas;
    private Camera backgroundCamera;
    private readonly RectTransform[] bars = new RectTransform[4];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        instance = null;
        new GameObject("PortraitScreenController").AddComponent<StandaloneResolutionSetter>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
#if UNITY_STANDALONE
        Screen.SetResolution(540, 960, false);
#endif
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureBackgroundCamera();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Establish the reference area before scene controllers capture UI positions.
        ApplyViewport();
        Canvas.ForceUpdateCanvases();
    }

    public static RectTransform GetContentRoot(Canvas canvas)
    {
        if (instance != null && instance.viewports.TryGetValue(canvas, out var root) && root != null)
            return root;
        return canvas.transform as RectTransform;
    }

    private void LateUpdate() => ApplyViewport();

    private void ApplyViewport()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;

        var safeArea = Screen.safeArea;
        if (safeArea.width <= 0f || safeArea.height <= 0f)
            safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
        float scale = Mathf.Min(safeArea.width / 1080f, safeArea.height / 1920f);
        float width = 1080f * scale / Screen.width;
        float height = 1920f * scale / Screen.height;
        var viewport = new Rect(safeArea.center.x / Screen.width - width * 0.5f,
            safeArea.center.y / Screen.height - height * 0.5f, width, height);
        EnsureBackgroundCamera();
        backgroundCamera.depth = -1000f;

        // Preserve each scene camera's placement within the portrait game area.
        foreach (var camera in Camera.allCameras)
        {
            if (camera == backgroundCamera || camera.targetTexture != null) continue;
            backgroundCamera.depth = Mathf.Min(backgroundCamera.depth, camera.depth - 1f);
            if (!cameraRects.TryGetValue(camera, out var original))
            {
                original = camera.rect;
                cameraRects.Add(camera, original);
            }
            camera.rect = new Rect(viewport.x + original.x * width,
                viewport.y + original.y * height, original.width * width, original.height * height);
        }

        // Overlay canvases do not follow Camera.rect, so constrain their UI separately.
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (canvas == barsCanvas || !canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            // The fader looks up its overlay as a direct child and already covers the screen in black.
            if (canvas.GetComponentInParent<Mathcalibur.UI.SceneTransitionFader>() != null) continue;
            if (!viewports.TryGetValue(canvas, out var root))
            {
                var obj = new GameObject("PortraitViewport", typeof(RectTransform), typeof(RectMask2D));
                root = obj.GetComponent<RectTransform>();
                root.SetParent(canvas.transform, false);
                root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = Vector2.zero;
                root.sizeDelta = new Vector2(1080f, 1920f);
                viewports.Add(canvas, root);
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = scale;
            }
            canvas.scaleFactor = scale;
            root.anchoredPosition = (safeArea.center - new Vector2(Screen.width, Screen.height) * 0.5f) / scale;
            // Include UI created later, such as battle HUDs and transition overlays.
            // Move in the original sibling order: reversing this puts backgrounds
            // and full-screen dimmers in front of the game's buttons and artwork.
            for (int i = 0; i < canvas.transform.childCount;)
            {
                var child = canvas.transform.GetChild(i);
                if (child != root) child.SetParent(root, false);
                else i++;
            }
        }

        EnsureBars();
        SetBar(0, Vector2.zero, new Vector2(viewport.xMin, 1));
        SetBar(1, new Vector2(viewport.xMax, 0), Vector2.one);
        SetBar(2, new Vector2(viewport.xMin, 0), new Vector2(viewport.xMax, viewport.yMin));
        SetBar(3, new Vector2(viewport.xMin, viewport.yMax), new Vector2(viewport.xMax, 1));
    }

    private void EnsureBackgroundCamera()
    {
        if (backgroundCamera != null) return;
        var obj = new GameObject("PortraitBackgroundClear", typeof(Camera));
        obj.transform.SetParent(transform, false);
        backgroundCamera = obj.GetComponent<Camera>();
        // Battle's camera intentionally covers only the combat area. Clear the
        // whole target first so uncovered UI pixels cannot retain previous frames.
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        backgroundCamera.backgroundColor = Color.black;
        backgroundCamera.cullingMask = 0;
        backgroundCamera.depth = -1000f;
        backgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);
        backgroundCamera.allowHDR = false;
        backgroundCamera.allowMSAA = false;
        backgroundCamera.useOcclusionCulling = false;
    }

    private void EnsureBars()
    {
        if (barsCanvas != null) return;
        var obj = new GameObject("PortraitBlackBars", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        obj.transform.SetParent(transform, false);
        barsCanvas = obj.GetComponent<Canvas>();
        barsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        barsCanvas.sortingOrder = short.MaxValue;
        for (int i = 0; i < bars.Length; i++)
        {
            var bar = new GameObject("BlackBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(obj.transform, false);
            bar.GetComponent<Image>().color = Color.black;
            bars[i] = bar.GetComponent<RectTransform>();
        }
    }

    private void SetBar(int index, Vector2 min, Vector2 max)
    {
        // A matching 9:16 window needs no black overlay at all.
        bool visible = max.x - min.x > 0.0001f && max.y - min.y > 0.0001f;
        bars[index].gameObject.SetActive(visible);
        if (!visible) return;
        bars[index].anchorMin = min;
        bars[index].anchorMax = max;
        bars[index].offsetMin = bars[index].offsetMax = Vector2.zero;
    }
}
