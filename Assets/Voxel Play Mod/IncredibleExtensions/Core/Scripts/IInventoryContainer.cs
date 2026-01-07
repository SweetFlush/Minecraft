using VoxelPlay;

namespace IncredibleExtensions.VPAddons{
    public interface IInventoryContainer
    {
        InventoryItem GetItem(int slotIndex);
        void SetItem(int slotIndex, InventoryItem item);
        int GetSlotCount();
        
    }
}