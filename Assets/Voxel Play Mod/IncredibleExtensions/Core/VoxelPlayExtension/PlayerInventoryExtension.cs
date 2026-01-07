using UnityEngine;
using IncredibleExtensions.VPAddons;

namespace VoxelPlay{
    /// <summary>
    /// TODO: NEEDS TO ADD DURABILITY WHEN DROPPING OBJECTS!
    /// </summary>
    public partial class VoxelPlayPlayer: IInventoryContainer
    {
        public InventoryItem GetItem(int slotIndex)
        {
             if(slotIndex >= 0 && slotIndex < playerItems.Count){
            if(playerItems[slotIndex]!=null){
                return playerItems[slotIndex];
            }else
            return InventoryItem.Null;
        }
        return InventoryItem.Null; 
        }

        public int GetSlotCount()
        {
            throw new System.NotImplementedException();
        }

        public void SetItem(int slotIndex, InventoryItem item)
        {
            InventoryItem itemToConsume = InventoryItem.Null;  
            if (slotIndex >= 0 && slotIndex < playerItems.Count)
            {
                
                // Check if the slot itself is null
                if (playerItems[slotIndex] == null)
                {

                    // Debug.Log($"Player slot is null.");
                    if(item!=null){
                        // Debug.Log($"Item is not null, adding object to player inventory.");
                        playerItems.RemoveAt(slotIndex);
                        AddInventoryItem(item.item, item.durabilityLeft, item.quantity);
                    }else{
                        playerItems.RemoveAt(slotIndex);
                    }
                }
                else
                {
                    // Debug.Log($"Consuming item at index {slotIndex}: {playerItems[slotIndex].item}");
                    if(playerItems[slotIndex].item!=null){
                        itemToConsume=playerItems[slotIndex];
                    }
                    if(item.item==null){
                        playerItems.RemoveAt(slotIndex);
                    }else
                        playerItems[slotIndex] = item;// Now that we know chestItems[slotIndex] is not null, set the item

                    // Debug.Log($"Slot is not null, swapping.");
                }                
            }
            else
            {
                // Debug.LogWarning($"Invalid slot index {slotIndex} for player. Adding the object via code");
                AddInventoryItem(item.item, item.durabilityLeft, item.quantity);

            } 
        }

        public void DropAllItems(){
            Debug.Log("Should drop all items from player");
            var spawnPos= new Vector3(transform.position.x,transform.position.y+1,transform.position.z);
            foreach( var item in playerItems){
                if(item!=InventoryItem.Null){
                    VoxelPlayEnvironment.instance.ItemSpawn(item.item.name,spawnPos,(int)item.quantity);  //Needs to handle durability!
                    //might be needed the "Voxel Spawn" aswell
                    Debug.Log("Object spawned");
                }
            }
        }

    }
}
