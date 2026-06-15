using System;
using System.Collections.Generic;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mathcalibur.Battle
{
    [CreateAssetMenu(menuName = "Mathcalibur/Battle Config", fileName = "BattleConfig")]
    public class BattleConfig : ScriptableObject
    {
        [Header("보드")]
        [InspectorName("가로 칸 수")]
        [SerializeField] private int columns = 5;
        [InspectorName("세로 칸 수")]
        [SerializeField] private int rows = 5;
        [InspectorName("보드 배경 색상")]
        [SerializeField] private Color boardBackgroundColor = new(0f, 0f, 0f, 0.2f);
        [InspectorName("보드 배경 이미지")]
        [SerializeField] private Sprite boardBackgroundSprite;
        [InspectorName("보드 배경 이미지 색상")]
        [SerializeField] private Color boardBackgroundSpriteTint = Color.white;

        [Header("전투")]
        [InspectorName("플레이어 최대 HP")]
        [SerializeField] private int playerMaxHp = 100;
        [InspectorName("적 기본 HP")]
        [SerializeField] private int enemyMaxHp = 100;
        [InspectorName("적 공격력")]
        [SerializeField] private int enemyAttackDamage = 20;
        [InspectorName("적 공격 주기(유효 수식 턴)")]
        [SerializeField] private int enemyAttackEveryValidTurns = 2;
        [InspectorName("적 공격 딜레이(초)")]
        [SerializeField] private float enemyAttackDelaySeconds = 0.35f;
        [InspectorName("피격 카메라 흔들림 시간(초)")]
        [SerializeField] private float enemyAttackShakeDuration = 0.18f;
        [InspectorName("피격 카메라 Y회전 강도")]
        [SerializeField] private float enemyAttackShakeRotationStrength = 12f;
        [InspectorName("실드 환산 비율")]
        [SerializeField] private float shieldConversionRate = 1f;

        [Header("퍼즐 생성 확률")]
        [InspectorName("기본 숫자 비율")]
        [SerializeField] private int defaultNumberSpawnRatio = 85;
        [InspectorName("기본 연산자 비율")]
        [SerializeField] private int defaultOperatorSpawnRatio = 15;

        [Header("수식")]
        [InspectorName("최소 수식 길이")]
        [SerializeField] private int minExpressionLength = 3;
        [InspectorName("최대 수식 길이")]
        [SerializeField] private int maxExpressionLength = 5;

        [Header("공통 UI")]
        [InspectorName("UI 폰트")]
        [SerializeField] private TMP_FontAsset uiFont;
        [FormerlySerializedAs("uiFontSizeScale")]
        [InspectorName("상점 폰트 크기 배율")]
        [SerializeField] private float shopFontSizeScale = 1.25f;
        [InspectorName("타일 폰트 크기 배율")]
        [SerializeField] private float tileFontSizeScale = 1.25f;
        [InspectorName("타일 크기 배율")]
        [SerializeField] private float tileSizeScale = 1f;

        [Header("상점 UI")]
        [InspectorName("상점 패널 글자 색상")]
        [SerializeField] private Color shopPanelTextColor = Color.black;
        [InspectorName("상점 버튼 글자 색상")]
        [SerializeField] private Color shopButtonTextColor = Color.black;
        [InspectorName("상점 메인 패널 색상")]
        [SerializeField] private Color shopMainPanelColor = new(0.12f, 0.12f, 0.12f, 0.95f);
        [InspectorName("상점 배경 어둡기 색상")]
        [SerializeField] private Color shopDimColor = new(0f, 0f, 0f, 0.75f);
        [InspectorName("상점 메인 패널 이미지")]
        [SerializeField] private Sprite shopMainPanelSprite;
        [FormerlySerializedAs("shopPanelSide")]
        [InspectorName("상점 메인 패널 한 변 길이")]
        [SerializeField] private float shopMainPanelSide = 860f;
        [InspectorName("상점 구매 확인 패널 색상")]
        [SerializeField] private Color shopConfirmPanelColor = new(0.08f, 0.08f, 0.08f, 0.97f);
        [InspectorName("상점 구매 확인 배경 어둡기 색상")]
        [SerializeField] private Color shopConfirmDimColor = new(0f, 0f, 0f, 0.55f);
        [InspectorName("상점 구매 확인 패널 이미지")]
        [SerializeField] private Sprite shopConfirmPanelSprite;
        [InspectorName("상점 구매 확인 패널 한 변 길이")]
        [SerializeField] private float shopConfirmPanelSide = 620f;
        [InspectorName("상점 슬롯 버튼 한 변 길이")]
        [SerializeField] private float shopSlotButtonSide = 230f;
        [InspectorName("상점 무료 슬롯 버튼 이미지 설정")]
        [SerializeField] private ButtonArtworkStyle shopFreeSlotButtonStyle;
        [InspectorName("상점 유료 슬롯 버튼 이미지 설정")]
        [SerializeField] private ButtonArtworkStyle shopPaidSlotButtonStyle;
        [FormerlySerializedAs("actionButtonHeight")]
        [InspectorName("상점 메인 버튼 가로")]
        [SerializeField] private float shopMainActionButtonWidth = 260f;
        [InspectorName("상점 메인 버튼 세로")]
        [SerializeField] private float shopMainActionButtonHeight = 110f;
        [InspectorName("상점 구매 확인 버튼 가로")]
        [SerializeField] private float shopConfirmActionButtonWidth = 260f;
        [InspectorName("상점 구매 확인 버튼 세로")]
        [SerializeField] private float shopConfirmActionButtonHeight = 110f;

        [Header("시작 유니크 UI")]
        [InspectorName("시작 유니크 패널 글자 색상")]
        [SerializeField] private Color startingUniquePanelTextColor = Color.black;
        [InspectorName("시작 유니크 버튼 글자 색상")]
        [SerializeField] private Color startingUniqueButtonTextColor = Color.black;
        [InspectorName("시작 유니크 메인 패널 색상")]
        [SerializeField] private Color startingUniqueMainPanelColor = new(0.10f, 0.10f, 0.10f, 0.97f);
        [InspectorName("시작 유니크 메인 패널 이미지")]
        [SerializeField] private Sprite startingUniqueMainPanelSprite;
        [InspectorName("시작 유니크 선택 버튼 가로")]
        [SerializeField] private float startingUniqueButtonWidth = 280f;
        [InspectorName("시작 유니크 선택 버튼 세로")]
        [SerializeField] private float startingUniqueButtonHeight = 220f;
        [InspectorName("시작 유니크 선택 버튼 이미지 설정")]
        [SerializeField] private ButtonArtworkStyle startingUniqueSelectionButtonStyle;
        [InspectorName("시작 유니크 확인 패널 색상")]
        [SerializeField] private Color startingUniqueConfirmPanelColor = new(0.08f, 0.08f, 0.08f, 0.98f);
        [InspectorName("시작 유니크 확인 패널 이미지")]
        [SerializeField] private Sprite startingUniqueConfirmPanelSprite;
        [InspectorName("시작 유니크 확인 패널 한 변 길이")]
        [SerializeField] private float startingUniqueConfirmPanelSide = 620f;
        [InspectorName("시작 유니크 확인 버튼 가로")]
        [SerializeField] private float startingUniqueConfirmActionButtonWidth = 260f;
        [InspectorName("시작 유니크 확인 버튼 세로")]
        [SerializeField] private float startingUniqueConfirmActionButtonHeight = 110f;

        [Header("전투 UI")]
        [InspectorName("전투 버튼 가로")]
        [SerializeField] private float battleActionButtonWidth = 260f;
        [InspectorName("전투 버튼 세로")]
        [SerializeField] private float battleActionButtonHeight = 110f;
        [InspectorName("가방 배경 어둡기 색상")]
        [SerializeField] private Color bagDimColor = new(0f, 0f, 0f, 0.72f);
        [InspectorName("확률표 배경 어둡기 색상")]
        [SerializeField] private Color percentageDimColor = new(0f, 0f, 0f, 0.72f);


        [Serializable]
        public struct ButtonArtworkStyle
        {
            [InspectorName("버튼 배경 이미지")]
            public Sprite BackgroundSprite;
            [InspectorName("버튼 배경 색상")]
            public Color BackgroundColor;
            [InspectorName("전면 이미지")]
            public Sprite ContentSprite;
            [InspectorName("전면 이미지 색상")]
            public Color ContentColor;
            [InspectorName("전면 이미지 크기")]
            public Vector2 ContentSize;
            [InspectorName("전면 이미지 있을 때 텍스트 숨김")]
            public bool HideLabelWhenContentSpriteAssigned;
        }

        [Header("자동 줄 제거 안내")]
        [Tooltip("플레이 도중 새로 생성된 타일 때문에 자동 줄 제거가 발생했을 때, 제거 전에 보여줄 시간입니다.")]
        [InspectorName("자동 줄 제거 미리보기 시간")]
        [SerializeField] private float autoLineClearPreviewSeconds = 0.8f;
        [Tooltip("숫자 줄 제거 시 선택 이미지가 다음 타일로 넘어가기까지의 간격입니다.")]
        [InspectorName("줄 제거 순차 선택 간격")]
        [SerializeField] private float autoLineClearSequentialSelectionInterval = 0.08f;
        [Tooltip("연산자 타일만으로 한 줄이 제거될 때 적에게 주는 고정 피해입니다.")]
        [InspectorName("연산자 줄 제거 고정 피해")]
        [SerializeField] private int operatorLineClearFixedDamage = 50;

        [Header("타일 낙하 애니메이션")]
        [Tooltip("타일이 위에서 아래로 떨어질 때 걸리는 시간입니다.")]
        [InspectorName("낙하 시간")]
        [SerializeField] private float tileFallDuration = 0.22f;
        [Tooltip("타일이 떨어질 때 적용되는 움직임 곡선입니다.")]
        [InspectorName("낙하 이징")]
        [SerializeField] private Ease tileFallEase = Ease.OutQuad;
        [Tooltip("타일이 착지할 때 얼마나 튀어 오를지 정합니다.")]
        [InspectorName("착지 바운스 높이")]
        [SerializeField] private float tileLandingBounceOffset = 18f;
        [Tooltip("착지 후 튕기는 애니메이션 시간입니다.")]
        [InspectorName("착지 바운스 시간")]
        [SerializeField] private float tileLandingBounceDuration = 0.18f;
        [Tooltip("착지 후 흔들리듯 튀는 횟수입니다.")]
        [InspectorName("착지 바운스 횟수")]
        [SerializeField] private int tileLandingBounceFrequency = 2;
        [Tooltip("바운스가 얼마나 빨리 잦아들지 정하는 값입니다. 0에 가까울수록 빨리 멈춥니다.")]
        [InspectorName("착지 바운스 감쇠 비율")]
        [SerializeField] [Range(0f, 1f)] private float tileLandingBounceDampingRatio = 0.65f;

        [Serializable]
        public struct WeightedNumber
        {
            [Tooltip("보드에 생성될 숫자 값입니다.")]
            [InspectorName("숫자")]
            public int Value;
            [Tooltip("숫자가 등장할 상대적 비율입니다. 높을수록 더 자주 나옵니다.")]
            [InspectorName("가중치")]
            public int Weight;
        }

        [Serializable]
        public struct WeightedOperator
        {
            [Tooltip("보드에 생성될 연산자 종류입니다.")]
            [InspectorName("연산자")]
            public OperatorType Value;
            [Tooltip("연산자가 등장할 상대적 비율입니다. 높을수록 더 자주 나옵니다.")]
            [InspectorName("가중치")]
            public int Weight;
        }

        [Serializable]
        public struct NumberTileSpriteEntry
        {
            [Tooltip("적용할 숫자입니다.")]
            [InspectorName("숫자")]
            public int Value;
            [Tooltip("기본 상태에서 표시할 스프라이트입니다.")]
            [InspectorName("기본 이미지")]
            public Sprite NormalSprite;
            [Tooltip("선택(활성화) 상태에서 표시할 스프라이트입니다. 비워두면 기본 이미지를 사용합니다.")]
            [InspectorName("선택 이미지")]
            public Sprite SelectedSprite;
        }

        [Serializable]
        public struct OperatorTileSpriteEntry
        {
            [Tooltip("적용할 연산자입니다.")]
            [InspectorName("연산자")]
            public OperatorType Value;
            [Tooltip("기본 상태에서 표시할 스프라이트입니다.")]
            [InspectorName("기본 이미지")]
            public Sprite NormalSprite;
            [Tooltip("선택(활성화) 상태에서 표시할 스프라이트입니다. 비워두면 기본 이미지를 사용합니다.")]
            [InspectorName("선택 이미지")]
            public Sprite SelectedSprite;
        }

        [Header("숫자 타일 가중치")]
        [Tooltip("숫자 타일이 생성될 때 어떤 숫자가 얼마나 자주 나올지 정합니다.")]
        [SerializeField] private List<WeightedNumber> numberWeights = new()
        {
            new() { Value = 1, Weight = 20 }, new() { Value = 2, Weight = 20 }, new() { Value = 3, Weight = 20 },
            new() { Value = 4, Weight = 20 }, new() { Value = 5, Weight = 9 }, new() { Value = 6, Weight = 5 },
            new() { Value = 7, Weight = 3 }, new() { Value = 8, Weight = 2 }, new() { Value = 9, Weight = 1 },
        };

        [Header("연산자 타일 가중치")]
        [Tooltip("연산자 타일이 생성될 때 어떤 연산자가 얼마나 자주 나올지 정합니다.")]
        [SerializeField] private List<WeightedOperator> operatorWeights = new()
        {
            new() { Value = OperatorType.Add, Weight = 45 },
            new() { Value = OperatorType.Subtract, Weight = 25 },
            new() { Value = OperatorType.Multiply, Weight = 20 },
            new() { Value = OperatorType.Divide, Weight = 10 },
        };

        [Header("타일 이미지")]
        [Tooltip("숫자 타일별 기본/선택 이미지를 Inspector에서 연결합니다.")]
        [SerializeField] private List<NumberTileSpriteEntry> numberTileSprites = new()
        {
            new() { Value = 1 }, new() { Value = 2 }, new() { Value = 3 },
            new() { Value = 4 }, new() { Value = 5 }, new() { Value = 6 },
            new() { Value = 7 }, new() { Value = 8 }, new() { Value = 9 },
        };
        [Tooltip("연산자 타일별 기본/선택 이미지를 Inspector에서 연결합니다.")]
        [SerializeField] private List<OperatorTileSpriteEntry> operatorTileSprites = new()
        {
            new() { Value = OperatorType.Add },
            new() { Value = OperatorType.Subtract },
            new() { Value = OperatorType.Multiply },
            new() { Value = OperatorType.Divide },
        };
        [Tooltip("타일 이미지가 있을 때 텍스트를 함께 표시할지 여부입니다.")]
        [InspectorName("이미지 있을 때 텍스트 표시")]
        [SerializeField] private bool showTileLabelWhenSpriteAssigned;

        public int Columns => columns;
        public int Rows => rows;
        public Color BoardBackgroundColor => boardBackgroundColor;
        public Sprite BoardBackgroundSprite => boardBackgroundSprite;
        public Color BoardBackgroundSpriteTint => boardBackgroundSpriteTint;
        public int PlayerMaxHp => playerMaxHp;
        public int EnemyMaxHp => enemyMaxHp;
        public int EnemyAttackDamage => enemyAttackDamage;
        public int EnemyAttackEveryValidTurns => Mathf.Max(1, enemyAttackEveryValidTurns);
        public float EnemyAttackDelaySeconds => Mathf.Max(0f, enemyAttackDelaySeconds);
        public float EnemyAttackShakeDuration => Mathf.Max(0f, enemyAttackShakeDuration);
        public float EnemyAttackShakeRotationStrength => Mathf.Max(0f, enemyAttackShakeRotationStrength);
        public float ShieldConversionRate => Mathf.Max(0f, shieldConversionRate);
        public int DefaultNumberSpawnRatio => Mathf.Max(0, defaultNumberSpawnRatio);
        public int DefaultOperatorSpawnRatio => Mathf.Max(0, defaultOperatorSpawnRatio);
        public int MinExpressionLength => minExpressionLength;
        public int MaxExpressionLength => maxExpressionLength;
        public TMP_FontAsset UiFont => uiFont;
        public float ShopFontSizeScale => Mathf.Max(0.5f, shopFontSizeScale);
        public float TileFontSizeScale => Mathf.Max(0.5f, tileFontSizeScale);
        public float TileSizeScale => Mathf.Max(0.6f, tileSizeScale);
        public Color ShopPanelTextColor => shopPanelTextColor;
        public Color ShopButtonTextColor => shopButtonTextColor;
        public Color ShopMainPanelColor => shopMainPanelColor;
        public Color ShopDimColor => shopDimColor;
        public Sprite ShopMainPanelSprite => shopMainPanelSprite;
        public float ShopMainPanelSide => Mathf.Max(360f, shopMainPanelSide);
        public Color ShopConfirmPanelColor => shopConfirmPanelColor;
        public Color ShopConfirmDimColor => shopConfirmDimColor;
        public Sprite ShopConfirmPanelSprite => shopConfirmPanelSprite;
        public float ShopConfirmPanelSide => Mathf.Max(280f, shopConfirmPanelSide);
        public float ShopSlotButtonSide => Mathf.Max(120f, shopSlotButtonSide);
        public ButtonArtworkStyle ShopFreeSlotButtonStyle => shopFreeSlotButtonStyle;
        public ButtonArtworkStyle ShopPaidSlotButtonStyle => shopPaidSlotButtonStyle;
        public Color StartingUniquePanelTextColor => startingUniquePanelTextColor;
        public Color StartingUniqueButtonTextColor => startingUniqueButtonTextColor;
        public Color StartingUniqueMainPanelColor => startingUniqueMainPanelColor;
        public Sprite StartingUniqueMainPanelSprite => startingUniqueMainPanelSprite;
        public float StartingUniqueButtonHeight => Mathf.Max(64f, startingUniqueButtonHeight);
        public float StartingUniqueButtonWidth => Mathf.Max(120f, startingUniqueButtonWidth);
        public ButtonArtworkStyle StartingUniqueSelectionButtonStyle => startingUniqueSelectionButtonStyle;
        public Color StartingUniqueConfirmPanelColor => startingUniqueConfirmPanelColor;
        public Sprite StartingUniqueConfirmPanelSprite => startingUniqueConfirmPanelSprite;
        public float StartingUniqueConfirmPanelSide => Mathf.Max(280f, startingUniqueConfirmPanelSide);
        public float StartingUniqueConfirmActionButtonHeight => Mathf.Max(64f, startingUniqueConfirmActionButtonHeight);
        public float StartingUniqueConfirmActionButtonWidth => Mathf.Max(120f, startingUniqueConfirmActionButtonWidth);
        public float ShopMainActionButtonHeight => Mathf.Max(64f, shopMainActionButtonHeight);
        public float ShopMainActionButtonWidth => Mathf.Max(120f, shopMainActionButtonWidth);
        public float ShopConfirmActionButtonHeight => Mathf.Max(64f, shopConfirmActionButtonHeight);
        public float ShopConfirmActionButtonWidth => Mathf.Max(120f, shopConfirmActionButtonWidth);
        public float BattleActionButtonHeight => Mathf.Max(64f, battleActionButtonHeight);
        public float BattleActionButtonWidth => Mathf.Max(120f, battleActionButtonWidth);
        public Color BagDimColor => bagDimColor;
        public Color PercentageDimColor => percentageDimColor;
        public float AutoLineClearPreviewSeconds => Mathf.Max(0f, autoLineClearPreviewSeconds);
        public float AutoLineClearSequentialSelectionInterval => Mathf.Max(0f, autoLineClearSequentialSelectionInterval);
        public int OperatorLineClearFixedDamage => Mathf.Max(0, operatorLineClearFixedDamage);
        public float TileFallDuration => tileFallDuration;
        public Ease TileFallEase => tileFallEase;
        public float TileLandingBounceOffset => tileLandingBounceOffset;
        public float TileLandingBounceDuration => tileLandingBounceDuration;
        public int TileLandingBounceFrequency => tileLandingBounceFrequency;
        public float TileLandingBounceDampingRatio => tileLandingBounceDampingRatio;
        public IReadOnlyList<WeightedNumber> NumberWeights => numberWeights;
        public IReadOnlyList<WeightedOperator> OperatorWeights => operatorWeights;
        public IReadOnlyList<NumberTileSpriteEntry> NumberTileSprites => numberTileSprites;
        public IReadOnlyList<OperatorTileSpriteEntry> OperatorTileSprites => operatorTileSprites;
        public bool ShowTileLabelWhenSpriteAssigned => showTileLabelWhenSpriteAssigned;
    }
}
