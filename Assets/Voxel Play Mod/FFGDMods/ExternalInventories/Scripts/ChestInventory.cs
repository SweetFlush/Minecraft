using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;
namespace IncredibleExtensions.VPAddons{
    public class ChestInventory : MonoBehaviour
    {
        public List<ChestInventorySlot> newChestItems;

        public InventoryItem GetItem(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < newChestItems.Count ? newChestItems[slotIndex]._inventoryItem : InventoryItem.Null;
        }

        public void SetItem(int slotIndex, InventoryItem item)
        {
            if (slotIndex >= 0 && slotIndex < newChestItems.Count)
            {
                newChestItems[slotIndex]._inventoryItem = item;
            }
        }

        public int GetSlotCount()
        {
            return newChestItems.Count;
        }
    }
}