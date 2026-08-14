using System;
using Mathcalibur.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mathcalibur.UI
{
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [Header("1. 설정창 루트 오브젝트")]
        [Tooltip("설정창 뒤를 어둡게 덮는 전체화면 딤/배경 오브젝트입니다. 없으면 비워도 됩니다.")]
        [SerializeField] private GameObject dimRoot;
        [Tooltip("실제 설정창 패널 최상위 오브젝트입니다. 열기/닫기 때 켜고 끕니다.")]
        [SerializeField] private GameObject panelRoot;

        [Header("2. 열기 / 닫기 버튼")]
        [Tooltip("설정창을 여는 톱니바퀴/설정 버튼입니다. TitleScene에서 쓰면 연결하세요. BattleScene은 BattleSceneController의 설정 버튼을 써도 됩니다.")]
        [SerializeField] private Button openButton;
        [Tooltip("설정창 X/닫기 버튼입니다.")]
        [SerializeField] private Button closeButton;
        [Tooltip("설정창 뒤로가기/Back 버튼입니다. 닫기 버튼과 같은 버튼을 넣어도 됩니다.")]
        [SerializeField] private Button backButton;

        [Header("3. 볼륨 조절 - 배경음악 BGM")]
        [Tooltip("배경음악 볼륨을 담당하는 Slider 컴포넌트입니다. 슬라이더 루트 오브젝트를 넣으세요.")]
        [SerializeField] private Slider bgmSlider;
        [Tooltip("현재 볼륨 수치만큼 길이가 늘어나고 줄어드는 Bar/Fill 이미지입니다. Slider의 Fill Rect로 자동 연결됩니다.")]
        [SerializeField] private Image bgmFillImage;
        [Tooltip("사용자가 잡고 좌우로 움직이는 동그라미/버튼 Handle 이미지입니다. Slider의 Handle Rect와 Target Graphic으로 자동 연결됩니다.")]
        [SerializeField] private Image bgmHandleImage;
        [Tooltip("배경음악 볼륨 퍼센트 표시 텍스트입니다. 예: 80%")]
        [SerializeField] private TMP_Text bgmPercentText;

        [Header("4. 볼륨 조절 - 효과음 SFX")]
        [Tooltip("효과음 볼륨을 담당하는 Slider 컴포넌트입니다. 슬라이더 루트 오브젝트를 넣으세요.")]
        [SerializeField] private Slider sfxSlider;
        [Tooltip("현재 볼륨 수치만큼 길이가 늘어나고 줄어드는 Bar/Fill 이미지입니다. Slider의 Fill Rect로 자동 연결됩니다.")]
        [SerializeField] private Image sfxFillImage;
        [Tooltip("사용자가 잡고 좌우로 움직이는 동그라미/버튼 Handle 이미지입니다. Slider의 Handle Rect와 Target Graphic으로 자동 연결됩니다.")]
        [SerializeField] private Image sfxHandleImage;
        [Tooltip("효과음 볼륨 퍼센트 표시 텍스트입니다. 예: 80%")]
        [SerializeField] private TMP_Text sfxPercentText;

        [Header("5. 진동 설정")]
        [Tooltip("진동 ON/OFF를 바꾸는 버튼입니다.")]
        [SerializeField] private Button vibrationButton;
        [Tooltip("진동 버튼의 Image입니다. ON/OFF 스프라이트를 바꿀 때 사용합니다.")]
        [SerializeField] private Image vibrationButtonImage;
        [Tooltip("진동 상태 텍스트입니다. Vibration: ON/OFF로 갱신됩니다.")]
        [SerializeField] private TMP_Text vibrationStatusText;
        [Tooltip("진동 켜짐 상태 버튼 이미지입니다.")]
        [SerializeField] private Sprite vibrationOnSprite;
        [Tooltip("진동 꺼짐 상태 버튼 이미지입니다.")]
        [SerializeField] private Sprite vibrationOffSprite;

        [Header("6. 전투 씬 전용 버튼")]
        [Tooltip("현재 스테이지 재시작 버튼입니다. BattleSceneController가 기존 RetryCurrentStage 흐름을 연결합니다.")]
        [SerializeField] private Button retryCurrentStageButton;
        [Tooltip("1스테이지부터 다시 시작 버튼입니다. BattleSceneController가 기존 RestartFromBeginning 흐름을 연결합니다.")]
        [SerializeField] private Button restartFromBeginningButton;
        [Tooltip("타이틀로 돌아가기 버튼입니다. BattleSceneController의 기존 씬 전환/페이드 흐름을 호출합니다.")]
        [SerializeField] private Button returnToTitleButton;

        [Header("7. 타이틀 씬 전용 버튼")]
        [Tooltip("게임 방법/튜토리얼 다시 보기 버튼입니다. 아직 연결할 튜토리얼 기능이 없으면 비워두세요.")]
        [SerializeField] private Button tutorialButton;

        [Header("8. 추가 이벤트 연결용 - 필요할 때만 사용")]
        [Tooltip("현재 스테이지 재시작 버튼 클릭 시 추가로 호출할 이벤트입니다. 보통은 비워둡니다.")]
        [SerializeField] private UnityEvent onRetryCurrentStage;
        [Tooltip("1스테이지부터 다시 시작 버튼 클릭 시 추가로 호출할 이벤트입니다. 보통은 비워둡니다.")]
        [SerializeField] private UnityEvent onRestartFromBeginning;
        [Tooltip("타이틀로 돌아가기 버튼 클릭 시 추가로 호출할 이벤트입니다. 보통은 비워둡니다.")]
        [SerializeField] private UnityEvent onReturnToTitle;
        [Tooltip("게임 방법/튜토리얼 버튼 클릭 시 호출할 이벤트입니다. 필요하면 인스펙터에서 연결하세요.")]
        [SerializeField] private UnityEvent onTutorial;

        private Action _retryCurrentStageAction;
        private Action _restartFromBeginningAction;
        private Action _returnToTitleAction;
        private Action _tutorialAction;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            BindStaticControls();
            Close();
        }

        private void OnEnable()
        {
            SyncVolumeUi();
            RefreshVibrationUi();
        }

        public void ConfigureBattleActions(Action retryCurrentStage, Action restartFromBeginning, Action returnToTitle)
        {
            _retryCurrentStageAction = retryCurrentStage;
            _restartFromBeginningAction = restartFromBeginning;
            _returnToTitleAction = returnToTitle;
            BindActionButtons();
        }

        public void ConfigureTitleActions(Action tutorial = null)
        {
            _tutorialAction = tutorial;
            BindActionButtons();
        }

        public void Open()
        {
            SyncVolumeUi();
            RefreshVibrationUi();
            _isOpen = true;
            SetActive(dimRoot, true);
            SetActive(panelRoot, true);
            panelRoot?.transform.SetAsLastSibling();
        }

        public void Close()
        {
            _isOpen = false;
            SetActive(panelRoot, false);
            SetActive(dimRoot, false);
        }

        public void Toggle()
        {
            if (_isOpen)
            {
                Close();
                return;
            }

            Open();
        }

        private void BindStaticControls()
        {
            BindButton(openButton, Toggle);
            BindButton(closeButton, Close);
            BindButton(backButton, Close);
            BindButton(vibrationButton, ToggleVibration);
            ConfigureSliderVisuals(bgmSlider, bgmFillImage, bgmHandleImage);
            ConfigureSliderVisuals(sfxSlider, sfxFillImage, sfxHandleImage);

            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
                bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
                sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            BindActionButtons();
        }

        private void BindActionButtons()
        {
            BindButton(retryCurrentStageButton, InvokeRetryCurrentStage);
            BindButton(restartFromBeginningButton, InvokeRestartFromBeginning);
            BindButton(returnToTitleButton, InvokeReturnToTitle);
            BindButton(tutorialButton, InvokeTutorial);
        }

        private void SyncVolumeUi()
        {
            var bgmVolume = GameAudioManager.Instance != null ? GameAudioManager.Instance.MusicVolume : 1f;
            var sfxVolume = GameAudioManager.Instance != null ? GameAudioManager.Instance.SfxVolume : 1f;

            if (bgmSlider != null)
            {
                bgmSlider.SetValueWithoutNotify(Mathf.Clamp01(bgmVolume));
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(Mathf.Clamp01(sfxVolume));
            }

            RefreshVolumePercentTexts();
        }

        private void OnBgmVolumeChanged(float value)
        {
            GameAudioManager.Instance?.SetBgmVolume(value);
            RefreshVolumePercentTexts();
        }

        private void OnSfxVolumeChanged(float value)
        {
            GameAudioManager.Instance?.SetSfxVolume(value);
            RefreshVolumePercentTexts();
        }

        private void RefreshVolumePercentTexts()
        {
            if (bgmPercentText != null)
            {
                var value = bgmSlider != null ? bgmSlider.value : GameAudioManager.Instance != null ? GameAudioManager.Instance.MusicVolume : 1f;
                bgmPercentText.text = FormatPercent(value);
            }

            if (sfxPercentText != null)
            {
                var value = sfxSlider != null ? sfxSlider.value : GameAudioManager.Instance != null ? GameAudioManager.Instance.SfxVolume : 1f;
                sfxPercentText.text = FormatPercent(value);
            }
        }

        private void ToggleVibration()
        {
            HapticManager.Instance.ToggleEnabled();
            RefreshVibrationUi();
        }

        private void RefreshVibrationUi()
        {
            var isEnabled = HapticManager.Instance.IsEnabled;
            if (vibrationStatusText != null)
            {
                vibrationStatusText.text = isEnabled ? "Vibration: ON" : "Vibration: OFF";
            }

            if (vibrationButtonImage == null)
            {
                return;
            }

            var sprite = isEnabled ? vibrationOnSprite : vibrationOffSprite;
            if (sprite != null)
            {
                vibrationButtonImage.sprite = sprite;
                vibrationButtonImage.type = Image.Type.Sliced;
                vibrationButtonImage.color = Color.white;
                return;
            }

            vibrationButtonImage.color = isEnabled ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
        }

        private void InvokeRetryCurrentStage()
        {
            Close();
            _retryCurrentStageAction?.Invoke();
            onRetryCurrentStage?.Invoke();
        }

        private void InvokeRestartFromBeginning()
        {
            Close();
            _restartFromBeginningAction?.Invoke();
            onRestartFromBeginning?.Invoke();
        }

        private void InvokeReturnToTitle()
        {
            Close();
            _returnToTitleAction?.Invoke();
            onReturnToTitle?.Invoke();
        }

        private void InvokeTutorial()
        {
            Close();
            _tutorialAction?.Invoke();
            onTutorial?.Invoke();
        }

        private static void ConfigureSliderVisuals(Slider slider, Image fillImage, Image handleImage)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            if (fillImage != null)
            {
                slider.fillRect = fillImage.rectTransform;
            }

            if (handleImage != null)
            {
                slider.handleRect = handleImage.rectTransform;
                slider.targetGraphic = handleImage;
            }
        }

        private static void BindButton(Button button, Action callback)
        {
            if (button == null || callback == null)
            {
                return;
            }

            button.onClick.AddListener(() =>
            {
                GameAudioManager.Instance?.PlayButtonClickSfx();
                callback();
            });
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static string FormatPercent(float value)
        {
            return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }
    }
}
