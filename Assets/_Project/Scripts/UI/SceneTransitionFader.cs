using System.Collections;
using Mathcalibur.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mathcalibur.UI
{
    public class SceneTransitionFader : MonoBehaviour
    {
        private const string TransitionCanvasName = "SceneTransitionCanvas";
        private const string OverlayName = "FadeOverlay";

        [SerializeField] private CanvasGroup canvasGroup;

        private static SceneTransitionFader _instance;
        private Coroutine _runningTransition;

        public static SceneTransitionFader Instance => EnsureInstance();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCanvasGroup();
            SetAlpha(0f);
            SetBlocksRaycasts(false);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public IEnumerator FadeOut(float duration)
        {
            yield return Fade(1f, duration);
        }

        public IEnumerator FadeIn(float duration)
        {
            yield return Fade(0f, duration);
        }

        public static void BeginFadeOutLoadSceneFadeIn(
            string sceneName,
            float fadeOutDuration,
            float fadeInDuration,
            bool fadeOutMusic,
            float musicFadeDuration)
        {
            var fader = EnsureInstance();

            if (fader._runningTransition != null)
            {
                fader.StopCoroutine(fader._runningTransition);
            }

            fader._runningTransition = fader.StartCoroutine(
                fader.FadeOutLoadSceneFadeInRoutine(
                    sceneName,
                    fadeOutDuration,
                    fadeInDuration,
                    fadeOutMusic,
                    musicFadeDuration));
        }

        // 기존 호출부가 남아 있어도 터지지 않게 호환용으로 유지.
        public static IEnumerator FadeOutLoadSceneFadeIn(
            string sceneName,
            float fadeOutDuration,
            float fadeInDuration,
            bool fadeOutMusic,
            float musicFadeDuration)
        {
            BeginFadeOutLoadSceneFadeIn(
                sceneName,
                fadeOutDuration,
                fadeInDuration,
                fadeOutMusic,
                musicFadeDuration);

            yield break;
        }

        private IEnumerator FadeOutLoadSceneFadeInRoutine(
            string sceneName,
            float fadeOutDuration,
            float fadeInDuration,
            bool fadeOutMusic,
            float musicFadeDuration)
        {
            Coroutine musicCoroutine = null;

            if (fadeOutMusic && GameAudioManager.Instance != null)
            {
                musicCoroutine = StartCoroutine(GameAudioManager.Instance.FadeOutMusic(musicFadeDuration));
            }

            yield return FadeOut(fadeOutDuration);

            if (musicCoroutine != null)
            {
                yield return musicCoroutine;
            }

            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                var loadOperation = SceneManager.LoadSceneAsync(sceneName);
                if (loadOperation != null)
                {
                    while (!loadOperation.isDone)
                    {
                        yield return null;
                    }
                }

                // 새 씬의 Awake/Start가 한 프레임 처리될 시간을 준다.
                yield return null;
            }

            yield return FadeIn(fadeInDuration);

            _runningTransition = null;
            Destroy(gameObject);
        }

        private IEnumerator Fade(float targetAlpha, float duration)
        {
            EnsureCanvasGroup();

            if (canvasGroup == null)
            {
                yield break;
            }

            SetBlocksRaycasts(true);

            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
                SetBlocksRaycasts(targetAlpha > 0f);
                yield break;
            }

            var startAlpha = canvasGroup.alpha;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetAlpha(targetAlpha);
            SetBlocksRaycasts(targetAlpha > 0f);
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        private void SetBlocksRaycasts(bool blocks)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = blocks;
                canvasGroup.interactable = false;
            }
        }

        private static SceneTransitionFader EnsureInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindAnyObjectByType<SceneTransitionFader>();

            if (_instance != null)
            {
                _instance.EnsureCanvasGroup();
                return _instance;
            }

            var root = new GameObject("SceneTransitionFader");
            _instance = root.AddComponent<SceneTransitionFader>();
            _instance.EnsureCanvasGroup();
            return _instance;
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup != null)
            {
                return;
            }

            Canvas canvas = null;

            var canvasTransform = transform.Find(TransitionCanvasName);
            if (canvasTransform != null)
            {
                canvas = canvasTransform.GetComponent<Canvas>();
            }

            if (canvas == null)
            {
                var canvasObject = new GameObject(
                    TransitionCanvasName,
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                canvasObject.transform.SetParent(transform, false);

                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            var overlay = canvas.transform.Find(OverlayName) as RectTransform;

            if (overlay == null)
            {
                var overlayObject = new GameObject(
                    OverlayName,
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(Image));

                overlay = overlayObject.GetComponent<RectTransform>();
                overlay.SetParent(canvas.transform, false);
                overlay.anchorMin = Vector2.zero;
                overlay.anchorMax = Vector2.one;
                overlay.offsetMin = Vector2.zero;
                overlay.offsetMax = Vector2.zero;
                overlay.pivot = new Vector2(0.5f, 0.5f);

                var image = overlayObject.GetComponent<Image>();
                image.color = Color.black;
                image.raycastTarget = true;
            }

            canvasGroup = overlay.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}