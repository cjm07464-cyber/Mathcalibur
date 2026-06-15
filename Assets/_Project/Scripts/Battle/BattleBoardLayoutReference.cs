using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mathcalibur.Items;
using System;
using UnityEngine.Serialization;

namespace Mathcalibur.Battle
{
    [DisallowMultipleComponent]
    public class BattleBoardLayoutReference : MonoBehaviour
    {
        [System.Serializable]
        public class CombatModeButtonReference
        {
            [SerializeField] private Image image;
            [SerializeField] private Sprite selectedSprite;
            [SerializeField] private Color normalColor = Color.white;
            [SerializeField] private Color selectedColor = Color.white;
            [SerializeField] private Color normalTextColor = Color.black;
            [SerializeField] private Color selectedTextColor = Color.black;

            private Sprite _cachedNormalSprite;

            public Image Image => image;
            public Sprite SelectedSprite => selectedSprite;
            public Color NormalColor => normalColor;
            public Color SelectedColor => selectedColor;
            public Color NormalTextColor => normalTextColor;
            public Color SelectedTextColor => selectedTextColor;

            public Sprite GetNormalSprite()
            {
                if (_cachedNormalSprite == null && image != null)
                {
                    _cachedNormalSprite = image.sprite;
                }

                return _cachedNormalSprite;
            }
        }

        [System.Serializable]
        public class UniqueItemPresentationTextReference
        {
            [SerializeField] private TMP_Text numberText;

            public TMP_Text NumberText => numberText;
            public bool HasAnyAssigned => numberText != null;
        }

        [System.Serializable]
        public class UniqueItemExplainTextReference
        {
            [SerializeField] private TMP_Text tendencyText;
            [SerializeField] private TMP_Text conditionText;
            [SerializeField] private TMP_Text effectText;

            public TMP_Text TendencyText => tendencyText;
            public TMP_Text ConditionText => conditionText;
            public TMP_Text EffectText => effectText;
        }

        [System.Serializable]
        public class ItemSlotReference
        {
            [SerializeField] private Button button;
            [SerializeField] private TMP_Text nameText;
            [SerializeField] private TMP_Text descriptionText;
            [SerializeField] private TMP_Text priceText;
            [SerializeField] private Image categoryIconImage;
            [SerializeField] private Image auraImage;
            [SerializeField] private UniqueItemPresentationTextReference uniqueItemPresentationTexts;

            public Button Button => button;
            public TMP_Text NameText => nameText;
            public TMP_Text DescriptionText => descriptionText;
            public TMP_Text PriceText => priceText;
            public Image CategoryIconImage => categoryIconImage;
            public Image AuraImage => auraImage;
            public UniqueItemPresentationTextReference UniqueItemPresentationTexts => uniqueItemPresentationTexts;
        }

        [System.Serializable]
        public class ItemRarityAuraSet
        {
            [SerializeField] private Sprite commonSprite;
            [SerializeField] private Sprite rareSprite;
            [SerializeField] private Sprite legendarySprite;

            public Sprite GetSprite(ItemData item)
            {
                if (item == null)
                {
                    return null;
                }

                if (item.Category == ItemCategory.UniqueItem)
                {
                    return null;
                }

                return item.Rarity switch
                {
                    ItemRarity.Common => commonSprite,
                    ItemRarity.Rare => rareSprite,
                    ItemRarity.Legendary => legendarySprite,
                    _ => null,
                };
            }
        }

        [System.Serializable]
        public class ItemCategoryIconSet
        {
            private const string HealingPotionItemId = "ITEM_HEALING_POTION";
            private const string AttackPotionItemId = "ITEM_ATTACK_POTION";

            [Header("Active Item")]
            [FormerlySerializedAs("activeItemIcon")]
            [SerializeField] private Sprite healingPotionIcon;
            [SerializeField] private Sprite attackPotionIcon;
            [Header("Other Category")]
            [SerializeField] private Sprite passiveItemIcon;
            [SerializeField] private Sprite boardDeckUpgradeIcon;
            [SerializeField] private Sprite connectionLimitUpgradeIcon;
            [Header("Unique Item 1~9")]
            [SerializeField] private Sprite uniqueItem1Icon;
            [SerializeField] private Sprite uniqueItem2Icon;
            [SerializeField] private Sprite uniqueItem3Icon;
            [SerializeField] private Sprite uniqueItem4Icon;
            [SerializeField] private Sprite uniqueItem5Icon;
            [SerializeField] private Sprite uniqueItem6Icon;
            [SerializeField] private Sprite uniqueItem7Icon;
            [SerializeField] private Sprite uniqueItem8Icon;
            [SerializeField] private Sprite uniqueItem9Icon;

            public Sprite GetIcon(ItemData item)
            {
                if (item == null)
                {
                    return null;
                }

                if (item.Category == ItemCategory.UniqueItem)
                {
                    var uniqueIcon = GetUniqueItemIcon(item.itemId);
                    if (uniqueIcon != null)
                    {
                        return uniqueIcon;
                    }
                }

                return item.Category switch
                {
                    ItemCategory.ActiveItem => GetActiveItemIcon(item.itemId),
                    ItemCategory.PassiveItem => passiveItemIcon,
                    ItemCategory.BoardDeckUpgrade => boardDeckUpgradeIcon,
                    ItemCategory.ConnectionLimitUpgrade => connectionLimitUpgradeIcon,
                    ItemCategory.UniqueItem => null,
                    _ => null,
                };
            }

            private Sprite GetActiveItemIcon(string itemId)
            {
                return itemId switch
                {
                    HealingPotionItemId => healingPotionIcon,
                    AttackPotionItemId => attackPotionIcon,
                    _ => null,
                };
            }

            private Sprite GetUniqueItemIcon(string itemId)
            {
                var uniqueIndex = ParseUniqueItemIndex(itemId);
                return uniqueIndex switch
                {
                    1 => uniqueItem1Icon,
                    2 => uniqueItem2Icon,
                    3 => uniqueItem3Icon,
                    4 => uniqueItem4Icon,
                    5 => uniqueItem5Icon,
                    6 => uniqueItem6Icon,
                    7 => uniqueItem7Icon,
                    8 => uniqueItem8Icon,
                    9 => uniqueItem9Icon,
                    _ => null,
                };
            }

            private static int ParseUniqueItemIndex(string itemId)
            {
                if (string.IsNullOrWhiteSpace(itemId) || !itemId.StartsWith("UNIQUE_", StringComparison.Ordinal))
                {
                    return 0;
                }

                var parts = itemId.Split('_');
                return parts.Length > 1 && int.TryParse(parts[1], out var index) ? index : 0;
            }
        }

        [System.Serializable]
        public class BagItemIconSet
        {
            private const string HealingPotionItemId = "ITEM_HEALING_POTION";
            private const string AttackPotionItemId = "ITEM_ATTACK_POTION";

            [SerializeField] private Sprite healingPotionIcon;
            [SerializeField] private Sprite attackPotionIcon;

            public Sprite GetIcon(ItemData item)
            {
                if (item == null)
                {
                    return null;
                }

                return item.itemId switch
                {
                    HealingPotionItemId => healingPotionIcon,
                    AttackPotionItemId => attackPotionIcon,
                    _ => null,
                };
            }
        }

        [System.Serializable]
        public class StartingUniqueLayoutReference
        {
            [System.Serializable]
            public class SlotReference
            {
                [SerializeField] private Button button;
                [SerializeField] private TMP_Text nameText;
                [SerializeField] private TMP_Text descriptionText;
                [SerializeField] private Image categoryIconImage;
                [SerializeField] private UniqueItemPresentationTextReference uniqueItemPresentationTexts;

                public Button Button => button;
                public TMP_Text NameText => nameText;
                public TMP_Text DescriptionText => descriptionText;
                public Image CategoryIconImage => categoryIconImage;
                public UniqueItemPresentationTextReference UniqueItemPresentationTexts => uniqueItemPresentationTexts;
            }

            [SerializeField] private RectTransform overlayRoot;
            [SerializeField] private RectTransform panelRoot;
            [SerializeField] private SlotReference[] itemSlots = new SlotReference[3];
            [SerializeField] private GameObject[] selectionAuraObjects = new GameObject[3];
            [SerializeField] private TMP_Text explainNameText;
            [SerializeField] private UniqueItemExplainTextReference explainTextReferences;
            [SerializeField] private Button selectButton;

            public RectTransform OverlayRoot => overlayRoot;
            public RectTransform PanelRoot => panelRoot;
            public SlotReference[] ItemSlots => itemSlots;
            public GameObject[] SelectionAuraObjects => selectionAuraObjects;
            public TMP_Text ExplainNameText => explainNameText;
            public UniqueItemExplainTextReference ExplainTextReferences => explainTextReferences;
            public Button SelectButton => selectButton;
            public bool HasSceneLayout => overlayRoot != null || panelRoot != null;
        }

        [System.Serializable]
        public class BattleHudReference
        {
            [SerializeField] private TMP_Text playerHpText;
            [SerializeField] private TMP_Text defenseText;
            [SerializeField] private TMP_Text enemyHpText;
            [SerializeField] private TMP_Text turnText;
            [SerializeField] private TMP_Text expressionText;
            [SerializeField] private TMP_Text resultText;
            [SerializeField] private TMP_Text validationSymbolText;
            [SerializeField] private TMP_Text validationLabelText;
            [SerializeField] private Color validColor = Color.green;
            [SerializeField] private Color invalidColor = Color.red;
            [SerializeField] private Image enemyHpBarImage;
            [SerializeField] private Button killEnemyButton;

            public TMP_Text PlayerHpText => playerHpText;
            public TMP_Text DefenseText => defenseText;
            public TMP_Text EnemyHpText => enemyHpText;
            public TMP_Text TurnText => turnText;
            public TMP_Text ExpressionText => expressionText;
            public TMP_Text ResultText => resultText;
            public TMP_Text ValidationSymbolText => validationSymbolText;
            public TMP_Text ValidationLabelText => validationLabelText;
            public Color ValidColor => validColor;
            public Color InvalidColor => invalidColor;
            public Image EnemyHpBarImage => enemyHpBarImage;
            public Button KillEnemyButton => killEnemyButton;
        }

        [System.Serializable]
        public class BagItemSlotReference
        {
            [SerializeField] private Button button;
            [SerializeField] private Image itemImage;
            [SerializeField] private TMP_Text countText;

            public Button Button => button;
            public Image ItemImage => itemImage;
            public TMP_Text CountText => countText;
        }

        [System.Serializable]
        public class BagLayoutReference
        {
            [SerializeField] private Button bagButton;
            [SerializeField] private RectTransform panelRoot;
            [SerializeField] private BagItemSlotReference[] itemSlots = new BagItemSlotReference[2];

            public Button BagButton => bagButton;
            public RectTransform PanelRoot => panelRoot;
            public BagItemSlotReference[] ItemSlots => itemSlots;
        }

        [System.Serializable]
        public class WeightBarReference
        {
            [SerializeField] private RectTransform imageRect;

            public RectTransform ImageRect => imageRect;
        }

        [System.Serializable]
        public class PercentageLayoutReference
        {
            [SerializeField] private Button percentageButton;
            [SerializeField] private Button closeButton;
            [SerializeField] private RectTransform panelRoot;
            [SerializeField] private WeightBarReference[] numberBars = new WeightBarReference[9];
            [SerializeField] private WeightBarReference addBar;
            [SerializeField] private WeightBarReference subtractBar;
            [SerializeField] private WeightBarReference multiplyBar;
            [SerializeField] private WeightBarReference divideBar;

            public Button PercentageButton => percentageButton;
            public Button CloseButton => closeButton;
            public RectTransform PanelRoot => panelRoot;
            public WeightBarReference[] NumberBars => numberBars;
            public WeightBarReference AddBar => addBar;
            public WeightBarReference SubtractBar => subtractBar;
            public WeightBarReference MultiplyBar => multiplyBar;
            public WeightBarReference DivideBar => divideBar;
        }

        [System.Serializable]
        public class ShopLayoutReference
        {
            [SerializeField] private RectTransform overlayRoot;
            [SerializeField] private RectTransform panelRoot;
            [SerializeField] private ItemSlotReference[] freeItemSlots = new ItemSlotReference[3];
            [SerializeField] private ItemSlotReference[] paidItemSlots = new ItemSlotReference[3];
            [SerializeField] private TMP_Text goldText;
            [SerializeField] private Button rerollButton;
            [SerializeField] private TMP_Text rerollText;
            [SerializeField] private Button exitButton;
            [SerializeField] private Button nextStageButton;
            [SerializeField] private RectTransform confirmPanelRoot;
            [SerializeField] private RectTransform confirmPreviewRoot;
            [SerializeField] private TMP_Text confirmNameText;
            [SerializeField] private TMP_Text confirmDescriptionText;
            [SerializeField] private TMP_Text confirmPriceText;
            [SerializeField] private Button purchaseButton;
            [SerializeField] private Button cancelButton;

            public RectTransform OverlayRoot => overlayRoot;
            public RectTransform PanelRoot => panelRoot;
            public ItemSlotReference[] FreeItemSlots => freeItemSlots;
            public ItemSlotReference[] PaidItemSlots => paidItemSlots;
            public TMP_Text GoldText => goldText;
            public Button RerollButton => rerollButton;
            public TMP_Text RerollText => rerollText;
            public Button ExitButton => exitButton;
            public Button NextStageButton => nextStageButton;
            public RectTransform ConfirmPanelRoot => confirmPanelRoot;
            public RectTransform ConfirmPreviewRoot => confirmPreviewRoot;
            public TMP_Text ConfirmNameText => confirmNameText;
            public TMP_Text ConfirmDescriptionText => confirmDescriptionText;
            public TMP_Text ConfirmPriceText => confirmPriceText;
            public Button PurchaseButton => purchaseButton;
            public Button CancelButton => cancelButton;
            public bool HasSceneLayout => overlayRoot != null || panelRoot != null;
        }

        [Header("타일 배치 기준")]
        [SerializeField] private RectTransform tilePanel;
        [SerializeField] private RectTransform tileStartPoint;
        [InspectorName("타일 가로 간격")]
        [SerializeField] private float tileSpacingX = 12f;
        [InspectorName("타일 세로 간격")]
        [SerializeField] private float tileSpacingY = 12f;

        [Header("전투 모드 버튼")]
        [SerializeField] private CombatModeButtonReference attackModeButton;
        [SerializeField] private CombatModeButtonReference defenseModeButton;
        [Header("전투 HUD")]
        [SerializeField] private BattleHudReference battleHud;
        [Header("가방 UI")]
        [SerializeField] private BagLayoutReference bagLayout;
        [Header("퍼센트 표 UI")]
        [SerializeField] private PercentageLayoutReference percentageLayout;

        [Header("시작 Unique Item UI")]
        [SerializeField] private StartingUniqueLayoutReference startingUniqueLayout;

        [Header("상점 UI")]
        [SerializeField] private ShopLayoutReference shopLayout;
        [Header("아이템 분야별 아이콘")]
        [SerializeField] private ItemCategoryIconSet itemCategoryIcons;
        [Header("가방 전용 아이콘")]
        [SerializeField] private BagItemIconSet bagItemIcons;
        [Header("상점 포션 아이콘 배율")]
        [SerializeField] private Vector2 shopActiveItemIconScale = new(0.8f, 0.8f);
        [Header("상점 아이템 아우라 색상")]
        [SerializeField] private ItemRarityAuraSet itemRarityAuras;

        public RectTransform TilePanel => tilePanel;
        public RectTransform TileStartPoint => tileStartPoint;
        public Vector2 TileSpacing => new(Mathf.Max(0f, tileSpacingX), Mathf.Max(0f, tileSpacingY));
        public bool HasCustomStartPoint => tileStartPoint != null;
        public CombatModeButtonReference AttackModeButton => attackModeButton;
        public CombatModeButtonReference DefenseModeButton => defenseModeButton;
        public BattleHudReference BattleHud => battleHud;
        public BagLayoutReference BagLayout => bagLayout;
        public PercentageLayoutReference PercentageLayout => percentageLayout;
        public StartingUniqueLayoutReference StartingUniqueLayout => startingUniqueLayout;
        public ShopLayoutReference ShopLayout => shopLayout;
        public ItemCategoryIconSet ItemCategoryIcons => itemCategoryIcons;
        public BagItemIconSet BagItemIcons => bagItemIcons;
        public Vector2 ShopActiveItemIconScale => new(Mathf.Max(0.1f, shopActiveItemIconScale.x), Mathf.Max(0.1f, shopActiveItemIconScale.y));
        public ItemRarityAuraSet ItemRarityAuras => itemRarityAuras;
    }
}
