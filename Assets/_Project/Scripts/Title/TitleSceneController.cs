using Mathcalibur.Audio;
using Mathcalibur.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mathcalibur.Title
{
    public enum GameDifficulty
    {
        Easy,
        Normal,
        Hard,
    }

    public static class GameSessionState
    {
        public static GameDifficulty SelectedDifficulty { get; private set; } = GameDifficulty.Normal;

        public static void SetDifficulty(GameDifficulty difficulty)
        {
            SelectedDifficulty = difficulty;
        }
    }

    public class TitleSceneController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string battleSceneName = "BattleScene";

        [Header("Transition")]
        [SerializeField] private float fadeOutDuration = 0.75f;
        [SerializeField] private float fadeInDuration = 0.75f;
        [SerializeField] private float musicFadeOutDuration = 0.75f;

        [Header("Main Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button quitGameButton;

        [Header("Level Panel")]
        [SerializeField] private GameObject levelPanelRoot;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private RectTransform startBattleButtonRect;
        [SerializeField] private Sprite easyStartButtonSprite;
        [SerializeField] private Sprite nonEasyStartButtonSprite;

        private GameDifficulty? _pendingDifficulty;
        private Button _generatedStartBattleButton;
        private bool _isTransitioning;
        private RectTransform _startMenuBlackBackgroundRoot;
        private RectTransform _levelMenuContentRoot;
        private readonly Dictionary<RectTransform, Vector2> _normalizedAnchoredPositions = new();

        private void Awake()
        {
            GameAudioManager.Instance?.PlayTitleBgm();
            BindButton(startGameButton, OpenLevelPanel);
            BindButton(quitGameButton, QuitGame);
            BindButton(easyButton, () => SelectDifficulty(GameDifficulty.Easy));
            BindButton(normalButton, () => SelectDifficulty(GameDifficulty.Normal));
            BindButton(hardButton, () => SelectDifficulty(GameDifficulty.Hard));

            EnsureStartMenuBlackBackground();
            CacheLevelMenuResponsiveLayout();

            if (startBattleButtonRect != null)
            {
                startBattleButtonRect.gameObject.SetActive(false);
            }

            SetLevelPanelVisible(false);
        }

        private void Start()
        {
            Canvas.ForceUpdateCanvases();
            ApplyLevelMenuResponsiveLayout();
            StartCoroutine(ApplyResponsiveLayoutNextFrame());
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            ApplyLevelMenuResponsiveLayout();
        }

        public void OpenLevelPanel()
        {
            ClearPendingDifficultySelection();
            SetLevelPanelVisible(true);
            Canvas.ForceUpdateCanvases();
            ApplyLevelMenuResponsiveLayout();
        }

        public void CloseLevelPanel()
        {
            ClearPendingDifficultySelection();
            SetLevelPanelVisible(false);
        }

        public void SelectDifficulty(GameDifficulty difficulty)
        {
            _pendingDifficulty = difficulty;
            ShowStartBattleButton(difficulty);
        }

        public void StartSelectedBattle()
        {
            if (!_pendingDifficulty.HasValue)
            {
                return;
            }

            StartBattle(_pendingDifficulty.Value);
        }

        public void StartBattle(GameDifficulty difficulty)
        {
            if (_isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            SetTitleButtonsInteractable(false);
            GameSessionState.SetDifficulty(difficulty);

            var fadeOutMusic = difficulty == GameDifficulty.Easy;

            SceneTransitionFader.BeginFadeOutLoadSceneFadeIn(
                battleSceneName,
                fadeOutDuration,
                fadeInDuration,
                fadeOutMusic,
                musicFadeOutDuration);
        }


        public void QuitGame()
        {
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void SetLevelPanelVisible(bool visible)
        {
            if (levelPanelRoot != null)
            {
                levelPanelRoot.SetActive(visible);
            }
        }

        private void ShowStartBattleButton(GameDifficulty difficulty)
        {
            if (startBattleButtonRect == null)
            {
                return;
            }

            if (_generatedStartBattleButton == null)
            {
                var buttonObject = startBattleButtonRect.gameObject;
                var buttonImage = buttonObject.GetComponent<Image>();
                if (buttonImage == null)
                {
                    buttonImage = buttonObject.AddComponent<Image>();
                }

                _generatedStartBattleButton = buttonObject.GetComponent<Button>();
                if (_generatedStartBattleButton == null)
                {
                    _generatedStartBattleButton = buttonObject.AddComponent<Button>();
                }
            }

            var image = _generatedStartBattleButton.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = difficulty == GameDifficulty.Easy
                    ? easyStartButtonSprite
                    : nonEasyStartButtonSprite;
                image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                image.preserveAspect = image.sprite != null;
                image.color = Color.white;
            }

            BindButton(_generatedStartBattleButton, StartSelectedBattle);
            startBattleButtonRect.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            ApplyNormalizedAnchoredPosition(startBattleButtonRect);
        }

        private void ClearPendingDifficultySelection()
        {
            _pendingDifficulty = null;
            if (_generatedStartBattleButton != null)
            {
                _generatedStartBattleButton.gameObject.SetActive(false);
            }
        }

        private void SetTitleButtonsInteractable(bool interactable)
        {
            if (startGameButton != null) startGameButton.interactable = interactable;
            if (quitGameButton != null) quitGameButton.interactable = interactable;
            if (easyButton != null) easyButton.interactable = interactable;
            if (normalButton != null) normalButton.interactable = interactable;
            if (hardButton != null) hardButton.interactable = interactable;
            if (_generatedStartBattleButton != null) _generatedStartBattleButton.interactable = interactable;
        }

        private void EnsureStartMenuBlackBackground()
        {
            var startMenuRoot = startGameButton != null ? startGameButton.transform.parent as RectTransform : null;
            if (startMenuRoot == null)
            {
                return;
            }

            var parent = startMenuRoot.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            _startMenuBlackBackgroundRoot = FindOrCreateFullscreenSolidBackground(parent, startMenuRoot.GetSiblingIndex(), "StartMenuBlackBackgroundRuntime", Color.black);
        }

        private static RectTransform FindOrCreateFullscreenSolidBackground(RectTransform parent, int siblingIndex, string name, Color color)
        {
            if (parent == null)
            {
                return null;
            }

            RectTransform background = null;
            for (var i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) is RectTransform candidate && candidate.name == name)
                {
                    background = candidate;
                    break;
                }
            }

            if (background == null)
            {
                var backgroundObject = new GameObject(name, typeof(RectTransform), typeof(Image));
                backgroundObject.layer = parent.gameObject.layer;
                background = backgroundObject.GetComponent<RectTransform>();
                background.SetParent(parent, false);
            }

            background.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            background.localScale = Vector3.one;

            var image = background.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                image.raycastTarget = false;
            }

            return background;
        }

        private void CacheLevelMenuResponsiveLayout()
        {
            _levelMenuContentRoot = easyButton != null
                ? easyButton.transform.parent as RectTransform
                : startBattleButtonRect != null
                    ? startBattleButtonRect.parent as RectTransform
                    : null;

            CacheNormalizedAnchoredPosition(easyButton != null ? easyButton.transform as RectTransform : null);
            CacheNormalizedAnchoredPosition(normalButton != null ? normalButton.transform as RectTransform : null);
            CacheNormalizedAnchoredPosition(hardButton != null ? hardButton.transform as RectTransform : null);
            CacheNormalizedAnchoredPosition(startBattleButtonRect);
        }

        private void ApplyLevelMenuResponsiveLayout()
        {
            ApplyNormalizedAnchoredPosition(easyButton != null ? easyButton.transform as RectTransform : null);
            ApplyNormalizedAnchoredPosition(normalButton != null ? normalButton.transform as RectTransform : null);
            ApplyNormalizedAnchoredPosition(hardButton != null ? hardButton.transform as RectTransform : null);
            ApplyNormalizedAnchoredPosition(startBattleButtonRect);
        }

        private IEnumerator ApplyResponsiveLayoutNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            ApplyLevelMenuResponsiveLayout();
        }

        private void CacheNormalizedAnchoredPosition(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            var parent = rect.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var size = parent.rect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                return;
            }

            _normalizedAnchoredPositions[rect] = new Vector2(rect.anchoredPosition.x / size.x, rect.anchoredPosition.y / size.y);
        }

        private void ApplyNormalizedAnchoredPosition(RectTransform rect)
        {
            if (rect == null || !_normalizedAnchoredPositions.TryGetValue(rect, out var normalized))
            {
                return;
            }

            var parent = rect.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var size = parent.rect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                return;
            }

            rect.anchoredPosition = new Vector2(normalized.x * size.x, normalized.y * size.y);
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                GameAudioManager.Instance?.PlayButtonClickSfx();
                callback?.Invoke();
            });
        }
    }
}
