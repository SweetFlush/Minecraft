using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons{

    public class ChestInventoryUI : MonoBehaviour
    {
        [SerializeField]
        List<GameObject> currentInventorySlots;
        
        [SerializeField]
        InventoryItem [] chestInventoryItems;

        [SerializeField]
        GameObject buttonTemplate;
        [SerializeField]
        GameObject rowTemplate;

        [SerializeField]
        int objectsPerRow;

        [SerializeField]
        GameObject chestInventoryLayout;

        public CustomGameUI CustomGameUI {set;private get;}

        
        [NonSerialized]
        public VoxelPlayEnvironment env;

        IInventoryContainer currerntInventoryContainer;
            


        public void InitNewChest(List<ChestInventorySlot> chestItems, IInventoryContainer _inventoryContainer){
            currerntInventoryContainer=_inventoryContainer;
            chestInventoryItems= new InventoryItem[chestItems.Count];
            CleanOldList();
            CreateNewList(chestItems);
            if (env == null) {
                    env = VoxelPlayEnvironment.instance;
                }
            
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                env.input.enabled = false;
                
        }
        void CleanOldList(){
            if(currentInventorySlots.Count==0) return;
            foreach(GameObject inventorySlot in currentInventorySlots){
                Destroy(inventorySlot.gameObject);
            }
            var oldRows=chestInventoryLayout.transform.GetComponentsInChildren<Transform>();
            for(int i=0;i<oldRows.Length;i++){
                if(oldRows[i]!=chestInventoryLayout.transform){
                    Destroy(oldRows[i].gameObject);
                }
            }
            currentInventorySlots.Clear();
        }

        void CreateNewList(List<ChestInventorySlot> chestItems){
            int objectsCreatedCount = 0;

            for (int j = 0; j < chestItems.Count; j++)
            {
                Transform row = Instantiate(rowTemplate, chestInventoryLayout.transform).transform;
                row.gameObject.SetActive(true);

                for (int i = 0; i < objectsPerRow; i++)
                {
                    if (objectsCreatedCount == chestItems.Count)
                    {
                        return;
                    }

                    // Directly reference the InventoryItem instead of creating a new one
                    if (chestItems[objectsCreatedCount] != null && chestItems[objectsCreatedCount]._inventoryItem.item != null)
                    {
                        // Assign the same InventoryItem reference instead of creating a new instance
                        chestInventoryItems[objectsCreatedCount] = chestItems[objectsCreatedCount]._inventoryItem;
                    }
                    else
                    {
                        // If no item exists, assign a new empty InventoryItem
                        chestInventoryItems[objectsCreatedCount] = new InventoryItem
                        {
                            item = null,
                            quantity = 0
                        };
                    }

                    // Instantiate and set up the slot
                    var slot = Instantiate(buttonTemplate);
                    currentInventorySlots.Add(slot);
                    slot.transform.SetParent(row, false);
                    slot.SetActive(true);

                    // Store the index for the CopyInventoryItem delegate
                    int aux = objectsCreatedCount;
                    slot.GetComponent<InventorySlot>().SetInventorySlot(aux,currerntInventoryContainer,false);
                    slot.GetComponent<Button>().onClick.AddListener(delegate () {
                        // if(chestItems[objectsCreatedCount-1]!=null)
                        //     CopyWithSwapManager(aux,chestItems[objectsCreatedCount-1]._inventoryItem,slot);
                        // else
                        //     CopyWithSwapManager(aux,InventoryItem.Null,slot);

                    });

                    // Set the slot icon based on the chest item
                    if (chestItems[objectsCreatedCount] != null)
                    {
                        SetSlotIcon(slot, chestItems[objectsCreatedCount]._inventoryItem);
                    }
                    else
                    {
                        SetSlotIcon(slot, new InventoryItem { item = null, quantity = 0 });
                    }

                    objectsCreatedCount++;
                }
            }
        }




            void SetSlotIcon(GameObject slot, InventoryItem inventoryItem){
                Text quantityShadow = slot.transform.Find ("QuantityShadow").GetComponent<Text> ();
                Text quantityText = slot.transform.Find ("QuantityShadow/QuantityText").GetComponent<Text> ();
                GameObject keyHint = slot.transform.Find("KeyCodeShadow").gameObject;
                keyHint.SetActive(false);

                if(inventoryItem==InventoryItem.Null){
                    quantityText.text = "";
                    quantityShadow.text="";
                    return;
                }
                var rawImage = slot.GetComponent<RawImage>();
                rawImage.color=inventoryItem.item.color;
                rawImage.texture = inventoryItem.item.icon;
                quantityText.text = inventoryItem.quantity.ToString();
                quantityShadow.text = inventoryItem.quantity.ToString();

            }

            void CopyWithSwapManager(int targetSlotIndex, InventoryItem inventoryItem, GameObject slot){


                InventorySwapManager.Instance.SelectItem(currerntInventoryContainer,targetSlotIndex, false); //Either add a callback to know when they swapped to reload the icon, or call the SetSloticon from the swap manager
                
            }


            
    }
}