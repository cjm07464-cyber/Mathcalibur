using TMPro;
using UnityEngine;

namespace Mathcalibur.Battle
{
    public class BattleHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text expressionText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text messageText;

        public void SetHp(int playerHp, int enemyHp)
        {
            playerHpText.text = $"Player HP: {playerHp}";
            enemyHpText.text = $"Enemy HP: {enemyHp}";
        }

        public void SetCountdown(int turnsLeft) => countdownText.text = $"Enemy Attack In: {turnsLeft}";
        public void SetExpression(string value) => expressionText.text = $"Expression: {value}";
        public void SetResult(string value) => resultText.text = $"Result: {value}";
        public void SetMessage(string value) => messageText.text = value;
    }
}
