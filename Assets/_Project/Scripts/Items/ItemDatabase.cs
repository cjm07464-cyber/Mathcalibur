using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mathcalibur.Items
{
    public sealed class ItemDatabase
    {
        private const string DefaultResourcePath = "MathcaliburItems";

        private readonly List<ItemData> _items = new();
        private readonly Dictionary<string, ItemData> _itemsById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _intConfigs = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _boolConfigs = new(StringComparer.Ordinal);

        public IReadOnlyList<ItemData> Items => _items;

        public static ItemDatabase LoadDefault()
        {
            var asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
            {
                Debug.LogError($"ItemDatabase missing resource at Resources/{DefaultResourcePath}.json");
                return new ItemDatabase();
            }

            return FromJson(asset.text);
        }

        public static ItemDatabase FromJson(string json)
        {
            var database = new ItemDatabase();
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("ItemDatabase JSON is empty.");
                return database;
            }

            ItemDatabaseDocument document;
            try
            {
                document = JsonUtility.FromJson<ItemDatabaseDocument>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to parse ItemDatabase JSON: {exception.Message}");
                return database;
            }

            if (document == null)
            {
                Debug.LogError("ItemDatabase JSON produced null document.");
                return database;
            }

            foreach (var entry in document.intConfigs ?? Array.Empty<ConfigIntEntry>())
            {
                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    Debug.LogError("ItemDatabase intConfig has empty key.");
                    continue;
                }

                database._intConfigs[entry.key] = entry.value;
            }

            foreach (var entry in document.boolConfigs ?? Array.Empty<ConfigBoolEntry>())
            {
                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    Debug.LogError("ItemDatabase boolConfig has empty key.");
                    continue;
                }

                database._boolConfigs[entry.key] = entry.value;
            }

            foreach (var item in document.items ?? Array.Empty<ItemData>())
            {
                database.RegisterItem(item);
            }

            return database;
        }

        public bool TryGetItem(string itemId, out ItemData item)
        {
            return _itemsById.TryGetValue(itemId, out item);
        }

        public int GetIntConfig(string key)
        {
            if (_intConfigs.TryGetValue(key, out var value))
            {
                return value;
            }

            Debug.LogError($"Missing int config key: {key}");
            return 0;
        }

        public bool GetBoolConfig(string key, bool fallback = false)
        {
            return _boolConfigs.TryGetValue(key, out var value) ? value : fallback;
        }

        public int ResolvePrice(ItemData item)
        {
            return GetIntConfig(item.priceConfigKey);
        }

        public int ResolveEffectInt(ItemData item, string parameterKey)
        {
            if (!item.TryGetParameter(parameterKey, out var configKey))
            {
                Debug.LogError($"Missing parameter {parameterKey} for item {item.itemId}");
                return 0;
            }

            return GetIntConfig(configKey);
        }

        public int GetPaidRarityWeight(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => GetIntConfig("TEMP_PAID_RARITY_COMMON_WEIGHT"),
                ItemRarity.Rare => GetIntConfig("TEMP_PAID_RARITY_RARE_WEIGHT"),
                ItemRarity.Legendary => GetIntConfig("TEMP_PAID_RARITY_LEGENDARY_WEIGHT"),
                _ => 0,
            };
        }

        public int GetActiveItemStackLimit(ItemData item)
        {
            return GetIntConfig("TEMP_DEFAULT_ACTIVE_ITEM_MAX_STACK");
        }

        private void RegisterItem(ItemData item)
        {
            if (item == null)
            {
                Debug.LogError("Null item entry in item database.");
                return;
            }

            if (string.IsNullOrWhiteSpace(item.itemId))
            {
                item.Disable("empty_itemId");
                Debug.LogError("ItemDatabase item has empty itemId.");
                _items.Add(item);
                return;
            }

            if (_itemsById.ContainsKey(item.itemId))
            {
                item.Disable("duplicate_itemId");
                Debug.LogError($"Duplicate itemId in item database: {item.itemId}");
                _items.Add(item);
                return;
            }

            if (!TryParseCategory(item.itemCategory, out var category))
            {
                item.Disable("invalid_itemCategory");
                Debug.LogError($"Invalid itemCategory for {item.itemId}: {item.itemCategory}");
                _items.Add(item);
                return;
            }

            if (!TryParseRarity(item.rarity, out var rarity))
            {
                item.Disable("invalid_rarity");
                Debug.LogError($"Invalid rarity for {item.itemId}: {item.rarity}");
                _items.Add(item);
                return;
            }

            if (!TryParseEffectType(item.effectType, out var effectType))
            {
                item.Disable("invalid_effectType");
                Debug.LogError($"Invalid effectType for {item.itemId}: {item.effectType}");
                _items.Add(item);
                return;
            }

            if (string.IsNullOrWhiteSpace(item.priceConfigKey) || !_intConfigs.ContainsKey(item.priceConfigKey))
            {
                item.Disable("invalid_priceConfigKey");
                Debug.LogError($"Invalid priceConfigKey for {item.itemId}: {item.priceConfigKey}");
                _items.Add(item);
                return;
            }

            if (item.unlockStage < 0)
            {
                item.Disable("invalid_unlockStage");
                Debug.LogError($"Invalid unlockStage for {item.itemId}: {item.unlockStage}");
                _items.Add(item);
                return;
            }

            var modifiers = new List<SpawnWeightModifier>();
            var parameters = new List<EffectParameter>();
            if (!TryParseEffectPayload(item, effectType, modifiers, parameters))
            {
                _items.Add(item);
                return;
            }

            item.SetParsed(category, rarity, effectType, modifiers, parameters);
            _items.Add(item);
            _itemsById[item.itemId] = item;
        }

        private bool TryParseEffectPayload(ItemData item, ItemEffectType effectType, List<SpawnWeightModifier> modifiers, List<EffectParameter> parameters)
        {
            var payload = item.effectPayload ?? string.Empty;
            if (effectType == ItemEffectType.ModifySpawnWeights)
            {
                foreach (var clause in SplitAndTrim(payload, ';'))
                {
                    var parts = SplitPairAndTrim(clause, ':');
                    if (parts.Length != 2)
                    {
                        item.Disable("invalid_effectPayload");
                        Debug.LogError($"Invalid modifier clause for {item.itemId}: {clause}");
                        return false;
                    }

                    var modifierConfigKey = parts[1];
                    if (!_intConfigs.ContainsKey(modifierConfigKey))
                    {
                        item.Disable("invalid_modifierConfigKey");
                        Debug.LogError($"Unknown modifierConfigKey for {item.itemId}: {modifierConfigKey}");
                        return false;
                    }

                    if (!TryParseTargets(parts[0], modifierConfigKey, modifiers))
                    {
                        item.Disable("invalid_targetValue");
                        Debug.LogError($"Invalid target definition for {item.itemId}: {parts[0]}");
                        return false;
                    }
                }

                return true;
            }

            foreach (var clause in SplitAndTrim(payload, ';'))
            {
                var parts = SplitPairAndTrim(clause, ':');
                if (parts.Length != 2)
                {
                    item.Disable("invalid_effectPayload");
                    Debug.LogError($"Invalid effect payload for {item.itemId}: {clause}");
                    return false;
                }

                if (!_intConfigs.ContainsKey(parts[1]))
                {
                    item.Disable("invalid_effectPayload");
                    Debug.LogError($"Unknown config key in payload for {item.itemId}: {parts[1]}");
                    return false;
                }

                parameters.Add(new EffectParameter { key = parts[0], value = parts[1] });
            }

            return true;
        }

        private static bool TryParseTargets(string lhs, string modifierConfigKey, List<SpawnWeightModifier> output)
        {
            var normalized = lhs.Trim();
            SpawnTargetType targetType;
            string valueText;
            if (normalized.StartsWith("number ", StringComparison.Ordinal))
            {
                targetType = SpawnTargetType.Number;
                valueText = normalized[7..];
            }
            else if (normalized.StartsWith("numbers ", StringComparison.Ordinal))
            {
                targetType = SpawnTargetType.Number;
                valueText = normalized[8..];
            }
            else if (normalized.StartsWith("operator ", StringComparison.Ordinal))
            {
                targetType = SpawnTargetType.Operator;
                valueText = normalized[9..];
            }
            else if (normalized.StartsWith("operators ", StringComparison.Ordinal))
            {
                targetType = SpawnTargetType.Operator;
                valueText = normalized[10..];
            }
            else
            {
                return false;
            }

            foreach (var rawValue in SplitAndTrim(valueText, ','))
            {
                var value = targetType == SpawnTargetType.Operator ? CanonicalizeOperator(rawValue) : rawValue.Trim();
                if (targetType == SpawnTargetType.Number)
                {
                    if (!int.TryParse(value, out _))
                    {
                        return false;
                    }
                }
                else if (value is not ("+" or "-" or "x" or "÷"))
                {
                    return false;
                }

                output.Add(new SpawnWeightModifier
                {
                    targetType = targetType,
                    targetValue = value,
                    modifierConfigKey = modifierConfigKey,
                });
            }

            return true;
        }

        public static string CanonicalizeOperator(string raw)
        {
            return raw.Trim() switch
            {
                "*" => "x",
                "×" => "x",
                "/" => "÷",
                _ => raw.Trim(),
            };
        }

        private static bool TryParseCategory(string raw, out ItemCategory category)
        {
            return Enum.TryParse(raw, ignoreCase: false, out category)
                && Enum.IsDefined(typeof(ItemCategory), category);
        }

        private static bool TryParseRarity(string raw, out ItemRarity rarity)
        {
            return Enum.TryParse(raw, ignoreCase: false, out rarity)
                && Enum.IsDefined(typeof(ItemRarity), rarity);
        }

        private static bool TryParseEffectType(string raw, out ItemEffectType effectType)
        {
            return Enum.TryParse(raw, ignoreCase: false, out effectType)
                && Enum.IsDefined(typeof(ItemEffectType), effectType);
        }

        private static string[] SplitAndTrim(string value, char separator)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            return value
                .Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
                .ToArray();
        }

        private static string[] SplitPairAndTrim(string value, char separator)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            var parts = value.Split(new[] { separator }, 2, StringSplitOptions.None);
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }

            return parts;
        }

        [Serializable]
        private sealed class ItemDatabaseDocument
        {
            public ConfigIntEntry[] intConfigs;
            public ConfigBoolEntry[] boolConfigs;
            public ItemData[] items;
        }

        [Serializable]
        private sealed class ConfigIntEntry
        {
            public string key;
            public int value;
        }

        [Serializable]
        private sealed class ConfigBoolEntry
        {
            public string key;
            public bool value;
        }
    }
}
