using System.Collections;
using Mathcalibur.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Mathcalibur.UI
{
    public class GlobalClickAuraManager : MonoBehaviour
    {
        [Header("Aura")]
        [SerializeField] private Sprite auraSprite;
        [SerializeField] private Vector2 auraSize = new Vector2(140f, 140f);
        [SerializeField] private float duration = 0.4f;
        [SerializeField] private float startScale = 0.55f;
        [SerializeField] private float endScale = 1.15f;
        [SerializeField] private Color auraColor = Color.white;

        [Header("Optional")]
        [SerializeField] private Material auraMaterial;

        private Canvas _canvas;
        private RectTransform _layer;

        private static GlobalClickAuraManager _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCanvas();
        }

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    GameAudioManager.Instance?.PlayGeneralTouchSfx();
                    Spawn(touch.position);
                }

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                GameAudioManager.Instance?.PlayGeneralTouchSfx();
                Spawn(Input.mousePosition);
            }
        }

        private void EnsureCanvas()
        {
            if (_canvas != null && _layer != null)
            {
                return;
            }

            var canvasObject = new GameObject("GlobalClickAuraCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue - 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var layerObject = new GameObject("ClickAuraLayer", typeof(RectTransform));
            layerObject.transform.SetParent(canvasObject.transform, false);

            _layer = layerObject.GetComponent<RectTransform>();
            _layer.anchorMin = Vector2.zero;
            _layer.anchorMax = Vector2.one;
            _layer.offsetMin = Vector2.zero;
            _layer.offsetMax = Vector2.zero;
            _layer.pivot = new Vector2(0.5f, 0.5f);
        }

        private void Spawn(Vector2 screenPosition)
        {
            if (auraSprite == null)
            {
                return;
            }

            EnsureCanvas();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _layer,
                    screenPosition,
                    null,
                    out var localPoint))
            {
                return;
            }

            var obj = new GameObject("ClickAura", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            obj.transform.SetParent(_layer, false);

            var rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = localPoint;
            rect.sizeDelta = auraSize;
            rect.localScale = Vector3.one * startScale;

            var image = obj.GetComponent<Image>();
            image.sprite = auraSprite;
            image.color = auraColor;
            image.raycastTarget = false;
            image.preserveAspect = true;

            if (auraMaterial != null)
            {
                image.material = auraMaterial;
            }

            var canvasGroup = obj.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            StartCoroutine(FadeAndDestroy(obj, rect, canvasGroup));
        }

        private IEnumerator FadeAndDestroy(GameObject obj, RectTransform rect, CanvasGroup canvasGroup)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                }

                if (rect != null)
                {
                    var scale = Mathf.Lerp(startScale, endScale, t);
                    rect.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }
}
