using System.Collections.Generic;

namespace Mathcalibur.Items
{
    public sealed class RuntimeItemInventory
    {
        private readonly Dictionary<string, int> _acquisitionCounts = new();
        private readonly Dictionary<string, int> _activeItemCounts = new();
        private readonly List<string> _activeItemAcquisitionOrder = new();

        public int PendingNextAttackMultiplierPercent { get; private set; }
        public IReadOnlyDictionary<string, int> AcquisitionCounts => _acquisitionCounts;
        public IReadOnlyDictionary<string, int> ActiveItemCounts => _activeItemCounts;
        public IReadOnlyList<string> ActiveItemAcquisitionOrder => _activeItemAcquisitionOrder;

        public int GetAcquisitionCount(string itemId)
        {
            return _acquisitionCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        public int GetActiveItemCount(string itemId)
        {
            return _activeItemCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        public bool HasAcquiredItem(string itemId)
        {
            return GetAcquisitionCount(itemId) > 0;
        }

        public void RegisterAcquisition(ItemData item)
        {
            if (item == null)
            {
                return;
            }

            _acquisitionCounts[item.itemId] = GetAcquisitionCount(item.itemId) + 1;
            if (item.Category == ItemCategory.ActiveItem)
            {
                if (GetActiveItemCount(item.itemId) <= 0)
                {
                    _activeItemAcquisitionOrder.Remove(item.itemId);
                    _activeItemAcquisitionOrder.Add(item.itemId);
                }

                _activeItemCounts[item.itemId] = GetActiveItemCount(item.itemId) + 1;
            }
        }

        public bool ConsumeActiveItem(string itemId)
        {
            var current = GetActiveItemCount(itemId);
            if (current <= 0)
            {
                return false;
            }

            _activeItemCounts[itemId] = current - 1;
            if (_activeItemCounts[itemId] <= 0)
            {
                _activeItemAcquisitionOrder.Remove(itemId);
            }

            return true;
        }

        public bool HasPendingAttackMultiplier()
        {
            return PendingNextAttackMultiplierPercent > 0;
        }

        public void SetPendingAttackMultiplierPercent(int percent)
        {
            PendingNextAttackMultiplierPercent = percent;
        }

        public void ClearPendingAttackMultiplier()
        {
            PendingNextAttackMultiplierPercent = 0;
        }
    }
}
