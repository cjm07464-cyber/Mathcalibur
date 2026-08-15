using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LitMotion;
using LitMotion.Extensions;
using Mathcalibur.Audio;
using Mathcalibur.Items;
using Mathcalibur.Title;
using Mathcalibur.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mathcalibur.Battle
{
    public enum CombatMode
    {
        Attack,
        Defense,
    }

    public enum EnemyType
    {
        Wolf,
        Orc,
        StoneGolem,
        DemonKing,
    }

    public class BattleSceneController : MonoBehaviour, IItemEffectRuntime
    {
        [Serializable]
        private sealed class EnemyVisualEntry
        {
            [SerializeField] private EnemyType enemyType = EnemyType.Wolf;
            [SerializeField] private GameObject root;
            [SerializeField] private Animator animator;
            [SerializeField] private Transform hitVfxPoint;
            [SerializeField] private string attackTriggerName = "Attack";
            [SerializeField] private string hitTriggerName = "Hit";
            [SerializeField] private string deathTriggerName = string.Empty;
            public EnemyType EnemyType => enemyType;
            public GameObject Root => root;
            public Animator Animator => animator;
            public Transform HitVfxPoint => hitVfxPoint;
            public string AttackTriggerName => attackTriggerName;
            public string HitTriggerName => hitTriggerName;
            public string DeathTriggerName => deathTriggerName;
        }

        [Serializable]
        private sealed class UniqueHudSlot
        {
            [SerializeField] private Button button;
            [SerializeField] private Image slotFrameImage;
            [SerializeField] private Image iconImage;

            public Button Button => button;
            public Image SlotFrameImage => slotFrameImage;
            public Image IconImage => iconImage;
        }

        [Serializable]
        private sealed class UniqueIconEntry
        {
            [SerializeField] private string itemId;
            [SerializeField] private Sprite icon;

            public string ItemId => itemId;
            public Sprite Icon => icon;
        }

        [SerializeField] private BattleConfig config;
        [SerializeField] private BattleAnimationManager battleAnimationManager;
        [Header("Enemy Visuals")]
        [SerializeField] private EnemyVisualEntry[] enemyVisualEntries = Array.Empty<EnemyVisualEntry>();
        [Header("Transition")]
        [SerializeField] private float fadeOutDuration = 0.75f;
        [SerializeField] private float fadeInDuration = 0.75f;
        [SerializeField] private float musicFadeOutDuration = 0.75f;
        [Header("Convenience HUD")]
        [SerializeField] private TMP_Text currentGoldText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text enemyAttackInfoText;
        [SerializeField] private TMP_Text turnInfoText;
        [Header("Stage UI")]
        [Tooltip("현재 진행 중인 스테이지를 표시할 TextMeshPro 텍스트입니다. 예: 3스테이지")]
        [SerializeField] private TMP_Text currentStageDisplayText;
        [Tooltip("현재 적 공격력 숫자만 표시할 TextMeshPro 텍스트입니다.")]
        [SerializeField] private TMP_Text enemyAttackDamageValueText;
        [Tooltip("스테이지/전체 클리어 안내를 띄울 검은 패널 루트입니다.")]
        [SerializeField] private GameObject stageClearPanelRoot;
        [Tooltip("스테이지/전체 클리어 안내 문구를 표시할 TextMeshPro 텍스트입니다.")]
        [SerializeField] private TMP_Text stageClearMessageText;
        [Tooltip("클리어 안내 패널을 보여주는 시간(초)입니다.")]
        [Min(0f)]
        [SerializeField] private float stageClearPanelDisplaySeconds = 1f;
        [Tooltip("모든 스테이지 클리어 시 검은 배경 이미지 알파가 1까지 올라가는 속도입니다.")]
        [Min(0.01f)]
        [SerializeField] private float allStageClearPanelFadeSpeed = 1f;
        [Tooltip("모든 스테이지 클리어 시 검게 페이드할 패널 Image입니다. 비워두면 Stage Clear Panel Root의 Image를 사용합니다.")]
        [SerializeField] private Image allStageClearFadeImage;
        [Tooltip("모든 스테이지 클리어 패널에서 타이틀로 돌아가는 버튼입니다.")]
        [SerializeField] private Button allStageClearReturnToTitleButton;
        [Header("Settings")]
        [SerializeField] private SettingsPanelController settingsPanelController;
        [SerializeField] private TutorialPanelController tutorialPanelController;
        [Min(0f)]
        [SerializeField] private float startingUniqueTutorialDelaySeconds = 0.5f;
        [Min(0f)]
        [SerializeField] private float postStartingUniqueBattleTutorialDelaySeconds = 0.3f;
        [SerializeField] private RectTransform settingsPanelRoot;
        [SerializeField] private Image settingsBackgroundImage;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private TMP_Text bgmPercentText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text sfxPercentText;
        [SerializeField] private Button settingsRestartCurrentStageButton;
        [SerializeField] private Button settingsBeginAtStage1Button;
        [SerializeField] private Button settingsToTitleButton;
        [SerializeField] private Button settingsGoBackButton;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private Button settingsVibrationButton;
        [SerializeField] private Image settingsVibrationButtonImage;
        [SerializeField] private TMP_Text settingsVibrationStatusText;
        [Header("Settings Images")]
        [SerializeField] private Sprite settingsBackgroundSprite;
        [SerializeField] private Sprite settingsBarSprite;
        [SerializeField] private Sprite settingsButtonSprite;
        [SerializeField] private Sprite settingsRestartButtonSprite;
        [SerializeField] private Sprite settingsBeginAtStage1ButtonSprite;
        [SerializeField] private Sprite settingsToTitleButtonSprite;
        [SerializeField] private Sprite settingsGoBackButtonSprite;
        [SerializeField] private Sprite settingsSliderHandleSprite;
        [SerializeField] private Sprite settingsVibrationOnSprite;
        [SerializeField] private Sprite settingsVibrationOffSprite;
        [Header("Drag Count")]
        [SerializeField] private TMP_Text dragCountText;
        [Header("Auto Line Clear Damage UI")]
        [Tooltip("자동 줄 제거 데미지 표시 패널입니다.")]
        [SerializeField] private GameObject autoLineClearDamagePanelRoot;
        [Tooltip("자동 줄 제거 누적 데미지 숫자를 표시할 Text입니다.")]
        [SerializeField] private TMP_Text autoLineClearDamageText;
        [Tooltip("자동 줄 제거 데미지 숫자가 초당 몇씩 올라갈지 정합니다.")]
        [Min(1f)]
        [SerializeField] private float autoLineClearDamageCountUpSpeed = 60f;
        [Tooltip("자동 줄 제거 최종 데미지 적용 시 텍스트가 커지는 배율입니다.")]
        [Min(0f)]
        [SerializeField] private float autoLineClearDamagePunchScale = 0.25f;
        [Tooltip("자동 줄 제거 최종 데미지 텍스트 크기 연출 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float autoLineClearDamagePunchDuration = 0.25f;
        [Tooltip("자동 줄 제거 최종 데미지 결과를 화면에 유지하는 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float autoLineClearDamageResultDisplaySeconds = 0.5f;
        [Header("Unique Inventory HUD")]
        [SerializeField] private UniqueHudSlot[] uniqueHudSlots = Array.Empty<UniqueHudSlot>();
        [SerializeField] private Sprite uniqueEmptySlotSprite;
        [SerializeField] private UniqueIconEntry[] uniqueHudIconSprites = Array.Empty<UniqueIconEntry>();
        [Header("Unique HUD Info Panel")]
        [Tooltip("획득 유니크 아이템 설명창 전체 루트입니다. 직접 만든 패널을 연결하면 자동 생성 대신 사용합니다.")]
        [SerializeField] private RectTransform uniqueHudInfoPanelRoot;
        [Tooltip("설명창 뒤 어두운 배경/오버레이 루트입니다. 없으면 panelRoot를 사용합니다.")]
        [SerializeField] private RectTransform uniqueHudInfoOverlayRoot;
        [Tooltip("설명창에 표시할 유니크 아이콘 위치입니다. 비워두면 아이콘 프리뷰는 생략됩니다.")]
        [SerializeField] private RectTransform uniqueHudInfoPreviewRoot;
        [Tooltip("설명창 아이템 이름 텍스트입니다.")]
        [SerializeField] private TMP_Text uniqueHudInfoNameText;
        [Tooltip("설명창 아이템 설명 텍스트입니다.")]
        [SerializeField] private TMP_Text uniqueHudInfoDescriptionText;
        [Tooltip("설명창을 닫는 확인 버튼입니다.")]
        [SerializeField] private Button uniqueHudInfoConfirmButton;
        private BattleTileView[,] _grid;
        private RectTransform _boardRoot;
        private RectTransform _boardContainer;
        private RectTransform _tileLayoutRoot;
        private RectTransform _gameplayContainer;
        private BattleHudView _hud;
        private Camera _uiCamera;
        private Camera _shakeCamera;
        private BattleBoardLayoutReference _boardLayoutReference;
        private readonly List<BattleTileView> _selection = new();
        private readonly Dictionary<int, int> _numberWeightModifiers = new();
        private readonly Dictionary<string, int> _operatorWeightModifiers = new(StringComparer.Ordinal);
        private readonly Dictionary<int, int> _cachedNumberWeights = new();
        private readonly Dictionary<string, int> _cachedOperatorWeights = new(StringComparer.Ordinal);
        private readonly HashSet<BattleTileView> _unique9TransformedTiles = new();
        private readonly List<Button> _freeButtons = new();
        private readonly List<Button> _paidButtons = new();
        private readonly List<BattleBoardLayoutReference.ItemSlotReference> _freeButtonSlotReferences = new();
        private readonly List<BattleBoardLayoutReference.ItemSlotReference> _paidButtonSlotReferences = new();
        private readonly List<ShopSlotData> _freeSlots = new();
        private readonly List<ShopSlotData> _paidSlots = new();
        private readonly ItemEligibilityChecker _itemEligibilityChecker = new();
        private readonly ItemEffectResolver _itemEffectResolver = new();

        private bool _dragging;
        private bool _enemyDeathHandledThisStage;
        private int _playerHp;
        private int _enemyHp;
        private int _playerShield;
        private int _validTurnCount;
        private int _unique1UsedOneCountThisStage;
        private int _currentPlayerMaxHp;
        private int _currentMaxConnectionLength;
        private float _cellSize;
        private CombatMode _currentCombatMode = CombatMode.Attack;
        private RectTransform _shopOverlayRoot;
        private RectTransform _shopPanel;
        private RectTransform _shopDimRoot;
        private Transform _shopPanelOriginalParent;
        private int _shopPanelOriginalSiblingIndex;
        private RectTransform _shopConfirmPanel;
        private RectTransform _startUniqueOverlayRoot;
        private RectTransform _startUniquePanel;
        private RectTransform _activeItemConfirmOverlayRoot;
        private RectTransform _activeItemConfirmPanel;
        private RectTransform _defeatOverlayRoot;
        private RectTransform _defeatPanel;
        private RectTransform _defeatBlackBackgroundRoot;
        private RectTransform _uniqueHudInfoOverlayRoot;
        private RectTransform _uniqueHudInfoPanel;
        private RectTransform _uniqueHudInfoPreviewRoot;
        private Image _stageClearFadeImage;
        private Color _stageClearFadeImageBaseColor;
        private bool _stageClearFadeImageBaseColorCaptured;
        private string _stageClearDefaultMessage = string.Empty;
        private TMP_Text _uniqueHudInfoTitleText;
        private TMP_Text _uniqueHudInfoDescriptionText;
        private GameObject _uniqueHudInfoPreviewInstance;
        private RectTransform _mobileExitOverlayRoot;
        private RectTransform _mobileExitPanel;
        private RectTransform _runtimeStatusPanel;
        private RectTransform _settingsDimRoot;
        private RectTransform _settingsPanel;
        private TMP_Text _shopGoldText;
        private TMP_Text _rerollText;
        private TMP_Text _shopConfirmTitleText;
        private TMP_Text _shopConfirmDescriptionText;
        private TMP_Text _shopConfirmCostText;
        private RectTransform _shopConfirmPreviewRoot;
        private GameObject _shopConfirmPreviewInstance;
        private TMP_Text _startUniqueExplainTitleText;
        private TMP_Text _activeItemConfirmTitleText;
        private TMP_Text _activeItemConfirmDescriptionText;
        private TMP_Text _defeatTitleText;
        private TMP_Text _defeatDescriptionText;
        private TMP_Text _defeatMaxDamageText;
        private Button _attackModeButton;
        private Button _defenseModeButton;
        private Button _killEnemyButton;
        private Button _bagButton;
        private RectTransform _bagPanelRoot;
        private RectTransform _bagDimRoot;
        private Transform _bagPanelOriginalParent;
        private int _bagPanelOriginalSiblingIndex;
        private Vector2? _bagButtonNormalizedPosition;
        private Vector2? _bagPanelNormalizedPosition;
        private Vector2? _bagButtonLeftAnchoredPosition;
        private Vector2? _bagPanelLeftAnchoredPosition;
        private readonly List<BattleBoardLayoutReference.BagItemSlotReference> _bagItemSlotReferences = new();
        private Button _percentageButton;
        private RectTransform _percentagePanelRoot;
        private RectTransform _percentageDimRoot;
        private Transform _percentagePanelOriginalParent;
        private int _percentagePanelOriginalSiblingIndex;
        private Vector2? _percentageButtonNormalizedPosition;
        private Vector2? _percentagePanelNormalizedPosition;
        private Vector2? _percentageButtonRightAnchoredPosition;
        private Vector2? _percentagePanelRightAnchoredPosition;
        private Vector2? _boardPanelNormalizedPosition;
        private readonly Dictionary<RectTransform, Vector2> _percentageBarBaseSizes = new();
        private bool _freePurchaseDone;
        private bool _isResolvingTurn;
        private bool _shopOpen;
        private bool _unique1TransformReady;
        private bool _startingUniqueSelectionOpen;
        private bool _startingUniqueSelectionResolved;
        private bool _startingUniqueConfirmTransitioning;
        private bool _activeItemConfirmOpen;
        private bool _defeatOverlayOpen;
        private bool _defeatTransitioning;
        private bool _mobileExitOverlayOpen;
        private bool _settingsPanelOpen;
        private MotionHandle _autoLineClearDamageCountMotionHandle;
        private MotionHandle _autoLineClearDamagePunchMotionHandle;
        private bool _startingUniqueTutorialShownThisRun;
        private bool _postStartingUniqueBattleTutorialShownThisRun;
        private bool _shopTutorialShownThisRun;
        private Coroutine _startingUniqueTutorialCoroutine;
        private Coroutine _postStartingUniqueBattleTutorialCoroutine;
        private bool _waitingToShowStartingUniqueAfterTutorial;
        private int? _pendingStartingUniqueSelectionIndex;
        private string _pendingActiveItemId;
        private ShopSelectionContext? _pendingShopSelection;
        private RuntimePlayerState _playerState;
        private StageDefinition _currentStage;
        private EnemyType[] _stageEnemyOrder;
        private ItemDatabase _itemDatabase;
        private RuntimeItemInventory _runtimeItemInventory;
        private Button _rerollButton;
        private Button _nextStageButton;
        private Button _shopPurchaseButton;
        private RectTransform _shopConfirmDimRoot;
        private TMP_FontAsset _resolvedUiFont;
        private Coroutine _cameraShakeCoroutine;
        private Quaternion _cameraOriginalLocalRotation;
        private readonly List<ItemData> _startingUniqueCandidates = new();
        private readonly List<Button> _startingUniqueButtons = new();
        private readonly List<BattleBoardLayoutReference.StartingUniqueLayoutReference.SlotReference> _startingUniqueSlotReferences = new();
        private readonly List<GameObject> _startingUniqueSelectionAuras = new();
        private readonly List<string> _acquiredUniqueHudItemIds = new();
        private readonly Dictionary<string, UniqueItemPresentationText> _uniqueItemPresentationTexts = new(StringComparer.Ordinal);
        private readonly Dictionary<Image, Vector3> _slotIconBaseScales = new();
        private readonly HashSet<string> _missingUniqueHudIconWarnings = new(StringComparer.Ordinal);
        private int _lastAutoLineClearDamage;
        private int _leftEdgeNumberLineClearStreak;
        private int _rightEdgeNumberLineClearStreak;
        private bool _forceOperatorOnNextLeftEdgeRefill;
        private bool _forceOperatorOnNextRightEdgeRefill;
        private bool _usingRuntimeStartingUniqueFallback;
        private int _highestDamageThisRun;
        private RuntimeStageSnapshot _stageStartSnapshot;
        private const string SlotIconChildName = "Icon";
        private static readonly string[] SlotAuraChildNames = { "Auta", "Aura" };

        private enum LineClearDirection
        {
            Horizontal,
            Vertical,
        }

        private sealed class LineClearGroup
        {
            public LineClearGroup(TileKind kind, LineClearDirection direction, List<BattleTileView> tiles)
            {
                Kind = kind;
                Direction = direction;
                Tiles = tiles;
            }

            public TileKind Kind { get; }
            public LineClearDirection Direction { get; }
            public List<BattleTileView> Tiles { get; }
        }

        private sealed class UniqueItemPresentationText
        {
            public string Number;
            public string NameKo;
            public string CardSummaryKo;
            public string TendencyKo;
            public string ConditionKo;
            public string EffectKo;
            public string NoteKo;
        }

        private const string DefaultBattleConfigResourcePath = "BattleConfig";
        private const int MaxAutoLineClearLoops = 10;
        private const int MaxStage = 6;
        private const float StageClearGoldRewardMultiplier = 1.5f;
        private const float EdgeColumnOperatorChanceMultiplier = 0.5f;
        private const int OperatorLineClearFixedDamage = 10;
        private const int EdgeNumberLineClearForceThreshold = 3;
        private const int MaxForcedEdgeOperatorsPerRefill = 2;
        private const int InitialBoardMinLineOperators = 1;
        private const int InitialBoardMaxLineOperators = 2;
        private const int OperatorWeightBiasAmount = 5;
        private const int FallbackUniqueInventoryHudSlots = 9;
        private const string Unique1ItemId = "UNIQUE_1_AWAKENED_ONE";
        private const string Unique2ItemId = "UNIQUE_2_PROBABILITY_STRIKE";
        private const string Unique3ItemId = "UNIQUE_3_TRINITY";
        private const string Unique4ItemId = "UNIQUE_4_ORDER_OF_OPERATIONS";
        private const string Unique5ItemId = "UNIQUE_5_SHIELD_NUMBER";
        private const string Unique6ItemId = "UNIQUE_6_FLAT_WEALTH";
        private const string Unique7ItemId = "UNIQUE_7_DAVID";
        private const string Unique8ItemId = "UNIQUE_8_PERCENT_WEALTH";
        private const string Unique9ItemId = "UNIQUE_9_ODINS_NINE_TRIALS";
        private const string HealingPotionItemId = "ITEM_HEALING_POTION";
        private const string AttackPotionItemId = "ITEM_ATTACK_POTION";

        private const float BagResponsiveOffsetX = -0.015f;
        private const float PercentageResponsiveOffsetX = 0.015f;

        private void Awake()
        {
            if (config == null)
            {
                config = Resources.Load<BattleConfig>(DefaultBattleConfigResourcePath);
            }

            if (config == null)
            {
                Debug.LogWarning($"BattleConfig missing resource at Resources/{DefaultBattleConfigResourcePath}.asset. Using runtime defaults.");
                config = ScriptableObject.CreateInstance<BattleConfig>();
            }
            if (battleAnimationManager == null)
            {
                battleAnimationManager = FindAnyObjectByType<BattleAnimationManager>();
            }
            ResolveSettingsPanelController();
            ResolveTutorialPanelController();
            ResolveUiFont();
            CaptureStageClearDefaultMessage();
            BindButton(allStageClearReturnToTitleButton, OnMenuButtonPressed);
            SetAllStageClearReturnToTitleButtonVisible(false);
            SetStageClearPanelVisible(false);
            SetAutoLineClearDamagePanelVisible(false);
            _itemDatabase = ItemDatabase.LoadDefault();
            _runtimeItemInventory = new RuntimeItemInventory();
            LoadUniqueItemPresentationTexts();
            _currentPlayerMaxHp = config.PlayerMaxHp;
            _currentMaxConnectionLength = config.MaxExpressionLength;
            RebuildCachedSpawnWeights();
        }

        private void Start()
        {
            EnsureUiExists();
            Canvas.ForceUpdateCanvases();
            ResolveBoardLayoutReference();
            RefreshUniqueInventoryHud();
            BuildInitialBoard();
            InitBattle();
            TryPlayBattleBgmAfterStartingUniqueSelection();
            ApplyResponsiveScenePositions();
            StartCoroutine(ApplyResponsiveLayoutNextFrame());
            StartCoroutine(ValidateBattleSceneStartup());
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_grid == null || _tileLayoutRoot == null)
            {
                return;
            }

            UpdateLayoutRegions();
            ApplyResponsiveScenePositions();
            RefreshBoardVisualLayout();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                HandleBackNavigation();
                return;
            }

            if (_waitingToShowStartingUniqueAfterTutorial && !ShouldBlockTutorialInputFrame())
            {
                _waitingToShowStartingUniqueAfterTutorial = false;
                EnsureStartingUniqueSelection();
                CaptureStageStartSnapshotIfReady();
                TryPlayBattleBgmAfterStartingUniqueSelection();
            }

            if (_playerHp <= 0 || _enemyHp <= 0 || _shopOpen || _startingUniqueSelectionOpen || _activeItemConfirmOpen || _defeatOverlayOpen || IsUniqueHudInfoPanelOpen() || IsSettingsPanelOpen() || ShouldBlockTutorialInputFrame() || _isResolvingTurn || IsBagPanelOpen() || IsPercentagePanelOpen())
            {
                return;
            }

            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (ShouldBlockBoardInput(Input.mousePosition))
                {
                    _dragging = false;
                    return;
                }

                _dragging = true;
                ClearSelectionVisual();
                TryAddTileAtScreen(Input.mousePosition);
            }

            if (_dragging && Input.GetMouseButton(0))
            {
                TryAddTileAtScreen(Input.mousePosition);
            }

            if (_dragging && Input.GetMouseButtonUp(0))
            {
                _dragging = false;
                ConfirmSelection();
            }
        }

        private bool ShouldBlockBoardInput(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var result in results)
            {
                var go = result.gameObject;
                if (go == null)
                {
                    continue;
                }

                if (_tileLayoutRoot != null && go.transform.IsChildOf(_tileLayoutRoot))
                {
                    continue;
                }

                if (go.GetComponentInParent<Button>() != null)
                {
                    return true;
                }

                if (go.GetComponentInParent<ScrollRect>() != null)
                {
                    return true;
                }

                if (go.GetComponentInParent<Toggle>() != null)
                {
                    return true;
                }

                if (go.GetComponentInParent<Slider>() != null)
                {
                    return true;
                }

                if (go.GetComponentInParent<InputField>() != null || go.GetComponentInParent<TMP_InputField>() != null)
                {
                    return true;
                }

                if ((_shopOpen && _shopOverlayRoot != null && go.transform.IsChildOf(_shopOverlayRoot)) ||
                    (_startingUniqueSelectionOpen && _startUniqueOverlayRoot != null && go.transform.IsChildOf(_startUniqueOverlayRoot)) ||
                    (_activeItemConfirmOpen && _activeItemConfirmOverlayRoot != null && go.transform.IsChildOf(_activeItemConfirmOverlayRoot)) ||
                    (_defeatOverlayOpen && _defeatOverlayRoot != null && go.transform.IsChildOf(_defeatOverlayRoot)) ||
                    (_settingsPanelOpen && settingsPanelRoot != null && go.transform.IsChildOf(settingsPanelRoot)))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureUiExists()
        {
            var canvas = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate != null && candidate.GetComponentInParent<SceneTransitionFader>() == null);

            if (canvas == null)
            {
                var canvasGo = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            _uiCamera = null;
            _boardLayoutReference = FindAnyObjectByType<BattleBoardLayoutReference>();
            _shakeCamera = Camera.main ?? FindAnyObjectByType<Camera>();
            if (_shakeCamera != null)
            {
                _cameraOriginalLocalRotation = _shakeCamera.transform.localRotation;
            }

            BuildHudAndBoardRoots(canvas.transform as RectTransform);
        }

        private void BuildHudAndBoardRoots(RectTransform canvasRoot)
        {
            _boardContainer = CreateUiPanel("BoardContainer", canvasRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            _boardContainer.pivot = new Vector2(0.5f, 0f);
            _boardContainer.anchoredPosition = Vector2.zero;

            var boardContainerFitter = _boardContainer.GetComponent<AspectRatioFitter>();
            if (boardContainerFitter == null)
            {
                boardContainerFitter = _boardContainer.gameObject.AddComponent<AspectRatioFitter>();
            }

            boardContainerFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            boardContainerFitter.aspectRatio = GetBoardAspectRatio();

            _boardRoot = CreateUiPanel("BoardArea", _boardContainer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bg = _boardRoot.gameObject.AddComponent<Image>();
            bg.sprite = config.BoardBackgroundSprite;
            bg.type = Image.Type.Simple;
            bg.preserveAspect = false;
            bg.color = config.BoardBackgroundSprite != null ? config.BoardBackgroundSpriteTint : config.BoardBackgroundColor;
            bg.raycastTarget = false;

            _gameplayContainer = CreateUiPanel("GameplayContainer", canvasRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var gameplayBg = _gameplayContainer.gameObject.AddComponent<Image>();
            gameplayBg.color = new Color(0f, 0f, 0f, 0f);
            gameplayBg.raycastTarget = false;

            var hudRoot = CreateUiPanel("CombatArea", _gameplayContainer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _hud = hudRoot.gameObject.AddComponent<BattleHudView>();

            var hudReference = _boardLayoutReference != null ? _boardLayoutReference.BattleHud : null;
            typeof(BattleHudView).GetField("playerHpText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference?.PlayerHpText);
            typeof(BattleHudView).GetField("defenseText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference?.DefenseText);
            typeof(BattleHudView).GetField("enemyHpText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference?.EnemyHpText);
            typeof(BattleHudView).GetField("countdownText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference?.TurnText);
            typeof(BattleHudView).GetField("enemyHpBarImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference?.EnemyHpBarImage);
            typeof(BattleHudView).GetField("expressionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference?.ExpressionText);
            typeof(BattleHudView).GetField("resultText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference?.ResultText);
            typeof(BattleHudView).GetField("validationSymbolText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference?.ValidationSymbolText);
            typeof(BattleHudView).GetField("validationLabelText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference?.ValidationLabelText);
            typeof(BattleHudView).GetField("validColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference != null ? hudReference.ValidColor : Color.green);
            typeof(BattleHudView).GetField("invalidColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, hudReference != null ? hudReference.InvalidColor : Color.red);
            _killEnemyButton = hudReference?.KillEnemyButton;
            if (_killEnemyButton != null)
            {
                BindButton(_killEnemyButton, KillCurrentEnemyForDebug);
            }

            BuildCombatModeControls(hudRoot);
            BuildConvenienceHud(hudRoot);
            BuildSettingsPanel(canvasRoot);
            BindBagLayout();
            BindPercentageLayout();
            BuildStartingUniqueSelectionOverlay(canvasRoot);
            BuildShopPanel();
            BuildUniqueHudInfoOverlay(canvasRoot);
            BuildActiveItemConfirmOverlay(canvasRoot);
            BuildDefeatOverlay(canvasRoot);
            BuildMobileExitOverlay(canvasRoot);
            EnsureShoppingParentsActive();
            HideSceneBoundStartingUniqueLayout();
            HideSceneBoundShopLayout();
            HideSceneBoundDefeatLayout();
            Canvas.ForceUpdateCanvases();
            ResolveBoardLayoutReference();
            UpdateLayoutRegions();
        }

        private void BuildCombatModeControls(RectTransform hudRoot)
        {
            var panel = CreateUiPanel("ModePanel", hudRoot, new Vector2(0.62f, 0.72f), new Vector2(0.96f, 0.90f), Vector2.zero, Vector2.zero);

            _attackModeButton = ResolveCombatModeButton(CombatMode.Attack, _boardLayoutReference != null ? _boardLayoutReference.AttackModeButton : null);
            _defenseModeButton = ResolveCombatModeButton(CombatMode.Defense, _boardLayoutReference != null ? _boardLayoutReference.DefenseModeButton : null);
            RefreshCombatModeButtons();
        }

        private void BuildConvenienceHud(RectTransform hudRoot)
        {
            if (hudRoot == null)
            {
                return;
            }

            HideConvenienceStatusHud();

            if (settingsPanelController == null || !settingsPanelController.HasOpenButton)
            {
                var settingsButton = CreateActionButton(hudRoot, "Settings", new Vector2(0.86f, 0.94f), ToggleSettingsPanel, false, 180f, 64f, config.ShopFontSizeScale);
                SetButtonTextColor(settingsButton, config.ShopButtonTextColor);
            }
        }

        private void HideConvenienceStatusHud()
        {
            if (_runtimeStatusPanel != null)
            {
                _runtimeStatusPanel.gameObject.SetActive(false);
            }

            SetTextObjectActive(currentGoldText, false);
            SetTextObjectActive(stageText, false);
            SetTextObjectActive(enemyAttackInfoText, false);
            SetTextObjectActive(turnInfoText, false);
        }

        private static void SetTextObjectActive(TMP_Text text, bool active)
        {
            if (text != null)
            {
                text.gameObject.SetActive(active);
            }
        }

        private void ResolveSettingsPanelController()
        {
            if (settingsPanelController != null)
            {
                return;
            }

            settingsPanelController = GetComponent<SettingsPanelController>();
            if (settingsPanelController != null)
            {
                return;
            }

            settingsPanelController = FindAnyObjectByType<SettingsPanelController>(FindObjectsInactive.Include);
        }

        private void ResolveTutorialPanelController()
        {
            if (tutorialPanelController == null)
            {
                tutorialPanelController = FindAnyObjectByType<TutorialPanelController>(FindObjectsInactive.Include);
            }
        }

        private void TryOpenStartingUniqueSelectionTutorial()
        {
            if (_startingUniqueTutorialShownThisRun
                || _startingUniqueSelectionResolved
                || !_startingUniqueSelectionOpen
                || _startingUniqueConfirmTransitioning
                || _startingUniqueTutorialCoroutine != null)
            {
                return;
            }

            _startingUniqueTutorialCoroutine = StartCoroutine(OpenStartingUniqueSelectionTutorialAfterDelay());
        }

        private void TryOpenPostStartingUniqueBattleTutorial()
        {
            if (_postStartingUniqueBattleTutorialShownThisRun
                || !_startingUniqueSelectionResolved
                || _startingUniqueSelectionOpen
                || _postStartingUniqueBattleTutorialCoroutine != null)
            {
                return;
            }

            _postStartingUniqueBattleTutorialCoroutine = StartCoroutine(OpenPostStartingUniqueBattleTutorialAfterDelay());
        }

        private void TryOpenShopTutorial()
        {
            if (_shopTutorialShownThisRun)
            {
                return;
            }

            if (TryOpenTutorialPage(5))
            {
                _shopTutorialShownThisRun = true;
            }
        }

        private IEnumerator OpenStartingUniqueSelectionTutorialAfterDelay()
        {
            var delaySeconds = Mathf.Max(0f, startingUniqueTutorialDelaySeconds);
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            _startingUniqueTutorialCoroutine = null;
            if (_startingUniqueTutorialShownThisRun
                || _startingUniqueSelectionResolved
                || !_startingUniqueSelectionOpen
                || _startingUniqueConfirmTransitioning)
            {
                yield break;
            }

            if (TryOpenTutorialPage(0))
            {
                _startingUniqueTutorialShownThisRun = true;
            }
        }

        private IEnumerator OpenPostStartingUniqueBattleTutorialAfterDelay()
        {
            var delaySeconds = Mathf.Max(0f, postStartingUniqueBattleTutorialDelaySeconds);
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            _postStartingUniqueBattleTutorialCoroutine = null;
            if (_postStartingUniqueBattleTutorialShownThisRun || !_startingUniqueSelectionResolved || _startingUniqueSelectionOpen)
            {
                yield break;
            }

            if (TryOpenTutorialRange(1, 4))
            {
                _postStartingUniqueBattleTutorialShownThisRun = true;
            }
        }

        private bool TryOpenTutorialPage(int pageIndex)
        {
            ResolveTutorialPanelController();
            if (tutorialPanelController == null)
            {
                return false;
            }

            tutorialPanelController.OpenPage(pageIndex);
            return tutorialPanelController.IsOpen;
        }

        private bool TryOpenTutorialRange(int startIndex, int endIndex)
        {
            ResolveTutorialPanelController();
            if (tutorialPanelController == null)
            {
                return false;
            }

            tutorialPanelController.OpenRange(startIndex, endIndex);
            return tutorialPanelController.IsOpen;
        }

        private void ResetTutorialRunState()
        {
            CancelPendingTutorialCoroutines();
            _startingUniqueTutorialShownThisRun = false;
            _postStartingUniqueBattleTutorialShownThisRun = false;
            _shopTutorialShownThisRun = false;
            _waitingToShowStartingUniqueAfterTutorial = false;
        }

        private void CancelPendingTutorialCoroutines()
        {
            if (_startingUniqueTutorialCoroutine != null)
            {
                StopCoroutine(_startingUniqueTutorialCoroutine);
                _startingUniqueTutorialCoroutine = null;
            }

            if (_postStartingUniqueBattleTutorialCoroutine != null)
            {
                StopCoroutine(_postStartingUniqueBattleTutorialCoroutine);
                _postStartingUniqueBattleTutorialCoroutine = null;
            }
        }

        private void CloseTutorialPanelIfOpen()
        {
            if (tutorialPanelController != null && tutorialPanelController.IsOpen)
            {
                tutorialPanelController.Close();
            }
        }

        private bool IsTutorialPanelOpen()
        {
            return tutorialPanelController != null && tutorialPanelController.IsOpen;
        }

        private bool ShouldBlockTutorialInputFrame()
        {
            return tutorialPanelController != null && (tutorialPanelController.IsOpen || tutorialPanelController.ClosedThisFrame);
        }

        private void BuildSettingsPanel(RectTransform canvasRoot)
        {
            ResolveSettingsPanelController();

            if (settingsPanelController != null)
            {
                settingsPanelController.ConfigureOpenAction(ToggleSettingsPanel);
                settingsPanelController.ConfigureCloseAction(CloseSettingsPanel);
                settingsPanelController.ConfigureBattleActions(
                    OnSettingsRetryCurrentStage,
                    OnSettingsRestartFromBeginning,
                    OnSettingsReturnToTitle);
                settingsPanelController.Close();
                _settingsPanelOpen = false;
                return;
            }

            if (canvasRoot == null)
            {
                return;
            }

            _settingsDimRoot = CreateUiPanel("SettingsDimOverlay", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dimImage = _settingsDimRoot.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.55f);
            dimImage.raycastTarget = true;

            if (settingsPanelRoot == null)
            {
                _settingsPanel = CreateCenteredSquarePanel("SettingsPanel", _settingsDimRoot, config.ShopConfirmPanelSide);
                settingsPanelRoot = _settingsPanel;
            }
            else
            {
                settingsPanelRoot.SetParent(_settingsDimRoot, false);
            }

            settingsBackgroundImage = settingsBackgroundImage != null
                ? settingsBackgroundImage
                : settingsPanelRoot.GetComponent<Image>() ?? settingsPanelRoot.gameObject.AddComponent<Image>();
            ApplySettingsPanelVisual();

            var titleText = CreateText("SettingsTitle", settingsPanelRoot, new Vector2(0.5f, 0.88f), 42f, config.ShopFontSizeScale);
            titleText.rectTransform.anchorMin = titleText.rectTransform.anchorMax = new Vector2(0.5f, 0.88f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            titleText.rectTransform.sizeDelta = new Vector2(720f, 72f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = config.ShopPanelTextColor;
            titleText.text = "Settings";

            EnsureSettingsVolumeControls();
            EnsureSettingsVibrationControl();
            EnsureSettingsActionButtons();
            BindSettingsControls();

            _settingsDimRoot.gameObject.SetActive(false);
            settingsPanelRoot.gameObject.SetActive(false);
            _settingsPanelOpen = false;
        }

        private void ApplySettingsPanelVisual()
        {
            if (settingsBackgroundImage == null)
            {
                return;
            }

            var sprite = settingsBackgroundSprite != null ? settingsBackgroundSprite : config.ShopConfirmPanelSprite;
            ApplyPanelVisual(settingsBackgroundImage, sprite, config.ShopConfirmPanelColor);
        }

        private void EnsureSettingsVolumeControls()
        {
            if (bgmSlider == null)
            {
                bgmSlider = CreateSettingsSlider("BGM", new Vector2(0.50f, 0.74f), out var percentText);
                bgmPercentText = percentText;
            }

            if (sfxSlider == null)
            {
                sfxSlider = CreateSettingsSlider("SFX", new Vector2(0.50f, 0.62f), out var percentText);
                sfxPercentText = percentText;
            }
        }

        private Slider CreateSettingsSlider(string label, Vector2 anchor, out TMP_Text percentText)
        {
            var labelText = CreateText(label + "Label", settingsPanelRoot, new Vector2(0.18f, anchor.y), 28f, config.ShopFontSizeScale);
            labelText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            labelText.rectTransform.sizeDelta = new Vector2(180f, 48f);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = config.ShopPanelTextColor;
            labelText.text = label;

            var sliderRoot = CreateUiPanel(label + "Slider", settingsPanelRoot, anchor, anchor, Vector2.zero, Vector2.zero);
            sliderRoot.pivot = new Vector2(0.5f, 0.5f);
            sliderRoot.sizeDelta = new Vector2(430f, 40f);

            var background = new GameObject("Background", typeof(Image)).GetComponent<Image>();
            background.transform.SetParent(sliderRoot, false);
            background.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            background.rectTransform.anchorMax = new Vector2(1f, 0.65f);
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;
            background.sprite = settingsBarSprite;
            background.type = settingsBarSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = new Color(0.22f, 0.22f, 0.22f, 1f);

            var fillArea = CreateUiPanel("Fill Area", sliderRoot, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f), Vector2.zero, Vector2.zero);
            var fill = new GameObject("Fill", typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(fillArea, false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            fill.sprite = settingsBarSprite;
            fill.type = settingsBarSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            fill.color = new Color(0.82f, 0.82f, 0.82f, 1f);

            var handleArea = CreateUiPanel("Handle Slide Area", sliderRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handle = new GameObject("Handle", typeof(Image)).GetComponent<Image>();
            handle.transform.SetParent(handleArea, false);
            handle.rectTransform.sizeDelta = new Vector2(34f, 34f);
            handle.sprite = settingsSliderHandleSprite;
            handle.color = Color.white;

            var slider = sliderRoot.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;

            percentText = CreateText(label + "PercentText", settingsPanelRoot, new Vector2(0.84f, anchor.y), 26f, config.ShopFontSizeScale);
            percentText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            percentText.rectTransform.sizeDelta = new Vector2(140f, 48f);
            percentText.alignment = TextAlignmentOptions.Center;
            percentText.color = config.ShopPanelTextColor;
            return slider;
        }

        private void EnsureSettingsActionButtons()
        {
            settingsRestartCurrentStageButton ??= CreateSettingsButton("Retry Stage", new Vector2(0.5f, 0.38f), OnSettingsRetryCurrentStage, settingsRestartButtonSprite);
            settingsBeginAtStage1Button ??= CreateSettingsButton("Restart Run", new Vector2(0.5f, 0.28f), OnSettingsRestartFromBeginning, settingsBeginAtStage1ButtonSprite);
            settingsToTitleButton ??= CreateSettingsButton("Title", new Vector2(0.5f, 0.18f), OnSettingsReturnToTitle, settingsToTitleButtonSprite);
            settingsGoBackButton ??= CreateSettingsButton("Back", new Vector2(0.5f, 0.08f), CloseSettingsPanel, settingsGoBackButtonSprite);
            settingsCloseButton ??= settingsGoBackButton;
        }

        private void EnsureSettingsVibrationControl()
        {
            settingsVibrationButton ??= CreateSettingsButton("Vibration: ON", new Vector2(0.5f, 0.50f), OnSettingsVibrationPressed, settingsVibrationOnSprite);
            settingsVibrationButtonImage ??= GetButtonImage(settingsVibrationButton);
            settingsVibrationStatusText ??= GetButtonVisualRefs(settingsVibrationButton)?.Label ?? settingsVibrationButton.GetComponentInChildren<TextMeshProUGUI>();
            RefreshSettingsVibrationUi();
        }

        private Button CreateSettingsButton(string label, Vector2 anchor, Action callback, Sprite sprite)
        {
            var button = CreateActionButton(settingsPanelRoot, label, anchor, callback, false, config.ShopConfirmActionButtonWidth * 1.55f, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            ApplySettingsButtonSprite(button, sprite);
            SetButtonTextColor(button, config.ShopButtonTextColor);
            return button;
        }

        private void ApplySettingsButtonSprite(Button button, Sprite overrideSprite)
        {
            var image = GetButtonImage(button);
            if (image == null)
            {
                return;
            }

            var sprite = overrideSprite != null ? overrideSprite : settingsButtonSprite;
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
        }

        private void BindSettingsControls()
        {
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

            BindButton(settingsRestartCurrentStageButton, OnSettingsRetryCurrentStage);
            BindButton(settingsBeginAtStage1Button, OnSettingsRestartFromBeginning);
            BindButton(settingsToTitleButton, OnSettingsReturnToTitle);
            BindButton(settingsGoBackButton, CloseSettingsPanel);
            BindButton(settingsCloseButton, CloseSettingsPanel);
            BindButton(settingsVibrationButton, OnSettingsVibrationPressed);
        }

        private Button ResolveCombatModeButton(CombatMode mode, BattleBoardLayoutReference.CombatModeButtonReference buttonReference)
        {
            if (buttonReference?.Image == null)
            {
                return null;
            }

            var button = buttonReference.Image.GetComponent<Button>();
            if (button == null)
            {
                button = buttonReference.Image.gameObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SetCombatMode(mode));
            ApplyCombatModeButtonBaseState(button, buttonReference, false);
            return button;
        }

        private void RefreshCombatModeButtons()
        {
            ApplyCombatModeButtonState(_attackModeButton, _boardLayoutReference != null ? _boardLayoutReference.AttackModeButton : null, _currentCombatMode == CombatMode.Attack);
            ApplyCombatModeButtonState(_defenseModeButton, _boardLayoutReference != null ? _boardLayoutReference.DefenseModeButton : null, _currentCombatMode == CombatMode.Defense);
        }

        private void ApplyCombatModeButtonState(Button button, BattleBoardLayoutReference.CombatModeButtonReference buttonReference, bool isSelected)
        {
            if (button == null || buttonReference?.Image == null)
            {
                return;
            }

            ApplyCombatModeButtonBaseState(button, buttonReference, isSelected);
        }

        private static void ApplyCombatModeButtonBaseState(Button button, BattleBoardLayoutReference.CombatModeButtonReference buttonReference, bool isSelected)
        {
            if (button == null || buttonReference?.Image == null)
            {
                return;
            }

            var image = buttonReference.Image;
            var normalSprite = buttonReference.GetNormalSprite();
            var sprite = isSelected && buttonReference.SelectedSprite != null ? buttonReference.SelectedSprite : normalSprite;
            var color = isSelected ? buttonReference.SelectedColor : buttonReference.NormalColor;
            ApplyButtonVisual(button, sprite, color);
            SetButtonTextColor(button, isSelected ? buttonReference.SelectedTextColor : buttonReference.NormalTextColor);
        }

        private void BuildStartingUniqueSelectionOverlay(RectTransform canvasRoot)
        {
            _startingUniqueButtons.Clear();
            _startingUniqueSlotReferences.Clear();
            _startingUniqueSelectionAuras.Clear();
            _usingRuntimeStartingUniqueFallback = false;

            if (TryBuildStartingUniqueSceneLayout())
            {
                return;
            }

            CreateRuntimeStartingUniqueSelectionOverlay(canvasRoot);
        }

        private void CreateRuntimeStartingUniqueSelectionOverlay(RectTransform canvasRoot)
        {
            _usingRuntimeStartingUniqueFallback = true;

            _startUniqueOverlayRoot = CreateUiPanel("StartingUniqueOverlay", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var overlayImage = _startUniqueOverlayRoot.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.78f);
            overlayImage.raycastTarget = true;

            _startUniquePanel = CreateCenteredSquarePanel("StartingUniquePanel", _startUniqueOverlayRoot, config.ShopMainPanelSide);
            var panelImage = _startUniquePanel.gameObject.AddComponent<Image>();
            ApplyPanelVisual(panelImage, config.StartingUniqueMainPanelSprite, config.StartingUniqueMainPanelColor);

            var title = CreateText("StartingUniqueTitle", _startUniquePanel, new Vector2(0.5f, 0.88f), 42f, config.ShopFontSizeScale);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0.88f);
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title.rectTransform.sizeDelta = new Vector2(760f, 70f);
            title.alignment = TextAlignmentOptions.Center;
            title.text = "시작 유니크 선택";
            title.color = config.StartingUniquePanelTextColor;

            var subtitle = CreateText("StartingUniqueSubtitle", _startUniquePanel, new Vector2(0.5f, 0.78f), 24f, config.ShopFontSizeScale);
            subtitle.rectTransform.anchorMin = subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.78f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.sizeDelta = new Vector2(800f, 60f);
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.text = "이름만 먼저 보여줍니다. 눌러서 설명을 확인한 뒤 결정합니다.";
            subtitle.color = config.StartingUniquePanelTextColor;

            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var button = CreateActionButton(_startUniquePanel, $"유니크 {i + 1}", new Vector2((i + 0.5f) / 3f, 0.42f), () => OpenStartingUniqueConfirmPanel(index), false, config.StartingUniqueButtonWidth, config.StartingUniqueButtonHeight, config.ShopFontSizeScale, config.StartingUniqueSelectionButtonStyle);
                button.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                SetButtonTextColor(button, config.StartingUniqueButtonTextColor);
                _startingUniqueButtons.Add(button);
            }

            var backButton = CreateActionButton(_startUniquePanel, "뒤로가기", new Vector2(0.5f, 0.10f), ReturnToTitleScene, false, config.StartingUniqueButtonWidth, config.StartingUniqueButtonHeight, config.ShopFontSizeScale, config.StartingUniqueSelectionButtonStyle);
            SetButtonTextColor(backButton, config.StartingUniqueButtonTextColor);

            BuildStartingUniqueExplainBindings();
            _startUniqueOverlayRoot.gameObject.SetActive(false);
        }

        private bool TryBuildStartingUniqueSceneLayout()
        {
            var layout = _boardLayoutReference?.StartingUniqueLayout;
            if (layout == null || !layout.HasSceneLayout)
            {
                return false;
            }

            _startUniqueOverlayRoot = layout.OverlayRoot != null ? layout.OverlayRoot : layout.PanelRoot;
            _startUniquePanel = layout.PanelRoot != null ? layout.PanelRoot : layout.OverlayRoot;
            if (_startUniqueOverlayRoot == null || _startUniquePanel == null)
            {
                return false;
            }

            var itemSlots = layout.ItemSlots ?? Array.Empty<BattleBoardLayoutReference.StartingUniqueLayoutReference.SlotReference>();
            var auraObjects = layout.SelectionAuraObjects ?? Array.Empty<GameObject>();
            for (var i = 0; i < itemSlots.Length; i++)
            {
                var slotReference = itemSlots[i];
                _startingUniqueSlotReferences.Add(slotReference);
                _startingUniqueSelectionAuras.Add(i < auraObjects.Length ? auraObjects[i] : null);
                if (slotReference?.Button == null)
                {
                    _startingUniqueButtons.Add(null);
                    continue;
                }

                var index = i;
                BindButton(slotReference.Button, () => OpenStartingUniqueConfirmPanel(index));
                _startingUniqueButtons.Add(slotReference.Button);
            }

            BuildStartingUniqueExplainBindings();
            BindButton(ResolveStartingUniqueBackButton(layout), ReturnToTitleScene);
            SetStartingUniqueSelectionAura(null);
            _startUniqueOverlayRoot.gameObject.SetActive(false);
            return true;
        }

        private Button ResolveStartingUniqueBackButton(BattleBoardLayoutReference.StartingUniqueLayoutReference layout)
        {
            if (layout == null)
            {
                return null;
            }

            if (layout.BackButton != null)
            {
                return layout.BackButton;
            }

            var searchRoots = new[] { layout.OverlayRoot, layout.PanelRoot };
            foreach (var root in searchRoots)
            {
                var backTransform = root != null ? root.Find("BackImage") : null;
                if (backTransform == null)
                {
                    continue;
                }

                var button = backTransform.GetComponent<Button>();
                if (button == null)
                {
                    button = backTransform.gameObject.AddComponent<Button>();
                }

                var image = backTransform.GetComponent<Image>();
                if (image != null)
                {
                    button.targetGraphic = image;
                }

                return button;
            }

            return null;
        }

        private void ReturnToTitleScene()
        {
            OnMenuButtonPressed();
        }

        private void HideSceneBoundStartingUniqueLayout()
        {
            var layout = _boardLayoutReference?.StartingUniqueLayout;
            if (layout == null || !layout.HasSceneLayout)
            {
                return;
            }

            if (_defeatBlackBackgroundRoot != null)
            {
                _defeatBlackBackgroundRoot.gameObject.SetActive(false);
            }

            if (layout.PanelRoot != null)
            {
                layout.PanelRoot.gameObject.SetActive(false);
            }

            if (layout.OverlayRoot != null)
            {
                layout.OverlayRoot.gameObject.SetActive(false);
            }
        }

        private void EnsureShoppingParentsActive()
        {
            EnsureParentActive(_startUniqueOverlayRoot);
            EnsureParentActive(_shopOverlayRoot);
        }

        private static void EnsureParentActive(Component child)
        {
            if (child == null || child.transform.parent == null)
            {
                return;
            }

            child.transform.parent.gameObject.SetActive(true);
        }

        private bool TryBuildShopSceneLayout()
        {
            var layout = _boardLayoutReference?.ShopLayout;
            if (layout == null || !layout.HasSceneLayout)
            {
                return false;
            }

            _shopOverlayRoot = layout.OverlayRoot != null ? layout.OverlayRoot : layout.PanelRoot;
            _shopPanel = layout.PanelRoot != null ? layout.PanelRoot : layout.OverlayRoot;
            if (_shopOverlayRoot == null || _shopPanel == null)
            {
                return false;
            }
            _shopDimRoot = EnsureFullscreenDimOverlay(_shopDimRoot, "ShopDimOverlay", config.ShopDimColor);
            SetDimOverlayVisible(_shopDimRoot, false);

            _shopGoldText = layout.GoldText;
            _rerollButton = layout.RerollButton;
            _rerollText = layout.RerollText;
            _nextStageButton = layout.NextStageButton;

            if (_rerollButton != null)
            {
                BindButton(_rerollButton, OnRerollPressed);
            }

            if (_nextStageButton != null)
            {
                BindButton(_nextStageButton, OnNextStagePressed);
            }

            BindShopSlotReferences(layout.FreeItemSlots, true, _freeButtons, _freeButtonSlotReferences);
            BindShopSlotReferences(layout.PaidItemSlots, false, _paidButtons, _paidButtonSlotReferences);

            BuildShopConfirmPanel();
            _shopOverlayRoot.gameObject.SetActive(false);
            return true;
        }

        private void HideSceneBoundShopLayout()
        {
            var layout = _boardLayoutReference?.ShopLayout;
            if (layout == null || !layout.HasSceneLayout)
            {
                return;
            }

            if (_defeatBlackBackgroundRoot != null)
            {
                _defeatBlackBackgroundRoot.gameObject.SetActive(false);
            }

            if (layout.PanelRoot != null)
            {
                layout.PanelRoot.gameObject.SetActive(false);
            }

            if (layout.OverlayRoot != null)
            {
                layout.OverlayRoot.gameObject.SetActive(false);
            }
        }

        private void BindShopSlotReferences(
            BattleBoardLayoutReference.ItemSlotReference[] slotReferences,
            bool isFree,
            List<Button> buttonTargets,
            List<BattleBoardLayoutReference.ItemSlotReference> referenceTargets)
        {
            if (slotReferences == null)
            {
                return;
            }

            for (var i = 0; i < slotReferences.Length; i++)
            {
                var slotReference = slotReferences[i];
                referenceTargets.Add(slotReference);
                if (slotReference?.Button == null)
                {
                    buttonTargets.Add(null);
                    continue;
                }

                var index = i;
                BindButton(slotReference.Button, () => OnShopSlotPressed(isFree, index));
                buttonTargets.Add(slotReference.Button);
            }
        }

        private void BuildActiveItemConfirmOverlay(RectTransform canvasRoot)
        {
            _activeItemConfirmOverlayRoot = CreateUiPanel("ActiveItemConfirmOverlay", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var overlayImage = _activeItemConfirmOverlayRoot.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.72f);
            overlayImage.raycastTarget = true;

            _activeItemConfirmPanel = CreateCenteredSquarePanel("ActiveItemConfirmPanel", _activeItemConfirmOverlayRoot, config.ShopConfirmPanelSide);
            var panelImage = _activeItemConfirmPanel.gameObject.AddComponent<Image>();
            ApplyPanelVisual(panelImage, config.ShopConfirmPanelSprite, config.ShopConfirmPanelColor);

            _activeItemConfirmTitleText = CreateText("ActiveItemConfirmTitle", _activeItemConfirmPanel, new Vector2(0.5f, 0.84f), 42f, config.ShopFontSizeScale);
            _activeItemConfirmTitleText.rectTransform.anchorMin = _activeItemConfirmTitleText.rectTransform.anchorMax = new Vector2(0.5f, 0.84f);
            _activeItemConfirmTitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _activeItemConfirmTitleText.rectTransform.sizeDelta = new Vector2(760f, 70f);
            _activeItemConfirmTitleText.alignment = TextAlignmentOptions.Center;
            _activeItemConfirmTitleText.color = config.ShopPanelTextColor;

            _activeItemConfirmDescriptionText = CreateText("ActiveItemConfirmDescription", _activeItemConfirmPanel, new Vector2(0.5f, 0.52f), 28f, config.ShopFontSizeScale);
            _activeItemConfirmDescriptionText.rectTransform.anchorMin = _activeItemConfirmDescriptionText.rectTransform.anchorMax = new Vector2(0.5f, 0.52f);
            _activeItemConfirmDescriptionText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _activeItemConfirmDescriptionText.rectTransform.sizeDelta = new Vector2(760f, 280f);
            _activeItemConfirmDescriptionText.alignment = TextAlignmentOptions.TopLeft;
            _activeItemConfirmDescriptionText.enableWordWrapping = true;
            _activeItemConfirmDescriptionText.overflowMode = TextOverflowModes.Overflow;
            _activeItemConfirmDescriptionText.color = config.ShopPanelTextColor;

            var cancelButton = CreateActionButton(_activeItemConfirmPanel, "취소", new Vector2(0.28f, 0.10f), CloseActiveItemConfirmPanel, false, config.ShopConfirmActionButtonWidth, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(cancelButton, config.ShopButtonTextColor);
            var confirmButton = CreateActionButton(_activeItemConfirmPanel, "사용", new Vector2(0.72f, 0.10f), ConfirmPendingActiveItemUse, false, config.ShopConfirmActionButtonWidth, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(confirmButton, config.ShopButtonTextColor);
            _activeItemConfirmOverlayRoot.gameObject.SetActive(false);
        }

        private void OpenActiveItemConfirmPanel(ItemData item)
        {
            if (item == null || _activeItemConfirmOverlayRoot == null || _activeItemConfirmPanel == null)
            {
                return;
            }

            _pendingActiveItemId = item.itemId;
            _activeItemConfirmOpen = true;
            _activeItemConfirmTitleText.text = item.displayName;
            _activeItemConfirmDescriptionText.text = $"현재 체력이 이미 최대치입니다.\n정말 {item.displayName}을(를) 사용하시겠습니까?\n\n사용하면 아이템은 소모되고 체력은 회복되지 않습니다.";
            _activeItemConfirmOverlayRoot.gameObject.SetActive(true);
            _activeItemConfirmPanel.gameObject.SetActive(true);
        }

        private void CloseActiveItemConfirmPanel()
        {
            _pendingActiveItemId = null;
            _activeItemConfirmOpen = false;
            if (_activeItemConfirmOverlayRoot != null)
            {
                _activeItemConfirmOverlayRoot.gameObject.SetActive(false);
            }
        }

        private void ConfirmPendingActiveItemUse()
        {
            if (string.IsNullOrEmpty(_pendingActiveItemId))
            {
                CloseActiveItemConfirmPanel();
                return;
            }

            var itemId = _pendingActiveItemId;
            CloseActiveItemConfirmPanel();
            TryUseActiveItemNow(itemId);
        }

        private void BuildDefeatOverlay(RectTransform canvasRoot)
        {
            if (TryBuildDefeatSceneLayout())
            {
                return;
            }

            _defeatOverlayRoot = CreateUiPanel("DefeatOverlay", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var overlayImage = _defeatOverlayRoot.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.78f);
            overlayImage.raycastTarget = true;

            _defeatPanel = CreateCenteredSquarePanel("DefeatPanel", _defeatOverlayRoot, config.ShopConfirmPanelSide);
            var panelImage = _defeatPanel.gameObject.AddComponent<Image>();
            ApplyPanelVisual(panelImage, config.ShopConfirmPanelSprite, config.ShopConfirmPanelColor);

            _defeatTitleText = CreateText("DefeatTitle", _defeatPanel, new Vector2(0.5f, 0.82f), 42f, config.ShopFontSizeScale);
            _defeatTitleText.rectTransform.anchorMin = _defeatTitleText.rectTransform.anchorMax = new Vector2(0.5f, 0.82f);
            _defeatTitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _defeatTitleText.rectTransform.sizeDelta = new Vector2(760f, 70f);
            _defeatTitleText.alignment = TextAlignmentOptions.Center;
            _defeatTitleText.color = config.ShopPanelTextColor;
            _defeatTitleText.text = "패배";

            _defeatDescriptionText = CreateText("DefeatDescription", _defeatPanel, new Vector2(0.5f, 0.52f), 28f, config.ShopFontSizeScale);
            _defeatDescriptionText.rectTransform.anchorMin = _defeatDescriptionText.rectTransform.anchorMax = new Vector2(0.5f, 0.52f);
            _defeatDescriptionText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _defeatDescriptionText.rectTransform.sizeDelta = new Vector2(760f, 220f);
            _defeatDescriptionText.alignment = TextAlignmentOptions.Center;
            _defeatDescriptionText.enableWordWrapping = true;
            _defeatDescriptionText.overflowMode = TextOverflowModes.Overflow;
            _defeatDescriptionText.color = config.ShopPanelTextColor;
            _defeatDescriptionText.text = "다시 시작하거나 메뉴로 나갈 수 있습니다.";

            _defeatMaxDamageText = CreateText("DefeatMaxDamage", _defeatPanel, new Vector2(0.5f, 0.36f), 24f, config.ShopFontSizeScale);
            _defeatMaxDamageText.rectTransform.anchorMin = _defeatMaxDamageText.rectTransform.anchorMax = new Vector2(0.5f, 0.36f);
            _defeatMaxDamageText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _defeatMaxDamageText.rectTransform.sizeDelta = new Vector2(760f, 60f);
            _defeatMaxDamageText.alignment = TextAlignmentOptions.Center;
            _defeatMaxDamageText.color = config.ShopPanelTextColor;
            _defeatMaxDamageText.text = BuildDefeatMaxDamageText();

            var retryButton = CreateActionButton(_defeatPanel, "현재 스테이지 재도전", new Vector2(0.5f, 0.24f), RetryCurrentStageWithFade, false, config.ShopConfirmActionButtonWidth * 1.6f, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(retryButton, config.ShopButtonTextColor);
            var restartButton = CreateActionButton(_defeatPanel, "처음부터 다시 하기", new Vector2(0.5f, 0.13f), RestartFromBeginningWithFade, false, config.ShopConfirmActionButtonWidth * 1.6f, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(restartButton, config.ShopButtonTextColor);
            var menuButton = CreateActionButton(_defeatPanel, "타이틀로 돌아가기", new Vector2(0.5f, 0.02f), OnMenuButtonPressed, false, config.ShopConfirmActionButtonWidth * 1.6f, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(menuButton, config.ShopButtonTextColor);

            _defeatOverlayRoot.gameObject.SetActive(false);
        }

        private void OpenDefeatOverlay()
        {
            if (_defeatOverlayOpen || _defeatTransitioning)
            {
                return;
            }

            _defeatOverlayOpen = true;
            GameAudioManager.Instance?.PlayDefeatSfx();
            SetGameplayInteractionEnabled(false);
            CloseMobileExitOverlay();
            CloseBagPanel();
            ClosePercentagePanel();
            CloseActiveItemConfirmPanel();
            CloseShopConfirmPanel();
            if (_defeatMaxDamageText != null)
            {
                _defeatMaxDamageText.text = BuildDefeatMaxDamageText();
            }

            StartCoroutine(OpenDefeatOverlayWithFadeRoutine());
        }

        private IEnumerator OpenDefeatOverlayWithFadeRoutine()
        {
            _defeatTransitioning = true;
            yield return SceneTransitionFader.Instance.FadeOut(fadeOutDuration);
            ShowDefeatOverlayVisuals();
            yield return SceneTransitionFader.Instance.FadeIn(fadeInDuration);
            _defeatTransitioning = false;
        }

        private void ShowDefeatOverlayVisuals()
        {
            if (_defeatBlackBackgroundRoot != null)
            {
                EnsureHierarchyActive(_defeatBlackBackgroundRoot);
                _defeatBlackBackgroundRoot.SetAsLastSibling();
                _defeatBlackBackgroundRoot.gameObject.SetActive(true);
            }

            if (_defeatOverlayRoot != null)
            {
                EnsureHierarchyActive(_defeatOverlayRoot);
                _defeatOverlayRoot.SetAsLastSibling();
                _defeatOverlayRoot.gameObject.SetActive(true);
            }

            if (_defeatPanel != null)
            {
                EnsureHierarchyActive(_defeatPanel);
                _defeatPanel.gameObject.SetActive(true);
                _defeatPanel.SetAsLastSibling();
            }
        }

        private bool TryBuildDefeatSceneLayout()
        {
            var layout = _boardLayoutReference?.DefeatLayout;
            if (layout == null || !layout.HasSceneLayout)
            {
                return false;
            }

            _defeatOverlayRoot = layout.OverlayRoot != null ? layout.OverlayRoot : layout.PanelRoot;
            _defeatPanel = layout.PanelRoot != null ? layout.PanelRoot : layout.OverlayRoot;
            _defeatMaxDamageText = layout.MaxDamageText;

            if (_defeatMaxDamageText != null)
            {
                _defeatMaxDamageText.text = BuildDefeatMaxDamageText();
            }

            BindButton(layout.RetryCurrentStageButton, RetryCurrentStageWithFade);
            BindButton(layout.RestartFromBeginningButton, RestartFromBeginningWithFade);
            BindButton(layout.ReturnToTitleButton, OnMenuButtonPressed);
            BindButton(layout.DebugOpenButton, OpenDefeatOverlayForDebug);

            EnsureDefeatBlackBackground();

            if (_defeatOverlayRoot != null)
            {
                _defeatOverlayRoot.gameObject.SetActive(false);
            }

            if (_defeatBlackBackgroundRoot != null)
            {
                _defeatBlackBackgroundRoot.gameObject.SetActive(false);
            }

            return _defeatOverlayRoot != null || _defeatPanel != null;
        }

        private void EnsureDefeatBlackBackground()
        {
            var backgroundSource = _defeatOverlayRoot != null ? _defeatOverlayRoot : _defeatPanel;
            if (backgroundSource == null)
            {
                return;
            }

            var parent = backgroundSource.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            _defeatBlackBackgroundRoot = FindOrCreateFullscreenSolidBackground(parent, backgroundSource.GetSiblingIndex(), "DefeatBlackBackgroundRuntime", Color.black);
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

        private void HideSceneBoundDefeatLayout()
        {
            var layout = _boardLayoutReference?.DefeatLayout;
            if (layout == null || !layout.HasSceneLayout)
            {
                return;
            }

            if (_defeatBlackBackgroundRoot != null)
            {
                _defeatBlackBackgroundRoot.gameObject.SetActive(false);
            }

            if (layout.PanelRoot != null)
            {
                layout.PanelRoot.gameObject.SetActive(false);
            }

            if (layout.OverlayRoot != null)
            {
                layout.OverlayRoot.gameObject.SetActive(false);
            }
        }

        private void HideDefeatOverlayVisuals()
        {
            if (_defeatOverlayRoot != null)
            {
                _defeatOverlayRoot.gameObject.SetActive(false);
            }

            if (_defeatBlackBackgroundRoot != null)
            {
                _defeatBlackBackgroundRoot.gameObject.SetActive(false);
            }
        }

        private void RestartFromBeginning()
        {
            if (_cameraShakeCoroutine != null)
            {
                StopCoroutine(_cameraShakeCoroutine);
                _cameraShakeCoroutine = null;
            }

            if (_shakeCamera != null)
            {
                _shakeCamera.transform.localRotation = _cameraOriginalLocalRotation;
            }

            _isResolvingTurn = false;
            _shopOpen = false;
            _freePurchaseDone = false;
            _startingUniqueSelectionOpen = false;
            _startingUniqueSelectionResolved = false;
            _activeItemConfirmOpen = false;
            _defeatOverlayOpen = false;
            _mobileExitOverlayOpen = false;
            _pendingStartingUniqueSelectionIndex = null;
            _pendingActiveItemId = null;
            _pendingShopSelection = null;
            _dragging = false;
            ResetTutorialRunState();
            CloseTutorialPanelIfOpen();
            _startingUniqueCandidates.Clear();
            _highestDamageThisRun = 0;
            _stageStartSnapshot = null;

            if (_shopOverlayRoot != null)
            {
                _shopOverlayRoot.gameObject.SetActive(false);
            }
            SetDimOverlayVisible(_shopDimRoot, false);
            SetDimOverlayVisible(_shopConfirmDimRoot, false);
            SetDimOverlayVisible(_bagDimRoot, false);
            SetDimOverlayVisible(_percentageDimRoot, false);
            CloseSettingsPanel();
            RestoreShopPanelParent();

            if (_startUniqueOverlayRoot != null)
            {
                ClearStartingUniqueExplainTexts();
                _startUniqueOverlayRoot.gameObject.SetActive(false);
            }

            SetGameplayInteractionEnabled(true);

            if (_activeItemConfirmOverlayRoot != null)
            {
                _activeItemConfirmOverlayRoot.gameObject.SetActive(false);
            }

            HideDefeatOverlayVisuals();

            if (_mobileExitOverlayRoot != null)
            {
                _mobileExitOverlayRoot.gameObject.SetActive(false);
            }

            _playerState = new RuntimePlayerState();
            _stageEnemyOrder = null;
            _runtimeItemInventory = new RuntimeItemInventory();
            ResetUniqueInventoryHudState();
            _numberWeightModifiers.Clear();
            _operatorWeightModifiers.Clear();
            _currentPlayerMaxHp = config.PlayerMaxHp;
            _currentMaxConnectionLength = config.MaxExpressionLength;
            _playerHp = _currentPlayerMaxHp;
            _playerShield = 0;
            _validTurnCount = 0;
            _unique1UsedOneCountThisStage = 0;
            _unique1TransformReady = false;
            _currentCombatMode = CombatMode.Attack;

            ResetStageLocalBattleState();
            InitBattle();
        }

        private void RetryCurrentStage()
        {
            if (_stageStartSnapshot == null)
            {
                RestartFromBeginning();
                return;
            }

            if (_cameraShakeCoroutine != null)
            {
                StopCoroutine(_cameraShakeCoroutine);
                _cameraShakeCoroutine = null;
            }

            if (_shakeCamera != null)
            {
                _shakeCamera.transform.localRotation = _cameraOriginalLocalRotation;
            }

            _isResolvingTurn = false;
            _shopOpen = false;
            _freePurchaseDone = false;
            _startingUniqueSelectionOpen = false;
            _activeItemConfirmOpen = false;
            _defeatOverlayOpen = false;
            _mobileExitOverlayOpen = false;
            _pendingStartingUniqueSelectionIndex = null;
            _pendingActiveItemId = null;
            _pendingShopSelection = null;
            _dragging = false;
            CancelPendingTutorialCoroutines();
            _waitingToShowStartingUniqueAfterTutorial = false;
            CloseTutorialPanelIfOpen();
            _startingUniqueCandidates.Clear();

            if (_shopOverlayRoot != null)
            {
                _shopOverlayRoot.gameObject.SetActive(false);
            }

            if (_startUniqueOverlayRoot != null)
            {
                ClearStartingUniqueExplainTexts();
                _startUniqueOverlayRoot.gameObject.SetActive(false);
            }

            if (_activeItemConfirmOverlayRoot != null)
            {
                _activeItemConfirmOverlayRoot.gameObject.SetActive(false);
            }

            HideDefeatOverlayVisuals();

            if (_mobileExitOverlayRoot != null)
            {
                _mobileExitOverlayRoot.gameObject.SetActive(false);
            }

            SetDimOverlayVisible(_shopDimRoot, false);
            SetDimOverlayVisible(_shopConfirmDimRoot, false);
            SetDimOverlayVisible(_bagDimRoot, false);
            SetDimOverlayVisible(_percentageDimRoot, false);
            CloseSettingsPanel();
            RestoreShopPanelParent();
            CloseBagPanel();
            ClosePercentagePanel();
            RestoreStageStartSnapshot();
            SetGameplayInteractionEnabled(true);
            ResetStageLocalBattleState();
            InitBattle();
        }

        private void RetryCurrentStageWithFade()
        {
            BeginDefeatLocalTransition(RetryCurrentStage);
        }

        private void RestartFromBeginningWithFade()
        {
            BeginDefeatLocalTransition(RestartFromBeginning);
        }

        private void BeginDefeatLocalTransition(Action transitionAction)
        {
            if (_defeatTransitioning || transitionAction == null)
            {
                return;
            }

            StartCoroutine(DefeatLocalTransitionRoutine(transitionAction));
        }

        private IEnumerator DefeatLocalTransitionRoutine(Action transitionAction)
        {
            _defeatTransitioning = true;
            yield return SceneTransitionFader.Instance.FadeOut(fadeOutDuration);
            transitionAction();
            yield return SceneTransitionFader.Instance.FadeIn(fadeInDuration);
            _defeatTransitioning = false;
        }

        private void OnMenuButtonPressed()
        {
            if (_defeatTransitioning)
            {
                return;
            }

            _defeatTransitioning = true;
            SceneTransitionFader.BeginFadeOutLoadSceneFadeIn(
                "TitleScene",
                fadeOutDuration,
                fadeInDuration,
                true,
                musicFadeOutDuration);
        }

        private void OpenDefeatOverlayForDebug()
        {
            if (_shopOpen || _startingUniqueSelectionOpen || _activeItemConfirmOpen || _defeatOverlayOpen || _isResolvingTurn)
            {
                return;
            }

            _playerHp = 0;
            RefreshHud(string.Empty, "-");
            _hud.SetMessage("Defeat!");
            OpenDefeatOverlay();
        }

        private void BuildMobileExitOverlay(RectTransform canvasRoot)
        {
            _mobileExitOverlayRoot = CreateUiPanel("MobileExitOverlay", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var overlayImage = _mobileExitOverlayRoot.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.78f);
            overlayImage.raycastTarget = true;

            _mobileExitPanel = CreateUiPanel("MobileExitPanel", _mobileExitOverlayRoot, new Vector2(0.18f, 0.34f), new Vector2(0.82f, 0.64f), Vector2.zero, Vector2.zero);
            var panelImage = _mobileExitPanel.gameObject.AddComponent<Image>();
            panelImage.color = config.ShopConfirmPanelColor;

            var titleText = CreateText("MobileExitTitle", _mobileExitPanel, new Vector2(0.5f, 0.70f), 38f, config.ShopFontSizeScale);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = config.ShopPanelTextColor;
            titleText.text = "게임을 종료할까요?";

            var cancelButton = CreateActionButton(_mobileExitPanel, "취소", new Vector2(0.30f, 0.22f), CloseMobileExitOverlay, false, config.ShopConfirmActionButtonWidth, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            var quitButton = CreateActionButton(_mobileExitPanel, "종료", new Vector2(0.70f, 0.22f), QuitApplication, false, config.ShopConfirmActionButtonWidth, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(cancelButton, config.ShopButtonTextColor);
            SetButtonTextColor(quitButton, config.ShopButtonTextColor);

            _mobileExitOverlayRoot.gameObject.SetActive(false);
        }

        private void HandleMobileBackButton()
        {
            if (_mobileExitOverlayOpen)
            {
                CloseMobileExitOverlay();
                return;
            }

            if (_defeatOverlayOpen || _activeItemConfirmOpen || _shopOpen || _startingUniqueSelectionOpen || _isResolvingTurn)
            {
                return;
            }

            OpenMobileExitOverlay();
        }

        private void OpenMobileExitOverlay()
        {
            if (_mobileExitOverlayRoot == null)
            {
                return;
            }

            _mobileExitOverlayOpen = true;
            CloseBagPanel();
            ClosePercentagePanel();
            _mobileExitOverlayRoot.SetAsLastSibling();
            _mobileExitOverlayRoot.gameObject.SetActive(true);
            if (_mobileExitPanel != null)
            {
                _mobileExitPanel.SetAsLastSibling();
            }
        }

        private void CloseMobileExitOverlay()
        {
            _mobileExitOverlayOpen = false;
            if (_mobileExitOverlayRoot != null)
            {
                _mobileExitOverlayRoot.gameObject.SetActive(false);
            }
        }

        public void OpenSettingsPanel()
        {
            if (settingsPanelController == null && settingsPanelRoot == null)
            {
                return;
            }

            if (_dragging || _selection.Count > 0)
            {
                _dragging = false;
                ClearSelectionVisual();
            }

            CloseBagPanel();
            ClosePercentagePanel();

            if (settingsPanelController != null)
            {
                settingsPanelController.Open();
                _settingsPanelOpen = true;
                return;
            }

            SyncSettingsVolumeUi();
            RefreshSettingsVibrationUi();
            _settingsPanelOpen = true;
            if (_settingsDimRoot != null)
            {
                _settingsDimRoot.SetAsLastSibling();
                _settingsDimRoot.gameObject.SetActive(true);
            }

            settingsPanelRoot.SetAsLastSibling();
            settingsPanelRoot.gameObject.SetActive(true);
        }

        private void SyncSettingsVolumeUi()
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
                bgmPercentText.text = FormatVolumePercent(value);
            }

            if (sfxPercentText != null)
            {
                var value = sfxSlider != null ? sfxSlider.value : GameAudioManager.Instance != null ? GameAudioManager.Instance.SfxVolume : 1f;
                sfxPercentText.text = FormatVolumePercent(value);
            }
        }

        private static string FormatVolumePercent(float value)
        {
            return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }

        private void OnSettingsVibrationPressed()
        {
            HapticManager.Instance.ToggleEnabled();
            RefreshSettingsVibrationUi();
        }

        private void RefreshSettingsVibrationUi()
        {
            var isEnabled = HapticManager.Instance.IsEnabled;
            if (settingsVibrationStatusText != null)
            {
                settingsVibrationStatusText.text = isEnabled ? "Vibration: ON" : "Vibration: OFF";
            }

            if (settingsVibrationButtonImage != null)
            {
                var sprite = isEnabled ? settingsVibrationOnSprite : settingsVibrationOffSprite;
                if (sprite != null)
                {
                    settingsVibrationButtonImage.sprite = sprite;
                    settingsVibrationButtonImage.type = Image.Type.Sliced;
                    settingsVibrationButtonImage.color = Color.white;
                }
                else
                {
                    settingsVibrationButtonImage.color = isEnabled
                        ? Color.white
                        : new Color(0.55f, 0.55f, 0.55f, 1f);
                }
            }
        }

        private void OnSettingsRetryCurrentStage()
        {
            CloseSettingsPanel();
            BeginDefeatLocalTransition(RetryCurrentStage);
        }

        private void OnSettingsRestartFromBeginning()
        {
            CloseSettingsPanel();
            BeginDefeatLocalTransition(RestartFromBeginning);
        }

        private void OnSettingsReturnToTitle()
        {
            CloseSettingsPanel();
            OnMenuButtonPressed();
        }

        public void CloseSettingsPanel()
        {
            _settingsPanelOpen = false;
            if (settingsPanelController != null)
            {
                settingsPanelController.Close();
                return;
            }

            if (settingsPanelRoot != null)
            {
                settingsPanelRoot.gameObject.SetActive(false);
            }

            if (_settingsDimRoot != null)
            {
                _settingsDimRoot.gameObject.SetActive(false);
            }
        }

        public void ToggleSettingsPanel()
        {
            if (_settingsPanelOpen || (settingsPanelController != null && settingsPanelController.IsOpen))
            {
                CloseSettingsPanel();
                return;
            }

            OpenSettingsPanel();
        }

        private bool IsSettingsPanelOpen()
        {
            return _settingsPanelOpen || (settingsPanelController != null && settingsPanelController.IsOpen);
        }

        private void HandleBackNavigation()
        {
            if (IsSettingsPanelOpen())
            {
                CloseSettingsPanel();
                return;
            }

            if (IsTutorialPanelOpen())
            {
                tutorialPanelController.Close();
                return;
            }

            if (_activeItemConfirmOpen)
            {
                CloseActiveItemConfirmPanel();
                return;
            }

            if (IsUniqueHudInfoPanelOpen())
            {
                CloseUniqueHudInfoPanel();
                return;
            }

            if (_pendingShopSelection != null || _shopConfirmPanel != null && _shopConfirmPanel.gameObject.activeInHierarchy)
            {
                CloseShopConfirmPanel();
                return;
            }

            if (IsBagPanelOpen())
            {
                CloseBagPanel();
                return;
            }

            if (IsPercentagePanelOpen())
            {
                ClosePercentagePanel();
                return;
            }

            if (_mobileExitOverlayOpen)
            {
                CloseMobileExitOverlay();
                return;
            }
        }

        private static void QuitApplication()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void BuildStartingUniqueExplainBindings()
        {
            var layout = _boardLayoutReference?.StartingUniqueLayout;
            if (layout != null)
            {
                _startUniqueExplainTitleText = layout.ExplainNameText;
                if (layout.SelectButton != null)
                {
                    BindButton(layout.SelectButton, ConfirmPendingStartingUniqueSelection);
                }
            }
            ClearStartingUniqueExplainTexts();
        }

        private void BindBagLayout()
        {
            _bagItemSlotReferences.Clear();

            var bagLayout = _boardLayoutReference?.BagLayout;
            if (bagLayout == null)
            {
                return;
            }

            _bagButton = bagLayout.BagButton;
            _bagPanelRoot = bagLayout.PanelRoot;
            _bagDimRoot = EnsureFullscreenDimOverlay(_bagDimRoot, "BagDimOverlay", config.BagDimColor);

            if (_bagButton != null)
            {
                BindButton(_bagButton, ToggleBagPanel);
                _bagButtonNormalizedPosition = CaptureNormalizedPivotPosition(_bagButton.transform as RectTransform, GetTopOverlayParent());
                _bagButtonLeftAnchoredPosition = CaptureAnchoredPositionForCurrentAnchor(_bagButton.transform as RectTransform);
            }

            if (_bagPanelRoot != null)
            {
                _bagPanelNormalizedPosition = CaptureNormalizedPivotPosition(_bagPanelRoot, GetTopOverlayParent());
                _bagPanelLeftAnchoredPosition = CaptureAnchoredPositionForCurrentAnchor(_bagPanelRoot);
                _bagPanelOriginalParent = _bagPanelRoot.parent;
                _bagPanelOriginalSiblingIndex = _bagPanelRoot.GetSiblingIndex();
                BindPanelClickToClose(_bagPanelRoot, CloseBagPanel);
            }

            if (bagLayout.ItemSlots != null)
            {
                _bagItemSlotReferences.AddRange(bagLayout.ItemSlots);
            }

            for (var i = 0; i < _bagItemSlotReferences.Count; i++)
            {
                var slotIndex = i;
                var slotReference = _bagItemSlotReferences[i];
                if (slotReference?.Button != null)
                {
                    BindButton(slotReference.Button, () => OnBagItemSlotPressed(slotIndex));
                }
            }

            ConfigurePanelCloseIgnoredRoots(_bagPanelRoot, _bagItemSlotReferences.SelectMany(GetBagSlotIgnoredTransforms));

            if (_bagPanelRoot != null)
            {
                _bagPanelRoot.gameObject.SetActive(false);
            }

            RefreshBagUi();
        }

        private void ToggleBagPanel()
        {
            if (_bagPanelRoot == null)
            {
                return;
            }

            RefreshBagUi();
            var shouldOpen = !_bagPanelRoot.gameObject.activeSelf;
            _bagPanelRoot.gameObject.SetActive(shouldOpen);
            if (shouldOpen)
            {
                if (_dragging || _selection.Count > 0)
                {
                    _dragging = false;
                    ClearSelectionVisual();
                }

                SetDimOverlayVisible(_bagDimRoot, true);
                BringPanelToFront(_bagPanelRoot, ref _bagPanelOriginalParent, ref _bagPanelOriginalSiblingIndex);
                ApplyNormalizedPivotPosition(_bagPanelRoot, GetTopOverlayParent(), NudgeNormalizedX(_bagPanelNormalizedPosition, BagResponsiveOffsetX));
            }
            else
            {
                SetDimOverlayVisible(_bagDimRoot, false);
                RestorePanelParent(_bagPanelRoot, _bagPanelOriginalParent, _bagPanelOriginalSiblingIndex);
            }
        }

        private void CloseBagPanel()
        {
            if (_bagPanelRoot == null)
            {
                return;
            }

            SetDimOverlayVisible(_bagDimRoot, false);
            _bagPanelRoot.gameObject.SetActive(false);
            RestorePanelParent(_bagPanelRoot, _bagPanelOriginalParent, _bagPanelOriginalSiblingIndex);
        }

        private bool IsBagPanelOpen()
        {
            return _bagPanelRoot != null && _bagPanelRoot.gameObject.activeInHierarchy;
        }

        private void BindPercentageLayout()
        {
            _percentageBarBaseSizes.Clear();

            var percentageLayout = _boardLayoutReference?.PercentageLayout;
            if (percentageLayout == null)
            {
                return;
            }

            _percentageButton = percentageLayout.PercentageButton;
            _percentagePanelRoot = percentageLayout.PanelRoot;
            _percentageDimRoot = EnsureFullscreenDimOverlay(_percentageDimRoot, "PercentageDimOverlay", config.PercentageDimColor);

            if (_percentageButton != null)
            {
                BindButton(_percentageButton, TogglePercentagePanel);
                _percentageButtonNormalizedPosition = CaptureNormalizedPivotPosition(_percentageButton.transform as RectTransform, GetTopOverlayParent());
                _percentageButtonRightAnchoredPosition = CaptureAnchoredPositionForCurrentAnchor(_percentageButton.transform as RectTransform);
            }

            if (_percentagePanelRoot != null)
            {
                BindPanelClickToClose(_percentagePanelRoot, ClosePercentagePanel);
            }

            CachePercentageBarBaseSizes(percentageLayout.NumberBars?.Select(reference => reference?.ImageRect));
            CachePercentageBarBaseSizes(new[]
            {
                percentageLayout.AddBar?.ImageRect,
                percentageLayout.SubtractBar?.ImageRect,
                percentageLayout.MultiplyBar?.ImageRect,
                percentageLayout.DivideBar?.ImageRect,
            });

            if (_percentagePanelRoot != null)
            {
                _percentagePanelNormalizedPosition = CaptureNormalizedPivotPosition(_percentagePanelRoot, GetTopOverlayParent());
                _percentagePanelRightAnchoredPosition = CaptureAnchoredPositionForCurrentAnchor(_percentagePanelRoot);
                _percentagePanelOriginalParent = _percentagePanelRoot.parent;
                _percentagePanelOriginalSiblingIndex = _percentagePanelRoot.GetSiblingIndex();
                _percentagePanelRoot.gameObject.SetActive(false);
            }

            RefreshPercentageUi();
        }

        public void TogglePercentagePanel()
        {
            if (_percentagePanelRoot == null)
            {
                return;
            }

            RefreshPercentageUi();
            var shouldOpen = !_percentagePanelRoot.gameObject.activeSelf;
            _percentagePanelRoot.gameObject.SetActive(shouldOpen);
            if (shouldOpen)
            {
                SetDimOverlayVisible(_percentageDimRoot, true);
                BringPanelToFront(_percentagePanelRoot, ref _percentagePanelOriginalParent, ref _percentagePanelOriginalSiblingIndex);
                ApplyNormalizedPivotPosition(_percentagePanelRoot, GetTopOverlayParent(), NudgeNormalizedX(_percentagePanelNormalizedPosition, PercentageResponsiveOffsetX));
            }
            else
            {
                SetDimOverlayVisible(_percentageDimRoot, false);
                RestorePanelParent(_percentagePanelRoot, _percentagePanelOriginalParent, _percentagePanelOriginalSiblingIndex);
            }
        }

        private void ClosePercentagePanel()
        {
            if (_percentagePanelRoot == null)
            {
                return;
            }

            SetDimOverlayVisible(_percentageDimRoot, false);
            _percentagePanelRoot.gameObject.SetActive(false);
            RestorePanelParent(_percentagePanelRoot, _percentagePanelOriginalParent, _percentagePanelOriginalSiblingIndex);
        }

        private void ApplyResponsiveScenePositions()
        {
            var topOverlayParent = GetTopOverlayParent();
            ApplyNormalizedPivotPosition(_bagButton != null ? _bagButton.transform as RectTransform : null, topOverlayParent, NudgeNormalizedX(_bagButtonNormalizedPosition, BagResponsiveOffsetX));
            ApplyNormalizedPivotPosition(_percentageButton != null ? _percentageButton.transform as RectTransform : null, topOverlayParent, NudgeNormalizedX(_percentageButtonNormalizedPosition, PercentageResponsiveOffsetX));

            if (_tileLayoutRoot != null && _tileLayoutRoot != _boardRoot)
            {
                ApplyNormalizedPivotPosition(_tileLayoutRoot, topOverlayParent, _boardPanelNormalizedPosition);
            }

            if (IsBagPanelOpen())
            {
                ApplyNormalizedPivotPosition(_bagPanelRoot, topOverlayParent, NudgeNormalizedX(_bagPanelNormalizedPosition, BagResponsiveOffsetX));
            }

            if (IsPercentagePanelOpen())
            {
                ApplyNormalizedPivotPosition(_percentagePanelRoot, topOverlayParent, NudgeNormalizedX(_percentagePanelNormalizedPosition, PercentageResponsiveOffsetX));
            }

        }

        private IEnumerator ApplyResponsiveLayoutNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            ApplyResponsiveScenePositions();
        }

        private bool IsPercentagePanelOpen()
        {
            return _percentagePanelRoot != null && _percentagePanelRoot.gameObject.activeInHierarchy;
        }

        private void BindPanelClickToClose(RectTransform panelRoot, UnityAction closeAction)
        {
            if (panelRoot == null || closeAction == null)
            {
                return;
            }

            var button = panelRoot.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.enabled = false;
            }

            var closeHandler = panelRoot.GetComponent<PanelBackgroundCloseHandler>();
            if (closeHandler == null)
            {
                closeHandler = panelRoot.gameObject.AddComponent<PanelBackgroundCloseHandler>();
            }

            closeHandler.Bind(closeAction);
        }

        private void ConfigurePanelCloseIgnoredRoots(RectTransform panelRoot, IEnumerable<Transform> ignoredRoots)
        {
            if (panelRoot == null)
            {
                return;
            }

            var closeHandler = panelRoot.GetComponent<PanelBackgroundCloseHandler>();
            if (closeHandler == null)
            {
                return;
            }

            closeHandler.SetIgnoredRoots(ignoredRoots);
        }

        private IEnumerable<Transform> GetBagSlotIgnoredTransforms(BattleBoardLayoutReference.BagItemSlotReference slotReference)
        {
            if (slotReference == null)
            {
                yield break;
            }

            if (slotReference.Button != null)
            {
                yield return slotReference.Button.transform;
            }

            if (slotReference.ItemImage != null)
            {
                yield return slotReference.ItemImage.transform;
            }

            if (slotReference.CountText != null)
            {
                yield return slotReference.CountText.transform;
            }
        }

        private void BringPanelToFront(RectTransform panelRoot, ref Transform originalParent, ref int originalSiblingIndex)
        {
            if (panelRoot == null)
            {
                return;
            }

            originalParent ??= panelRoot.parent;
            originalSiblingIndex = panelRoot.GetSiblingIndex();

            var topRoot = _gameplayContainer != null ? _gameplayContainer.parent as RectTransform : null;
            if (topRoot == null)
            {
                panelRoot.SetAsLastSibling();
                return;
            }

            panelRoot.SetParent(topRoot, true);
            panelRoot.SetAsLastSibling();
        }

        private static void RestorePanelParent(RectTransform panelRoot, Transform originalParent, int originalSiblingIndex)
        {
            if (panelRoot == null || originalParent == null)
            {
                return;
            }

            if (panelRoot.parent != originalParent)
            {
                panelRoot.SetParent(originalParent, true);
            }

            var safeIndex = Mathf.Clamp(originalSiblingIndex, 0, panelRoot.parent.childCount - 1);
            panelRoot.SetSiblingIndex(safeIndex);
        }

        private void RestoreShopPanelParent()
        {
            var shopFrontTarget = _shopPanel != null ? _shopPanel : _shopOverlayRoot;
            if (shopFrontTarget == null)
            {
                return;
            }

            RestorePanelParent(shopFrontTarget, _shopPanelOriginalParent, _shopPanelOriginalSiblingIndex);
        }

        private RectTransform EnsureFullscreenDimOverlay(RectTransform existingOverlay, string name, Color color)
        {
            if (existingOverlay != null)
            {
                EnsureDimOverlayVisual(existingOverlay, color);
                return existingOverlay;
            }

            var parent = GetTopOverlayParent();
            if (parent == null)
            {
                return null;
            }

            var overlay = CreateUiPanel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureDimOverlayVisual(overlay, color);
            overlay.gameObject.SetActive(false);
            return overlay;
        }

        private RectTransform GetTopOverlayParent()
        {
            return _gameplayContainer != null
                ? _gameplayContainer.parent as RectTransform
                : _boardRoot != null
                    ? _boardRoot.parent as RectTransform
                    : null;
        }

        private static Vector2? CaptureNormalizedPivotPosition(RectTransform rect, RectTransform referenceParent)
        {
            if (rect == null || referenceParent == null)
            {
                return null;
            }

            var parentSize = referenceParent.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return null;
            }

            var worldPivot = rect.TransformPoint(new Vector3(rect.rect.xMin + rect.rect.width * rect.pivot.x, rect.rect.yMin + rect.rect.height * rect.pivot.y, 0f));
            var localPivot = referenceParent.InverseTransformPoint(worldPivot);
            var normalizedX = Mathf.InverseLerp(referenceParent.rect.xMin, referenceParent.rect.xMax, localPivot.x);
            var normalizedY = Mathf.InverseLerp(referenceParent.rect.yMin, referenceParent.rect.yMax, localPivot.y);
            return new Vector2(normalizedX, normalizedY);
        }

        private static Vector2? CaptureAnchoredPositionForCurrentAnchor(RectTransform rect)
        {
            if (rect == null)
            {
                return null;
            }

            return rect.anchoredPosition;
        }

        private static Vector2? NudgeNormalizedX(Vector2? normalizedPosition, float deltaX)
        {
            if (!normalizedPosition.HasValue)
            {
                return null;
            }

            var value = normalizedPosition.Value;
            value.x = Mathf.Clamp01(value.x + deltaX);
            return value;
        }

        private static void ApplyNormalizedPivotPosition(RectTransform rect, RectTransform referenceParent, Vector2? normalizedPosition)
        {
            if (rect == null || referenceParent == null || !normalizedPosition.HasValue)
            {
                return;
            }

            var normalized = normalizedPosition.Value;
            rect.anchorMin = normalized;
            rect.anchorMax = normalized;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void EnsureDimOverlayVisual(RectTransform overlay, Color color)
        {
            if (overlay == null)
            {
                return;
            }

            var image = overlay.GetComponent<Image>();
            if (image == null)
            {
                image = overlay.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = true;
        }

        private static void SetDimOverlayVisible(RectTransform overlay, bool visible)
        {
            if (overlay == null)
            {
                return;
            }

            overlay.gameObject.SetActive(visible);
            if (visible)
            {
                overlay.SetAsLastSibling();
            }
        }

        private static void PlaceDimOverlayBehind(RectTransform overlay, RectTransform target)
        {
            if (overlay == null || target == null || target.parent == null)
            {
                return;
            }

            if (overlay.parent != target.parent)
            {
                overlay.SetParent(target.parent, false);
                overlay.anchorMin = Vector2.zero;
                overlay.anchorMax = Vector2.one;
                overlay.offsetMin = Vector2.zero;
                overlay.offsetMax = Vector2.zero;
                overlay.pivot = new Vector2(0.5f, 0.5f);
            }

            var siblingIndex = Mathf.Max(0, target.GetSiblingIndex());
            overlay.SetSiblingIndex(siblingIndex);
            target.SetSiblingIndex(Mathf.Min(overlay.parent.childCount - 1, siblingIndex + 1));
        }

        private void CachePercentageBarBaseSizes(IEnumerable<RectTransform> rects)
        {
            if (rects == null)
            {
                return;
            }

            foreach (var rect in rects)
            {
                if (rect == null || _percentageBarBaseSizes.ContainsKey(rect))
                {
                    continue;
                }

                _percentageBarBaseSizes[rect] = rect.sizeDelta;
            }
        }

        private void UpdateLayoutRegions()
        {
            if (_gameplayContainer == null || _boardContainer == null)
            {
                return;
            }

            var boardContainerFitter = _boardContainer.GetComponent<AspectRatioFitter>();
            if (boardContainerFitter != null)
            {
                boardContainerFitter.aspectRatio = GetBoardAspectRatio();
            }

            Canvas.ForceUpdateCanvases();
            var boardHeight = _boardContainer.gameObject.activeSelf ? _boardContainer.rect.height : 0f;
            _boardContainer.anchoredPosition = Vector2.zero;
            if (_tileLayoutRoot != null && _tileLayoutRoot != _boardRoot)
            {
                ApplyNormalizedPivotPosition(_tileLayoutRoot, GetTopOverlayParent(), _boardPanelNormalizedPosition);
            }
            _gameplayContainer.anchorMin = new Vector2(0f, 0f);
            _gameplayContainer.anchorMax = new Vector2(1f, 1f);
            _gameplayContainer.pivot = new Vector2(0.5f, 0.5f);
            _gameplayContainer.offsetMin = new Vector2(0f, boardHeight);
            _gameplayContainer.offsetMax = Vector2.zero;
        }

        private float GetBoardAspectRatio()
        {
            var columns = Mathf.Max(1, config != null ? config.Columns : 1);
            var rows = Mathf.Max(1, config != null ? config.Rows : 1);
            return columns / (float)rows;
        }

        private void BuildBoard()
        {
            _grid = new BattleTileView[config.Columns, config.Rows];
            var layoutMetrics = GetBoardLayoutMetrics();
            _cellSize = layoutMetrics.CellSize;
            for (var y = 0; y < config.Rows; y++)
            {
                for (var x = 0; x < config.Columns; x++)
                {
                    var tile = CreateTile(x, y, layoutMetrics);
                    SpawnInitialBoardTileValue(tile, x, y);
                    _grid[x, y] = tile;
                }
            }
        }

        private void BuildInitialBoard()
        {
            BuildBoard();
            if (HasUniqueItem(Unique4ItemId))
            {
                return;
            }

            ResolveAutoLineClears(false);
            ApplyInitialBoardOperatorLineCorrection();
        }

        private void ApplyInitialBoardOperatorLineCorrection()
        {
            if (_grid == null || config.Columns <= 0 || config.Rows <= 0)
            {
                return;
            }

            var bottomRow = config.Rows - 1;
            SetInitialBoardTileNumber(0, bottomRow);
            if (config.Columns > 1)
            {
                SetInitialBoardTileNumber(config.Columns - 1, bottomRow);
            }

            ApplyInitialBoardLineOperatorCorrection(GetInitialBoardColumnPositions(0, bottomRow));
            if (config.Columns > 1)
            {
                ApplyInitialBoardLineOperatorCorrection(GetInitialBoardColumnPositions(config.Columns - 1, bottomRow));
            }

            ApplyInitialBoardLineOperatorCorrection(GetInitialBoardBottomRowPositions(bottomRow));
        }

        private List<Vector2Int> GetInitialBoardColumnPositions(int x, int excludedBottomRow)
        {
            var positions = new List<Vector2Int>();
            for (var y = 0; y < excludedBottomRow; y++)
            {
                positions.Add(new Vector2Int(x, y));
            }

            return positions;
        }

        private List<Vector2Int> GetInitialBoardBottomRowPositions(int bottomRow)
        {
            var positions = new List<Vector2Int>();
            for (var x = 1; x < config.Columns - 1; x++)
            {
                positions.Add(new Vector2Int(x, bottomRow));
            }

            return positions;
        }

        private void ApplyInitialBoardLineOperatorCorrection(List<Vector2Int> linePositions)
        {
            if (linePositions == null || linePositions.Count == 0)
            {
                return;
            }

            var operatorPositions = PickInitialBoardOperatorPositions(linePositions);
            foreach (var position in linePositions)
            {
                if (operatorPositions.Contains(position))
                {
                    SetInitialBoardTileOperator(position.x, position.y);
                }
                else
                {
                    SetInitialBoardTileNumber(position.x, position.y);
                }
            }
        }

        private HashSet<Vector2Int> PickInitialBoardOperatorPositions(List<Vector2Int> linePositions)
        {
            var operatorPositions = new HashSet<Vector2Int>();
            var maxOperatorCount = Mathf.Min(InitialBoardMaxLineOperators, (linePositions.Count + 1) / 2);
            if (maxOperatorCount <= 0)
            {
                return operatorPositions;
            }

            var operatorCount = UnityEngine.Random.Range(InitialBoardMinLineOperators, maxOperatorCount + 1);
            if (operatorCount <= 1)
            {
                operatorPositions.Add(linePositions[UnityEngine.Random.Range(0, linePositions.Count)]);
                return operatorPositions;
            }

            var nonAdjacentPairs = new List<(Vector2Int First, Vector2Int Second)>();
            for (var i = 0; i < linePositions.Count - 1; i++)
            {
                for (var j = i + 1; j < linePositions.Count; j++)
                {
                    if (!AreAdjacentBoardPositions(linePositions[i], linePositions[j]))
                    {
                        nonAdjacentPairs.Add((linePositions[i], linePositions[j]));
                    }
                }
            }

            if (nonAdjacentPairs.Count == 0)
            {
                operatorPositions.Add(linePositions[UnityEngine.Random.Range(0, linePositions.Count)]);
                return operatorPositions;
            }

            var pair = nonAdjacentPairs[UnityEngine.Random.Range(0, nonAdjacentPairs.Count)];
            operatorPositions.Add(pair.First);
            operatorPositions.Add(pair.Second);
            return operatorPositions;
        }

        private static bool AreAdjacentBoardPositions(Vector2Int left, Vector2Int right)
        {
            return Mathf.Abs(left.x - right.x) + Mathf.Abs(left.y - right.y) == 1;
        }

        private void SetInitialBoardTileNumber(int x, int y)
        {
            if (!IsValidGridPosition(x, y))
            {
                return;
            }

            var tile = _grid[x, y];
            if (tile == null)
            {
                return;
            }

            tile.SetNumber(PickNumber());
            ApplyTileSpriteVisual(tile);
        }

        private void SetInitialBoardTileOperator(int x, int y)
        {
            if (!IsValidGridPosition(x, y))
            {
                return;
            }

            var tile = _grid[x, y];
            if (tile == null)
            {
                return;
            }

            tile.SetOperator(PickOperator());
            ApplyTileSpriteVisual(tile);
        }

        private bool IsValidGridPosition(int x, int y)
        {
            return x >= 0
                && y >= 0
                && _grid != null
                && x < _grid.GetLength(0)
                && y < _grid.GetLength(1);
        }

        private void ResetStageLocalBattleState()
        {
            _dragging = false;
            ClearSelectionVisual();
            ClearBoardTiles();
            _validTurnCount = 0;
            _unique1UsedOneCountThisStage = 0;
            _unique1TransformReady = false;
            ResetEdgeNumberLineClearCorrection();
            RebuildCachedSpawnWeights();
            BuildInitialBoard();
            _playerShield = 0;
            RefreshHud(string.Empty, "-");
            _hud.SetMessage(string.Empty);
        }

        private void ClearBoardTiles()
        {
            _unique9TransformedTiles.Clear();
            if (_grid == null)
            {
                return;
            }

            for (var x = 0; x < _grid.GetLength(0); x++)
            {
                for (var y = 0; y < _grid.GetLength(1); y++)
                {
                    var tile = _grid[x, y];
                    if (tile != null)
                    {
                        Destroy(tile.gameObject);
                    }

                    _grid[x, y] = null;
                }
            }
        }

        private void InitBattle()
        {
            _playerState ??= new RuntimePlayerState();
            if (_playerState.CurrentStage <= 0)
            {
                _playerState.CurrentStage = 1;
            }

            _currentStage = GetStageDefinition(_playerState.CurrentStage);
            ApplyEnemyVisual(_currentStage.EnemyType);
            if (_playerHp <= 0)
            {
                _playerHp = _currentPlayerMaxHp;
            }

            _enemyHp = _currentStage.EnemyHp;
            _validTurnCount = 0;
            _unique1UsedOneCountThisStage = 0;
            _unique1TransformReady = false;
            _playerShield = 0;
            ResetCombatModeToAttack(true);
            _enemyDeathHandledThisStage = false;
            RebuildCachedSpawnWeights();
            RefreshHud(string.Empty, "-");
            UpdateCurrentStageDisplay();
            _hud.SetMessage($"Stage {_playerState.CurrentStage}: {_currentStage.EnemyName}");
            EnsureStartingUniqueSelection();
            TryOpenStartingUniqueSelectionTutorial();
            CaptureStageStartSnapshotIfReady();
        }

        private void ApplyEnemyVisual(EnemyType enemyType)
        {
            var entries = enemyVisualEntries ?? Array.Empty<EnemyVisualEntry>();
            EnemyVisualEntry selectedEntry = null;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (selectedEntry == null && entry != null && entry.EnemyType == enemyType)
                {
                    selectedEntry = entry;
                }
            }

            if (selectedEntry == null)
            {
                battleAnimationManager?.SetEnemyRuntimeBindings(null, null, null, null, null);
                return;
            }

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry?.Root != null)
                {
                    entry.Root.SetActive(ReferenceEquals(entry, selectedEntry));
                }
            }

            battleAnimationManager?.SetEnemyRuntimeBindings(
                selectedEntry.Animator,
                selectedEntry.HitVfxPoint,
                selectedEntry.AttackTriggerName,
                selectedEntry.HitTriggerName,
                selectedEntry.DeathTriggerName);
        }

        private IEnumerator ValidateBattleSceneStartup()
        {
            yield return null;

            ResolveBoardLayoutReference();

            if ((_startUniqueOverlayRoot == null || _startingUniqueButtons.Count == 0) && TryBuildStartingUniqueSceneLayout())
            {
                BuildStartingUniqueExplainBindings();
            }

            if (_startingUniqueSelectionResolved || _startingUniqueSelectionOpen)
            {
                if (_startingUniqueSelectionOpen)
                {
                    ShowStartingUniqueOverlay();
                }
                yield break;
            }

            EnsureStartingUniqueSelection();

            if (_startingUniqueCandidates.Count == 0)
            {
                Debug.LogWarning("Starting unique selection did not populate candidates during BattleScene startup.");
                yield break;
            }

            if (_startingUniqueSelectionOpen && (_startUniqueOverlayRoot == null || !_startUniqueOverlayRoot.gameObject.activeInHierarchy))
            {
                ForceRuntimeStartingUniqueOverlayFallback();
                RefreshStartingUniqueOverlay();
                ShowStartingUniqueOverlay();
            }
        }

        private BattleTileView CreateTile(int x, int y, BoardLayoutMetrics layoutMetrics)
        {
            var go = new GameObject($"Tile_{x}_{y}", typeof(Image), typeof(BattleTileView));
            var image = go.GetComponent<Image>();
            image.raycastTarget = true;

            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_tileLayoutRoot, false);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            var text = new GameObject("Label", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(rt, false);
            text.alignment = TextAlignmentOptions.Center;
            ApplyUiFont(text);
            text.fontSize = ScaleFont(Mathf.Max(24f, layoutMetrics.CellSize * 0.35f), config.TileFontSizeScale);
            text.color = Color.black;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            var tile = go.GetComponent<BattleTileView>();
            typeof(BattleTileView).GetField("background", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(tile, image);
            typeof(BattleTileView).GetField("label", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(tile, text);
            tile.SetGridPos(x, y);
            tile.SetBoardVisualLayout(layoutMetrics.TileSize, GetTileAnchoredPosition(x, y, layoutMetrics));
            return tile;
        }

        private void ApplyTileSpriteVisual(BattleTileView tile)
        {
            if (tile == null)
            {
                return;
            }

            switch (tile.Kind)
            {
                case TileKind.Number:
                    {
                        if (TryGetUnique9TransformedTileSprites(tile, out var unique9NormalSprite, out var unique9SelectedSprite))
                        {
                            tile.ConfigureSprites(unique9NormalSprite, unique9SelectedSprite, config.ShowTileLabelWhenSpriteAssigned);
                            break;
                        }

                        if (TryGetUnique1ReadyTileSprites(tile.NumberValue, out var unique1ReadySpriteEntry))
                        {
                            tile.ConfigureSprites(unique1ReadySpriteEntry.NormalSprite, unique1ReadySpriteEntry.SelectedSprite, config.ShowTileLabelWhenSpriteAssigned);
                            break;
                        }

                        if (TryGetUniqueNumberTileSprites(tile.NumberValue, out var uniqueSpriteEntry))
                        {
                            tile.ConfigureSprites(uniqueSpriteEntry.NormalSprite, uniqueSpriteEntry.SelectedSprite, config.ShowTileLabelWhenSpriteAssigned);
                            break;
                        }

                        var spriteEntry = config.NumberTileSprites.FirstOrDefault(entry => entry.Value == tile.NumberValue);
                        tile.ConfigureSprites(spriteEntry.NormalSprite, spriteEntry.SelectedSprite, config.ShowTileLabelWhenSpriteAssigned);
                        break;
                    }
                case TileKind.Operator:
                    {
                        var spriteEntry = config.OperatorTileSprites.FirstOrDefault(entry => entry.Value == tile.Operator);
                        tile.ConfigureSprites(spriteEntry.NormalSprite, spriteEntry.SelectedSprite, config.ShowTileLabelWhenSpriteAssigned);
                        break;
                    }
            }
        }

        private bool TryGetUnique9TransformedTileSprites(BattleTileView tile, out Sprite normalSprite, out Sprite selectedSprite)
        {
            normalSprite = null;
            selectedSprite = null;
            if (tile == null || tile.Kind != TileKind.Number || tile.NumberValue != 9 || !_unique9TransformedTiles.Contains(tile))
            {
                return false;
            }

            normalSprite = config.Unique9TransformedNineNormalSprite;
            selectedSprite = config.Unique9TransformedNineSelectedSprite != null ? config.Unique9TransformedNineSelectedSprite : normalSprite;
            return normalSprite != null || selectedSprite != null;
        }

        private bool TryGetUnique1ReadyTileSprites(int numberValue, out BattleConfig.NumberTileSpriteEntry spriteEntry)
        {
            spriteEntry = default;
            return false;
        }

        private bool TryGetUniqueNumberTileSprites(int numberValue, out BattleConfig.UniqueNumberTileSpriteEntry spriteEntry)
        {
            spriteEntry = default;
            var requiredUniqueItemId = numberValue switch
            {
                1 => Unique1ItemId,
                2 => Unique2ItemId,
                3 or 6 or 9 => Unique3ItemId,
                5 => Unique5ItemId,
                _ => null,
            };

            if (requiredUniqueItemId == null || !HasUniqueItem(requiredUniqueItemId))
            {
                return false;
            }

            spriteEntry = config.UniqueNumberTileSprites.FirstOrDefault(entry => entry.Value == numberValue);
            return spriteEntry.Value == numberValue && (spriteEntry.NormalSprite != null || spriteEntry.SelectedSprite != null);
        }

        private void RefreshBoardTileSpriteVisuals()
        {
            if (_grid == null)
            {
                return;
            }

            for (var x = 0; x < config.Columns; x++)
            {
                for (var y = 0; y < config.Rows; y++)
                {
                    ApplyTileSpriteVisual(_grid[x, y]);
                }
            }
        }

        private void SpawnInitialBoardTileValue(BattleTileView tile, int x, int y)
        {
            if (HasUniqueItem(Unique4ItemId))
            {
                if ((x + y) % 2 == 0)
                {
                    tile.SetNumber(PickNumber());
                }
                else
                {
                    tile.SetOperator(PickOperator());
                }

                ApplyTileSpriteVisual(tile);
                return;
            }

            SpawnTileValue(tile, x, y);
        }

        private void SpawnTileValue(BattleTileView tile, int x, int y, bool forceOperator = false)
        {
            if (forceOperator)
            {
                tile.SetOperator(PickOperator());
                ApplyTileSpriteVisual(tile);
                return;
            }

            var numberChance = GetNumberChanceForCell(x, y);
            if (UnityEngine.Random.value < numberChance)
            {
                tile.SetNumber(PickNumber());
                ApplyTileSpriteVisual(tile);
                return;
            }

            tile.SetOperator(PickOperator());
            ApplyTileSpriteVisual(tile);
        }

        private int PickNumber()
        {
            var total = _cachedNumberWeights.Values.Where(weight => weight > 0).Sum();
            if (total <= 0)
            {
                return 1;
            }

            var roll = UnityEngine.Random.Range(1, total + 1);
            var running = 0;
            foreach (var entry in _cachedNumberWeights.OrderBy(pair => pair.Key))
            {
                if (entry.Value <= 0)
                {
                    continue;
                }

                running += entry.Value;
                if (roll <= running)
                {
                    return entry.Key;
                }
            }

            return 1;
        }

        private OperatorType PickOperator()
        {
            var total = _cachedOperatorWeights.Values.Where(weight => weight > 0).Sum();
            if (total <= 0)
            {
                return OperatorType.Add;
            }

            var roll = UnityEngine.Random.Range(1, total + 1);
            var running = 0;
            foreach (var entry in _cachedOperatorWeights.OrderBy(pair => pair.Key))
            {
                if (entry.Value <= 0)
                {
                    continue;
                }

                running += entry.Value;
                if (roll <= running)
                {
                    return entry.Key switch
                    {
                        "+" => OperatorType.Add,
                        "-" => OperatorType.Subtract,
                        "x" => OperatorType.Multiply,
                        "÷" => OperatorType.Divide,
                        _ => OperatorType.Add,
                    };
                }
            }

            return OperatorType.Add;
        }

        private void TryAddTileAtScreen(Vector2 pos)
        {
            if (_tileLayoutRoot == null || _grid == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_tileLayoutRoot, pos, _uiCamera, out _))
            {
                return;
            }

            foreach (var tile in _grid)
            {
                if (tile == null)
                {
                    continue;
                }

                var tileRect = tile.GetComponent<RectTransform>();
                if (!RectTransformUtility.RectangleContainsScreenPoint(tileRect, pos, _uiCamera))
                {
                    continue;
                }

                TryAppendTile(tile);
                return;
            }
        }

        private void TryAppendTile(BattleTileView tile)
        {
            if (_selection.Count > 0)
            {
                var last = _selection[^1];
                if (_selection.Count >= 2 && tile == _selection[^2])
                {
                    _selection[^1].SetSelected(false);
                    _selection.RemoveAt(_selection.Count - 1);
                    RefreshHud(GetExpressionString(), "-");
                    return;
                }

                if (_selection.Contains(tile))
                {
                    return;
                }

                if (Mathf.Abs(last.X - tile.X) + Mathf.Abs(last.Y - tile.Y) != 1)
                {
                    return;
                }
            }

            if (_selection.Count >= _currentMaxConnectionLength)
            {
                return;
            }

            var isFirstSelectedTile = _selection.Count == 0;
            _selection.Add(tile);
            tile.SetSelected(true);
            if (!isFirstSelectedTile)
            {
                GameAudioManager.Instance?.PlayDragTouchSfx();
                HapticManager.Instance.PlayLight();
            }
            RefreshHud(GetExpressionString(), "-");
        }

        private void ConfirmSelection()
        {
            if (!TryBuildSelectionContext(out var context, out var error))
            {
                _hud.SetMessage($"Invalid: {error}");
                GameAudioManager.Instance?.PlayInvalidSelectionSfx();
                ClearSelectionVisual();
                RefreshHud(string.Empty, "-");
                return;
            }

            _validTurnCount++;
            context = BuildSelectionContextFromCurrentBoard();

            if (!TryCalculateExpression(context.CalculationNumbers, context.Operators, out var baseResult, out error))
            {
                _hud.SetMessage($"Invalid: {error}");
                GameAudioManager.Instance?.PlayInvalidSelectionSfx();
                ClearSelectionVisual();
                RefreshHud(string.Empty, "-");
                return;
            }

            GameAudioManager.Instance?.PlayReleaseTouchSfx();

            var uniqueOutcome = ResolveUniqueOutcome(context, baseResult);

            var enemyHpBefore = _enemyHp;
            ApplyCombatResult(baseResult, uniqueOutcome);
            var dealtDamage = Mathf.Max(0, enemyHpBefore - _enemyHp);
            if (_currentCombatMode == CombatMode.Attack && dealtDamage >= 20)
            {
                HapticManager.Instance.PlayHeavy();
            }

            GameAudioManager.Instance?.PlayExpressionConfirmSfx();

            UpdateUnique1State(context);

            var resultText = $"{baseResult}";
            var shouldEnemyAttack = _enemyHp > 0 && _validTurnCount % _currentStage.EnemyAttackCycle == 0;
            StartCoroutine(ResolveBoardAfterSelection(resultText, uniqueOutcome.Message, shouldEnemyAttack, dealtDamage));
        }

        private IEnumerator ResolveBoardAfterSelection(string resultText, string resultMessage, bool shouldEnemyAttack, int dealtDamage)
        {
            _isResolvingTurn = true;
            if (_currentCombatMode == CombatMode.Attack && dealtDamage > 0 && battleAnimationManager != null)
            {
                yield return battleAnimationManager.PlayAttackByDamageRoutine(dealtDamage);
            }

            yield return ResolveBoard();
            UpdateHighestDamageThisRun(dealtDamage + _lastAutoLineClearDamage);
            yield return ApplyUnique9BoardTransformIfNeeded();

            RefreshHud(string.Empty, resultText);
            _hud.SetMessage(BuildBoardResolutionMessage(resultMessage));

            // 결과값을 아주 잠깐 보여줄 시간
            yield return new WaitForSeconds(1.5f);

            if (_enemyHp <= 0)
            {
                RefreshHud(string.Empty, "-");
                yield return HandleEnemyDeathThenStageClear();
                yield break;
            }

            if (shouldEnemyAttack)
            {
                yield return ResolveEnemyAttackAfterDelay(resultText);
                yield break;
            }

            if (_playerHp <= 0)
            {
                RefreshHud(string.Empty, "-");
                _hud.SetMessage("Defeat!");
                OpenDefeatOverlay();
                _isResolvingTurn = false;
                yield break;
            }

            RefreshHud(string.Empty, "-");
            _isResolvingTurn = false;
        }

        private IEnumerator ResolveEnemyAttackAfterDelay(string resultText)
        {
            _isResolvingTurn = true;

            var delaySeconds = config.EnemyAttackDelaySeconds;
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            if (_enemyHp <= 0)
            {
                _isResolvingTurn = false;
                yield break;
            }

            var damageAfterShield = 0;
            void ApplyEnemyAttackDamage()
            {
                var enemyAttackDamage = Mathf.Max(0, _currentStage.EnemyAttackDamage);
                damageAfterShield = Mathf.Max(0, enemyAttackDamage - _playerShield);
                _playerShield = Mathf.Max(0, _playerShield - enemyAttackDamage);
                _playerHp = Mathf.Max(0, _playerHp - damageAfterShield);
                TriggerEnemyAttackCameraShake(damageAfterShield > 0);
                if (damageAfterShield > 0)
                {
                    HapticManager.Instance.PlayMedium();
                }
                RefreshHud(string.Empty, resultText);
                _hud.SetMessage(damageAfterShield > 0 ? $"Enemy attacked for {damageAfterShield}!" : "Enemy attack was blocked by shield!");
            }

            if (battleAnimationManager != null)
            {
                yield return battleAnimationManager.PlayEnemyAttackRoutine(ApplyEnemyAttackDamage);
            }
            else
            {
                ApplyEnemyAttackDamage();
            }

            ResetCombatModeToAttack(true);

            if (_playerHp <= 0)
            {
                RefreshHud(string.Empty, "-");
                _hud.SetMessage("Defeat!");
                OpenDefeatOverlay();
                _isResolvingTurn = false;
                yield break;
            }

            RefreshHud(string.Empty, "-");
            _isResolvingTurn = false;
        }

        private void TriggerEnemyAttackCameraShake(bool didTakeDamage)
        {
            if (!didTakeDamage || _shakeCamera == null)
            {
                return;
            }

            if (_cameraShakeCoroutine != null)
            {
                StopCoroutine(_cameraShakeCoroutine);
                _shakeCamera.transform.localRotation = _cameraOriginalLocalRotation;
            }

            _cameraShakeCoroutine = StartCoroutine(PlayEnemyAttackCameraShake());
        }

        private IEnumerator PlayEnemyAttackCameraShake()
        {
            var duration = config.EnemyAttackShakeDuration;
            var strength = config.EnemyAttackShakeRotationStrength;
            if (duration <= 0f || strength <= 0f || _shakeCamera == null)
            {
                _cameraShakeCoroutine = null;
                yield break;
            }

            var maxYawAngle = Mathf.Lerp(0f, 12f, Mathf.Clamp01(strength / 10f));
            var oscillationSpeed = Mathf.Lerp(14f, 28f, Mathf.Clamp01(strength / 10f));
            var elapsed = 0f;
            while (elapsed < duration)
            {
                var normalized = elapsed / duration;
                var damping = 1f - normalized;
                var yawOffset = Mathf.Sin(elapsed * oscillationSpeed) * maxYawAngle * damping;
                _shakeCamera.transform.localRotation = _cameraOriginalLocalRotation * Quaternion.Euler(0f, yawOffset, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeCamera.transform.localRotation = _cameraOriginalLocalRotation;
            _cameraShakeCoroutine = null;
        }

        private void ApplyCombatResult(int baseResult, UniqueOutcome uniqueOutcome)
        {
            if (_currentCombatMode == CombatMode.Attack)
            {
                var adjustedAttackResult = ApplyPendingAttackMultiplier(baseResult);
                if (baseResult > 0)
                {
                    _enemyHp = Mathf.Max(0, _enemyHp - adjustedAttackResult);
                }
                else if (baseResult < 0)
                {
                    _enemyHp = Mathf.Min(_currentStage.EnemyHp, _enemyHp + Math.Abs(baseResult));
                }

                if (uniqueOutcome.BonusDamage > 0)
                {
                    _enemyHp = Mathf.Max(0, _enemyHp - uniqueOutcome.BonusDamage);
                }

                if (uniqueOutcome.ShieldBonus > 0)
                {
                    _playerShield += uniqueOutcome.ShieldBonus;
                }
            }
            else
            {
                var baseShield = 0;
                if (baseResult < 0)
                {
                    _enemyHp = Mathf.Min(_currentStage.EnemyHp, _enemyHp + Math.Abs(baseResult));
                }
                else
                {
                    baseShield = Mathf.Max(0, Mathf.CeilToInt(baseResult * config.ShieldConversionRate));
                }

                _playerShield += baseShield;
                if (uniqueOutcome.ShieldBonus > 0)
                {
                    _playerShield += uniqueOutcome.ShieldBonus;
                }
            }
        }

        private int ApplyPendingAttackMultiplier(int baseResult)
        {
            if (_currentCombatMode != CombatMode.Attack || !_runtimeItemInventory.HasPendingAttackMultiplier())
            {
                return baseResult;
            }

            var percent = _runtimeItemInventory.PendingNextAttackMultiplierPercent;
            _runtimeItemInventory.ClearPendingAttackMultiplier();
            if (baseResult <= 0)
            {
                return baseResult;
            }

            var numerator = baseResult * percent;
            return (numerator + 99) / 100;
        }

        private bool TryBuildSelectionContext(out SelectionContext context, out string error)
        {
            context = default;
            error = string.Empty;
            if (_selection.Count < config.MinExpressionLength)
            {
                error = "Too short";
                return false;
            }

            if (_selection.Count > _currentMaxConnectionLength)
            {
                error = "Too long";
                return false;
            }

            if (_selection[0].Kind != TileKind.Number || _selection[^1].Kind != TileKind.Number)
            {
                error = "Must start/end with number";
                return false;
            }

            for (var i = 0; i < _selection.Count; i++)
            {
                var expected = i % 2 == 0 ? TileKind.Number : TileKind.Operator;
                if (_selection[i].Kind != expected)
                {
                    error = "Must alternate number/operator";
                    return false;
                }
            }

            context = BuildSelectionContextFromCurrentBoard();
            return true;
        }

        private SelectionContext BuildSelectionContextFromCurrentBoard()
        {
            var values = new List<int> { _selection[0].NumberValue };
            var calculationValues = new List<int> { _selection[0].NumberValue };
            var ops = new List<OperatorType>();
            for (var i = 1; i < _selection.Count; i += 2)
            {
                ops.Add(_selection[i].Operator);
                values.Add(_selection[i + 1].NumberValue);
                calculationValues.Add(_selection[i + 1].NumberValue);
            }

            return new SelectionContext(values, calculationValues, ops, _selection.Count);
        }

        private bool TryCalculateExpression(List<int> values, List<OperatorType> operators, out int result, out string error)
        {
            result = 0;
            error = string.Empty;
            var workingValues = new List<int>(values);
            var workingOperators = new List<OperatorType>(operators);

            for (var i = 0; i < workingOperators.Count;)
            {
                if (workingOperators[i] is OperatorType.Multiply or OperatorType.Divide)
                {
                    if (workingOperators[i] == OperatorType.Divide && workingValues[i + 1] == 0)
                    {
                        error = "Divide by zero";
                        return false;
                    }

                    workingValues[i] = workingOperators[i] == OperatorType.Multiply ? workingValues[i] * workingValues[i + 1] : workingValues[i] / workingValues[i + 1];
                    workingValues.RemoveAt(i + 1);
                    workingOperators.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            result = workingValues[0];
            for (var i = 0; i < workingOperators.Count; i++)
            {
                result = workingOperators[i] == OperatorType.Add ? result + workingValues[i + 1] : result - workingValues[i + 1];
            }
            return true;
        }

        private UniqueOutcome ResolveUniqueOutcome(SelectionContext context, int baseResult)
        {
            var bonusDamage = 0;
            var shieldBonus = 0;
            var messageParts = new List<string>();
            var isAttack = _currentCombatMode == CombatMode.Attack;
            var finalNumbers = context.FinalNumbers;

            if (HasUniqueItem(Unique2ItemId) && isAttack && baseResult > 0)
            {
                if (_itemDatabase.TryGetItem(Unique2ItemId, out var unique2))
                {
                    var chance = unique2 != null
                        ? Mathf.Min(
                            _itemDatabase.ResolveEffectInt(unique2, "baseChancePercent") + CountNumber(finalNumbers, 2) * _itemDatabase.ResolveEffectInt(unique2, "chancePerTwoPercent"),
                            _itemDatabase.ResolveEffectInt(unique2, "maxChancePercent"))
                        : 0;

                    if (UnityEngine.Random.Range(0, 100) < chance)
                    {
                        bonusDamage += Mathf.CeilToInt(baseResult * (_itemDatabase.ResolveEffectInt(unique2, "bonusDamagePercent") / 100f));
                        messageParts.Add("유니크 2 발동");
                    }
                }
            }

            var unique3StackCount = 0;
            unique3StackCount += finalNumbers.Contains(3) ? 1 : 0;
            unique3StackCount += finalNumbers.Contains(6) ? 1 : 0;
            unique3StackCount += finalNumbers.Contains(9) ? 1 : 0;
            if (HasUniqueItem(Unique3ItemId) && unique3StackCount > 0)
            {
                if (_itemDatabase.TryGetItem(Unique3ItemId, out var unique3))
                {
                    if (isAttack)
                    {
                        bonusDamage += unique3StackCount * _itemDatabase.ResolveEffectInt(unique3, "attackBonusDamage");
                        shieldBonus += unique3StackCount * _itemDatabase.ResolveEffectInt(unique3, "attackShieldBonus");
                    }
                    else
                    {
                        shieldBonus += unique3StackCount * _itemDatabase.ResolveEffectInt(unique3, "defenseShieldBonus");
                    }

                    messageParts.Add("유니크 3 발동");
                }
            }

            if (HasUniqueItem(Unique5ItemId))
            {
                if (_itemDatabase.TryGetItem(Unique5ItemId, out var unique5))
                {
                    var countFive = CountNumber(finalNumbers, 5);
                    if (countFive > 0)
                    {
                        shieldBonus += countFive * _itemDatabase.ResolveEffectInt(unique5, "shieldPerFive");
                        messageParts.Add("유니크 5 발동");
                    }
                }
            }

            if (HasUniqueItem(Unique7ItemId) && isAttack && context.ExpressionLength == 7 && baseResult > 0)
            {
                if (_itemDatabase.TryGetItem(Unique7ItemId, out var unique7))
                {
                    bonusDamage += Mathf.CeilToInt(baseResult * (_itemDatabase.ResolveEffectInt(unique7, "bonusDamagePercent") / 100f));
                    messageParts.Add("유니크 7 발동");
                }
            }

            var message = messageParts.Count > 0 ? string.Join(", ", messageParts) : "Valid expression!";
            return new UniqueOutcome(bonusDamage, shieldBonus, message);
        }

        private void UpdateUnique1State(SelectionContext context)
        {
            if (!HasUniqueItem(Unique1ItemId) || !_itemDatabase.TryGetItem(Unique1ItemId, out var unique1))
            {
                return;
            }

            var countOnes = CountNumber(context.CalculationNumbers, 1);
            _unique1UsedOneCountThisStage += countOnes;
            if (_unique1UsedOneCountThisStage >= _itemDatabase.ResolveEffectInt(unique1, "requiredOneCount"))
            {
                _unique1UsedOneCountThisStage = 0;
                ApplyUnique1BoardTransform();
            }
        }

        private void ApplyUnique1BoardTransform()
        {
            if (_grid == null)
            {
                return;
            }

            for (var x = 0; x < _grid.GetLength(0); x++)
            {
                for (var y = 0; y < _grid.GetLength(1); y++)
                {
                    var tile = _grid[x, y];
                    if (tile == null || tile.Kind != TileKind.Number || tile.NumberValue != 1)
                    {
                        continue;
                    }

                    tile.SetNumber(11);
                    ApplyTileSpriteVisual(tile);
                }
            }
        }

        private IEnumerator ApplyUnique9BoardTransformIfNeeded()
        {
            if (!HasUniqueItem(Unique9ItemId))
            {
                yield break;
            }

            var numberTiles = new List<BattleTileView>();
            for (var x = 0; x < config.Columns; x++)
            {
                for (var y = 0; y < config.Rows; y++)
                {
                    var tile = _grid[x, y];
                    if (tile != null && tile.Kind == TileKind.Number && tile.NumberValue != 9)
                    {
                        numberTiles.Add(tile);
                    }
                }
            }

            if (numberTiles.Count == 0)
            {
                yield break;
            }

            var selectedTile = numberTiles[UnityEngine.Random.Range(0, numberTiles.Count)];
            selectedTile.SetNumber(9);
            yield return ShowUnique9TransformPreview(selectedTile);
            _unique9TransformedTiles.Add(selectedTile);
            ApplyTileSpriteVisual(selectedTile);
        }

        private IEnumerator ShowUnique9TransformPreview(BattleTileView tile)
        {
            if (tile == null)
            {
                yield break;
            }

            var normalSprite = config.Unique9TransformPreviewNormalSprite;
            var hasPreviewSprite = normalSprite != null;
            var previewSeconds = config.Unique9TransformPreviewSeconds;
            if (!hasPreviewSprite || previewSeconds <= 0f)
            {
                yield break;
            }

            tile.ConfigureSprites(normalSprite, normalSprite, config.ShowTileLabelWhenSpriteAssigned);
            yield return new WaitForSeconds(previewSeconds);
        }

        private float GetNumberChanceForCell(int x, int y)
        {
            var baseOperatorChance = 1f - GetBaseNumberChanceForCell(x, y);
            var adjustedOperatorChance = GetColumnAdjustedOperatorChance(
                x,
                y,
                config.Columns,
                baseOperatorChance);
            return Mathf.Clamp01(1f - adjustedOperatorChance);
        }

        private float GetBaseNumberChanceForCell(int x, int y)
        {
            if (!HasUniqueItem(Unique4ItemId) || !_itemDatabase.TryGetItem(Unique4ItemId, out var unique4))
            {
                var totalDefaultRatio = Mathf.Max(1, config.DefaultNumberSpawnRatio + config.DefaultOperatorSpawnRatio);
                return Mathf.Clamp01(config.DefaultNumberSpawnRatio / (float)totalDefaultRatio);
            }

            var isACell = (x + y) % 2 == 0;
            var numberRatio = isACell
                ? _itemDatabase.ResolveEffectInt(unique4, "aCellNumberRatio")
                : _itemDatabase.ResolveEffectInt(unique4, "bCellNumberRatio");
            var operatorRatio = isACell
                ? _itemDatabase.ResolveEffectInt(unique4, "aCellOperatorRatio")
                : _itemDatabase.ResolveEffectInt(unique4, "bCellOperatorRatio");

            var total = Mathf.Max(1, numberRatio + operatorRatio);
            return Mathf.Clamp01(numberRatio / (float)total);
        }

        private float GetColumnAdjustedOperatorChance(
            int columnIndex,
            int rowIndex,
            int columnCount,
            float baseOperatorChance)
        {
            baseOperatorChance = Mathf.Clamp01(baseOperatorChance);
            var innerColumnCount = columnCount - 2;
            if (innerColumnCount <= 0)
            {
                return baseOperatorChance;
            }

            var isEdgeColumn = columnIndex == 0 || columnIndex == columnCount - 1;
            if (isEdgeColumn)
            {
                return Mathf.Clamp01(baseOperatorChance * EdgeColumnOperatorChanceMultiplier);
            }

            var firstEdgeOperatorChance = 1f - GetBaseNumberChanceForCell(0, rowIndex);
            var lastEdgeOperatorChance = 1f - GetBaseNumberChanceForCell(columnCount - 1, rowIndex);
            var removedEdgeOperatorChance =
                (firstEdgeOperatorChance + lastEdgeOperatorChance)
                * (1f - EdgeColumnOperatorChanceMultiplier);
            var innerColumnBonus = removedEdgeOperatorChance / innerColumnCount;
            return Mathf.Clamp01(baseOperatorChance + innerColumnBonus);
        }

        private void SetCombatMode(CombatMode mode)
        {
            SetCombatMode(mode, true);
        }

        private void SetCombatMode(CombatMode mode, bool playSfx, bool force = false)
        {
            if (_isResolvingTurn && !force)
            {
                return;
            }

            var changed = _currentCombatMode != mode;
            _currentCombatMode = mode;
            battleAnimationManager?.SetPlayerCombatMode(mode);
            RefreshCombatModeButtons();
            RefreshHud(GetExpressionString(), "-");
            _hud.SetMessage(mode == CombatMode.Attack ? "Attack Mode" : "Defense Mode");
            if (changed && playSfx)
            {
                GameAudioManager.Instance?.PlayCombatModeSwitchSfx();
            }
        }

        private void ResetCombatModeToAttack(bool silent)
        {
            SetCombatMode(CombatMode.Attack, !silent, true);
        }

        private void EnsureStartingUniqueSelection()
        {
            if (IsEasyDifficulty())
            {
                _startingUniqueCandidates.Clear();
                _startingUniqueSelectionResolved = true;
                _startingUniqueSelectionOpen = false;
                if (_startUniqueOverlayRoot != null)
                {
                    _startUniqueOverlayRoot.gameObject.SetActive(false);
                }

                SetGameplayInteractionEnabled(true);
                return;
            }

            if (_startingUniqueSelectionResolved || _startUniqueOverlayRoot == null)
            {
                return;
            }

            _startingUniqueCandidates.Clear();
            var chosenIds = new HashSet<string>(StringComparer.Ordinal);
            var count = Mathf.Max(1, _itemDatabase.GetIntConfig("TEMP_STARTING_UNIQUE_CANDIDATE_COUNT"));
            for (var i = 0; i < count; i++)
            {
                var item = PickRandomEligibleItem(ItemSlotKind.Unique, chosenIds, null);
                if (item == null)
                {
                    break;
                }

                chosenIds.Add(item.itemId);
                _startingUniqueCandidates.Add(item);
            }

            if (_startingUniqueCandidates.Count == 0)
            {
                FillStartingUniqueCandidatesFallback(count, chosenIds);
            }

            if (_startingUniqueCandidates.Count == 0)
            {
                _startingUniqueSelectionResolved = true;
                _startingUniqueSelectionOpen = false;
                if (_startUniqueOverlayRoot != null)
                {
                    _startUniqueOverlayRoot.gameObject.SetActive(false);
                }
                SetGameplayInteractionEnabled(true);
                return;
            }

            RefreshStartingUniqueOverlay();
            _startingUniqueSelectionOpen = true;
            SetGameplayInteractionEnabled(false);
            ShowStartingUniqueOverlay();
        }

        private void FillStartingUniqueCandidatesFallback(int count, HashSet<string> chosenIds)
        {
            var upcomingStageNumber = GetUpcomingStageNumber();
            var fallbackItems = _itemDatabase.Items
                .Where(item => item.IsValid)
                .Where(item => item.Category == ItemCategory.UniqueItem)
                .Where(item => item.unlockStage <= upcomingStageNumber)
                .Where(item => !chosenIds.Contains(item.itemId))
                .Take(Mathf.Max(1, count))
                .ToList();

            foreach (var item in fallbackItems)
            {
                if (item == null || chosenIds.Contains(item.itemId))
                {
                    continue;
                }

                chosenIds.Add(item.itemId);
                _startingUniqueCandidates.Add(item);
            }
        }

        private void ShowStartingUniqueOverlay()
        {
            if (_startUniqueOverlayRoot == null)
            {
                return;
            }

            EnsureShoppingParentsActive();
            if (_shopOverlayRoot != null)
            {
                _shopOverlayRoot.gameObject.SetActive(false);
            }

            EnsureHierarchyActive(_startUniqueOverlayRoot);
            _startUniqueOverlayRoot.gameObject.SetActive(true);

            if (_startUniquePanel != null)
            {
                EnsureHierarchyActive(_startUniquePanel);
                _startUniquePanel.gameObject.SetActive(true);
            }

            _startUniqueOverlayRoot.SetAsLastSibling();
            SetCanvasGroupInteraction(_startUniqueOverlayRoot, true);
            if (_startUniquePanel != null)
            {
                SetCanvasGroupInteraction(_startUniquePanel, true);
            }
        }

        private void ForceRuntimeStartingUniqueOverlayFallback()
        {
            if (_usingRuntimeStartingUniqueFallback)
            {
                return;
            }

            if (_startUniqueOverlayRoot != null)
            {
                _startUniqueOverlayRoot.gameObject.SetActive(false);
            }

            _startingUniqueButtons.Clear();
            _startingUniqueSlotReferences.Clear();
            _startingUniqueSelectionAuras.Clear();

            var canvasRoot = _gameplayContainer != null
                ? _gameplayContainer.parent as RectTransform
                : FindAnyObjectByType<Canvas>()?.transform as RectTransform;
            if (canvasRoot == null)
            {
                return;
            }

            CreateRuntimeStartingUniqueSelectionOverlay(canvasRoot);
        }

        private void RefreshStartingUniqueOverlay()
        {
            if (_startUniqueOverlayRoot == null)
            {
                return;
            }

            SetStartingUniqueSelectionAura(null);

            for (var i = 0; i < 3; i++)
            {
                if (i >= _startingUniqueButtons.Count)
                {
                    continue;
                }

                var button = _startingUniqueButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i >= _startingUniqueCandidates.Count)
                {
                    button.gameObject.SetActive(false);
                    ApplyStartingUniqueSlotVisuals(GetStartingUniqueSlotReference(i), null, string.Empty, string.Empty);
                    continue;
                }

                var item = _startingUniqueCandidates[i];
                var presentationName = GetUniqueItemPresentation(item)?.NameKo;
                var displayName = string.IsNullOrWhiteSpace(presentationName) ? item.displayName : presentationName;
                button.gameObject.SetActive(true);
                ApplyStartingUniqueSlotVisuals(
                    GetStartingUniqueSlotReference(i),
                    item,
                    displayName,
                    GetStartingUniqueCardDescriptionText(item));
                var text = GetButtonVisualRefs(button)?.Label ?? button.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = displayName;
                }
            }

            ClearStartingUniqueExplainTexts();
        }

        private void OpenStartingUniqueConfirmPanel(int index)
        {
            if (index < 0 || index >= _startingUniqueCandidates.Count)
            {
                return;
            }

            var item = _startingUniqueCandidates[index];
            _pendingStartingUniqueSelectionIndex = index;
            SetStartingUniqueSelectionAura(index);
            if (_startUniqueExplainTitleText != null)
            {
                var presentationName = GetUniqueItemPresentation(item)?.NameKo;
                _startUniqueExplainTitleText.text = string.IsNullOrWhiteSpace(presentationName) ? item.displayName : presentationName;
            }

            ApplyUniqueItemExplainTexts(_boardLayoutReference?.StartingUniqueLayout?.ExplainTextReferences, item);
        }

        private void ClearStartingUniqueExplainTexts()
        {
            _pendingStartingUniqueSelectionIndex = null;
            SetStartingUniqueSelectionAura(null);
            if (_startUniqueExplainTitleText != null)
            {
                _startUniqueExplainTitleText.text = string.Empty;
            }

            ApplyUniqueItemExplainTexts(_boardLayoutReference?.StartingUniqueLayout?.ExplainTextReferences, null);
        }

        private void ConfirmPendingStartingUniqueSelection()
        {
            if (_startingUniqueConfirmTransitioning)
            {
                return;
            }

            StartCoroutine(ConfirmPendingStartingUniqueSelectionRoutine());
        }

        private IEnumerator ConfirmPendingStartingUniqueSelectionRoutine()
        {
            if (_pendingStartingUniqueSelectionIndex == null)
            {
                yield break;
            }

            var index = _pendingStartingUniqueSelectionIndex.Value;
            if (index < 0 || index >= _startingUniqueCandidates.Count)
            {
                ClearStartingUniqueExplainTexts();
                yield break;
            }

            _startingUniqueConfirmTransitioning = true;
            SetCanvasGroupInteraction(_startUniqueOverlayRoot, false);
            Coroutine musicFadeCoroutine = null;
            if (GameAudioManager.Instance != null)
            {
                musicFadeCoroutine = StartCoroutine(GameAudioManager.Instance.FadeOutMusic(musicFadeOutDuration));
            }

            yield return SceneTransitionFader.Instance.FadeOut(fadeOutDuration);
            if (musicFadeCoroutine != null)
            {
                yield return musicFadeCoroutine;
            }

            var item = _startingUniqueCandidates[index];
            _itemEffectResolver.ApplyAcquiredItem(item, _runtimeItemInventory, _itemDatabase, this);
            RegisterUniqueInventoryHudItem(item);
            _startingUniqueSelectionResolved = true;
            _startingUniqueSelectionOpen = false;
            ClearStartingUniqueExplainTexts();
            if (_startUniqueOverlayRoot != null)
            {
                _startUniqueOverlayRoot.gameObject.SetActive(false);
            }
            SetGameplayInteractionEnabled(true);
            RebuildCachedSpawnWeights();
            ResetStageLocalBattleState();
            InitBattle();
            TryPlayBattleBgmAfterStartingUniqueSelection();
            yield return SceneTransitionFader.Instance.FadeIn(fadeInDuration);
            TryOpenPostStartingUniqueBattleTutorial();
            _startingUniqueConfirmTransitioning = false;
        }

        private void TryPlayBattleBgmAfterStartingUniqueSelection()
        {
            if (_startingUniqueSelectionOpen || _waitingToShowStartingUniqueAfterTutorial || IsTutorialPanelOpen())
            {
                return;
            }

            GameAudioManager.Instance?.PlayBattleBgm();
        }

        private void SetGameplayInteractionEnabled(bool enabled)
        {
            SetCanvasGroupInteraction(_boardContainer, enabled);
            SetCanvasGroupInteraction(_gameplayContainer, enabled);
        }

        private void UpdateHighestDamageThisRun(int damage)
        {
            if (damage <= _highestDamageThisRun)
            {
                return;
            }

            _highestDamageThisRun = damage;
            if (_defeatMaxDamageText != null)
            {
                _defeatMaxDamageText.text = BuildDefeatMaxDamageText();
            }
        }

        private string BuildDefeatMaxDamageText()
        {
            return _highestDamageThisRun.ToString();
        }

        private void CaptureStageStartSnapshotIfReady()
        {
            if (_startingUniqueSelectionOpen || _playerState == null || _runtimeItemInventory == null)
            {
                return;
            }

            _stageStartSnapshot = new RuntimeStageSnapshot
            {
                CurrentStage = _playerState.CurrentStage,
                Gold = _playerState.Gold,
                RerollUsedCountThisRun = _playerState.RerollUsedCountThisRun,
                PlayerHp = _playerHp,
                CurrentPlayerMaxHp = _currentPlayerMaxHp,
                CurrentMaxConnectionLength = _currentMaxConnectionLength,
                NumberWeightModifiers = new Dictionary<int, int>(_numberWeightModifiers),
                OperatorWeightModifiers = new Dictionary<string, int>(_operatorWeightModifiers, StringComparer.Ordinal),
                InventorySnapshot = _runtimeItemInventory.CaptureSnapshot(),
                UniqueHudItemIds = new List<string>(_acquiredUniqueHudItemIds),
            };
        }

        private void RestoreStageStartSnapshot()
        {
            if (_stageStartSnapshot == null)
            {
                return;
            }

            _playerState ??= new RuntimePlayerState();
            _runtimeItemInventory ??= new RuntimeItemInventory();

            _playerState.CurrentStage = _stageStartSnapshot.CurrentStage;
            _playerState.Gold = _stageStartSnapshot.Gold;
            _playerState.RerollUsedCountThisRun = _stageStartSnapshot.RerollUsedCountThisRun;
            _playerHp = _stageStartSnapshot.PlayerHp;
            _currentPlayerMaxHp = _stageStartSnapshot.CurrentPlayerMaxHp;
            _currentMaxConnectionLength = _stageStartSnapshot.CurrentMaxConnectionLength;

            _numberWeightModifiers.Clear();
            foreach (var entry in _stageStartSnapshot.NumberWeightModifiers)
            {
                _numberWeightModifiers[entry.Key] = entry.Value;
            }

            _operatorWeightModifiers.Clear();
            foreach (var entry in _stageStartSnapshot.OperatorWeightModifiers)
            {
                _operatorWeightModifiers[entry.Key] = entry.Value;
            }

            _runtimeItemInventory.RestoreSnapshot(_stageStartSnapshot.InventorySnapshot);
            RestoreUniqueInventoryHudState(_stageStartSnapshot.UniqueHudItemIds);
            _startingUniqueSelectionResolved = true;
            RebuildCachedSpawnWeights();
            RefreshBoardTileSpriteVisuals();
        }

        private void ResetUniqueInventoryHudState()
        {
            _acquiredUniqueHudItemIds.Clear();
            RefreshUniqueInventoryHud();
        }

        private void RestoreUniqueInventoryHudState(IEnumerable<string> itemIds)
        {
            _acquiredUniqueHudItemIds.Clear();
            if (itemIds != null)
            {
                foreach (var itemId in itemIds)
                {
                    var normalizedItemId = itemId?.Trim();
                    if (string.IsNullOrEmpty(normalizedItemId)
                        || _runtimeItemInventory != null && !_runtimeItemInventory.HasAcquiredItem(normalizedItemId))
                    {
                        continue;
                    }

                    TryAddUniqueInventoryHudItemId(normalizedItemId);
                }
            }

            RefreshUniqueInventoryHud();
        }

        private void BuildUniqueHudInfoOverlay(RectTransform canvasRoot)
        {
            _uniqueHudInfoOverlayRoot = uniqueHudInfoOverlayRoot != null
                ? uniqueHudInfoOverlayRoot
                : uniqueHudInfoPanelRoot != null
                    ? uniqueHudInfoPanelRoot
                    : null;
            _uniqueHudInfoPanel = uniqueHudInfoPanelRoot != null
                ? uniqueHudInfoPanelRoot
                : _uniqueHudInfoOverlayRoot;
            _uniqueHudInfoPreviewRoot = uniqueHudInfoPreviewRoot;
            _uniqueHudInfoTitleText = uniqueHudInfoNameText;
            _uniqueHudInfoDescriptionText = uniqueHudInfoDescriptionText;

            if (_uniqueHudInfoOverlayRoot != null || _uniqueHudInfoPanel != null)
            {
                BindButton(uniqueHudInfoConfirmButton, CloseUniqueHudInfoPanel);
                SetUniqueHudInfoVisible(false);
                return;
            }

            if (canvasRoot == null)
            {
                return;
            }

            _uniqueHudInfoOverlayRoot = CreateUiPanel("UniqueHudInfoOverlay", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureDimOverlayVisual(_uniqueHudInfoOverlayRoot, config.ShopConfirmDimColor);

            _uniqueHudInfoPanel = CreateCenteredSquarePanel("UniqueHudInfoPanel", _uniqueHudInfoOverlayRoot, config.ShopConfirmPanelSide);
            var panelImage = _uniqueHudInfoPanel.gameObject.AddComponent<Image>();
            ApplyPanelVisual(panelImage, config.ShopConfirmPanelSprite, config.ShopConfirmPanelColor);

            _uniqueHudInfoPreviewRoot = CreateUiPanel("UniqueHudInfoPreviewRoot", _uniqueHudInfoPanel, new Vector2(0.38f, 0.68f), new Vector2(0.62f, 0.88f), Vector2.zero, Vector2.zero);

            _uniqueHudInfoTitleText = CreateText("UniqueHudInfoTitle", _uniqueHudInfoPanel, new Vector2(0.5f, 0.60f), 42f, config.ShopFontSizeScale);
            _uniqueHudInfoTitleText.rectTransform.anchorMin = _uniqueHudInfoTitleText.rectTransform.anchorMax = new Vector2(0.5f, 0.60f);
            _uniqueHudInfoTitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _uniqueHudInfoTitleText.rectTransform.sizeDelta = new Vector2(760f, 70f);
            _uniqueHudInfoTitleText.alignment = TextAlignmentOptions.Center;
            _uniqueHudInfoTitleText.color = config.ShopPanelTextColor;

            _uniqueHudInfoDescriptionText = CreateText("UniqueHudInfoDescription", _uniqueHudInfoPanel, new Vector2(0.5f, 0.42f), 28f, config.ShopFontSizeScale);
            _uniqueHudInfoDescriptionText.rectTransform.anchorMin = _uniqueHudInfoDescriptionText.rectTransform.anchorMax = new Vector2(0.5f, 0.42f);
            _uniqueHudInfoDescriptionText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _uniqueHudInfoDescriptionText.rectTransform.sizeDelta = new Vector2(760f, 280f);
            _uniqueHudInfoDescriptionText.alignment = TextAlignmentOptions.TopLeft;
            _uniqueHudInfoDescriptionText.enableWordWrapping = true;
            _uniqueHudInfoDescriptionText.overflowMode = TextOverflowModes.Overflow;
            _uniqueHudInfoDescriptionText.color = config.ShopPanelTextColor;

            var okButton = CreateActionButton(_uniqueHudInfoPanel, "확인", new Vector2(0.5f, 0.10f), CloseUniqueHudInfoPanel, false, config.ShopConfirmActionButtonWidth, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(okButton, config.ShopButtonTextColor);

            SetUniqueHudInfoVisible(false);
        }

        private void RegisterUniqueInventoryHudItem(ItemData item)
        {
            if (item == null || item.Category != ItemCategory.UniqueItem)
            {
                return;
            }

            if (TryAddUniqueInventoryHudItemId(item.itemId))
            {
                RefreshUniqueInventoryHud();
            }
        }

        private bool TryAddUniqueInventoryHudItemId(string itemId)
        {
            itemId = itemId?.Trim();
            if (string.IsNullOrEmpty(itemId)
                || _acquiredUniqueHudItemIds.Any(existing => string.Equals(existing, itemId, StringComparison.Ordinal))
                || _acquiredUniqueHudItemIds.Count >= GetUniqueInventoryHudCapacity())
            {
                return false;
            }

            _acquiredUniqueHudItemIds.Add(itemId);
            return true;
        }

        private int GetUniqueInventoryHudCapacity()
        {
            var configuredSlotCount = uniqueHudSlots != null ? uniqueHudSlots.Length : 0;
            return Mathf.Max(0, configuredSlotCount > 0 ? configuredSlotCount : FallbackUniqueInventoryHudSlots);
        }

        private void RefreshUniqueInventoryHud()
        {
            var slots = uniqueHudSlots ?? Array.Empty<UniqueHudSlot>();
            for (var i = 0; i < slots.Length; i++)
            {
                ApplyUniqueInventoryHudEmptySlot(slots[i]);
                ConfigureUniqueInventoryHudSlotButton(slots[i], i, false);
            }

            var displayCount = Mathf.Min(GetUniqueInventoryHudCapacity(), slots.Length, _acquiredUniqueHudItemIds.Count);
            for (var i = 0; i < displayCount; i++)
            {
                var itemId = _acquiredUniqueHudItemIds[i];
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                ConfigureUniqueInventoryHudSlotButton(slots[i], i, true);
                if (TryGetUniqueInventoryHudIcon(itemId, out var icon))
                {
                    ApplyUniqueInventoryHudIcon(slots[i], icon);
                    continue;
                }

                WarnMissingUniqueInventoryHudIcon(itemId);
            }
        }

        private void ApplyUniqueInventoryHudEmptySlot(UniqueHudSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            var slotFrameImage = slot.SlotFrameImage;
            if (slotFrameImage != null)
            {
                if (uniqueEmptySlotSprite != null)
                {
                    slotFrameImage.sprite = uniqueEmptySlotSprite;
                }

                slotFrameImage.enabled = true;
                var frameColor = slotFrameImage.color;
                if (frameColor.a <= 0f)
                {
                    slotFrameImage.color = new Color(frameColor.r, frameColor.g, frameColor.b, 1f);
                }
            }

            var iconImage = slot.IconImage;
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = null;
            iconImage.enabled = false;
            iconImage.gameObject.SetActive(false);
            var iconColor = iconImage.color;
            iconImage.color = new Color(iconColor.r, iconColor.g, iconColor.b, 0f);
        }

        private static void ApplyUniqueInventoryHudIcon(UniqueHudSlot slot, Sprite icon)
        {
            if (slot?.IconImage == null || icon == null)
            {
                return;
            }

            var iconImage = slot.IconImage;
            iconImage.gameObject.SetActive(true);
            iconImage.enabled = true;
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            var iconColor = iconImage.color;
            iconImage.color = new Color(iconColor.r, iconColor.g, iconColor.b, 1f);
        }

        private bool TryGetUniqueInventoryHudIcon(string itemId, out Sprite icon)
        {
            icon = null;
            if (_itemDatabase != null && _itemDatabase.TryGetItem(itemId, out var item))
            {
                icon = _boardLayoutReference?.ItemCategoryIcons?.GetIcon(item);
                if (icon != null)
                {
                    return true;
                }
            }

            var entries = uniqueHudIconSprites ?? Array.Empty<UniqueIconEntry>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null || !string.Equals(entry.ItemId?.Trim(), itemId, StringComparison.Ordinal))
                {
                    continue;
                }

                icon = entry.Icon;
                return icon != null;
            }

            return false;
        }

        private void WarnMissingUniqueInventoryHudIcon(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !_missingUniqueHudIconWarnings.Add(itemId))
            {
                return;
            }

            Debug.LogWarning($"Unique inventory HUD icon is not assigned for itemId '{itemId}'.");
        }

        private void ConfigureUniqueInventoryHudSlotButton(UniqueHudSlot slot, int index, bool hasItem)
        {
            var button = ResolveUniqueInventoryHudSlotButton(slot);
            if (button == null)
            {
                return;
            }

            button.enabled = true;
            button.interactable = hasItem;
            EnsureUniqueInventoryHudSlotRaycastTarget(slot, button, hasItem);
            if (hasItem)
            {
                var capturedIndex = index;
                BindButton(button, () => OpenUniqueHudInfoPanel(capturedIndex));
            }
            else
            {
                BindButton(button, null);
            }
        }

        private static void EnsureUniqueInventoryHudSlotRaycastTarget(UniqueHudSlot slot, Button button, bool hasItem)
        {
            if (slot == null || button == null)
            {
                return;
            }

            var targetGraphic = button.targetGraphic != null
                ? button.targetGraphic
                : slot.SlotFrameImage != null
                    ? slot.SlotFrameImage
                    : slot.IconImage;
            if (targetGraphic != null)
            {
                button.targetGraphic = targetGraphic;
                targetGraphic.raycastTarget = hasItem;
            }

            if (slot.SlotFrameImage != null)
            {
                slot.SlotFrameImage.raycastTarget = hasItem;
            }

            if (slot.IconImage != null)
            {
                slot.IconImage.raycastTarget = false;
            }
        }

        private static Button ResolveUniqueInventoryHudSlotButton(UniqueHudSlot slot)
        {
            if (slot == null)
            {
                return null;
            }

            if (slot.Button != null)
            {
                return slot.Button;
            }

            var targetObject = slot.SlotFrameImage != null
                ? slot.SlotFrameImage.gameObject
                : slot.IconImage != null
                    ? slot.IconImage.gameObject
                    : null;
            if (targetObject == null)
            {
                return null;
            }

            var button = targetObject.GetComponent<Button>();
            if (button == null)
            {
                button = targetObject.AddComponent<Button>();
            }

            if (button.targetGraphic == null)
            {
                button.targetGraphic = targetObject.GetComponent<Graphic>();
            }

            return button;
        }

        private bool IsUniqueHudInfoPanelOpen()
        {
            return _uniqueHudInfoOverlayRoot != null && _uniqueHudInfoOverlayRoot.gameObject.activeInHierarchy;
        }

        private void OpenUniqueHudInfoPanel(int uniqueHudSlotIndex)
        {
            if (_uniqueHudInfoOverlayRoot == null
                || uniqueHudSlotIndex < 0
                || uniqueHudSlotIndex >= _acquiredUniqueHudItemIds.Count)
            {
                return;
            }

            var itemId = _acquiredUniqueHudItemIds[uniqueHudSlotIndex];
            if (string.IsNullOrWhiteSpace(itemId) || _itemDatabase == null || !_itemDatabase.TryGetItem(itemId, out var item))
            {
                return;
            }

            if (_uniqueHudInfoTitleText != null)
            {
                _uniqueHudInfoTitleText.text = item.displayName;
            }

            if (_uniqueHudInfoDescriptionText != null)
            {
                _uniqueHudInfoDescriptionText.text = string.IsNullOrWhiteSpace(item.uiDescriptionKo) ? "설명 없음" : item.uiDescriptionKo;
            }

            RefreshUniqueHudInfoPreview(itemId);
            SetUniqueHudInfoVisible(true);
            (_uniqueHudInfoOverlayRoot != null ? _uniqueHudInfoOverlayRoot : _uniqueHudInfoPanel)?.SetAsLastSibling();
        }

        private void CloseUniqueHudInfoPanel()
        {
            ClearUniqueHudInfoPreview();
            SetUniqueHudInfoVisible(false);
        }

        private void SetUniqueHudInfoVisible(bool visible)
        {
            if (_uniqueHudInfoOverlayRoot != null)
            {
                _uniqueHudInfoOverlayRoot.gameObject.SetActive(visible);
            }
            else if (_uniqueHudInfoPanel != null)
            {
                _uniqueHudInfoPanel.gameObject.SetActive(visible);
            }
        }

        private void RefreshUniqueHudInfoPreview(string itemId)
        {
            if (_uniqueHudInfoPreviewRoot == null)
            {
                return;
            }

            ClearUniqueHudInfoPreview();
            if (!TryGetUniqueInventoryHudIcon(itemId, out var icon) || icon == null)
            {
                return;
            }

            _uniqueHudInfoPreviewInstance = CreateUiPanel("UniqueHudInfoPreviewIcon", _uniqueHudInfoPreviewRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var iconImage = _uniqueHudInfoPreviewInstance.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        private void ClearUniqueHudInfoPreview()
        {
            if (_uniqueHudInfoPreviewInstance == null)
            {
                return;
            }

            Destroy(_uniqueHudInfoPreviewInstance);
            _uniqueHudInfoPreviewInstance = null;
        }

        private static void SetCanvasGroupInteraction(Component target, bool enabled)
        {
            if (target == null)
            {
                return;
            }

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

        private static void EnsureHierarchyActive(Transform leaf)
        {
            var current = leaf;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }

                current = current.parent;
            }
        }

        private void SetStartingUniqueSelectionAura(int? selectedIndex)
        {
            for (var i = 0; i < _startingUniqueSelectionAuras.Count; i++)
            {
                var auraObject = _startingUniqueSelectionAuras[i];
                if (auraObject == null)
                {
                    continue;
                }

                auraObject.SetActive(selectedIndex.HasValue && selectedIndex.Value == i);
            }
        }

        private bool HasUniqueItem(string itemId)
        {
            return _runtimeItemInventory.HasAcquiredItem(itemId);
        }

        private static int CountNumber(IEnumerable<int> numbers, int target)
        {
            return numbers.Count(value => value == target);
        }

        private IEnumerator ResolveBoard()
        {
            _lastAutoLineClearDamage = 0;
            ResetAutoLineClearDamagePresentation();
            var selectedTiles = _selection.ToList();
            ClearSelectionVisual();
            RemoveTiles(selectedTiles, false);
            ApplyGravityAndRefill(true);
            yield return ResolveAutoLineClearsDuringGameplay();
        }

        private IEnumerator ResolveAutoLineClearsDuringGameplay()
        {
            for (var loop = 0; loop < MaxAutoLineClearLoops; loop++)
            {
                var lineGroups = FindSameTypeLineGroups();
                if (lineGroups.Count == 0)
                {
                    yield return ApplyPendingAutoLineClearDamageRoutine();
                    yield break;
                }

                var settleDelay = GetBoardAnimationSettleDelay();
                if (settleDelay > 0f)
                {
                    yield return new WaitForSeconds(settleDelay);
                }

                yield return PreviewAutoLineClearSequence(lineGroups);

                var previewDelay = config.AutoLineClearPreviewSeconds;
                if (previewDelay > 0f)
                {
                    yield return new WaitForSeconds(previewDelay);
                }

                var lineTiles = FlattenLineClearGroups(lineGroups);
                var lineDamages = CalculateAutoLineClearDamages(lineGroups);
                foreach (var damage in lineDamages)
                {
                    if (damage <= 0)
                    {
                        continue;
                    }

                    var previousDamage = _lastAutoLineClearDamage;
                    _lastAutoLineClearDamage += damage;
                    yield return AnimateAutoLineClearDamageText(previousDamage, _lastAutoLineClearDamage);
                }

                TrackEdgeNumberLineClearStreaks(lineGroups);
                RemoveTiles(lineTiles, false);
                ApplyGravityAndRefill(true);
            }

            yield return ApplyPendingAutoLineClearDamageRoutine();
        }

        private void ResolveAutoLineClears(bool animate)
        {
            for (var loop = 0; loop < MaxAutoLineClearLoops; loop++)
            {
                var lineGroups = FindSameTypeLineGroups();
                if (lineGroups.Count == 0)
                {
                    break;
                }

                var lineTiles = FlattenLineClearGroups(lineGroups);
                RemoveTiles(lineTiles, !animate);
                ApplyGravityAndRefill(animate);
            }
        }

        private List<LineClearGroup> FindSameTypeLineGroups()
        {
            var groups = new List<LineClearGroup>();
            for (var y = 0; y < config.Rows; y++)
            {
                var kind = _grid[0, y].Kind;
                var isSame = true;
                for (var x = 1; x < config.Columns; x++)
                {
                    if (_grid[x, y].Kind != kind)
                    {
                        isSame = false;
                        break;
                    }
                }

                if (!isSame)
                {
                    continue;
                }

                var rowTiles = new List<BattleTileView>(config.Columns);
                for (var x = 0; x < config.Columns; x++)
                {
                    rowTiles.Add(_grid[x, y]);
                }

                groups.Add(new LineClearGroup(kind, LineClearDirection.Horizontal, rowTiles));
            }

            for (var x = 0; x < config.Columns; x++)
            {
                var kind = _grid[x, 0].Kind;
                var isSame = true;
                for (var y = 1; y < config.Rows; y++)
                {
                    if (_grid[x, y].Kind != kind)
                    {
                        isSame = false;
                        break;
                    }
                }

                if (!isSame)
                {
                    continue;
                }

                var columnTiles = new List<BattleTileView>(config.Rows);
                for (var y = 0; y < config.Rows; y++)
                {
                    columnTiles.Add(_grid[x, y]);
                }

                columnTiles.Sort((left, right) => right.Y.CompareTo(left.Y));
                groups.Add(new LineClearGroup(kind, LineClearDirection.Vertical, columnTiles));
            }

            return groups;
        }

        private static HashSet<BattleTileView> FlattenLineClearGroups(IEnumerable<LineClearGroup> lineGroups)
        {
            var toClear = new HashSet<BattleTileView>();
            foreach (var group in lineGroups)
            {
                foreach (var tile in group.Tiles)
                {
                    if (tile != null)
                    {
                        toClear.Add(tile);
                    }
                }
            }

            return toClear;
        }

        private IEnumerator PreviewAutoLineClearSequence(IEnumerable<LineClearGroup> lineGroups)
        {
            var stepDelay = config.AutoLineClearSequentialSelectionInterval;
            if (stepDelay <= 0f)
            {
                yield break;
            }

            var alreadyPreviewed = new HashSet<BattleTileView>();
            foreach (var group in lineGroups)
            {
                foreach (var tile in group.Tiles)
                {
                    if (tile == null || !alreadyPreviewed.Add(tile))
                    {
                        continue;
                    }

                    tile.SetSelected(true);
                    yield return new WaitForSeconds(stepDelay);
                }
            }
        }

        private List<int> CalculateAutoLineClearDamages(IEnumerable<LineClearGroup> lineGroups)
        {
            var damages = new List<int>();
            foreach (var group in lineGroups)
            {
                var damage = CalculateAutoLineClearDamage(group);
                if (damage > 0)
                {
                    damages.Add(damage);
                }
            }

            return damages;
        }

        private int CalculateAutoLineClearDamage(LineClearGroup group)
        {
            if (group == null)
            {
                return 0;
            }

            if (group.Kind == TileKind.Operator)
            {
                return OperatorLineClearFixedDamage;
            }

            var numberValueSum = 0;
            var numberCount = 0;
            foreach (var tile in group.Tiles)
            {
                if (tile == null || tile.Kind != TileKind.Number)
                {
                    continue;
                }

                numberValueSum += tile.NumberValue;
                numberCount++;
            }

            return numberCount > 0 ? Mathf.CeilToInt(numberValueSum / (float)numberCount) : 0;
        }

        private IEnumerator ApplyPendingAutoLineClearDamageRoutine()
        {
            if (_lastAutoLineClearDamage <= 0 || _enemyHp <= 0)
            {
                yield break;
            }

            _enemyHp = Mathf.Max(0, _enemyHp - _lastAutoLineClearDamage);
            RefreshHud(string.Empty, "-");
            yield return PlayAutoLineClearDamageFinalPunch();
            var resultDisplaySeconds = Mathf.Max(0f, autoLineClearDamageResultDisplaySeconds);
            if (resultDisplaySeconds > 0f)
            {
                yield return new WaitForSeconds(resultDisplaySeconds);
            }

            SetAutoLineClearDamagePanelVisible(false);
        }

        private IEnumerator AnimateAutoLineClearDamageText(int from, int to)
        {
            if (autoLineClearDamageText == null)
            {
                yield break;
            }

            SetAutoLineClearDamagePanelVisible(true);
            CancelAutoLineClearDamageCountMotion();
            var delta = Mathf.Abs(to - from);
            var speed = Mathf.Max(1f, autoLineClearDamageCountUpSpeed);
            var duration = delta / speed;
            autoLineClearDamageText.text = from.ToString();
            if (duration <= 0f)
            {
                autoLineClearDamageText.text = to.ToString();
                yield break;
            }

            _autoLineClearDamageCountMotionHandle = LMotion.Create((float)from, to, duration)
                .Bind(value => autoLineClearDamageText.text = Mathf.RoundToInt(value).ToString())
                .AddTo(this);
            yield return new WaitForSeconds(duration);
            autoLineClearDamageText.text = to.ToString();
        }

        private IEnumerator PlayAutoLineClearDamageFinalPunch()
        {
            if (autoLineClearDamageText == null || autoLineClearDamagePunchScale <= 0f || autoLineClearDamagePunchDuration <= 0f)
            {
                yield break;
            }

            CancelAutoLineClearDamagePunchMotion();
            var rect = autoLineClearDamageText.rectTransform;
            var baseScale = rect.localScale;
            _autoLineClearDamagePunchMotionHandle = LMotion.Punch.Create(baseScale, Vector3.one * autoLineClearDamagePunchScale, autoLineClearDamagePunchDuration)
                .BindToLocalScale(rect)
                .AddTo(this);
            yield return new WaitForSeconds(autoLineClearDamagePunchDuration);
            rect.localScale = baseScale;
        }

        private void ResetAutoLineClearDamagePresentation()
        {
            CancelAutoLineClearDamageCountMotion();
            CancelAutoLineClearDamagePunchMotion();
            if (autoLineClearDamageText != null)
            {
                autoLineClearDamageText.text = "0";
            }

            SetAutoLineClearDamagePanelVisible(false);
        }

        private void SetAutoLineClearDamagePanelVisible(bool visible)
        {
            if (autoLineClearDamagePanelRoot != null)
            {
                autoLineClearDamagePanelRoot.SetActive(visible);
            }
            else if (autoLineClearDamageText != null)
            {
                autoLineClearDamageText.gameObject.SetActive(visible);
            }
        }

        private void CancelAutoLineClearDamageCountMotion()
        {
            if (_autoLineClearDamageCountMotionHandle.IsActive())
            {
                _autoLineClearDamageCountMotionHandle.Cancel();
            }
        }

        private void CancelAutoLineClearDamagePunchMotion()
        {
            if (_autoLineClearDamagePunchMotionHandle.IsActive())
            {
                _autoLineClearDamagePunchMotionHandle.Cancel();
            }
        }

        private void TrackEdgeNumberLineClearStreaks(IEnumerable<LineClearGroup> lineGroups)
        {
            foreach (var group in lineGroups)
            {
                if (group == null || group.Direction != LineClearDirection.Vertical || group.Tiles.Count == 0)
                {
                    continue;
                }

                var columnIndex = group.Tiles[0].X;
                if (!IsEdgeColumn(columnIndex))
                {
                    continue;
                }

                if (group.Kind == TileKind.Number)
                {
                    IncrementEdgeNumberLineClearStreak(columnIndex);
                }
                else if (group.Kind == TileKind.Operator)
                {
                    ResetEdgeNumberLineClearCorrection(columnIndex);
                }
            }
        }

        private void IncrementEdgeNumberLineClearStreak(int columnIndex)
        {
            if (IsLeftEdgeColumn(columnIndex))
            {
                _leftEdgeNumberLineClearStreak++;
                if (_leftEdgeNumberLineClearStreak >= EdgeNumberLineClearForceThreshold)
                {
                    _forceOperatorOnNextLeftEdgeRefill = true;
                }

                return;
            }

            _rightEdgeNumberLineClearStreak++;
            if (_rightEdgeNumberLineClearStreak >= EdgeNumberLineClearForceThreshold)
            {
                _forceOperatorOnNextRightEdgeRefill = true;
            }
        }

        private void ResetEdgeNumberLineClearCorrection()
        {
            _leftEdgeNumberLineClearStreak = 0;
            _rightEdgeNumberLineClearStreak = 0;
            _forceOperatorOnNextLeftEdgeRefill = false;
            _forceOperatorOnNextRightEdgeRefill = false;
        }

        private void ResetEdgeNumberLineClearCorrection(int columnIndex)
        {
            if (IsLeftEdgeColumn(columnIndex))
            {
                _leftEdgeNumberLineClearStreak = 0;
                _forceOperatorOnNextLeftEdgeRefill = false;
                return;
            }

            _rightEdgeNumberLineClearStreak = 0;
            _forceOperatorOnNextRightEdgeRefill = false;
        }

        private bool IsEdgeColumn(int columnIndex)
        {
            return columnIndex == 0 || columnIndex == config.Columns - 1;
        }

        private bool IsLeftEdgeColumn(int columnIndex)
        {
            return columnIndex == 0;
        }

        private string BuildBoardResolutionMessage(string resultMessage)
        {
            var baseMessage = string.IsNullOrWhiteSpace(resultMessage) ? string.Empty : resultMessage.Trim();
            if (_lastAutoLineClearDamage <= 0)
            {
                return baseMessage;
            }

            var autoLineDamageMessage = $"줄 제거 데미지 {_lastAutoLineClearDamage}";
            if (string.IsNullOrEmpty(baseMessage))
            {
                return autoLineDamageMessage;
            }

            return $"{baseMessage}\n{autoLineDamageMessage}";
        }

        private void RemoveTiles(IEnumerable<BattleTileView> tiles, bool destroyImmediately)
        {
            foreach (var tile in tiles)
            {
                if (tile == null)
                {
                    continue;
                }

                _unique9TransformedTiles.Remove(tile);
                _grid[tile.X, tile.Y] = null;
                if (destroyImmediately)
                {
                    DestroyImmediate(tile.gameObject);
                }
                else
                {
                    Destroy(tile.gameObject);
                }
            }
        }

        private float GetBoardAnimationSettleDelay()
        {
            var delay = config.TileFallDuration;
            if (config.TileLandingBounceOffset > 0f && config.TileLandingBounceDuration > 0f)
            {
                delay += config.TileLandingBounceDuration;
            }

            return Mathf.Max(0f, delay);
        }

        private void ApplyGravityAndRefill(bool animate)
        {
            var layoutMetrics = GetBoardLayoutMetrics();
            _cellSize = layoutMetrics.CellSize;
            for (var x = 0; x < config.Columns; x++)
            {
                var writeY = config.Rows - 1;
                for (var y = config.Rows - 1; y >= 0; y--)
                {
                    var tile = _grid[x, y];
                    if (tile == null)
                    {
                        continue;
                    }

                    _grid[x, y] = null;
                    _grid[x, writeY] = tile;
                    var startPosition = GetTileAnchoredPosition(x, y, layoutMetrics);
                    var targetPosition = GetTileAnchoredPosition(x, writeY, layoutMetrics);
                    tile.SetGridPos(x, writeY);
                    if (animate)
                    {
                        tile.AnimateBoardFall(startPosition, targetPosition, layoutMetrics.TileSize, config);
                    }
                    else
                    {
                        tile.SetBoardVisualLayout(layoutMetrics.TileSize, targetPosition);
                    }
                    writeY--;
                }

                var spawnIndex = 0;
                var forcedOperatorRows = GetForcedOperatorRowsForRefill(x, writeY + 1);
                for (var y = writeY; y >= 0; y--)
                {
                    var tile = CreateTile(x, y, layoutMetrics);
                    SpawnTileValue(tile, x, y, forcedOperatorRows.Contains(y));
                    var targetPosition = GetTileAnchoredPosition(x, y, layoutMetrics);
                    if (animate)
                    {
                        var startPosition = GetTileAnchoredPosition(x, -(spawnIndex + 1), layoutMetrics);
                        tile.AnimateBoardFall(startPosition, targetPosition, layoutMetrics.TileSize, config);
                    }
                    else
                    {
                        tile.SetBoardVisualLayout(layoutMetrics.TileSize, targetPosition);
                    }
                    _grid[x, y] = tile;
                    spawnIndex++;
                }
            }
        }

        private HashSet<int> GetForcedOperatorRowsForRefill(int columnIndex, int refillCount)
        {
            var forcedRows = new HashSet<int>();
            if (refillCount <= 0 || !IsEdgeColumn(columnIndex))
            {
                return forcedRows;
            }

            var shouldForce = IsLeftEdgeColumn(columnIndex)
                ? _forceOperatorOnNextLeftEdgeRefill
                : _forceOperatorOnNextRightEdgeRefill;
            if (!shouldForce)
            {
                return forcedRows;
            }

            var forceCount = UnityEngine.Random.Range(1, Mathf.Min(MaxForcedEdgeOperatorsPerRefill, refillCount) + 1);
            var candidates = new List<int>(refillCount);
            for (var y = 0; y < refillCount; y++)
            {
                candidates.Add(y);
            }

            for (var i = 0; i < forceCount && candidates.Count > 0; i++)
            {
                var candidateIndex = UnityEngine.Random.Range(0, candidates.Count);
                forcedRows.Add(candidates[candidateIndex]);
                candidates.RemoveAt(candidateIndex);
            }

            if (forcedRows.Count > 0)
            {
                ResetEdgeNumberLineClearCorrection(columnIndex);
            }

            return forcedRows;
        }

        private void RefreshBoardVisualLayout()
        {
            var layoutMetrics = GetBoardLayoutMetrics();
            _cellSize = layoutMetrics.CellSize;
            for (var x = 0; x < config.Columns; x++)
            {
                for (var y = 0; y < config.Rows; y++)
                {
                    var tile = _grid[x, y];
                    if (tile == null)
                    {
                        continue;
                    }

                    tile.SetGridPos(x, y);
                    tile.SetBoardVisualLayout(layoutMetrics.TileSize, GetTileAnchoredPosition(x, y, layoutMetrics));
                }
            }
        }

        private Vector2 GetTileAnchoredPosition(int x, int y, BoardLayoutMetrics layoutMetrics)
        {
            return new Vector2(layoutMetrics.Origin.x + x * layoutMetrics.Step.x, layoutMetrics.Origin.y - y * layoutMetrics.Step.y);
        }

        private void ResolveBoardLayoutReference()
        {
            _boardLayoutReference = FindAnyObjectByType<BattleBoardLayoutReference>();
            _tileLayoutRoot = _boardLayoutReference != null && _boardLayoutReference.TilePanel != null
                ? _boardLayoutReference.TilePanel
                : _boardRoot;

            if (_tileLayoutRoot != null && _tileLayoutRoot != _boardRoot)
            {
                _boardPanelNormalizedPosition = CaptureNormalizedPivotPosition(_tileLayoutRoot, GetTopOverlayParent());
                ApplyNormalizedPivotPosition(_tileLayoutRoot, GetTopOverlayParent(), _boardPanelNormalizedPosition);
            }

            if (_boardContainer != null)
            {
                var usesExternalPanel = _tileLayoutRoot != null && _tileLayoutRoot != _boardRoot;
                _boardContainer.gameObject.SetActive(!usesExternalPanel);
            }
        }

        private BoardLayoutMetrics GetBoardLayoutMetrics()
        {
            var layoutRoot = _tileLayoutRoot != null ? _tileLayoutRoot : _boardRoot;
            var columns = Mathf.Max(1, config.Columns);
            var rows = Mathf.Max(1, config.Rows);
            var spacing = _boardLayoutReference != null ? _boardLayoutReference.TileSpacing : Vector2.zero;
            var panelWidth = Mathf.Max(1f, layoutRoot.rect.width);
            var panelHeight = Mathf.Max(1f, layoutRoot.rect.height);
            var cellWidth = panelWidth / columns;
            var cellHeight = panelHeight / rows;
            var cellSize = Mathf.Max(1f, Mathf.Min(cellWidth, cellHeight));
            var tileSide = Mathf.Max(1f, cellSize * config.TileSizeScale);
            var origin = _boardLayoutReference != null && _boardLayoutReference.HasCustomStartPoint
                ? GetAnchoredTopLeftPosition(layoutRoot, _boardLayoutReference.TileStartPoint)
                : new Vector2(0f, 0f);

            return new BoardLayoutMetrics(
                cellSize,
                new Vector2(tileSide + spacing.x, tileSide + spacing.y),
                new Vector2(tileSide, tileSide),
                origin);
        }

        private static Vector2 GetAnchoredTopLeftPosition(RectTransform parent, RectTransform target)
        {
            if (parent == null || target == null)
            {
                return Vector2.zero;
            }

            var localPoint = (Vector2)parent.InverseTransformPoint(target.position);
            return new Vector2(
                localPoint.x - parent.rect.xMin,
                localPoint.y - parent.rect.yMax);
        }

        private static Vector2 GetAnchoredTopLeftPosition(RectTransform parent, Vector2 localPoint)
        {
            if (parent == null)
            {
                return Vector2.zero;
            }

            return new Vector2(
                localPoint.x - parent.rect.xMin,
                localPoint.y - parent.rect.yMax);
        }

        private readonly struct BoardLayoutMetrics
        {
            public BoardLayoutMetrics(float cellSize, Vector2 step, Vector2 tileSize, Vector2 origin)
            {
                CellSize = cellSize;
                Step = step;
                TileSize = tileSize;
                Origin = origin;
            }

            public float CellSize { get; }
            public Vector2 Step { get; }
            public Vector2 TileSize { get; }
            public Vector2 Origin { get; }
        }

        private string GetExpressionString()
        {
            return string.Join(" ", _selection.Select(tile => tile.Kind == TileKind.Number ? tile.NumberValue.ToString() : tile.Operator switch
            {
                OperatorType.Add => "+",
                OperatorType.Subtract => "-",
                OperatorType.Multiply => "x",
                OperatorType.Divide => "÷",
                _ => "?",
            }));
        }

        private void ClearSelectionVisual()
        {
            foreach (var tile in _selection)
            {
                if (tile != null)
                {
                    tile.SetSelected(false);
                }
            }

            _selection.Clear();
        }
        private void RefreshDragCountDisplay()
        {
            if (dragCountText == null)
            {
                return;
            }

            var maxCount = Mathf.Max(0, _currentMaxConnectionLength);
            var selectedCount = _selection != null ? _selection.Count : 0;
            var remaining = Mathf.Max(0, maxCount - selectedCount);

            dragCountText.text = remaining.ToString();
        }
        private void RefreshHud(string expression, string result)
        {
            _hud.SetHp(_playerHp, _playerShield, _enemyHp, _currentStage.EnemyHp);
            var left = _currentStage.EnemyAttackCycle - (_validTurnCount % _currentStage.EnemyAttackCycle);
            _hud.SetCountdown(left);
            _hud.SetExpression(expression);
            _hud.SetResult(string.IsNullOrEmpty(result) || result == "-" ? string.Empty : result);
            _hud.SetValidationStatus(GetCurrentExpressionValidity());
            RefreshConvenienceHud();
            RefreshDragCountDisplay();
        }

        private void RefreshConvenienceHud()
        {
            HideConvenienceStatusHud();
            UpdateCurrentStageDisplay();
            UpdateEnemyAttackDamageDisplay();
        }

        private void UpdateCurrentStageDisplay()
        {
            if (currentStageDisplayText != null && _playerState != null)
            {
                currentStageDisplayText.text = $"{Mathf.Max(1, _playerState.CurrentStage)}스테이지";
            }
        }

        private void UpdateEnemyAttackDamageDisplay()
        {
            if (enemyAttackDamageValueText != null)
            {
                enemyAttackDamageValueText.text = _currentStage.EnemyAttackDamage.ToString();
            }
        }

        private int GetTurnsUntilEnemyAttack()
        {
            var attackCycle = Mathf.Max(1, _currentStage.EnemyAttackCycle);
            return attackCycle - (_validTurnCount % attackCycle);
        }

        private bool? GetCurrentExpressionValidity()
        {
            if (_selection == null || _selection.Count == 0)
            {
                return null;
            }

            return TryBuildSelectionContext(out _, out _);
        }

        private void OnStageCleared()
        {
            RestorePlayerHpToFull();
            var reward = GetStageClearGoldReward();
            _playerState.Gold += reward;
            RefreshHud(string.Empty, "-");
            if (_playerState.CurrentStage >= MaxStage)
            {
                _hud.SetMessage("Victory! Demon King defeated.");
                return;
            }

            _hud.SetMessage($"Stage {_playerState.CurrentStage} clear! +{reward} Gold");
            OpenShopPanel();
        }

        private void KillCurrentEnemyForDebug()
        {
            if (_enemyHp <= 0 || _shopOpen || _startingUniqueSelectionOpen || _activeItemConfirmOpen || _defeatOverlayOpen || _isResolvingTurn)
            {
                return;
            }

            _enemyHp = 0;
            RefreshHud(string.Empty, "-");
            StartCoroutine(HandleEnemyDeathThenStageClear());
        }

        private IEnumerator HandleEnemyDeathThenStageClear()
        {
            if (_enemyDeathHandledThisStage)
            {
                yield break;
            }

            _enemyDeathHandledThisStage = true;
            if (battleAnimationManager != null)
            {
                yield return battleAnimationManager.PlayEnemyDeathRoutine();
            }

            GameAudioManager.Instance?.PlayStageVictorySfx();
            yield return ShowStageClearPanelRoutine(_playerState.CurrentStage >= MaxStage);
            _isResolvingTurn = false;
            OnStageCleared();
        }

        private IEnumerator ShowStageClearPanelRoutine(bool allStagesCleared)
        {
            var hasPresentationTarget = stageClearPanelRoot != null || stageClearMessageText != null;
            if (!hasPresentationTarget)
            {
                yield break;
            }

            if (allStagesCleared)
            {
                if (stageClearMessageText != null)
                {
                    stageClearMessageText.text = _stageClearDefaultMessage;
                }

                SetStageClearPanelFadeImageAlpha(GetStageClearPanelBaseAlpha());
                SetAllStageClearReturnToTitleButtonVisible(true);
                SetStageClearPanelVisible(true);
                yield return FadeStageClearPanelImageAlphaToOne();
                yield break;
            }

            if (stageClearMessageText != null)
            {
                stageClearMessageText.text = $"{Mathf.Max(1, _playerState.CurrentStage)}스테이지\n클리어";
            }

            SetStageClearPanelFadeImageAlpha(GetStageClearPanelBaseAlpha());
            SetAllStageClearReturnToTitleButtonVisible(false);
            SetStageClearPanelVisible(true);

            var displaySeconds = Mathf.Max(0f, stageClearPanelDisplaySeconds);
            if (displaySeconds > 0f)
            {
                yield return new WaitForSeconds(displaySeconds);
            }

            SetStageClearPanelVisible(false);
        }

        private IEnumerator FadeStageClearPanelImageAlphaToOne()
        {
            var image = ResolveStageClearFadeImage();
            if (image == null)
            {
                yield break;
            }

            var speed = Mathf.Max(0.01f, allStageClearPanelFadeSpeed);
            while (image.color.a < 1f)
            {
                SetStageClearPanelFadeImageAlpha(Mathf.MoveTowards(image.color.a, 1f, speed * Time.deltaTime));
                yield return null;
            }
        }

        private void CaptureStageClearDefaultMessage()
        {
            _stageClearDefaultMessage = stageClearMessageText != null ? stageClearMessageText.text : string.Empty;
            CaptureStageClearFadeImageBaseColor();
        }

        private void SetStageClearPanelFadeImageAlpha(float alpha)
        {
            var image = ResolveStageClearFadeImage();
            if (image == null)
            {
                return;
            }

            var color = image.color;
            image.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        private float GetStageClearPanelBaseAlpha()
        {
            CaptureStageClearFadeImageBaseColor();
            return _stageClearFadeImageBaseColorCaptured ? _stageClearFadeImageBaseColor.a : 1f;
        }

        private Image ResolveStageClearFadeImage()
        {
            if (_stageClearFadeImage != null)
            {
                return _stageClearFadeImage;
            }

            _stageClearFadeImage = allStageClearFadeImage != null
                ? allStageClearFadeImage
                : stageClearPanelRoot != null
                    ? stageClearPanelRoot.GetComponent<Image>()
                    : null;
            CaptureStageClearFadeImageBaseColor();
            return _stageClearFadeImage;
        }

        private void CaptureStageClearFadeImageBaseColor()
        {
            if (_stageClearFadeImageBaseColorCaptured)
            {
                return;
            }

            var image = _stageClearFadeImage != null
                ? _stageClearFadeImage
                : allStageClearFadeImage != null
                    ? allStageClearFadeImage
                    : stageClearPanelRoot != null
                        ? stageClearPanelRoot.GetComponent<Image>()
                        : null;
            if (image == null)
            {
                return;
            }

            _stageClearFadeImage = image;
            _stageClearFadeImageBaseColor = image.color;
            _stageClearFadeImageBaseColorCaptured = true;
        }

        private void SetStageClearPanelVisible(bool visible)
        {
            if (stageClearPanelRoot != null)
            {
                stageClearPanelRoot.SetActive(visible);
            }
            else if (stageClearMessageText != null)
            {
                stageClearMessageText.gameObject.SetActive(visible);
            }
        }

        private void SetAllStageClearReturnToTitleButtonVisible(bool visible)
        {
            if (allStageClearReturnToTitleButton != null)
            {
                allStageClearReturnToTitleButton.gameObject.SetActive(visible);
            }
        }

        private int GetStageClearGoldReward()
        {
            var reward = _playerState.CurrentStage <= 3
                ? _itemDatabase.GetIntConfig("TEMP_STAGE_CLEAR_GOLD_1_TO_3")
                : _playerState.CurrentStage <= 6
                    ? _itemDatabase.GetIntConfig("TEMP_STAGE_CLEAR_GOLD_4_TO_6")
                    : _itemDatabase.GetIntConfig("TEMP_STAGE_CLEAR_GOLD_7_TO_9");
            if (HasUniqueItem(Unique8ItemId) && _itemDatabase.TryGetItem(Unique8ItemId, out var unique8))
            {
                reward = Mathf.CeilToInt(reward * (_itemDatabase.ResolveEffectInt(unique8, "goldMultiplierPercent") / 100f));
            }

            if (HasUniqueItem(Unique6ItemId) && _itemDatabase.TryGetItem(Unique6ItemId, out var unique6))
            {
                reward += _itemDatabase.ResolveEffectInt(unique6, "flatGoldBonus");
            }

            return Mathf.RoundToInt(reward * StageClearGoldRewardMultiplier);
        }

        private void OpenShopPanel()
        {
            if (_shopOverlayRoot == null)
            {
                BuildShopPanel();
            }

            _shopOpen = true;
            EnsureShoppingParentsActive();
            if (_startUniqueOverlayRoot != null)
            {
                _startUniqueOverlayRoot.gameObject.SetActive(false);
            }
            SetDimOverlayVisible(_shopDimRoot, true);

            var shopFrontTarget = _shopPanel != null ? _shopPanel : _shopOverlayRoot;
            if (_shopOverlayRoot != null && _shopOverlayRoot != shopFrontTarget)
            {
                EnsureHierarchyActive(_shopOverlayRoot);
                _shopOverlayRoot.gameObject.SetActive(true);
            }

            if (shopFrontTarget != null)
            {
                EnsureHierarchyActive(shopFrontTarget);
                shopFrontTarget.gameObject.SetActive(true);
                BringPanelToFront(shopFrontTarget, ref _shopPanelOriginalParent, ref _shopPanelOriginalSiblingIndex);
                PlaceDimOverlayBehind(_shopDimRoot, shopFrontTarget);
                shopFrontTarget.SetAsLastSibling();
            }

            RollShop(true);
            TryOpenShopTutorial();
        }

        private void BuildShopPanel()
        {
            _freeButtons.Clear();
            _paidButtons.Clear();
            _freeButtonSlotReferences.Clear();
            _paidButtonSlotReferences.Clear();

            if (TryBuildShopSceneLayout())
            {
                return;
            }

            var shopOverlayParent = _startUniqueOverlayRoot != null
                ? _startUniqueOverlayRoot.parent as RectTransform
                : _gameplayContainer?.parent as RectTransform ?? _boardRoot.parent as RectTransform;
            _shopOverlayRoot = CreateUiPanel("ShopOverlay", shopOverlayParent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var overlayImage = _shopOverlayRoot.gameObject.AddComponent<Image>();
            overlayImage.color = config.ShopDimColor;
            overlayImage.raycastTarget = true;

            _shopPanel = CreateCenteredSquarePanel("ShopPanel", _shopOverlayRoot, config.ShopMainPanelSide);
            var panelImage = _shopPanel.gameObject.AddComponent<Image>();
            ApplyPanelVisual(panelImage, config.ShopMainPanelSprite, config.ShopMainPanelColor);

            var freeRow = CreateUiPanel("FreeRow", _shopPanel, new Vector2(0.08f, 0.69f), new Vector2(0.92f, 0.91f), Vector2.zero, Vector2.zero);
            var paidRow = CreateUiPanel("PaidRow", _shopPanel, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.65f), Vector2.zero, Vector2.zero);
            var infoRow = CreateUiPanel("InfoRow", _shopPanel, new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.38f), Vector2.zero, Vector2.zero);
            var bottomRow = CreateUiPanel("BottomRow", _shopPanel, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.22f), Vector2.zero, Vector2.zero);

            _shopGoldText = CreateText("Gold", infoRow, new Vector2(0.72f, 0.5f), 38f, config.ShopFontSizeScale);
            _shopGoldText.rectTransform.pivot = new Vector2(0f, 0.5f);
            _shopGoldText.alignment = TextAlignmentOptions.MidlineLeft;
            _shopGoldText.rectTransform.sizeDelta = new Vector2(450f, 120f);
            _shopGoldText.color = config.ShopPanelTextColor;

            for (var i = 0; i < 3; i++)
            {
                _freeButtons.Add(CreateShopButton(freeRow, i, true));
                _paidButtons.Add(CreateShopButton(paidRow, i, false));
            }

            _rerollButton = CreateActionButton(infoRow, "Reroll", new Vector2(0.22f, 0.5f), OnRerollPressed, false, config.ShopMainActionButtonWidth, config.ShopMainActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(_rerollButton, config.ShopButtonTextColor);
            _rerollText = _rerollButton.GetComponentInChildren<TextMeshProUGUI>();
            _nextStageButton = CreateActionButton(bottomRow, "Next Stage", new Vector2(0.50f, 0.5f), OnNextStagePressed, false, config.ShopMainActionButtonWidth, config.ShopMainActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(_nextStageButton, config.ShopButtonTextColor);
            BuildShopConfirmPanel();
            _shopOverlayRoot.gameObject.SetActive(false);
        }

        private void BuildShopConfirmPanel()
        {
            _shopConfirmDimRoot = EnsureShopConfirmDimOverlay();

            if (_boardLayoutReference?.ShopLayout?.ConfirmPanelRoot != null)
            {
                var layout = _boardLayoutReference.ShopLayout;
                _shopConfirmPanel = layout.ConfirmPanelRoot;
                _shopConfirmPreviewRoot = layout.ConfirmPreviewRoot;
                _shopConfirmTitleText = layout.ConfirmNameText;
                _shopConfirmDescriptionText = layout.ConfirmDescriptionText;
                _shopConfirmCostText = layout.ConfirmPriceText;
                _shopPurchaseButton = layout.PurchaseButton;
                if (layout.CancelButton != null)
                {
                    BindButton(layout.CancelButton, CloseShopConfirmPanel);
                }

                if (_shopPurchaseButton != null)
                {
                    BindButton(_shopPurchaseButton, ConfirmPendingShopSelection);
                }

                _shopConfirmPanel.gameObject.SetActive(false);
                SetDimOverlayVisible(_shopConfirmDimRoot, false);
                return;
            }

            _shopConfirmPanel = CreateCenteredSquarePanel("ShopConfirmPanel", _shopPanel, config.ShopConfirmPanelSide);
            var panelImage = _shopConfirmPanel.gameObject.AddComponent<Image>();
            ApplyPanelVisual(panelImage, config.ShopConfirmPanelSprite, config.ShopConfirmPanelColor);
            _shopConfirmPreviewRoot = null;
            _shopConfirmPanel.anchorMin = _shopConfirmPanel.anchorMax = new Vector2(0.5f, 0.5f);
            _shopConfirmPanel.pivot = new Vector2(0.5f, 0.5f);
            _shopConfirmPanel.anchoredPosition = Vector2.zero;

            _shopConfirmTitleText = CreateText("ShopConfirmTitle", _shopConfirmPanel, new Vector2(0.5f, 0.86f), 42f, config.ShopFontSizeScale);
            _shopConfirmTitleText.rectTransform.anchorMin = _shopConfirmTitleText.rectTransform.anchorMax = new Vector2(0.5f, 0.86f);
            _shopConfirmTitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _shopConfirmTitleText.rectTransform.sizeDelta = new Vector2(760f, 70f);
            _shopConfirmTitleText.alignment = TextAlignmentOptions.Center;
            _shopConfirmTitleText.color = config.ShopPanelTextColor;

            _shopConfirmDescriptionText = CreateText("ShopConfirmDescription", _shopConfirmPanel, new Vector2(0.5f, 0.56f), 28f, config.ShopFontSizeScale);
            _shopConfirmDescriptionText.rectTransform.anchorMin = _shopConfirmDescriptionText.rectTransform.anchorMax = new Vector2(0.5f, 0.56f);
            _shopConfirmDescriptionText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _shopConfirmDescriptionText.rectTransform.sizeDelta = new Vector2(760f, 280f);
            _shopConfirmDescriptionText.alignment = TextAlignmentOptions.TopLeft;
            _shopConfirmDescriptionText.enableWordWrapping = true;
            _shopConfirmDescriptionText.overflowMode = TextOverflowModes.Overflow;
            _shopConfirmDescriptionText.color = config.ShopPanelTextColor;

            _shopConfirmCostText = CreateText("ShopConfirmCost", _shopConfirmPanel, new Vector2(0.5f, 0.24f), 30f, config.ShopFontSizeScale);
            _shopConfirmCostText.rectTransform.anchorMin = _shopConfirmCostText.rectTransform.anchorMax = new Vector2(0.5f, 0.24f);
            _shopConfirmCostText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _shopConfirmCostText.rectTransform.sizeDelta = new Vector2(760f, 60f);
            _shopConfirmCostText.alignment = TextAlignmentOptions.Center;
            _shopConfirmCostText.color = config.ShopPanelTextColor;

            var shopCancelButton = CreateActionButton(_shopConfirmPanel, "취소", new Vector2(0.28f, 0.10f), CloseShopConfirmPanel, false, config.ShopConfirmActionButtonWidth, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(shopCancelButton, config.ShopButtonTextColor);
            _shopPurchaseButton = CreateActionButton(_shopConfirmPanel, "구매", new Vector2(0.72f, 0.10f), ConfirmPendingShopSelection, false, config.ShopConfirmActionButtonWidth, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(_shopPurchaseButton, config.ShopButtonTextColor);
            _shopConfirmPanel.gameObject.SetActive(false);
            SetDimOverlayVisible(_shopConfirmDimRoot, false);
        }

        private RectTransform EnsureShopConfirmDimOverlay()
        {
            if (_shopOverlayRoot == null)
            {
                return null;
            }

            if (_shopConfirmDimRoot != null)
            {
                EnsureDimOverlayVisual(_shopConfirmDimRoot, config.ShopConfirmDimColor);
                return _shopConfirmDimRoot;
            }

            _shopConfirmDimRoot = CreateUiPanel("ShopConfirmDimOverlay", _shopOverlayRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureDimOverlayVisual(_shopConfirmDimRoot, config.ShopConfirmDimColor);
            _shopConfirmDimRoot.gameObject.SetActive(false);
            return _shopConfirmDimRoot;
        }

        private Button CreateShopButton(RectTransform rowRoot, int index, bool isFree)
        {
            var style = isFree ? config.ShopFreeSlotButtonStyle : config.ShopPaidSlotButtonStyle;
            var button = CreateActionButton(rowRoot, isFree ? "Free" : "Paid", new Vector2((index + 0.5f) / 3f, 0.5f), null, true, config.ShopSlotButtonSide, config.ShopSlotButtonSide, config.ShopFontSizeScale, style);
            SetButtonTextColor(button, config.ShopButtonTextColor);
            button.onClick.AddListener(() => OnShopSlotPressed(isFree, index));
            return button;
        }

        private BattleBoardLayoutReference.StartingUniqueLayoutReference.SlotReference GetStartingUniqueSlotReference(int index)
        {
            return index >= 0 && index < _startingUniqueSlotReferences.Count
                ? _startingUniqueSlotReferences[index]
                : null;
        }

        private BattleBoardLayoutReference.ItemSlotReference GetShopSlotReference(bool isFree, int index)
        {
            var references = isFree ? _freeButtonSlotReferences : _paidButtonSlotReferences;
            return index >= 0 && index < references.Count ? references[index] : null;
        }

        private void ApplyItemSlotVisuals(BattleBoardLayoutReference.ItemSlotReference slotReference, ItemData item, string name, string description, string price)
        {
            if (slotReference == null)
            {
                return;
            }

            if (slotReference.NameText != null)
            {
                slotReference.NameText.text = name ?? string.Empty;
            }

            if (slotReference.DescriptionText != null)
            {
                slotReference.DescriptionText.text = description ?? string.Empty;
            }

            if (slotReference.PriceText != null)
            {
                slotReference.PriceText.text = price ?? string.Empty;
            }

            ApplyUniqueItemSlotNumberText(slotReference.UniqueItemPresentationTexts, item);
            ApplyItemCategoryIcon(slotReference, item);
            ApplyItemSlotAura(slotReference, item);
        }

        private void ApplyStartingUniqueSlotVisuals(BattleBoardLayoutReference.StartingUniqueLayoutReference.SlotReference slotReference, ItemData item, string name, string description)
        {
            if (slotReference == null)
            {
                return;
            }

            if (slotReference.NameText != null)
            {
                slotReference.NameText.text = name ?? string.Empty;
            }

            if (slotReference.DescriptionText != null)
            {
                slotReference.DescriptionText.text = description ?? string.Empty;
            }

            ApplyUniqueItemSlotNumberText(slotReference.UniqueItemPresentationTexts, item);
            ApplyStartingUniqueItemCategoryIcon(slotReference, item);
        }

        private string GetStartingUniqueCardDescriptionText(ItemData item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(item.uiDescriptionKo))
            {
                var uniquePresentation = GetUniqueItemPresentation(item);
                if (!string.IsNullOrWhiteSpace(uniquePresentation?.CardSummaryKo))
                {
                    return uniquePresentation.CardSummaryKo;
                }

                return item.uiDescriptionKo;
            }

            var fallbackPresentation = GetUniqueItemPresentation(item);
            if (!string.IsNullOrWhiteSpace(fallbackPresentation?.CardSummaryKo))
            {
                return fallbackPresentation.CardSummaryKo;
            }

            if (fallbackPresentation != null)
            {
                return BuildUniqueItemPresentationSummary(fallbackPresentation);
            }

            return "설명 없음";
        }

        private void ApplyUniqueItemExplainTexts(BattleBoardLayoutReference.UniqueItemExplainTextReference textReference, ItemData item)
        {
            if (textReference == null)
            {
                return;
            }

            var presentation = GetUniqueItemPresentation(item);
            if (textReference.TendencyText != null)
            {
                textReference.TendencyText.text = presentation?.TendencyKo ?? string.Empty;
            }

            if (textReference.ConditionText != null)
            {
                textReference.ConditionText.text = presentation?.ConditionKo ?? string.Empty;
            }

            if (textReference.EffectText != null)
            {
                textReference.EffectText.text = presentation?.EffectKo ?? string.Empty;
            }
        }

        private void ApplyUniqueItemSlotNumberText(BattleBoardLayoutReference.UniqueItemPresentationTextReference textReference, ItemData item)
        {
            if (textReference == null)
            {
                return;
            }

            var presentation = GetUniqueItemPresentation(item);
            if (textReference.NumberText != null)
            {
                textReference.NumberText.text = presentation?.Number ?? string.Empty;
            }
        }

        private UniqueItemPresentationText GetUniqueItemPresentation(ItemData item)
        {
            if (item == null || item.Category != ItemCategory.UniqueItem)
            {
                return null;
            }

            return _uniqueItemPresentationTexts.TryGetValue(item.itemId ?? string.Empty, out var presentation)
                ? presentation
                : null;
        }

        private static string BuildUniqueItemPresentationSummary(UniqueItemPresentationText presentation)
        {
            if (presentation == null)
            {
                return "설명 없음";
            }

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(presentation.TendencyKo))
            {
                lines.Add($"성향: {presentation.TendencyKo}");
            }

            if (!string.IsNullOrWhiteSpace(presentation.ConditionKo))
            {
                lines.Add($"조건: {presentation.ConditionKo}");
            }

            if (!string.IsNullOrWhiteSpace(presentation.EffectKo))
            {
                lines.Add($"효과: {presentation.EffectKo}");
            }

            return lines.Count > 0 ? string.Join("\n", lines) : "설명 없음";
        }

        private void LoadUniqueItemPresentationTexts()
        {
            _uniqueItemPresentationTexts.Clear();

            var csvText = LoadUniqueItemPresentationCsvText();
            if (string.IsNullOrWhiteSpace(csvText))
            {
                return;
            }

            var rows = ParseCsvRows(csvText);
            if (rows.Count <= 1)
            {
                return;
            }

            var headers = rows[0]
                .Select((value, index) => new { value = (value ?? string.Empty).Trim().Trim('\uFEFF'), index })
                .ToDictionary(entry => entry.value, entry => entry.index, StringComparer.OrdinalIgnoreCase);

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0)
                {
                    continue;
                }

                var itemId = GetCsvValue(row, headers, "uniqueItemId");
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                _uniqueItemPresentationTexts[itemId] = new UniqueItemPresentationText
                {
                    Number = GetCsvValue(row, headers, "number"),
                    NameKo = GetCsvValue(row, headers, "nameKo"),
                    CardSummaryKo = GetCsvValue(row, headers, "cardSummaryKo"),
                    TendencyKo = GetCsvValue(row, headers, "tendencyKo"),
                    ConditionKo = GetCsvValue(row, headers, "conditionKo"),
                    EffectKo = GetCsvValue(row, headers, "effectKo"),
                    NoteKo = GetCsvValue(row, headers, "noteKo"),
                };
            }
        }

        private static string LoadUniqueItemPresentationCsvText()
        {
            var textAsset = Resources.Load<TextAsset>("Mathcalibur_UniqueItem_Text");
            if (textAsset != null && !string.IsNullOrWhiteSpace(textAsset.text))
            {
                return textAsset.text;
            }

            var docsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Mathcalibur_UniqueItem_Text.csv"));
            if (File.Exists(docsPath))
            {
                return File.ReadAllText(docsPath);
            }

            var typoDocsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Mathcalibur_UniqyeItem_Text.csv"));
            return File.Exists(typoDocsPath) ? File.ReadAllText(typoDocsPath) : string.Empty;
        }

        private static string GetCsvValue(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headers, string key)
        {
            if (!headers.TryGetValue(key, out var index))
            {
                return string.Empty;
            }

            return index >= 0 && index < row.Count ? row[index]?.Trim() ?? string.Empty : string.Empty;
        }

        private static List<List<string>> ParseCsvRows(string csvText)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentCell = new System.Text.StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < csvText.Length; i++)
            {
                var ch = csvText[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        currentCell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    continue;
                }

                if ((ch == '\n' || ch == '\r') && !inQuotes)
                {
                    if (ch == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    {
                        i++;
                    }

                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    continue;
                }

                currentCell.Append(ch);
            }

            if (currentCell.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentCell.ToString());
                rows.Add(currentRow);
            }

            return rows;
        }

        private void ApplyItemCategoryIcon(BattleBoardLayoutReference.ItemSlotReference slotReference, ItemData item)
        {
            var iconImage = ResolveSlotCategoryIconImage(slotReference);
            if (iconImage == null)
            {
                return;
            }

            var iconSprite = item != null
                ? _boardLayoutReference?.ItemCategoryIcons?.GetIcon(item)
                : null;

            iconImage.sprite = iconSprite;
            iconImage.enabled = iconSprite != null;
            ApplyShopIconScale(iconImage, item);
        }

        private void ApplyStartingUniqueItemCategoryIcon(BattleBoardLayoutReference.StartingUniqueLayoutReference.SlotReference slotReference, ItemData item)
        {
            var iconImage = slotReference?.CategoryIconImage;
            if (iconImage == null)
            {
                return;
            }

            var iconSprite = item != null
                ? _boardLayoutReference?.ItemCategoryIcons?.GetIcon(item)
                : null;

            iconImage.sprite = iconSprite;
            iconImage.enabled = iconSprite != null;
        }

        private void ApplyItemSlotAura(BattleBoardLayoutReference.ItemSlotReference slotReference, ItemData item)
        {
            var auraImage = ResolveSlotAuraImage(slotReference);
            if (auraImage == null)
            {
                return;
            }

            var auraSprite = item != null
                ? _boardLayoutReference?.ItemRarityAuras?.GetSprite(item)
                : null;

            auraImage.sprite = auraSprite;
            auraImage.enabled = auraSprite != null;
            if (auraSprite == null)
            {
                return;
            }

            auraImage.color = item != null && item.Category == ItemCategory.UniqueItem
                ? Color.red
                : Color.white;
            auraImage.preserveAspect = true;
        }

        private static Image ResolveSlotCategoryIconImage(BattleBoardLayoutReference.ItemSlotReference slotReference)
        {
            if (slotReference == null)
            {
                return null;
            }

            if (slotReference.CategoryIconImage != null)
            {
                return slotReference.CategoryIconImage;
            }

            return FindChildImageByName(slotReference.Button != null ? slotReference.Button.transform : null, SlotIconChildName);
        }

        private static Image ResolveSlotAuraImage(BattleBoardLayoutReference.ItemSlotReference slotReference)
        {
            if (slotReference == null)
            {
                return null;
            }

            if (slotReference.AuraImage != null)
            {
                return slotReference.AuraImage;
            }

            var root = slotReference.Button != null ? slotReference.Button.transform : null;
            foreach (var childName in SlotAuraChildNames)
            {
                var image = FindChildImageByName(root, childName);
                if (image != null)
                {
                    return image;
                }
            }

            return null;
        }

        private static Image FindChildImageByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return child.GetComponent<Image>();
            }

            return null;
        }

        private static void BindButton(Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (callback != null)
            {
                button.onClick.AddListener(() =>
                {
                    GameAudioManager.Instance?.PlayButtonClickSfx();
                    callback();
                });
            }
        }

        private static void SetButtonInteractableVisual(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            var image = GetButtonImage(button);
            if (image != null)
            {
                image.color = interactable
                    ? new Color(0.85f, 0.85f, 0.85f, 1f)
                    : new Color(0.35f, 0.35f, 0.35f, 1f);
            }
        }

        private static Image GetButtonImage(Button button)
        {
            if (button == null)
            {
                return null;
            }

            if (button.targetGraphic is Image targetImage)
            {
                return targetImage;
            }

            return button.GetComponent<Image>();
        }

        private Button CreateActionButton(RectTransform parent, string label, Vector2 anchor, Action callback, bool circular, float widthOverride, float heightOverride, float fontScale, BattleConfig.ButtonArtworkStyle? visualStyle = null)
        {
            var go = new GameObject(label + "Button", typeof(Image), typeof(Button), typeof(BattleButtonVisualRefs));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);

            var width = Mathf.Max(40f, widthOverride);
            var height = Mathf.Max(40f, heightOverride);
            rt.sizeDelta = new Vector2(width, height);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            var button = go.GetComponent<Button>();
            if (callback != null)
            {
                button.onClick.AddListener(() =>
                {
                    GameAudioManager.Instance?.PlayButtonClickSfx();
                    callback();
                });
            }

            var refs = go.GetComponent<BattleButtonVisualRefs>();
            refs.BackgroundImage = image;

            var contentImage = new GameObject("ContentImage", typeof(Image)).GetComponent<Image>();
            contentImage.transform.SetParent(rt, false);
            contentImage.rectTransform.anchorMin = contentImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            contentImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            contentImage.rectTransform.sizeDelta = new Vector2(width * 0.8f, height * 0.8f);
            contentImage.raycastTarget = false;
            refs.ContentImage = contentImage;

            var text = CreateText(label + "Label", rt, new Vector2(0.5f, 0.5f), circular ? 34f : 36f, fontScale);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.sizeDelta = new Vector2(width * 0.88f, height * 0.9f);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = Mathf.Max(16f, text.fontSize * 0.55f);
            text.text = label;
            refs.Label = text;

            ApplyButtonArtwork(refs, visualStyle, new Vector2(width, height));
            return button;
        }

        private void ApplyButtonArtwork(BattleButtonVisualRefs refs, BattleConfig.ButtonArtworkStyle? style, Vector2 buttonSize)
        {
            if (refs == null)
            {
                return;
            }

            var backgroundImage = refs.BackgroundImage;
            var contentImage = refs.ContentImage;
            var label = refs.Label;

            if (backgroundImage != null)
            {
                backgroundImage.sprite = style?.BackgroundSprite;
                backgroundImage.color = style?.BackgroundColor ?? new Color(0.85f, 0.85f, 0.85f, 1f);
                backgroundImage.type = backgroundImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                backgroundImage.preserveAspect = false;
            }

            if (contentImage != null)
            {
                var sprite = style?.ContentSprite;
                contentImage.sprite = sprite;
                contentImage.color = style?.ContentColor ?? Color.white;
                contentImage.type = sprite != null ? Image.Type.Simple : Image.Type.Simple;
                contentImage.preserveAspect = sprite != null;
                contentImage.enabled = sprite != null;
                var size = style?.ContentSize ?? Vector2.zero;
                contentImage.rectTransform.sizeDelta = size.x > 0f && size.y > 0f ? size : buttonSize * 0.8f;
            }

            if (label != null)
            {
                label.enabled = !(style?.HideLabelWhenContentSpriteAssigned ?? false) || style?.ContentSprite == null;
            }
        }

        private static BattleButtonVisualRefs GetButtonVisualRefs(Button button)
        {
            return button != null ? button.GetComponent<BattleButtonVisualRefs>() : null;
        }

        private void RollShop(bool resetLockedSlots = true)
        {
            CloseShopConfirmPanel();
            if (resetLockedSlots)
            {
                _freePurchaseDone = false;
                _freeSlots.Clear();
                _paidSlots.Clear();
            }

            RollFreeSlots(!resetLockedSlots);
            RollPaidSlots(!resetLockedSlots);
            RefreshShopUi();
        }

        private void RollFreeSlots(bool preserveLockedSlots)
        {
            var chosenIds = BuildVisibleShopItemIdSet(-1);
            for (var i = 0; i < 3; i++)
            {
                if (preserveLockedSlots && i < _freeSlots.Count && _freeSlots[i].IsLocked)
                {
                    continue;
                }

                var item = PickRandomEligibleItem(ItemSlotKind.Free, chosenIds, null);
                ShopSlotData newSlot;
                if (item == null)
                {
                    newSlot = ShopSlotData.CreateLockedPlaceholder(true, "Locked");
                }
                else
                {
                    chosenIds.Add(item.itemId);
                    newSlot = ShopSlotData.CreateItem(item, 0, true, ItemSlotKind.Free);
                }

                SetShopSlot(_freeSlots, i, newSlot);
            }
        }

        private void RollPaidSlots(bool preserveLockedSlots)
        {
            var chosenIds = BuildVisibleShopItemIdSet(-1);
            var useUniqueSlot = IsUniqueShop();
            for (var i = 0; i < 3; i++)
            {
                if (preserveLockedSlots && i < _paidSlots.Count && _paidSlots[i].IsLocked)
                {
                    continue;
                }

                ShopSlotData newSlot;
                if (useUniqueSlot && i == GetUniqueShopSlotIndex())
                {
                    var uniqueItem = PickRandomEligibleItem(ItemSlotKind.Unique, chosenIds, null);
                    if (uniqueItem == null)
                    {
                        Debug.LogWarning("Unique shop reached, but no eligible UniqueItem exists for the Unique Item Slot.");
                        newSlot = ShopSlotData.CreateLockedPlaceholder(false, "Unique\nLocked");
                        SetShopSlot(_paidSlots, i, newSlot);
                        continue;
                    }

                    chosenIds.Add(uniqueItem.itemId);
                    newSlot = ShopSlotData.CreateItem(uniqueItem, _itemDatabase.ResolvePrice(uniqueItem), false, ItemSlotKind.Unique);
                    SetShopSlot(_paidSlots, i, newSlot);
                    continue;
                }

                var item = PickRandomEligiblePaidItem(chosenIds);
                if (item == null)
                {
                    newSlot = ShopSlotData.CreateLockedPlaceholder(false, "Locked");
                }
                else
                {
                    chosenIds.Add(item.itemId);
                    newSlot = ShopSlotData.CreateItem(item, _itemDatabase.ResolvePrice(item), false, ItemSlotKind.Paid);
                }

                SetShopSlot(_paidSlots, i, newSlot);
            }
        }

        private static void SetShopSlot(List<ShopSlotData> slots, int index, ShopSlotData slot)
        {
            while (slots.Count <= index)
            {
                slots.Add(null);
            }

            slots[index] = slot;
        }

        private bool IsUniqueShop()
        {
            return true;
        }

        private static bool IsEasyDifficulty()
        {
            return GameSessionState.SelectedDifficulty == GameDifficulty.Easy;
        }

        private int GetUniqueShopSlotIndex()
        {
            return _itemDatabase.GetIntConfig("TEMP_UNIQUE_SLOT_ZERO_BASED_INDEX_IN_MIDDLE_ROW");
        }

        private ItemData PickRandomEligibleItem(ItemSlotKind slotKind, HashSet<string> excludedIds, ItemRarity? requiredRarity)
        {
            var upcomingStageNumber = GetUpcomingStageNumber();
            var pool = _itemDatabase.Items.Where(item => item.IsValid)
                .Where(item => !IsRemovedShopPotion(item))
                .Where(item => requiredRarity == null || item.Rarity == requiredRarity.Value)
                .Where(item => !excludedIds.Contains(item.itemId))
                .Where(item => _itemEligibilityChecker.IsEligible(item, slotKind, upcomingStageNumber, _runtimeItemInventory, _itemDatabase, out _))
                .ToList();
            if (pool.Count == 0)
            {
                return null;
            }

            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        private static bool IsRemovedShopPotion(ItemData item)
        {
            return item != null
                && (string.Equals(item.itemId, HealingPotionItemId, StringComparison.Ordinal)
                    || string.Equals(item.itemId, AttackPotionItemId, StringComparison.Ordinal));
        }

        private ItemData PickRandomEligiblePaidItem(HashSet<string> excludedIds)
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var rolledRarity = RollPaidRarity();
                var item = PickRandomEligibleItem(ItemSlotKind.Paid, excludedIds, rolledRarity);
                if (item != null)
                {
                    return item;
                }
            }

            return PickRandomEligibleItem(ItemSlotKind.Paid, excludedIds, null);
        }

        private ItemRarity RollPaidRarity()
        {
            var common = Mathf.Max(0, _itemDatabase.GetPaidRarityWeight(ItemRarity.Common));
            var rare = Mathf.Max(0, _itemDatabase.GetPaidRarityWeight(ItemRarity.Rare));
            var legendary = Mathf.Max(0, _itemDatabase.GetPaidRarityWeight(ItemRarity.Legendary));
            var total = common + rare + legendary;
            if (total <= 0)
            {
                return ItemRarity.Common;
            }

            var roll = UnityEngine.Random.Range(1, total + 1);
            if (roll <= common)
            {
                return ItemRarity.Common;
            }

            if (roll <= common + rare)
            {
                return ItemRarity.Rare;
            }

            return ItemRarity.Legendary;
        }

        private int GetUpcomingStageNumber()
        {
            return Mathf.Min(MaxStage, _playerState.CurrentStage + 1);
        }

        private void OnShopSlotPressed(bool isFree, int index)
        {
            var slots = isFree ? _freeSlots : _paidSlots;
            if (index < 0 || index >= slots.Count)
            {
                return;
            }

            var slot = slots[index];
            if (slot.IsLocked || slot.Item == null)
            {
                return;
            }

            if (!_itemEligibilityChecker.IsEligible(slot.Item, slot.SlotKind, GetUpcomingStageNumber(), _runtimeItemInventory, _itemDatabase, out _))
            {
                slot.IsLocked = true;
                slot.OverrideLabel = "Locked";
                RefreshShopUi();
                return;
            }

            OpenShopConfirmPanel(new ShopSelectionContext(isFree, index));
        }

        private void OpenShopConfirmPanel(ShopSelectionContext selection)
        {
            var slot = GetShopSlot(selection.IsFree, selection.Index);
            if (slot?.Item == null || _shopConfirmPanel == null)
            {
                return;
            }

            _pendingShopSelection = selection;
            var item = slot.Item;
            var canAfford = slot.IsFree || _playerState.Gold >= slot.Cost;
            var description = string.IsNullOrWhiteSpace(item.uiDescriptionKo) ? "설명 없음" : item.uiDescriptionKo;
            var costText = slot.IsFree
                ? "무료"
                : $"{slot.Cost}G";
            if (_shopConfirmTitleText != null)
            {
                _shopConfirmTitleText.text = item.displayName;
            }

            if (_shopConfirmDescriptionText != null)
            {
                _shopConfirmDescriptionText.text = description;
            }

            if (_shopConfirmCostText != null)
            {
                _shopConfirmCostText.text = costText;
            }

            if (_shopPurchaseButton != null)
            {
                _shopPurchaseButton.interactable = canAfford;
                SetButtonInteractableVisual(_shopPurchaseButton, canAfford);
            }

            RefreshShopConfirmPreview(selection);
            SetDimOverlayVisible(_shopConfirmDimRoot, true);
            _shopConfirmPanel.gameObject.SetActive(true);
            _shopConfirmPanel.SetAsLastSibling();
        }

        private void CloseShopConfirmPanel()
        {
            _pendingShopSelection = null;
            ClearShopConfirmPreview();
            SetDimOverlayVisible(_shopConfirmDimRoot, false);
            if (_shopConfirmPanel != null)
            {
                _shopConfirmPanel.gameObject.SetActive(false);
            }
        }

        private void ConfirmPendingShopSelection()
        {
            if (_pendingShopSelection == null)
            {
                return;
            }

            var selection = _pendingShopSelection.Value;
            var slot = GetShopSlot(selection.IsFree, selection.Index);
            if (slot?.Item == null || slot.IsLocked)
            {
                CloseShopConfirmPanel();
                RefreshShopUi();
                return;
            }

            if (!_itemEligibilityChecker.IsEligible(slot.Item, slot.SlotKind, GetUpcomingStageNumber(), _runtimeItemInventory, _itemDatabase, out _))
            {
                slot.IsLocked = true;
                slot.OverrideLabel = "Locked";
                CloseShopConfirmPanel();
                RefreshShopUi();
                return;
            }

            if (!selection.IsFree && _playerState.Gold < slot.Cost)
            {
                CloseShopConfirmPanel();
                RefreshShopUi();
                return;
            }

            var purchasedItemId = slot.Item.itemId;
            if (!selection.IsFree)
            {
                _playerState.Gold -= slot.Cost;
            }

            _itemEffectResolver.ApplyAcquiredItem(slot.Item, _runtimeItemInventory, _itemDatabase, this);
            RegisterUniqueInventoryHudItem(slot.Item);
            RefreshBoardTileSpriteVisuals();
            if (selection.IsFree)
            {
                _freePurchaseDone = true;
                slot.IsLocked = true;
                slot.OverrideLabel = "Selected";
                LockRemainingFreeShopSlots(selection.Index);
            }
            else
            {
                slot.IsLocked = true;
                slot.OverrideLabel = "Purchased";
            }

            ReevaluateVisibleDuplicateEligibility(purchasedItemId);
            CloseShopConfirmPanel();
            RefreshShopUi();
            RefreshHud(string.Empty, "-");
        }

        private void LockRemainingFreeShopSlots(int selectedIndex)
        {
            for (var i = 0; i < _freeSlots.Count; i++)
            {
                if (i == selectedIndex)
                {
                    continue;
                }

                var freeSlot = _freeSlots[i];
                if (freeSlot == null)
                {
                    continue;
                }

                freeSlot.IsLocked = true;
                freeSlot.OverrideLabel = "Locked";
            }
        }

        private HashSet<string> BuildVisibleShopItemIdSet(int excludedPaidSlotIndex)
        {
            var excludedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in _freeSlots)
            {
                if (!string.IsNullOrWhiteSpace(slot?.Item?.itemId))
                {
                    excludedIds.Add(slot.Item.itemId);
                }
            }

            for (var i = 0; i < _paidSlots.Count; i++)
            {
                if (i == excludedPaidSlotIndex)
                {
                    continue;
                }

                var itemId = _paidSlots[i]?.Item?.itemId;
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    excludedIds.Add(itemId);
                }
            }

            return excludedIds;
        }

        private void ReevaluateVisibleDuplicateEligibility(string itemId)
        {
            foreach (var slot in _freeSlots.Concat(_paidSlots))
            {
                if (slot.Item == null || slot.Item.itemId != itemId || slot.OverrideLabel == "Selected" || slot.OverrideLabel == "Purchased")
                {
                    continue;
                }

                if (_itemEligibilityChecker.IsEligible(slot.Item, slot.SlotKind, GetUpcomingStageNumber(), _runtimeItemInventory, _itemDatabase, out _))
                {
                    continue;
                }

                slot.IsLocked = true;
                slot.OverrideLabel = "Locked";
            }
        }

        private void RefreshShopUi()
        {
            if (_shopGoldText != null)
            {
                _shopGoldText.text = $"Gold: {_playerState.Gold}";
            }

            for (var i = 0; i < _freeButtons.Count; i++)
            {
                if (_freeButtons[i] == null || i >= _freeSlots.Count)
                {
                    continue;
                }

                BindSlotButton(_freeButtons[i], _freeSlots[i], _freeSlots[i].IsLocked, GetShopSlotReference(true, i));
            }

            for (var i = 0; i < _paidButtons.Count; i++)
            {
                if (_paidButtons[i] == null || i >= _paidSlots.Count)
                {
                    continue;
                }

                BindSlotButton(_paidButtons[i], _paidSlots[i], _paidSlots[i].IsLocked, GetShopSlotReference(false, i));
            }

            var rerollCost = GetCurrentRerollCost();
            if (_rerollButton != null)
            {
                var rerollText = _rerollText != null ? _rerollText : _rerollButton.GetComponentInChildren<TextMeshProUGUI>();
                if (rerollText != null)
                {
                    rerollText.text = $"{rerollCost}G";
                }

                _rerollButton.interactable = _playerState.Gold >= rerollCost;
                SetButtonInteractableVisual(_rerollButton, _rerollButton.interactable);
            }

            var requireFree = _itemDatabase.GetBoolConfig("TEMP_REQUIRE_FREE_ITEM_BEFORE_NEXT", true);
            if (_nextStageButton != null)
            {
                _nextStageButton.interactable = !requireFree || _freePurchaseDone;
                SetButtonInteractableVisual(_nextStageButton, _nextStageButton.interactable);
            }
        }

        private void RefreshShopConfirmPreview(ShopSelectionContext selection)
        {
            if (_shopConfirmPreviewRoot == null)
            {
                return;
            }

            ClearShopConfirmPreview();

            var slotReference = GetShopSlotReference(selection.IsFree, selection.Index);
            var sourceButton = slotReference?.Button;
            if (sourceButton == null)
            {
                return;
            }

            _shopConfirmPreviewInstance = Instantiate(sourceButton.gameObject, _shopConfirmPreviewRoot);
            _shopConfirmPreviewInstance.name = sourceButton.gameObject.name + "_Preview";

            var previewRect = _shopConfirmPreviewInstance.GetComponent<RectTransform>();
            if (previewRect != null)
            {
                previewRect.anchorMin = Vector2.zero;
                previewRect.anchorMax = Vector2.one;
                previewRect.pivot = new Vector2(0.5f, 0.5f);
                previewRect.anchoredPosition = Vector2.zero;
                previewRect.sizeDelta = Vector2.zero;
                previewRect.localScale = Vector3.one;
                previewRect.localRotation = Quaternion.identity;
            }

            foreach (var button in _shopConfirmPreviewInstance.GetComponentsInChildren<Button>(true))
            {
                button.interactable = false;
                button.enabled = false;
            }

            foreach (var graphic in _shopConfirmPreviewInstance.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        private void ClearShopConfirmPreview()
        {
            if (_shopConfirmPreviewInstance == null)
            {
                return;
            }

            Destroy(_shopConfirmPreviewInstance);
            _shopConfirmPreviewInstance = null;
        }

        private ShopSlotData GetShopSlot(bool isFree, int index)
        {
            var slots = isFree ? _freeSlots : _paidSlots;
            if (index < 0 || index >= slots.Count)
            {
                return null;
            }

            return slots[index];
        }

        private int GetCurrentRerollCost()
        {
            return _itemDatabase.GetIntConfig("TEMP_BASE_REROLL_COST") + _playerState.RerollUsedCountThisRun * _itemDatabase.GetIntConfig("TEMP_REROLL_COST_INCREASE");
        }

        private string GetShopSlotDescriptionText(ItemData item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            if (item.Category == ItemCategory.UniqueItem)
            {
                var presentation = GetUniqueItemPresentation(item);
                if (!string.IsNullOrWhiteSpace(presentation?.EffectKo))
                {
                    return presentation.EffectKo;
                }
            }

            return string.IsNullOrWhiteSpace(item.uiDescriptionKo) ? "설명 없음" : item.uiDescriptionKo;
        }

        private void BindSlotButton(Button button, ShopSlotData slot, bool forceLocked, BattleBoardLayoutReference.ItemSlotReference slotReference = null)
        {
            var label = slot.OverrideLabel;
            var itemName = slot.Item?.displayName ?? "Locked";
            var description = GetShopSlotDescriptionText(slot.Item);
            var price = slot.Item == null
                ? string.Empty
                : slot.IsFree
                    ? "무료"
                    : $"{slot.Cost}G";

            if (string.IsNullOrEmpty(label))
            {
                if (slot.Item == null)
                {
                    label = "Locked";
                }
                else
                {
                    label = slot.Item.displayName;
                }
            }

            ApplyItemSlotVisuals(slotReference, slot.Item, itemName, description, price);
            var text = GetButtonVisualRefs(button)?.Label ?? button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
            }

            var interactable = !forceLocked && slot.Item != null;
            button.interactable = interactable;
            var visualRefs = GetButtonVisualRefs(button);
            var baseColor = (slot.IsFree ? config.ShopFreeSlotButtonStyle : config.ShopPaidSlotButtonStyle).BackgroundColor;
            var enabledColor = baseColor.a > 0f ? baseColor : new Color(0.85f, 0.85f, 0.85f, 1f);
            var disabledColor = new Color(enabledColor.r * 0.45f, enabledColor.g * 0.45f, enabledColor.b * 0.45f, enabledColor.a <= 0f ? 1f : enabledColor.a);
            if (visualRefs?.BackgroundImage != null)
            {
                visualRefs.BackgroundImage.color = interactable ? enabledColor : disabledColor;
            }
            else
            {
                SetButtonInteractableVisual(button, interactable);
            }
        }

        private void OnRerollPressed()
        {
            var cost = GetCurrentRerollCost();
            if (_playerState.Gold < cost)
            {
                return;
            }

            _playerState.Gold -= cost;
            _playerState.RerollUsedCountThisRun++;
            RollShop(false);
            RefreshHud(string.Empty, "-");
        }

        private void OnNextStagePressed()
        {
            var requireFree = _itemDatabase.GetBoolConfig("TEMP_REQUIRE_FREE_ITEM_BEFORE_NEXT", true);
            if (requireFree && !_freePurchaseDone)
            {
                return;
            }

            CloseShopConfirmPanel();
            _shopOpen = false;
            SetDimOverlayVisible(_shopDimRoot, false);
            if (_shopOverlayRoot != null)
            {
                _shopOverlayRoot.gameObject.SetActive(false);
            }
            RestoreShopPanelParent();
            _playerState.CurrentStage++;
            ResetStageLocalBattleState();
            InitBattle();
        }

        private void TryUseActiveItemNow(string itemId)
        {
            if (_itemEffectResolver.TryUseActiveItem(itemId, _runtimeItemInventory, _itemDatabase, this, out var message))
            {
                RefreshHud(string.Empty, "-");
                _hud.SetMessage(message);
            }
            else if (!string.IsNullOrEmpty(message))
            {
                _hud.SetMessage(message);
            }

            RefreshBagUi();
        }

        private void OnBagItemSlotPressed(int slotIndex)
        {
            if (_isResolvingTurn)
            {
                return;
            }

            var orderedActiveItems = _runtimeItemInventory.ActiveItemAcquisitionOrder
                .Where(itemId => _runtimeItemInventory.GetActiveItemCount(itemId) > 0)
                .Select(itemId => _itemDatabase.TryGetItem(itemId, out var item) ? item : null)
                .Where(item => item != null)
                .Take(_bagItemSlotReferences.Count)
                .ToList();

            if (slotIndex < 0 || slotIndex >= orderedActiveItems.Count)
            {
                return;
            }

            var item = orderedActiveItems[slotIndex];
            if (item == null)
            {
                return;
            }

            if (_dragging || _selection.Count > 0)
            {
                _dragging = false;
                ClearSelectionVisual();
            }

            CloseBagPanel();

            if (item.EffectType == ItemEffectType.HealPlayer && _playerHp >= _currentPlayerMaxHp)
            {
                OpenActiveItemConfirmPanel(item);
                return;
            }

            TryUseActiveItemNow(item.itemId);
        }

        private void RefreshBagUi()
        {
            if (_bagItemSlotReferences.Count == 0)
            {
                return;
            }

            var orderedActiveItems = _runtimeItemInventory.ActiveItemAcquisitionOrder
                .Where(itemId => _runtimeItemInventory.GetActiveItemCount(itemId) > 0)
                .Select(itemId => _itemDatabase.TryGetItem(itemId, out var item) ? item : null)
                .Where(item => item != null)
                .Take(_bagItemSlotReferences.Count)
                .ToList();

            for (var i = 0; i < _bagItemSlotReferences.Count; i++)
            {
                var slot = _bagItemSlotReferences[i];
                if (slot == null)
                {
                    continue;
                }

                var item = i < orderedActiveItems.Count ? orderedActiveItems[i] : null;
                var count = item != null ? _runtimeItemInventory.GetActiveItemCount(item.itemId) : 0;
                var iconSprite = item != null ? _boardLayoutReference?.BagItemIcons?.GetIcon(item) : null;

                if (slot.ItemImage != null)
                {
                    slot.ItemImage.sprite = iconSprite;
                    slot.ItemImage.enabled = iconSprite != null;
                }

                if (slot.CountText != null)
                {
                    slot.CountText.text = count > 0 ? $"x {count}" : string.Empty;
                }

                if (slot.Button != null)
                {
                    slot.Button.interactable = count > 0;
                }
            }
        }

        private void ApplyShopIconScale(Image iconImage, ItemData item)
        {
            if (iconImage == null)
            {
                return;
            }

            if (!_slotIconBaseScales.TryGetValue(iconImage, out var baseScale))
            {
                baseScale = iconImage.rectTransform.localScale;
                _slotIconBaseScales[iconImage] = baseScale;
            }

            var scaleMultiplier = 1f;
            var scaleMultiplierVector = Vector3.one;
            if (item != null && item.Category == ItemCategory.ActiveItem)
            {
                var configuredScale = _boardLayoutReference != null
                    ? _boardLayoutReference.ShopActiveItemIconScale
                    : new Vector2(0.8f, 0.8f);
                scaleMultiplierVector = new Vector3(configuredScale.x, configuredScale.y, 1f);
            }

            if (item == null || item.Category != ItemCategory.ActiveItem)
            {
                scaleMultiplierVector = Vector3.one;
            }

            iconImage.rectTransform.localScale = new Vector3(
                baseScale.x * scaleMultiplierVector.x,
                baseScale.y * scaleMultiplierVector.y,
                baseScale.z * scaleMultiplierVector.z);
        }

        private void RefreshPercentageUi()
        {
            var percentageLayout = _boardLayoutReference?.PercentageLayout;
            if (percentageLayout == null)
            {
                return;
            }

            var totalNumberWeight = Mathf.Max(1, _cachedNumberWeights.Values.Where(weight => weight > 0).Sum());
            var totalOperatorWeight = Mathf.Max(1, _cachedOperatorWeights.Values.Where(weight => weight > 0).Sum());

            var numberBars = percentageLayout.NumberBars ?? Array.Empty<BattleBoardLayoutReference.WeightBarReference>();
            for (var i = 0; i < numberBars.Length; i++)
            {
                var barReference = numberBars[i];
                var rect = barReference?.ImageRect;
                var weight = _cachedNumberWeights.TryGetValue(i + 1, out var numberWeight) ? numberWeight : 0;
                ApplyPercentageBarSize(rect, weight, totalNumberWeight, true);
                ApplyPercentageValueText(barReference?.PercentageText, weight, totalNumberWeight);
            }

            ApplyPercentageBar(percentageLayout.AddBar, GetOperatorWeight("+"), totalOperatorWeight, false);
            ApplyPercentageBar(percentageLayout.SubtractBar, GetOperatorWeight("-"), totalOperatorWeight, false);
            ApplyPercentageBar(percentageLayout.MultiplyBar, GetOperatorWeight("x"), totalOperatorWeight, false);
            ApplyPercentageBar(percentageLayout.DivideBar, GetOperatorWeight("÷"), totalOperatorWeight, false);
        }

        private int GetOperatorWeight(string symbol)
        {
            return _cachedOperatorWeights.TryGetValue(symbol, out var weight) ? weight : 0;
        }

        private void ApplyPercentageBarSize(RectTransform rect, int currentWeight, int maxWeight, bool vertical)
        {
            if (rect == null || !_percentageBarBaseSizes.TryGetValue(rect, out var baseSize))
            {
                return;
            }

            var normalized = maxWeight <= 0 ? 0f : Mathf.Clamp01(currentWeight / (float)maxWeight);
            rect.sizeDelta = vertical
                ? new Vector2(baseSize.x, baseSize.y * normalized)
                : new Vector2(baseSize.x * normalized, baseSize.y);
        }

        private void ApplyPercentageBar(BattleBoardLayoutReference.WeightBarReference barReference, int currentWeight, int maxWeight, bool vertical)
        {
            if (barReference == null)
            {
                return;
            }

            ApplyPercentageBarSize(barReference.ImageRect, currentWeight, maxWeight, vertical);
            ApplyPercentageValueText(barReference.PercentageText, currentWeight, maxWeight);
        }

        private static void ApplyPercentageValueText(TMP_Text text, int currentWeight, int maxWeight)
        {
            if (text == null)
            {
                return;
            }

            var percent = maxWeight <= 0 ? 0 : Mathf.RoundToInt(currentWeight * 100f / maxWeight);
            text.text = $"{percent}%";
        }

        private StageDefinition GetStageDefinition(int stage)
        {
            if (stage >= MaxStage)
            {
                return StageDatabase.GetFinalBossStage(
                    config.DemonKingBaseHp,
                    config.EnemyAttackDamage,
                    config.EnemyAttackEveryValidTurns);
            }

            EnsureEnemyOrderForRun();
            var stageIndex = Mathf.Clamp(stage - 1, 0, _stageEnemyOrder.Length - 1);
            return StageDatabase.GetStage(stage, _stageEnemyOrder[stageIndex], GetBoardDeckUpgradeCount(), config);
        }

        private void EnsureEnemyOrderForRun()
        {
            if (_stageEnemyOrder != null && _stageEnemyOrder.Length == MaxStage - 1)
            {
                EnsureOpeningEnemyOrder();
                return;
            }

            _stageEnemyOrder = new EnemyType[MaxStage - 1];

            var firstEnemy = UnityEngine.Random.Range(0, 2) == 0 ? EnemyType.Wolf : EnemyType.Orc;
            _stageEnemyOrder[0] = firstEnemy;
            EnsureOpeningEnemyOrder();

            for (var blockStart = 3; blockStart < _stageEnemyOrder.Length; blockStart += 3)
            {
                var block = new[] { EnemyType.Wolf, EnemyType.Orc, EnemyType.StoneGolem };
                ShuffleEnemyTypes(block);
                var copyCount = Mathf.Min(block.Length, _stageEnemyOrder.Length - blockStart);
                Array.Copy(block, 0, _stageEnemyOrder, blockStart, copyCount);
            }
        }

        private void EnsureOpeningEnemyOrder()
        {
            if (_stageEnemyOrder == null || _stageEnemyOrder.Length < 3)
            {
                return;
            }

            var firstEnemy = _stageEnemyOrder[0] is EnemyType.Wolf or EnemyType.Orc
                ? _stageEnemyOrder[0]
                : UnityEngine.Random.Range(0, 2) == 0 ? EnemyType.Wolf : EnemyType.Orc;

            _stageEnemyOrder[0] = firstEnemy;
            _stageEnemyOrder[1] = firstEnemy == EnemyType.Wolf ? EnemyType.Orc : EnemyType.Wolf;
            _stageEnemyOrder[2] = EnemyType.StoneGolem;
        }

        private static void ShuffleEnemyTypes(EnemyType[] enemyTypes)
        {
            for (var i = enemyTypes.Length - 1; i > 0; i--)
            {
                var swapIndex = UnityEngine.Random.Range(0, i + 1);
                (enemyTypes[i], enemyTypes[swapIndex]) = (enemyTypes[swapIndex], enemyTypes[i]);
            }
        }

        private int GetBoardDeckUpgradeCount()
        {
            if (_runtimeItemInventory == null || _itemDatabase == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var acquisition in _runtimeItemInventory.AcquisitionCounts)
            {
                if (acquisition.Value <= 0
                    || !_itemDatabase.TryGetItem(acquisition.Key, out var item)
                    || item == null
                    || !item.IsValid
                    || item.Category != ItemCategory.BoardDeckUpgrade)
                {
                    continue;
                }

                count += acquisition.Value;
            }

            return count;
        }

        private void RebuildCachedSpawnWeightsInternal()
        {
            _cachedNumberWeights.Clear();
            foreach (var entry in config.NumberWeights)
            {
                var modifier = _numberWeightModifiers.TryGetValue(entry.Value, out var delta) ? delta : 0;
                _cachedNumberWeights[entry.Value] = Mathf.Max(0, entry.Weight + modifier);
            }

            _cachedOperatorWeights.Clear();
            foreach (var entry in config.OperatorWeights)
            {
                var symbol = entry.Value switch
                {
                    OperatorType.Add => "+",
                    OperatorType.Subtract => "-",
                    OperatorType.Multiply => "x",
                    OperatorType.Divide => "÷",
                    _ => string.Empty,
                };

                var modifier = _operatorWeightModifiers.TryGetValue(symbol, out var delta) ? delta : 0;
                var adjustedBaseWeight = Mathf.Max(0, entry.Weight + GetOperatorTypeWeightBias(entry.Value));
                var finalWeight = Mathf.Max(0, adjustedBaseWeight + modifier);
                if (_playerState == null || _playerState.CurrentStage < 3)
                {
                    if (symbol is "x" or "÷")
                    {
                        finalWeight = 0;
                    }
                }

                _cachedOperatorWeights[symbol] = finalWeight;
            }

            RefreshPercentageUi();
        }

        private static int GetOperatorTypeWeightBias(OperatorType operatorType)
        {
            return operatorType switch
            {
                OperatorType.Add => OperatorWeightBiasAmount,
                OperatorType.Subtract => -OperatorWeightBiasAmount,
                OperatorType.Multiply => OperatorWeightBiasAmount,
                OperatorType.Divide => -OperatorWeightBiasAmount,
                _ => 0,
            };
        }

        public bool CanUseActiveItem(ItemData item, out string reason)
        {
            if (_shopOpen || _startingUniqueSelectionOpen || _activeItemConfirmOpen || _defeatOverlayOpen)
            {
                reason = "Items cannot be used in the shop.";
                return false;
            }

            if (_dragging || _selection.Count > 0)
            {
                reason = "Items can only be used while input is idle.";
                return false;
            }

            if (_playerHp <= 0 || _enemyHp <= 0)
            {
                reason = "Items cannot be used right now.";
                return false;
            }

            if (item.EffectType == ItemEffectType.SetNextAttackMultiplier && _runtimeItemInventory.HasPendingAttackMultiplier())
            {
                reason = "Attack Potion is already armed.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void AddSpawnWeightModifier(SpawnWeightModifier modifier, int deltaValue)
        {
            if (modifier.targetType == SpawnTargetType.Number)
            {
                if (!int.TryParse(modifier.targetValue, out var number))
                {
                    return;
                }

                _numberWeightModifiers[number] = (_numberWeightModifiers.TryGetValue(number, out var current) ? current : 0) + deltaValue;
                return;
            }

            _operatorWeightModifiers[modifier.targetValue] = (_operatorWeightModifiers.TryGetValue(modifier.targetValue, out var currentOperatorDelta) ? currentOperatorDelta : 0) + deltaValue;
        }

        public void RebuildCachedSpawnWeights()
        {
            RebuildCachedSpawnWeightsInternal();
        }

        public void IncreaseConnectionLimit(int amount)
        {
            _currentMaxConnectionLength += amount;
        }

        public void IncreasePlayerMaxHpAndCurrentHp(int amount)
        {
            _currentPlayerMaxHp += amount;
            _playerHp = Mathf.Min(_currentPlayerMaxHp, _playerHp + amount);
        }

        public void HealPlayer(int amount)
        {
            _playerHp = Mathf.Min(_currentPlayerMaxHp, _playerHp + amount);
        }

        private void RestorePlayerHpToFull()
        {
            _playerHp = Mathf.Max(0, _currentPlayerMaxHp);
        }

        private static RectTransform CreateUiPanel(string name, RectTransform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static RectTransform CreateCenteredSquarePanel(string name, RectTransform parent, float side)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(side, side);
            return rt;
        }

        private TMP_Text CreateText(string name, RectTransform parent, Vector2 anchorPos, float fontSize, float fontScale)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            var text = go.GetComponent<TextMeshProUGUI>();
            var rt = text.rectTransform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(anchorPos.x, anchorPos.y);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(900, 120);
            ApplyUiFont(text);
            text.fontSize = ScaleFont(fontSize, fontScale);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.text = name;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private void ResolveUiFont()
        {
            _resolvedUiFont = config.UiFont;
            if (_resolvedUiFont != null)
            {
                return;
            }

            var osFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial Unicode MS" }, 90);
            if (osFont != null)
            {
                _resolvedUiFont = TMP_FontAsset.CreateFontAsset(osFont);
            }
        }

        private void ApplyUiFont(TMP_Text text)
        {
            if (_resolvedUiFont != null)
            {
                text.font = _resolvedUiFont;
            }
        }

        private float ScaleFont(float fontSize, float fontScale)
        {
            return fontSize * Mathf.Max(0.5f, fontScale);
        }

        private static void SetButtonTextColor(Button button, Color color)
        {
            if (button == null)
            {
                return;
            }

            var text = GetButtonVisualRefs(button)?.Label ?? button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = color;
            }
        }

        private static void ApplyButtonVisual(Button button, Sprite sprite, Color color)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = false;
        }

        private static void ApplyPanelVisual(Image image, Sprite sprite, Color color)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = false;
        }

        private sealed class RuntimePlayerState
        {
            public int CurrentStage = 1;
            public int Gold;
            public int RerollUsedCountThisRun;
        }

        private sealed class RuntimeStageSnapshot
        {
            public int CurrentStage;
            public int Gold;
            public int RerollUsedCountThisRun;
            public int PlayerHp;
            public int CurrentPlayerMaxHp;
            public int CurrentMaxConnectionLength;
            public Dictionary<int, int> NumberWeightModifiers = new();
            public Dictionary<string, int> OperatorWeightModifiers = new(StringComparer.Ordinal);
            public RuntimeItemInventory.Snapshot InventorySnapshot;
            public List<string> UniqueHudItemIds = new();
        }

        private sealed class ShopSlotData
        {
            public ItemData Item;
            public int Cost;
            public bool IsFree;
            public bool IsLocked;
            public string OverrideLabel;
            public ItemSlotKind SlotKind;

            public static ShopSlotData CreateItem(ItemData item, int cost, bool isFree, ItemSlotKind slotKind)
            {
                return new ShopSlotData { Item = item, Cost = cost, IsFree = isFree, SlotKind = slotKind };
            }

            public static ShopSlotData CreateLockedPlaceholder(bool isFree, string label)
            {
                return new ShopSlotData { Item = null, Cost = 0, IsFree = isFree, IsLocked = true, OverrideLabel = label, SlotKind = isFree ? ItemSlotKind.Free : ItemSlotKind.Paid };
            }
        }

        private readonly struct SelectionContext
        {
            public SelectionContext(List<int> finalNumbers, List<int> calculationNumbers, List<OperatorType> operators, int expressionLength)
            {
                FinalNumbers = finalNumbers;
                CalculationNumbers = calculationNumbers;
                Operators = operators;
                ExpressionLength = expressionLength;
            }

            public List<int> FinalNumbers { get; }
            public List<int> CalculationNumbers { get; }
            public List<OperatorType> Operators { get; }
            public int ExpressionLength { get; }
        }

        private readonly struct UniqueOutcome
        {
            public UniqueOutcome(int bonusDamage, int shieldBonus, string message)
            {
                BonusDamage = bonusDamage;
                ShieldBonus = shieldBonus;
                Message = message;
            }

            public int BonusDamage { get; }
            public int ShieldBonus { get; }
            public string Message { get; }
        }

        private readonly struct ShopSelectionContext
        {
            public ShopSelectionContext(bool isFree, int index)
            {
                IsFree = isFree;
                Index = index;
            }

            public bool IsFree { get; }
            public int Index { get; }
        }

        private readonly struct StageDefinition
        {
            public StageDefinition(
                EnemyType enemyType,
                string enemyName,
                int enemyHp,
                int enemyAttackDamage,
                int enemyAttackCycle)
            {
                EnemyType = enemyType;
                EnemyName = enemyName;
                EnemyHp = enemyHp;
                EnemyAttackDamage = enemyAttackDamage;
                EnemyAttackCycle = Mathf.Max(1, enemyAttackCycle);
            }

            public EnemyType EnemyType { get; }
            public string EnemyName { get; }
            public int EnemyHp { get; }
            public int EnemyAttackDamage { get; }
            public int EnemyAttackCycle { get; }
        }

        private readonly struct EnemyDefinition
        {
            public EnemyDefinition(
                EnemyType enemyType,
                string displayName,
                int baseHp,
                int baseAttackDamage,
                int attackCycle)
            {
                EnemyType = enemyType;
                DisplayName = displayName;
                BaseHp = baseHp;
                BaseAttackDamage = baseAttackDamage;
                AttackCycle = attackCycle;
            }

            public EnemyType EnemyType { get; }
            public string DisplayName { get; }
            public int BaseHp { get; }
            public int BaseAttackDamage { get; }
            public int AttackCycle { get; }
        }

        private static class StageDatabase
        {
            public static StageDefinition GetStage(int stage, EnemyType enemyType, int boardDeckUpgradeCount, BattleConfig config)
            {
                var enemy = GetEnemyDefinition(enemyType, config);
                var statMultiplier = stage switch
                {
                    <= 3 => 1f,
                    <= 6 => 1.5f,
                    _ => boardDeckUpgradeCount >= 6 ? 2.5f : 2f,
                };

                return new StageDefinition(
                    enemy.EnemyType,
                    enemy.DisplayName,
                    Mathf.RoundToInt(enemy.BaseHp * statMultiplier),
                    Mathf.RoundToInt(enemy.BaseAttackDamage * statMultiplier),
                    enemy.AttackCycle);
            }

            public static StageDefinition GetFinalBossStage(int placeholderBaseHp, int placeholderAttackDamage, int placeholderAttackCycle)
            {
                return new StageDefinition(
                    EnemyType.DemonKing,
                    "Demon King",
                    placeholderBaseHp,
                    placeholderAttackDamage,
                    placeholderAttackCycle);
            }

            private static EnemyDefinition GetEnemyDefinition(EnemyType enemyType, BattleConfig config)
            {
                var wolfHp = config != null ? config.WolfBaseHp : 40;
                var orcHp = config != null ? config.OrcBaseHp : 50;
                var stoneGolemHp = config != null ? config.StoneGolemBaseHp : 120;
                return enemyType switch
                {
                    EnemyType.Wolf => new EnemyDefinition(EnemyType.Wolf, "울프", wolfHp, 10, 2),
                    EnemyType.Orc => new EnemyDefinition(EnemyType.Orc, "오크", orcHp, 10, 3),
                    EnemyType.StoneGolem => new EnemyDefinition(EnemyType.StoneGolem, "스톤골렘", stoneGolemHp, 25, 5),
                    _ => new EnemyDefinition(EnemyType.Wolf, "울프", wolfHp, 10, 2),
                };
            }

        }
    }
}
