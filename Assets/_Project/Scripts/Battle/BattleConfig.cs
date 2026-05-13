using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mathcalibur.Battle
{
    [CreateAssetMenu(menuName = "Mathcalibur/Battle Config", fileName = "BattleConfig")]
    public class BattleConfig : ScriptableObject
    {
        [Header("Board")]
        [SerializeField] private int columns = 5;
        [SerializeField] private int rows = 4;
        [SerializeField] private float boardWidth = 1000f;
        [SerializeField] private float boardHeight = 1200f;

        [Header("Combat")]
        [SerializeField] private int playerMaxHp = 100;
        [SerializeField] private int enemyMaxHp = 100;
        [SerializeField] private int enemyAttackDamage = 20;
        [SerializeField] private int enemyAttackEveryValidTurns = 2;

        [Header("Expression")]
        [SerializeField] private int minExpressionLength = 3;
        [SerializeField] private int maxExpressionLength = 5;

        [Serializable]
        public struct WeightedNumber { public int Value; public int Weight; }

        [SerializeField] private List<WeightedNumber> numberWeights = new()
        {
            new() { Value = 1, Weight = 20 }, new() { Value = 2, Weight = 20 }, new() { Value = 3, Weight = 20 },
            new() { Value = 4, Weight = 20 }, new() { Value = 5, Weight = 9 }, new() { Value = 6, Weight = 5 },
            new() { Value = 7, Weight = 3 }, new() { Value = 8, Weight = 2 }, new() { Value = 9, Weight = 1 },
        };

        public int Columns => columns;
        public int Rows => rows;
        public float BoardWidth => boardWidth;
        public float BoardHeight => boardHeight;
        public int PlayerMaxHp => playerMaxHp;
        public int EnemyMaxHp => enemyMaxHp;
        public int EnemyAttackDamage => enemyAttackDamage;
        public int EnemyAttackEveryValidTurns => enemyAttackEveryValidTurns;
        public int MinExpressionLength => minExpressionLength;
        public int MaxExpressionLength => maxExpressionLength;
        public IReadOnlyList<WeightedNumber> NumberWeights => numberWeights;
    }
}
