using System;

namespace Mathcalibur.Items
{
    public sealed class ItemEligibilityChecker
    {
        public bool IsEligible(ItemData item, ItemSlotKind slotKind, int upcomingStageNumber, RuntimeItemInventory inventory, ItemDatabase database, out string reason)
        {
            reason = string.Empty;
            if (item == null)
            {
                reason = "missing_item";
                return false;
            }

            if (!item.IsValid)
            {
                reason = item.InvalidReason == string.Empty ? "invalid_item" : item.InvalidReason;
                return false;
            }

            if (item.unlockStage > upcomingStageNumber)
            {
                reason = "unlock_stage";
                return false;
            }

            if (item.TargetsLockedOperators && upcomingStageNumber < 3)
            {
                reason = "operator_lock";
                return false;
            }

            if (!IsCategoryAllowed(slotKind, item.Category))
            {
                reason = "slot_category";
                return false;
            }

            if (inventory.GetAcquisitionCount(item.itemId) >= item.maxAcquisitionsPerRun)
            {
                reason = "max_acquisitions";
                return false;
            }

            if (item.Category == ItemCategory.ActiveItem)
            {
                var stackLimit = database.GetActiveItemStackLimit(item);
                if (inventory.GetActiveItemCount(item.itemId) >= stackLimit)
                {
                    reason = "active_item_stack_limit";
                    return false;
                }
            }

            return true;
        }

        private static bool IsCategoryAllowed(ItemSlotKind slotKind, ItemCategory category)
        {
            return slotKind switch
            {
                ItemSlotKind.Free or ItemSlotKind.Paid => category is ItemCategory.ActiveItem or ItemCategory.PassiveItem or ItemCategory.BoardDeckUpgrade or ItemCategory.ConnectionLimitUpgrade,
                ItemSlotKind.Unique => category == ItemCategory.UniqueItem,
                _ => throw new ArgumentOutOfRangeException(nameof(slotKind), slotKind, null),
            };
        }
    }
}
