using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mathcalibur.Battle
{
    public enum TileKind { Number, Operator }
    public enum OperatorType { Add, Subtract, Multiply, Divide }

    public class BattleTileView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color numberColor = new(0.75f, 0.9f, 1f);
        [SerializeField] private Color operatorColor = new(1f, 0.85f, 0.75f);
        [SerializeField] private Color selectedColor = new(1f, 1f, 0.4f);

        public int X { get; private set; }
        public int Y { get; private set; }
        public TileKind Kind { get; private set; }
        public int NumberValue { get; private set; }
        public OperatorType Operator { get; private set; }

        private Color _baseColor;
        private RectTransform _rectTransform;
        private MotionHandle _moveHandle;
        private MotionHandle _bounceHandle;
        private Sprite _defaultSprite;
        private Sprite _selectedSprite;
        private bool _hasAssignedSprite;
        private bool _showLabelWhenSpriteAssigned;
        private bool _isSelected;

        private RectTransform CachedRectTransform => _rectTransform != null ? _rectTransform : _rectTransform = GetComponent<RectTransform>();

        public void SetGridPos(int x, int y) { X = x; Y = y; }

        public void SetBoardVisualLayout(Vector2 size, Vector2 anchoredPosition)
        {
            var rt = CachedRectTransform;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        public void ConfigureSprites(Sprite defaultSprite, Sprite selectedSprite, bool showLabelWhenSpriteAssigned)
        {
            _defaultSprite = defaultSprite;
            _selectedSprite = selectedSprite;
            _showLabelWhenSpriteAssigned = showLabelWhenSpriteAssigned;
            _hasAssignedSprite = defaultSprite != null || selectedSprite != null;
            RefreshVisualState();
        }

        public void AnimateBoardFall(Vector2 startAnchoredPosition, Vector2 targetAnchoredPosition, Vector2 size, BattleConfig config)
        {
            var rt = CachedRectTransform;
            rt.sizeDelta = size;

            if (_moveHandle.IsActive())
            {
                _moveHandle.Cancel();
            }

            if (_bounceHandle.IsActive())
            {
                _bounceHandle.Cancel();
            }

            rt.anchoredPosition = startAnchoredPosition;

            if ((targetAnchoredPosition - startAnchoredPosition).sqrMagnitude <= 0.0001f)
            {
                rt.anchoredPosition = targetAnchoredPosition;
                return;
            }

            if (config == null || config.TileFallDuration <= 0f)
            {
                rt.anchoredPosition = targetAnchoredPosition;
                return;
            }

            _moveHandle = LMotion.Create(startAnchoredPosition, targetAnchoredPosition, config.TileFallDuration)
                .WithEase(config.TileFallEase)
                .WithOnComplete(() =>
                {
                    if (config.TileLandingBounceOffset <= 0f || config.TileLandingBounceDuration <= 0f)
                    {
                        rt.anchoredPosition = targetAnchoredPosition;
                        return;
                    }

                    _bounceHandle = LMotion.Punch.Create(targetAnchoredPosition, new Vector2(0f, config.TileLandingBounceOffset), config.TileLandingBounceDuration)
                        .WithFrequency(Mathf.Max(1, config.TileLandingBounceFrequency))
                        .WithDampingRatio(config.TileLandingBounceDampingRatio)
                        .BindToAnchoredPosition(rt)
                        .AddTo(this);
                })
                .BindToAnchoredPosition(rt)
                .AddTo(this);
        }

        public void SetNumber(int value)
        {
            Kind = TileKind.Number;
            NumberValue = value;
            label.text = value.ToString();
            _baseColor = numberColor;
            RefreshVisualState();
        }

        public void SetOperator(OperatorType type)
        {
            Kind = TileKind.Operator;
            Operator = type;
            label.text = type switch
            {
                OperatorType.Add => "+",
                OperatorType.Subtract => "-",
                OperatorType.Multiply => "x",
                OperatorType.Divide => "÷",
                _ => "?"
            };
            _baseColor = operatorColor;
            RefreshVisualState();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            if (background == null)
            {
                return;
            }

            var normalSprite = _defaultSprite != null ? _defaultSprite : _selectedSprite;
            var selectedSprite = _selectedSprite != null ? _selectedSprite : normalSprite;
            var spriteToUse = _isSelected ? selectedSprite : normalSprite;
            background.sprite = spriteToUse;
            background.type = Image.Type.Simple;
            background.preserveAspect = spriteToUse != null;
            background.color = spriteToUse != null ? Color.white : (_isSelected ? selectedColor : _baseColor);

            if (label != null)
            {
                label.enabled = !_hasAssignedSprite || _showLabelWhenSpriteAssigned;
            }
        }
    }
}
