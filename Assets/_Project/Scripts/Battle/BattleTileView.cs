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

        public void SetGridPos(int x, int y) { X = x; Y = y; }

        public void SetNumber(int value)
        {
            Kind = TileKind.Number;
            NumberValue = value;
            label.text = value.ToString();
            _baseColor = numberColor;
            background.color = _baseColor;
        }

        public void SetOperator(OperatorType type)
        {
            Kind = TileKind.Operator;
            Operator = type;
            label.text = type switch
            {
                OperatorType.Add => "+",
                OperatorType.Subtract => "-",
                OperatorType.Multiply => "*",
                OperatorType.Divide => "/",
                _ => "?"
            };
            _baseColor = operatorColor;
            background.color = _baseColor;
        }

        public void SetSelected(bool selected) => background.color = selected ? selectedColor : _baseColor;
    }
}
