using System;

namespace Mathcalibur.Items
{
    public interface IItemEffectRuntime
    {
        bool CanUseActiveItem(ItemData item, out string reason);
        void AddSpawnWeightModifier(SpawnWeightModifier modifier, int deltaValue);
        void RebuildCachedSpawnWeights();
        void IncreaseConnectionLimit(int amount);
        void IncreasePlayerMaxHpAndCurrentHp(int amount);
        void HealPlayer(int amount);
    }

    public sealed class ItemEffectResolver
    {
        public void ApplyAcquiredItem(ItemData item, RuntimeItemInventory inventory, ItemDatabase database, IItemEffectRuntime runtime)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            inventory.RegisterAcquisition(item);

            switch (item.Category)
            {
                case ItemCategory.ActiveItem:
                    return;
                case ItemCategory.BoardDeckUpgrade:
                    ApplySpawnWeightModifiers(item, database, runtime);
                    return;
                case ItemCategory.ConnectionLimitUpgrade:
                    runtime.IncreaseConnectionLimit(database.ResolveEffectInt(item, "increaseValue"));
                    return;
                case ItemCategory.PassiveItem:
                    runtime.IncreasePlayerMaxHpAndCurrentHp(database.ResolveEffectInt(item, "increaseValue"));
                    return;
                case ItemCategory.UniqueItem:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool TryUseActiveItem(string itemId, RuntimeItemInventory inventory, ItemDatabase database, IItemEffectRuntime runtime, out string message)
        {
            message = string.Empty;
            if (!database.TryGetItem(itemId, out var item) || item.Category != ItemCategory.ActiveItem)
            {
                message = "Unknown active item.";
                return false;
            }

            if (inventory.GetActiveItemCount(itemId) <= 0)
            {
                message = $"{item.displayName} is empty.";
                return false;
            }

            if (!runtime.CanUseActiveItem(item, out var reason))
            {
                message = reason;
                return false;
            }

            switch (item.EffectType)
            {
                case ItemEffectType.HealPlayer:
                    runtime.HealPlayer(database.ResolveEffectInt(item, "healAmount"));
                    inventory.ConsumeActiveItem(itemId);
                    message = $"Used {item.displayName}.";
                    return true;

                case ItemEffectType.SetNextAttackMultiplier:
                    if (inventory.HasPendingAttackMultiplier())
                    {
                        message = "Attack Potion is already armed.";
                        return false;
                    }

                    inventory.SetPendingAttackMultiplierPercent(database.ResolveEffectInt(item, "multiplierPercent"));
                    inventory.ConsumeActiveItem(itemId);
                    message = $"Used {item.displayName}.";
                    return true;

                default:
                    message = $"Unsupported active effect: {item.EffectType}";
                    return false;
            }
        }

        private static void ApplySpawnWeightModifiers(ItemData item, ItemDatabase database, IItemEffectRuntime runtime)
        {
            foreach (var modifier in item.Modifiers)
            {
                runtime.AddSpawnWeightModifier(modifier, database.GetIntConfig(modifier.modifierConfigKey));
            }

            runtime.RebuildCachedSpawnWeights();
        }
    }
}
