using UnityEngine;
using UnityEngine.Events;
using VoxelPlay;


namespace IncredibleExtensions.VPAddons{

    [System.Serializable]
    public class ItemSwapEventData
    {
        public bool IsPlayerItemSwapped;
        public int InventoryIndex;
    }

    public class InventorySwapManager : MonoBehaviour
    {
        public static InventorySwapManager Instance;
        [SerializeField]
        private InventoryItem selectedInventoryItem;
        private IInventoryContainer selectedContainer;
        private int selectedSlotIndex;
        ItemSwapEventData currentItemSwapEventData = new ItemSwapEventData();

        VoxelPlayEnvironment env;

        public static UnityEvent<ItemSwapEventData> OnItemSwappedEvent = new UnityEvent<ItemSwapEventData>();  

        void Awake(){
            if(Instance==null){
                Instance=this;
            }else{
                Destroy(this.gameObject);
            }
            if(env==null){
                env=VoxelPlayEnvironment.instance;
            }

        }
        
        /// <summary>
        /// Method used to select/swap items
        /// </summary>
        /// <param name="container">indicates from which inventory the selected item comes from</param>
        /// <param name="slotIndex">the slot in the inventory</param>
        /// <param name="isPlayerSlot">indicates if it's a player inventory item</param>
        /// <param name="isRightClick"></param>
        public void SelectItem(IInventoryContainer container, int slotIndex, bool isPlayerSlot = false, bool isRightClick = false)
        {
            if (isPlayerSlot)
            {
                currentItemSwapEventData.IsPlayerItemSwapped = true;
            }

            // Get the item in the clicked slot
            InventoryItem clickedItem = container.GetItem(slotIndex);

            // Are we currently holding something?
            bool carryingItem = selectedInventoryItem.item != null;

            if (carryingItem)
            {
                bool hasDurability = selectedInventoryItem.item.GetPropertyValue("Durability",0)!=0;
                // --- CASE A: We ARE carrying something ---
                // If left-click: move entire stack
                // If right-click: move just 1 item
                if (!isRightClick)
                {

                    if(selectedInventoryItem.item==container.GetItem(slotIndex).item && !hasDurability){ //If the item clicked is the same as the item we got in the hand we merge them
                        
                        selectedInventoryItem.quantity+=container.GetItem(slotIndex).quantity;

                        // Place the merged stack in the clicked slot
                        container.SetItem(slotIndex, selectedInventoryItem);
                        selectedInventoryItem = InventoryItem.Null;

                    }else{ //Else we swap the item in hand and the clicked item
                        container.SetItem(slotIndex, selectedInventoryItem);

                        // Pick up whatever was in that slot
                        selectedInventoryItem = clickedItem;
                    }

                }
                else
                {
                    // RIGHT CLICK: Place just 1 item
                    if (selectedInventoryItem.quantity > 0)
                    {
                        // If the slot is empty, create a new item with quantity 1
                        if (clickedItem.item == null)
                        {
                            // clone the item definition, but quantity = 1
                            InventoryItem oneItem = new InventoryItem();

                            oneItem.item=selectedInventoryItem.item;                                        
                            oneItem.durabilityLeft=selectedInventoryItem.durabilityLeft; 
                            oneItem.quantity=1;                                       
                            container.SetItem(slotIndex, oneItem);
                            selectedInventoryItem.quantity--;
                        }
                        else
                        {
                            // If the slot has the same item and has not a durability, increase its quantity by 1
                            if (clickedItem.item == selectedInventoryItem.item && !hasDurability)
                            {
                                
                                clickedItem.quantity++;
                                container.SetItem(slotIndex, clickedItem);
                                // Remove 1 from the carried stack
                                selectedInventoryItem.quantity--;

                            }
                            // else /       /If you want to SWAP THE ITEM, uncomment this
                            // {
                            //     // Different item -> normal "swap" logic:
                            //     container.SetItem(slotIndex, selectedInventoryItem);
                            //     selectedInventoryItem = clickedItem;
                            // }
                        }


                        // If we used up the entire stack, clear selection
                        if (selectedInventoryItem.quantity <= 0)
                        {
                            selectedInventoryItem.item=null;
                            ClearSelection();
                        }
                    }
                }

                // If our hand ended up empty after placing
                if (selectedInventoryItem.item == null || selectedInventoryItem.quantity <= 0)
                {
                    ClearSelection();
                }
                else
                {
                    // Still carrying something, update drag icon
                    DragIconController.Instance.StartDragging(selectedInventoryItem);
                }

                OnItemSwappedEvent?.Invoke(currentItemSwapEventData);
                currentItemSwapEventData = new ItemSwapEventData();
            }
            else
            {
                // --- CASE B: We are NOT carrying anything ---
                // If the clicked slot is empty, do nothing
                if (clickedItem.item == null) return;

                // If left-click, pick up the entire stack
                if (!isRightClick)
                {
                    selectedInventoryItem = clickedItem;
                    container.SetItem(slotIndex, InventoryItem.Null);
                }
                else
                {
                    // RIGHT CLICK: pick up half the stack? (Minecraft style)
                    // Or just pick up 1 item? It's up to you.
                    float halfStack = Mathf.Floor(clickedItem.quantity / 2);
                    if (halfStack == 0) halfStack = 1;

                    // Example: pick up half
                    selectedInventoryItem = new InventoryItem();
                    selectedInventoryItem.item=clickedItem.item;                                        
                    selectedInventoryItem.durabilityLeft=clickedItem.durabilityLeft; 
                    selectedInventoryItem.quantity=halfStack;  
                    // reduce the slot's quantity by that amount
                    clickedItem.quantity -= halfStack;
                    if (clickedItem.quantity <= 0)
                    {
                        container.SetItem(slotIndex, InventoryItem.Null);
                    }
                    else
                    {
                        container.SetItem(slotIndex, clickedItem);
                    }
                }

                // If we did pick something up
                if (selectedInventoryItem.item != null && selectedInventoryItem.quantity > 0)
                {
                    selectedContainer = container;
                    selectedSlotIndex = slotIndex;
                    DragIconController.Instance.StartDragging(selectedInventoryItem);
                }
            }

            OnItemSwappedEvent?.Invoke(currentItemSwapEventData);
        }

        //Old method, deprecated
        // public void SwapItems(IInventoryContainer targetContainer, int targetSlotIndex)
        // {

        //     if (selectedContainer != null && selectedInventoryItem != null)
        //     {
        //         // Get the item in the target slot
        //         var targetItem = targetContainer.GetItem(targetSlotIndex);

        //         // Swap items between the two containers
        //         selectedContainer.SetItem(selectedSlotIndex, targetItem);
        //         targetContainer.SetItem(targetSlotIndex, selectedInventoryItem);

        //         // Clear the selection
        //         selectedInventoryItem = InventoryItem.Null;
        //         selectedContainer = null;
        //         selectedSlotIndex = -1;
        //     }
        //     OnItemSwappedEvent?.Invoke(currentItemSwapEventData);
        //     currentItemSwapEventData=new ItemSwapEventData();

        // }

        // public void ClearSelection(){
        //     selectedInventoryItem = InventoryItem.Null;
        //     selectedContainer=null;
        //     currentItemSwapEventData=new ItemSwapEventData();
        //     currentItemSwapEventData.IsPlayerItemSwapped=false;
        // }

        public void ClearSelection()
    {
        //If we're still carrying an item...
        if (selectedInventoryItem.item!=null)
        {
            //Attempt to put it back where it came from, if we still have a valid container and slot index
            if (selectedContainer != null && selectedSlotIndex >= 0)
            {
                var currentSlotItem = selectedContainer.GetItem(selectedSlotIndex);
                
                // If original slot is empty, put it back
                if (currentSlotItem.item==null)
                {
                    selectedContainer.SetItem(selectedSlotIndex, selectedInventoryItem);
                }
                else
                {
                    // Otherwise, drop it in the world (or handle however you prefer)
                    Debug.Log("Shoul drop item");
                    DropItemInWorld(selectedInventoryItem);
                }
            }
            else
            {
                // No valid container/slot: just drop it
                Debug.Log("Shoul drop item");
                
                
                DropItemInWorld(selectedInventoryItem);
            }
        }

        // 3. Finally, reset references so we are definitely "carrying" nothing
        selectedInventoryItem = InventoryItem.Null;
        selectedContainer = null;
        selectedSlotIndex = -1;

        // Reset your event data, flags, etc.
        currentItemSwapEventData = new ItemSwapEventData();
        currentItemSwapEventData.IsPlayerItemSwapped = false;

        DragIconController.Instance.StopDragging();
    }

    void DropItemInWorld(InventoryItem  inventoryItem){
        if(env==null){
                env=VoxelPlayEnvironment.instance;
            }
        Transform player = VoxelPlayPlayer.instance.GetTransform();
        Vector3 throwPosition = player.position + (player.transform.forward * 1f) + Vector3.up;
        Vector3 direction = player.forward; 
        // float throwForce = 15f;
        if (inventoryItem.item.category == ItemCategory.Voxel)  {
                    env.StackVoxelThrow(throwPosition, direction, 2f, inventoryItem.item.voxelType, Misc.color32White,inventoryItem.quantity);
                } else if (inventoryItem.item.category == ItemCategory.General||
            inventoryItem.item.category == ItemCategory.Torch) {
                    env.StackItemThrow(throwPosition, direction, 2f, inventoryItem.item,inventoryItem.quantity);
                }
        }

    }

}