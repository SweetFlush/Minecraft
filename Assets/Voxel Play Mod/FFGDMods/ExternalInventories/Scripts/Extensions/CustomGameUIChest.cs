using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;
using UnityEngine.UI;

namespace IncredibleExtensions.VPAddons{
    public partial class CustomGameUI:VoxelPlayUI
    {
        GameObject chestUI;

        ChestInventoryUI chestInventoryUI;


        Chest chestInUse;

        void EnableChestAddOn(){
            Chest.OnChestOpened+=ChestOpened;
            InventorySwapManager.OnItemSwappedEvent.AddListener(OnSwapDetected);
            
        }
        void DisableChestAddOn(){
            Chest.OnChestOpened-=ChestOpened;
            InventorySwapManager.OnItemSwappedEvent.RemoveListener(OnSwapDetected);
            
        }

        void InitializeChestAddOn(){
            chestUI=transform.Find("ChestInventory").gameObject;
            chestInventoryUI=chestUI.GetComponent<ChestInventoryUI>();
            chestInventoryUI.CustomGameUI=this;
        }

        partial void CheckChestReferences(){
            
            // inventoryWithCrafting=transform.Find("InventoryWithCrafting").gameObject;
            // inventoryItemTemplate = inventoryWithCrafting.transform.Find("ItemButtonTemplate").gameObject;
            // inventoryTitle = inventoryWithCrafting.transform.Find("Title").gameObject;
            // inventoryTitleText = inventoryWithCrafting.transform.Find("Title/Text").GetComponent<Text>();
            // craftingSystem=inventoryWithCrafting.transform.Find("CraftingSystem").gameObject.GetComponent<CraftingSystem>();
        }

        void ChestOpened(Chest chestItems){
            if(!TryToggleCustomInventory()){ //If we can't open the chest ui we just return;
                return;
            }
            chestInUse=chestItems;
            List<ChestInventorySlot> chestInventorySlots = chestItems.GetChestInventorySlots;
            Debug.Log("ChestOpened Event Received");
            chestUI.SetActive(true);
            chestInventoryUI.InitNewChest(chestInventorySlots, chestInUse as IInventoryContainer);
            NewCheckInventoryUI(chestUI.transform);

            InitInventoryContents();
            ToggleHotBar(false);
            
        }

        void CloseChest(){
            chestUI.SetActive(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            env.input.enabled = false;  
            selectedInventoryItem.item=null;
            chestInUse=null;

        }

        void ReinitializeChestInventory(int lenght){

        }

        void OnSwapDetected(ItemSwapEventData itemSwapEventData){
            if(chestInUse==null) return;
            List<ChestInventorySlot> chestInventorySlots = chestInUse.GetChestInventorySlots;
            chestInventoryUI.InitNewChest(chestInventorySlots, chestInUse as IInventoryContainer);
            NewCheckInventoryUI(chestUI.transform);
            InitInventoryContents();
        }

        



    }

}