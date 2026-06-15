using System;
using System.Collections.Generic;
using System.Linq;

namespace Mathcalibur.Items
{
    public enum ItemCategory
    {
        ActiveItem,
        PassiveItem,
        BoardDeckUpgrade,
        ConnectionLimitUpgrade,
        UniqueItem,
    }

    public enum ItemRarity
    {
        Common,
        Rare,
        Legendary,
    }

    public enum ItemEffectType
    {
        ModifySpawnWeights,
        HealPlayer,
        SetNextAttackMultiplier,
        IncreaseConnectionLimit,
        IncreaseMaxHpAndCurrentHp,
        AccumulateOneThenTransformNextExpression,
        ProbabilityBonusDamage,
        TrinityThreeSixNine,
        BoardGenerationOverride,
        ShieldPerFiveInExpression,
        FlatStageClearGoldBonus,
        ExactLengthSevenBonusDamage,
        PercentStageClearGoldBonus,
        OdinBoardWideNineTransform,
    }

    public enum ItemSlotKind
    {
        Free,
        Paid,
        Unique,
    }

    public enum SpawnTargetType
    {
        Number,
        Operator,
    }

    [Serializable]
    public sealed class SpawnWeightModifier
    {
        public SpawnTargetType targetType;
        public string targetValue;
        public string modifierConfigKey;
    }

    [Serializable]
    public sealed class EffectParameter
    {
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class ItemData
    {
        public string itemId;
        public string displayName;
        public string itemCategory;
        public string rarity;
        public string priceConfigKey;
        public int maxAcquisitionsPerRun;
        public string effectType;
        public string effectPayload;
        public int unlockStage;
        public string uiDescriptionKo;
        public string implementationNote;

        [NonSerialized] private ItemCategory _category;
        [NonSerialized] private ItemRarity _rarity;
        [NonSerialized] private ItemEffectType _effectType;
        [NonSerialized] private bool _parsed;
        [NonSerialized] private bool _valid = true;
        [NonSerialized] private string _invalidReason = string.Empty;
        [NonSerialized] private List<SpawnWeightModifier> _modifiers = new();
        [NonSerialized] private List<EffectParameter> _parameters = new();

        public ItemCategory Category => _category;
        public ItemRarity Rarity => _rarity;
        public ItemEffectType EffectType => _effectType;
        public bool IsParsed => _parsed;
        public bool IsValid => _valid;
        public string InvalidReason => _invalidReason;
        public IReadOnlyList<SpawnWeightModifier> Modifiers => _modifiers;
        public IReadOnlyList<EffectParameter> Parameters => _parameters;
        public bool TargetsLockedOperators => _modifiers.Any(m => m.targetType == SpawnTargetType.Operator && (m.targetValue == "x" || m.targetValue == "÷"));

        public void SetParsed(ItemCategory category, ItemRarity rarity, ItemEffectType effectType, List<SpawnWeightModifier> modifiers, List<EffectParameter> parameters)
        {
            _category = category;
            _rarity = rarity;
            _effectType = effectType;
            _modifiers = modifiers ?? new List<SpawnWeightModifier>();
            _parameters = parameters ?? new List<EffectParameter>();
            _parsed = true;
        }

        public void Disable(string reason)
        {
            _valid = false;
            _invalidReason = reason ?? string.Empty;
        }

        public bool TryGetParameter(string key, out string value)
        {
            var entry = _parameters.FirstOrDefault(parameter => string.Equals(parameter.key, key, StringComparison.Ordinal));
            if (entry == null)
            {
                value = string.Empty;
                return false;
            }

            value = entry.value ?? string.Empty;
            return true;
        }
    }
}
