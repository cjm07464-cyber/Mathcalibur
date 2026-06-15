using UnityEngine;
using UnityEngine.SceneManagement;
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

        private void Awake()
        {
            BindButton(startGameButton, OpenLevelPanel);
            BindButton(quitGameButton, QuitGame);
            BindButton(easyButton, () => SelectDifficulty(GameDifficulty.Easy));
            BindButton(normalButton, () => SelectDifficulty(GameDifficulty.Normal));
            BindButton(hardButton, () => SelectDifficulty(GameDifficulty.Hard));

            if (startBattleButtonRect != null)
            {
                startBattleButtonRect.gameObject.SetActive(false);
            }

            SetLevelPanelVisible(false);
        }

        public void OpenLevelPanel()
        {
            ClearPendingDifficultySelection();
            SetLevelPanelVisible(true);
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
            GameSessionState.SetDifficulty(difficulty);
            SceneManager.LoadScene(battleSceneName);
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
        }

        private void ClearPendingDifficultySelection()
        {
            _pendingDifficulty = null;
            if (_generatedStartBattleButton != null)
            {
                _generatedStartBattleButton.gameObject.SetActive(false);
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback);
        }
    }
}
