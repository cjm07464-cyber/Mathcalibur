using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mathcalibur.Battle
{
    public class BattleHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text defenseText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text expressionText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text validationSymbolText;
        [SerializeField] private TMP_Text validationLabelText;
        [SerializeField] private Color validColor = Color.green;
        [SerializeField] private Color invalidColor = Color.red;
        [SerializeField] private Image enemyHpBarImage;

        private MotionHandle _enemyHpBarMotionHandle;
        private int _lastEnemyHpMax = 1;

        public void SetHp(int playerHp, int shield, int enemyHp, int enemyHpMax)
        {
            if (playerHpText != null)
            {
                playerHpText.text = $"{playerHp}";
            }

            if (defenseText != null)
            {
                defenseText.text = $"{shield}";
            }

            if (enemyHpText != null)
            {
                enemyHpText.text = $"{enemyHp}";
            }

            SetEnemyHpBar(enemyHp, enemyHpMax);
        }

        public void SetCountdown(int turnsLeft)
        {
            if (countdownText != null)
            {
                countdownText.text = $"{turnsLeft}";
            }
        }

        public void SetExpression(string value)
        {
            if (expressionText != null)
            {
                expressionText.text = value ?? string.Empty;
            }
        }

        public void SetResult(string value)
        {
            if (resultText != null)
            {
                resultText.text = value ?? string.Empty;
            }
        }

        public void SetValidationStatus(bool? isValid)
        {
            if (!isValid.HasValue)
            {
                if (validationSymbolText != null)
                {
                    validationSymbolText.text = string.Empty;
                }

                if (validationLabelText != null)
                {
                    validationLabelText.text = string.Empty;
                }

                return;
            }

            var color = isValid.Value ? validColor : invalidColor;
            if (validationSymbolText != null)
            {
                validationSymbolText.text = isValid.Value ? "O" : "X";
                validationSymbolText.color = color;
            }

            if (validationLabelText != null)
            {
                validationLabelText.text = isValid.Value ? "Valid" : "Invalid";
                validationLabelText.color = color;
            }
        }

        public void SetMessage(string value)
        {
        }

        private void SetEnemyHpBar(int enemyHp, int enemyHpMax)
        {
            if (enemyHpBarImage == null)
            {
                return;
            }

            enemyHpBarImage.type = Image.Type.Filled;
            enemyHpBarImage.fillMethod = Image.FillMethod.Horizontal;
            enemyHpBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            enemyHpMax = Mathf.Max(1, enemyHpMax);
            var targetFill = Mathf.Clamp01(enemyHp / (float)enemyHpMax);
            var currentFill = enemyHpBarImage.fillAmount;

            if (_enemyHpBarMotionHandle.IsActive())
            {
                _enemyHpBarMotionHandle.Cancel();
            }

            if (_lastEnemyHpMax != enemyHpMax || Mathf.Approximately(currentFill, 0f) && enemyHp == enemyHpMax)
            {
                enemyHpBarImage.fillAmount = targetFill;
                _lastEnemyHpMax = enemyHpMax;
                return;
            }

            _enemyHpBarMotionHandle = LMotion.Create(currentFill, targetFill, 0.2f)
                .BindToFillAmount(enemyHpBarImage)
                .AddTo(this);
            _lastEnemyHpMax = enemyHpMax;
        }
    }
}
