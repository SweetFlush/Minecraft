using System.Collections.Generic;
using UnityEngine;


namespace VoxelPlay{
    public delegate void OnPlayerInventoryItemQuantityChangeWithDurability(ItemDefinition item, float quantity, int durability);

    /// <summary>
    /// This class extends the base VoxelPlayPlayer to add the durability to weapons.
    /// In case you don't have the Durability Addon, this will have no effect, but you can still use it to create your own tools durabiity system
    /// </summary>
    public partial class VoxelPlayPlayer{

        public event OnPlayerInventoryItemQuantityChangeWithDurability OnItemAddedWithDurability;
        
        public bool AddInventoryItem(ItemDefinition newItem, int durability, float _quantity)
        {
            if (newItem == null || items == null) {
                return false;
            }

            int itemsCount = items.Count;
            InventoryItem i;
            if(durability<=0 || _quantity==0)  //Should let the items create one copy for each object with durability
                for (int k = 0; k < itemsCount; k++) {
                    if (items[k].item == newItem) { // Check if item is already in inventory
                        i = items[k];
                        i.quantity += _quantity;
                        items[k] = i;
                        if (OnItemAdded != null) OnItemAdded(newItem, _quantity);
                        ShowSelectedItem();
                        return false;
                    }
                }
            i = new InventoryItem(); //Setup the item with all his stats
            i.item = newItem;
            i.quantity = _quantity;
            i.durabilityLeft = durability;
            items.Add(i); //Add the item needed to the player items list
            if (OnItemAddedWithDurability != null) OnItemAddedWithDurability(newItem, _quantity, durability);
            // if (OnItemAdded != null) OnItemAdded(newItem, _quantity);

            if (_selectedItemIndex < 0) {
                selectedItemIndex = items.Count - 1;
                ShowSelectedItem();
            }

            return true;
        }

        public InventoryItem ConsumeItemDurability(){
            if (this.items == null) {
                return InventoryItem.Null;
            }

            List<InventoryItem> items = this.items;

            if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count) {
                InventoryItem i = items[_selectedItemIndex];
                if (!_infiniteInventory) {
                    if (i.durabilityLeft <= 1) {
                        items.RemoveAt(_selectedItemIndex);
                        selectedItemIndex = -1;
                        i.durabilityLeft = 0;
                        i.quantity = 0;
                        UnSelectItem();
                        // SetHandItem(null);
                    } else {
                        i.durabilityLeft--;
                        items[_selectedItemIndex] = i; // update back because it's a struct
                    }
                }
                if (OnItemConsumed != null) OnItemConsumed(i.item, i.quantity); //might need to change/create a new event to accomodate the durability?
                ShowSelectedItem();
                return i;
            }
            return InventoryItem.Null;
        }

        /// <summary>
        /// Never used, but serves as a ground base if you need a repair system in the game.
        /// </summary>
        /// <param name="inventoryItem"></param>
        /// <param name="quantityToAdd"></param>
        /// <param name="maxDurability"></param>
        /// <returns></returns>
        public InventoryItem AddItemDurability(InventoryItem inventoryItem, int quantityToAdd, int maxDurability){
            if (this.items == null) {
                return InventoryItem.Null;
            }


            Mathf.Clamp(inventoryItem.durabilityLeft+quantityToAdd,0,maxDurability);
            return inventoryItem;

        }
    }
    
}