using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mathcalibur.Audio;
using Mathcalibur.Items;
using Mathcalibur.Title;
using Mathcalibur.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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

        [SerializeField] private BattleConfig config;
        [SerializeField] private BattleAnimationManager battleAnimationManager;
        [Header("Enemy Visuals")]
        [SerializeField] private EnemyVisualEntry[] enemyVisualEntries = Array.Empty<EnemyVisualEntry>();
        [Header("Transition")]
        [SerializeField] private float fadeOutDuration = 0.75f;
        [SerializeField] private float fadeInDuration = 0.75f;
        [SerializeField] private float musicFadeOutDuration = 0.75f;
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
        private RectTransform _shopConfirmPanel;
        private RectTransform _startUniqueOverlayRoot;
        private RectTransform _startUniquePanel;
        private RectTransform _activeItemConfirmOverlayRoot;
        private RectTransform _activeItemConfirmPanel;
        private RectTransform _defeatOverlayRoot;
        private RectTransform _defeatPanel;
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
        private Button _attackModeButton;
        private Button _defenseModeButton;
        private Button _killEnemyButton;
        private Button _bagButton;
        private RectTransform _bagPanelRoot;
        private RectTransform _bagDimRoot;
        private Transform _bagPanelOriginalParent;
        private int _bagPanelOriginalSiblingIndex;
        private readonly List<BattleBoardLayoutReference.BagItemSlotReference> _bagItemSlotReferences = new();
        private Button _percentageButton;
        private RectTransform _percentagePanelRoot;
        private RectTransform _percentageDimRoot;
        private Transform _percentagePanelOriginalParent;
        private int _percentagePanelOriginalSiblingIndex;
        private readonly Dictionary<RectTransform, Vector2> _percentageBarBaseSizes = new();
        private bool _freePurchaseDone;
        private bool _isResolvingTurn;
        private bool _shopOpen;
        private bool _shopSelectionMade;
        private bool _unique1TransformReady;
        private bool _startingUniqueSelectionOpen;
        private bool _startingUniqueSelectionResolved;
        private bool _startingUniqueConfirmTransitioning;
        private bool _activeItemConfirmOpen;
        private bool _defeatOverlayOpen;
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
        private readonly Dictionary<string, UniqueItemPresentationText> _uniqueItemPresentationTexts = new(StringComparer.Ordinal);
        private readonly Dictionary<Image, Vector3> _slotIconBaseScales = new();
        private int _lastAutoLineClearDamage;
        private bool _usingRuntimeStartingUniqueFallback;
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
        private const int MaxStage = 10;
        private const float StageClearGoldRewardMultiplier = 1.5f;
        private const float EdgeColumnOperatorChanceMultiplier = 0.5f;
        private const string Unique1ItemId = "UNIQUE_1_AWAKENED_ONE";
        private const string Unique2ItemId = "UNIQUE_2_PROBABILITY_STRIKE";
        private const string Unique3ItemId = "UNIQUE_3_TRINITY";
        private const string Unique4ItemId = "UNIQUE_4_ORDER_OF_OPERATIONS";
        private const string Unique5ItemId = "UNIQUE_5_SHIELD_NUMBER";
        private const string Unique6ItemId = "UNIQUE_6_FLAT_WEALTH";
        private const string Unique7ItemId = "UNIQUE_7_DAVID";
        private const string Unique8ItemId = "UNIQUE_8_PERCENT_WEALTH";
        private const string Unique9ItemId = "UNIQUE_9_ODINS_NINE_TRIALS";

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
            ResolveUiFont();
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
            BuildBoard();
            ResolveAutoLineClears(false);
            InitBattle();
            TryPlayBattleBgmAfterStartingUniqueSelection();
            StartCoroutine(ValidateBattleSceneStartup());
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_grid == null || _tileLayoutRoot == null)
            {
                return;
            }

            UpdateLayoutRegions();
            RefreshBoardVisualLayout();
        }

        private void Update()
        {
            if (_playerHp <= 0 || _enemyHp <= 0 || _shopOpen || _startingUniqueSelectionOpen || _activeItemConfirmOpen || _defeatOverlayOpen || _isResolvingTurn || IsBagPanelOpen() || IsPercentagePanelOpen())
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
                    (_defeatOverlayOpen && _defeatOverlayRoot != null && go.transform.IsChildOf(_defeatOverlayRoot)))
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
            bg.preserveAspect = config.BoardBackgroundSprite != null;
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
            BindBagLayout();
            BindPercentageLayout();
            BuildStartingUniqueSelectionOverlay(canvasRoot);
            BuildShopPanel();
            BuildActiveItemConfirmOverlay(canvasRoot);
            BuildDefeatOverlay(canvasRoot);
            EnsureShoppingParentsActive();
            HideSceneBoundStartingUniqueLayout();
            HideSceneBoundShopLayout();
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
            title.text = "시작 Unique Item 선택";
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
                var button = CreateActionButton(_startUniquePanel, $"Unique {i + 1}", new Vector2((i + 0.5f) / 3f, 0.42f), () => OpenStartingUniqueConfirmPanel(index), false, config.StartingUniqueButtonWidth, config.StartingUniqueButtonHeight, config.ShopFontSizeScale, config.StartingUniqueSelectionButtonStyle);
                button.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                SetButtonTextColor(button, config.StartingUniqueButtonTextColor);
                _startingUniqueButtons.Add(button);
            }

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
            SetStartingUniqueSelectionAura(null);
            _startUniqueOverlayRoot.gameObject.SetActive(false);
            return true;
        }

        private void HideSceneBoundStartingUniqueLayout()
        {
            var layout = _boardLayoutReference?.StartingUniqueLayout;
            if (layout == null || !layout.HasSceneLayout)
            {
                return;
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

            if (layout.ExitButton != null)
            {
                BindButton(layout.ExitButton, () => SceneManager.LoadScene("TitleScene"));
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

            var restartButton = CreateActionButton(_defeatPanel, "처음부터 다시 하기", new Vector2(0.5f, 0.24f), RestartFromBeginning, false, config.ShopConfirmActionButtonWidth * 1.6f, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(restartButton, config.ShopButtonTextColor);
            var menuButton = CreateActionButton(_defeatPanel, "메뉴로 나가기", new Vector2(0.5f, 0.10f), OnMenuButtonPressed, false, config.ShopConfirmActionButtonWidth * 1.6f, config.ShopConfirmActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(menuButton, config.ShopButtonTextColor);

            _defeatOverlayRoot.gameObject.SetActive(false);
        }

        private void OpenDefeatOverlay()
        {
            _defeatOverlayOpen = true;
            if (_defeatOverlayRoot != null)
            {
                _defeatOverlayRoot.gameObject.SetActive(true);
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
            _shopSelectionMade = false;
            _freePurchaseDone = false;
            _startingUniqueSelectionOpen = false;
            _startingUniqueSelectionResolved = false;
            _activeItemConfirmOpen = false;
            _defeatOverlayOpen = false;
            _pendingStartingUniqueSelectionIndex = null;
            _pendingActiveItemId = null;
            _pendingShopSelection = null;
            _dragging = false;
            _startingUniqueCandidates.Clear();

            if (_shopOverlayRoot != null)
            {
                _shopOverlayRoot.gameObject.SetActive(false);
            }
            SetDimOverlayVisible(_shopDimRoot, false);
            SetDimOverlayVisible(_shopConfirmDimRoot, false);
            SetDimOverlayVisible(_bagDimRoot, false);
            SetDimOverlayVisible(_percentageDimRoot, false);

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

            if (_defeatOverlayRoot != null)
            {
                _defeatOverlayRoot.gameObject.SetActive(false);
            }

            _playerState = new RuntimePlayerState();
            _stageEnemyOrder = null;
            _runtimeItemInventory = new RuntimeItemInventory();
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

        private void OnMenuButtonPressed()
        {
            _hud.SetMessage("메뉴는 아직 준비되지 않았습니다.");
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
            }

            if (_bagPanelRoot != null)
            {
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
                _percentagePanelOriginalParent = _percentagePanelRoot.parent;
                _percentagePanelOriginalSiblingIndex = _percentagePanelRoot.GetSiblingIndex();
                _percentagePanelRoot.gameObject.SetActive(false);
            }

            RefreshPercentageUi();
        }

        private void TogglePercentagePanel()
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
                    SpawnTileValue(tile, x, y);
                    _grid[x, y] = tile;
                }
            }
        }

        private void ResetStageLocalBattleState()
        {
            _dragging = false;
            ClearSelectionVisual();
            ClearBoardTiles();
            RebuildCachedSpawnWeights();
            BuildBoard();
            ResolveAutoLineClears(false);
            _validTurnCount = 0;
            _unique1UsedOneCountThisStage = 0;
            _unique1TransformReady = false;
            _playerShield = 0;
            RefreshHud(string.Empty, "-");
            _hud.SetMessage(string.Empty);
        }

        private void ClearBoardTiles()
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
            _currentCombatMode = CombatMode.Attack;
            _enemyDeathHandledThisStage = false;
            battleAnimationManager?.SetPlayerCombatMode(_currentCombatMode);
            RebuildCachedSpawnWeights();
            RefreshHud(string.Empty, "-");
            _hud.SetMessage($"Stage {_playerState.CurrentStage}: {_currentStage.EnemyName}");
            EnsureStartingUniqueSelection();
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

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry?.Root != null)
                {
                    entry.Root.SetActive(ReferenceEquals(entry, selectedEntry));
                }
            }

            if (selectedEntry == null)
            {
                battleAnimationManager?.SetEnemyRuntimeBindings(null, null, null, null, null);
                return;
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

        private void SpawnTileValue(BattleTileView tile, int x, int y)
        {
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

            _selection.Add(tile);
            tile.SetSelected(true);
            GameAudioManager.Instance?.PlayTileSelectSfx();
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
            ApplyUnique9BoardTransformIfNeeded();
            context = BuildSelectionContextFromCurrentBoard();

            var consumedUnique1Ready = _unique1TransformReady;
            if (_unique1TransformReady)
            {
                for (var i = 0; i < context.CalculationNumbers.Count; i++)
                {
                    if (context.CalculationNumbers[i] == 1)
                    {
                        context.CalculationNumbers[i] = 11;
                    }
                }
                _unique1TransformReady = false;
            }

            if (!TryCalculateExpression(context.CalculationNumbers, context.Operators, out var baseResult, out error))
            {
                _hud.SetMessage($"Invalid: {error}");
                GameAudioManager.Instance?.PlayInvalidSelectionSfx();
                ClearSelectionVisual();
                RefreshHud(string.Empty, "-");
                return;
            }

            var uniqueOutcome = ResolveUniqueOutcome(context, baseResult);

            var enemyHpBefore = _enemyHp;
            ApplyCombatResult(baseResult, uniqueOutcome);
            var dealtDamage = Mathf.Max(0, enemyHpBefore - _enemyHp);

            GameAudioManager.Instance?.PlayExpressionConfirmSfx();

            UpdateUnique1State(context, consumedUnique1Ready);

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
                damageAfterShield = Mathf.Max(0, _currentStage.EnemyAttackDamage - _playerShield);
                _playerShield = 0;
                _playerHp = Mathf.Max(0, _playerHp - damageAfterShield);
                TriggerEnemyAttackCameraShake(damageAfterShield > 0);
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

                _playerShield = Mathf.Max(_playerShield, baseShield);
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
                        messageParts.Add("Unique 2 발동");
                    }
                }
            }

            if (HasUniqueItem(Unique3ItemId) && finalNumbers.Contains(3) && finalNumbers.Contains(6) && finalNumbers.Contains(9))
            {
                if (_itemDatabase.TryGetItem(Unique3ItemId, out var unique3))
                {
                    if (isAttack)
                    {
                        bonusDamage += _itemDatabase.ResolveEffectInt(unique3, "attackBonusDamage");
                        shieldBonus += _itemDatabase.ResolveEffectInt(unique3, "attackShieldBonus");
                    }
                    else
                    {
                        shieldBonus += _itemDatabase.ResolveEffectInt(unique3, "defenseShieldBonus");
                    }

                    messageParts.Add("Unique 3 발동");
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
                        messageParts.Add("Unique 5 발동");
                    }
                }
            }

            if (HasUniqueItem(Unique7ItemId) && isAttack && context.ExpressionLength == 7 && baseResult > 0)
            {
                if (_itemDatabase.TryGetItem(Unique7ItemId, out var unique7))
                {
                    bonusDamage += Mathf.CeilToInt(baseResult * (_itemDatabase.ResolveEffectInt(unique7, "bonusDamagePercent") / 100f));
                    messageParts.Add("Unique 7 발동");
                }
            }

            var message = messageParts.Count > 0 ? string.Join(", ", messageParts) : "Valid expression!";
            return new UniqueOutcome(bonusDamage, shieldBonus, message);
        }

        private void UpdateUnique1State(SelectionContext context, bool consumedReadyState)
        {
            if (!HasUniqueItem(Unique1ItemId))
            {
                return;
            }

            if (consumedReadyState)
            {
                return;
            }

            if (!_itemDatabase.TryGetItem(Unique1ItemId, out var unique1))
            {
                return;
            }

            var countOnes = CountNumber(context.CalculationNumbers, 1);
            _unique1UsedOneCountThisStage += countOnes;
            if (_unique1UsedOneCountThisStage >= _itemDatabase.ResolveEffectInt(unique1, "requiredOneCount"))
            {
                _unique1UsedOneCountThisStage = 0;
                _unique1TransformReady = true;
            }
        }

        private void ApplyUnique9BoardTransformIfNeeded()
        {
            if (!HasUniqueItem(Unique9ItemId) || !_itemDatabase.TryGetItem(Unique9ItemId, out var unique9))
            {
                return;
            }

            var triggerTurnA = _itemDatabase.ResolveEffectInt(unique9, "triggerTurnA");
            var triggerTurnB = _itemDatabase.ResolveEffectInt(unique9, "triggerTurnB");
            if (_validTurnCount != triggerTurnA && _validTurnCount != triggerTurnB)
            {
                return;
            }

            var maxTransformValue = _itemDatabase.ResolveEffectInt(unique9, "maxTransformValue");
            var targetValue = _itemDatabase.ResolveEffectInt(unique9, "targetValue");
            for (var x = 0; x < config.Columns; x++)
            {
                for (var y = 0; y < config.Rows; y++)
                {
                    var tile = _grid[x, y];
                    if (tile == null || tile.Kind != TileKind.Number || tile.NumberValue > maxTransformValue)
                    {
                        continue;
                    }

                    tile.SetNumber(targetValue);
                    ApplyTileSpriteVisual(tile);
                }
            }
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
            if (_isResolvingTurn)
            {
                return;
            }

            _currentCombatMode = mode;
            battleAnimationManager?.SetPlayerCombatMode(mode);
            RefreshCombatModeButtons();
            RefreshHud(GetExpressionString(), "-");
            _hud.SetMessage(mode == CombatMode.Attack ? "Attack Mode" : "Defense Mode");
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
            _startingUniqueConfirmTransitioning = false;
        }

        private void TryPlayBattleBgmAfterStartingUniqueSelection()
        {
            if (_startingUniqueSelectionOpen)
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
                ApplyAutoLineClearDamage(lineGroups);
                RemoveTiles(lineTiles, false);
                ApplyGravityAndRefill(true);
            }
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

        private void ApplyAutoLineClearDamage(IEnumerable<LineClearGroup> lineGroups)
        {
            if (_enemyHp <= 0)
            {
                return;
            }

            var countedNumberTiles = new HashSet<BattleTileView>();
            var damage = 0;
            var numberValueSum = 0;
            foreach (var group in lineGroups)
            {
                if (group == null)
                {
                    continue;
                }

                if (group.Kind == TileKind.Operator)
                {
                    damage += config.OperatorLineClearFixedDamage;
                    continue;
                }

                foreach (var tile in group.Tiles)
                {
                    if (tile == null || tile.Kind != TileKind.Number || !countedNumberTiles.Add(tile))
                    {
                        continue;
                    }

                    numberValueSum += tile.NumberValue;
                }
            }

            if (countedNumberTiles.Count > 0)
            {
                damage += Mathf.RoundToInt(numberValueSum / (float)countedNumberTiles.Count);
            }

            if (damage <= 0)
            {
                return;
            }

            _lastAutoLineClearDamage += damage;
            _enemyHp = Mathf.Max(0, _enemyHp - damage);
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
                for (var y = writeY; y >= 0; y--)
                {
                    var tile = CreateTile(x, y, layoutMetrics);
                    SpawnTileValue(tile, x, y);
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

        private void RefreshHud(string expression, string result)
        {
            _hud.SetHp(_playerHp, _playerShield, _enemyHp, _currentStage.EnemyHp);
            var left = _currentStage.EnemyAttackCycle - (_validTurnCount % _currentStage.EnemyAttackCycle);
            _hud.SetCountdown(left);
            _hud.SetExpression(expression);
            _hud.SetResult(result);
            _hud.SetValidationStatus(GetCurrentExpressionValidity());
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
            var reward = GetStageClearGoldReward();
            _playerState.Gold += reward;
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

            _isResolvingTurn = false;
            OnStageCleared();
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
            _shopSelectionMade = false;
            EnsureShoppingParentsActive();
            if (_startUniqueOverlayRoot != null)
            {
                _startUniqueOverlayRoot.gameObject.SetActive(false);
            }
            SetDimOverlayVisible(_shopDimRoot, true);
            EnsureHierarchyActive(_shopOverlayRoot);
            if (_shopPanel != null)
            {
                EnsureHierarchyActive(_shopPanel);
            }
            _shopOverlayRoot.gameObject.SetActive(true);
            PlaceDimOverlayBehind(_shopDimRoot, _shopOverlayRoot);
            _shopOverlayRoot.SetAsLastSibling();
            RollShop();
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
            var exitButton = CreateActionButton(bottomRow, "Exit", new Vector2(0.20f, 0.5f), () => SceneManager.LoadScene("TitleScene"), false, config.ShopMainActionButtonWidth, config.ShopMainActionButtonHeight, config.ShopFontSizeScale);
            SetButtonTextColor(exitButton, config.ShopButtonTextColor);
            _nextStageButton = CreateActionButton(bottomRow, "Next Stage", new Vector2(0.80f, 0.5f), OnNextStagePressed, false, config.ShopMainActionButtonWidth, config.ShopMainActionButtonHeight, config.ShopFontSizeScale);
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

            auraImage.color = Color.white;
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

        private void RollShop()
        {
            CloseShopConfirmPanel();
            _freePurchaseDone = false;
            _freeSlots.Clear();
            _paidSlots.Clear();
            RollFreeSlots();
            RollPaidSlots();
            RefreshShopUi();
        }

        private void RollFreeSlots()
        {
            var chosenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < 3; i++)
            {
                var item = PickRandomEligibleItem(ItemSlotKind.Free, chosenIds, null);
                if (item == null)
                {
                    _freeSlots.Add(ShopSlotData.CreateLockedPlaceholder(true, "Locked"));
                    continue;
                }

                chosenIds.Add(item.itemId);
                _freeSlots.Add(ShopSlotData.CreateItem(item, 0, true, ItemSlotKind.Free));
            }
        }

        private void RollPaidSlots()
        {
            var chosenIds = new HashSet<string>(StringComparer.Ordinal);
            var useUniqueSlot = IsUniqueShop();
            for (var i = 0; i < 3; i++)
            {
                if (useUniqueSlot && i == GetUniqueShopSlotIndex())
                {
                    var uniqueItem = PickRandomEligibleItem(ItemSlotKind.Unique, chosenIds, null);
                    if (uniqueItem == null)
                    {
                        Debug.LogWarning("Unique shop reached, but no eligible UniqueItem exists for the Unique Item Slot.");
                        _paidSlots.Add(ShopSlotData.CreateLockedPlaceholder(false, "Unique\nLocked"));
                        continue;
                    }

                    chosenIds.Add(uniqueItem.itemId);
                    _paidSlots.Add(ShopSlotData.CreateItem(uniqueItem, _itemDatabase.ResolvePrice(uniqueItem), false, ItemSlotKind.Unique));
                    continue;
                }

                var item = PickRandomEligiblePaidItem(chosenIds);
                if (item == null)
                {
                    _paidSlots.Add(ShopSlotData.CreateLockedPlaceholder(false, "Locked"));
                    continue;
                }

                chosenIds.Add(item.itemId);
                _paidSlots.Add(ShopSlotData.CreateItem(item, _itemDatabase.ResolvePrice(item), false, ItemSlotKind.Paid));
            }
        }

        private bool IsUniqueShop()
        {
            if (IsEasyDifficulty())
            {
                return false;
            }

            return _playerState.CurrentStage == _itemDatabase.GetIntConfig("TEMP_UNIQUE_SHOP_STAGE_1")
                || _playerState.CurrentStage == _itemDatabase.GetIntConfig("TEMP_UNIQUE_SHOP_STAGE_2")
                || _playerState.CurrentStage == _itemDatabase.GetIntConfig("TEMP_UNIQUE_SHOP_STAGE_3");
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

            if (!selection.IsFree)
            {
                _playerState.Gold -= slot.Cost;
            }

            _itemEffectResolver.ApplyAcquiredItem(slot.Item, _runtimeItemInventory, _itemDatabase, this);
            _shopSelectionMade = true;

            if (selection.IsFree)
            {
                _freePurchaseDone = true;
                for (var i = 0; i < _freeSlots.Count; i++)
                {
                    _freeSlots[i].IsLocked = true;
                    _freeSlots[i].OverrideLabel = i == selection.Index ? "Selected" : "Locked";
                }
            }
            else
            {
                slot.IsLocked = true;
                slot.OverrideLabel = "Purchased";
            }

            ReevaluateVisibleDuplicateEligibility(slot.Item.itemId);
            CloseShopConfirmPanel();
            RefreshShopUi();
            RefreshHud(string.Empty, "-");
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

                BindSlotButton(_freeButtons[i], _freeSlots[i], _freeSlots[i].IsLocked || _freePurchaseDone && _freeSlots[i].OverrideLabel != "Selected", GetShopSlotReference(true, i));
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
                    rerollText.text = _shopSelectionMade ? "Locked" : $"{rerollCost}G";
                }

                _rerollButton.interactable = !_shopSelectionMade && _playerState.Gold >= rerollCost;
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

        private void BindSlotButton(Button button, ShopSlotData slot, bool forceLocked, BattleBoardLayoutReference.ItemSlotReference slotReference = null)
        {
            var label = slot.OverrideLabel;
            var itemName = slot.Item?.displayName ?? "Locked";
            var description = slot.Item != null
                ? (string.IsNullOrWhiteSpace(slot.Item.uiDescriptionKo) ? "설명 없음" : slot.Item.uiDescriptionKo)
                : string.Empty;
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
            if (_shopSelectionMade)
            {
                return;
            }

            var cost = GetCurrentRerollCost();
            if (_playerState.Gold < cost)
            {
                return;
            }

            _playerState.Gold -= cost;
            _playerState.RerollUsedCountThisRun++;
            RollShop();
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
            _shopOverlayRoot.gameObject.SetActive(false);
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
                var rect = numberBars[i]?.ImageRect;
                var weight = _cachedNumberWeights.TryGetValue(i + 1, out var numberWeight) ? numberWeight : 0;
                ApplyPercentageBarSize(rect, weight, totalNumberWeight, true);
            }

            ApplyPercentageBarSize(percentageLayout.AddBar?.ImageRect, GetOperatorWeight("+"), totalOperatorWeight, false);
            ApplyPercentageBarSize(percentageLayout.SubtractBar?.ImageRect, GetOperatorWeight("-"), totalOperatorWeight, false);
            ApplyPercentageBarSize(percentageLayout.MultiplyBar?.ImageRect, GetOperatorWeight("x"), totalOperatorWeight, false);
            ApplyPercentageBarSize(percentageLayout.DivideBar?.ImageRect, GetOperatorWeight("÷"), totalOperatorWeight, false);
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

        private StageDefinition GetStageDefinition(int stage)
        {
            if (stage >= MaxStage)
            {
                return StageDatabase.GetFinalBossStage(
                    config.EnemyMaxHp,
                    config.EnemyAttackDamage,
                    config.EnemyAttackEveryValidTurns);
            }

            EnsureEnemyOrderForRun();
            var stageIndex = Mathf.Clamp(stage - 1, 0, _stageEnemyOrder.Length - 1);
            return StageDatabase.GetStage(stage, _stageEnemyOrder[stageIndex], GetBoardDeckUpgradeCount());
        }

        private void EnsureEnemyOrderForRun()
        {
            if (_stageEnemyOrder != null && _stageEnemyOrder.Length == MaxStage - 1)
            {
                return;
            }

            _stageEnemyOrder = new EnemyType[MaxStage - 1];

            var firstEnemy = UnityEngine.Random.Range(0, 2) == 0 ? EnemyType.Wolf : EnemyType.Orc;
            _stageEnemyOrder[0] = firstEnemy;

            var firstBlockRemaining = firstEnemy == EnemyType.Wolf
                ? new[] { EnemyType.Orc, EnemyType.StoneGolem }
                : new[] { EnemyType.Wolf, EnemyType.StoneGolem };
            ShuffleEnemyTypes(firstBlockRemaining);
            _stageEnemyOrder[1] = firstBlockRemaining[0];
            _stageEnemyOrder[2] = firstBlockRemaining[1];

            for (var blockStart = 3; blockStart < _stageEnemyOrder.Length; blockStart += 3)
            {
                var block = new[] { EnemyType.Wolf, EnemyType.Orc, EnemyType.StoneGolem };
                ShuffleEnemyTypes(block);
                Array.Copy(block, 0, _stageEnemyOrder, blockStart, block.Length);
            }
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
                var finalWeight = Mathf.Max(0, entry.Weight + modifier);
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
            private const int ExistingFinalBossPlaceholderHpBonus = 180;

            public static StageDefinition GetStage(int stage, EnemyType enemyType, int boardDeckUpgradeCount)
            {
                var enemy = GetEnemyDefinition(enemyType);
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
                    placeholderBaseHp + ExistingFinalBossPlaceholderHpBonus,
                    placeholderAttackDamage,
                    placeholderAttackCycle);
            }

            private static EnemyDefinition GetEnemyDefinition(EnemyType enemyType)
            {
                return enemyType switch
                {
                    EnemyType.Wolf => new EnemyDefinition(EnemyType.Wolf, "울프", 40, 10, 2),
                    EnemyType.Orc => new EnemyDefinition(EnemyType.Orc, "오크", 50, 10, 3),
                    EnemyType.StoneGolem => new EnemyDefinition(EnemyType.StoneGolem, "스톤골렘", 120, 25, 5),
                    _ => new EnemyDefinition(EnemyType.Wolf, "울프", 40, 10, 2),
                };
            }
        }
    }
}
