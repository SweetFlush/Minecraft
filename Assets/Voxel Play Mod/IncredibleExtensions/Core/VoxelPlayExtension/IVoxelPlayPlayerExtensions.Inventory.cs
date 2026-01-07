
namespace VoxelPlay{
    public static partial class IVoxelPlayPlayerExtensions
    {
        public static bool HasItemWithIndex(this IVoxelPlayPlayer player, int itemIndex){
                if (player.items == null) {
                    return false;
                }
                if (itemIndex >= 0 && itemIndex < player.items.Count) {
                    if (player.items[itemIndex].item == null) {
                        return false;
                    }
                    return true;
                }else return false;
                    
            }

        
    }

    public partial interface IVoxelPlayPlayer{
        bool AddInventoryItem (ItemDefinition newItem, int durability,float _quantity);
        public void DropAllItems();

    }

    
}