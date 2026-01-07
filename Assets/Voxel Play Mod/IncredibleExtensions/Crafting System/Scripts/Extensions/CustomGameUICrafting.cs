using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;
using UnityEngine.UI;

namespace IncredibleExtensions.VPAddons{

    public partial class CustomGameUI : VoxelPlayUI
    {
        //If should open the inventory with crafting table by default;
        [SerializeField]
        bool defaultCrafting=false;

        //Added for the crafting system
        CraftingSystem craftingSystem;

        public bool IsInventoryWithCraftingOpen {
                get {
                    if (inventoryWithCrafting != null) {
                        return inventoryWithCrafting.activeSelf;
                    }
                    return false;
                }
            }

        void EnableCraftingAddon(){

        }

        void DisableCraftingAddon(){
            InventorySwapManager.OnItemSwappedEvent.RemoveListener(OnItemSwapped_Crafting);

        }

        void InitializeCraftingAddon(){
            InventorySwapManager.OnItemSwappedEvent.AddListener(OnItemSwapped_Crafting); 

        }

        partial void CheckCraftingReferences(){
            
            inventoryWithCrafting=transform.Find("InventoryWithCrafting").gameObject;
            // inventoryItemTemplate = inventoryWithCrafting.transform.Find("ItemButtonTemplate").gameObject;
            // inventoryTitle = inventoryWithCrafting.transform.Find("Title").gameObject;
            // inventoryTitleText = inventoryWithCrafting.transform.Find("Title/Text").GetComponent<Text>();
            craftingSystem=inventoryWithCrafting.transform.Find("CraftingSystem").gameObject.GetComponent<CraftingSystem>();
        }

        partial void TryForceCraftingUI(){
            if(!defaultCrafting) return;
            baseInventory.SetActive(false);
            NewCheckInventoryUI(inventoryWithCrafting.transform);
            InitInventoryContents();
            inventoryWithCrafting.SetActive(true);
            
        }

        void  CloseCraftingMenu(){
            inventoryWithCrafting.SetActive(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            env.input.enabled = true;  
            selectedInventoryItem.item=null;
            inventorySwapManager.ClearSelection();
            craftingSystem.Reset();

            // #if HAS_CRAFTING_ADDON
            //         craftingSystem.Reset();
            // 	#endif

        }

        void OnItemSwapped_Crafting(ItemSwapEventData itemSwapEventData){
                if(!IsInventoryWithCraftingOpen) return;
                NewCheckInventoryUI(inventoryWithCrafting.transform);
                InitInventoryContents();
            } 
    }
}