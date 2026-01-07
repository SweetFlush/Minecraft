using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons{
    public class CraftingItemSlot : MonoBehaviour, IInventoryContainer, IPointerClickHandler
    {
        [SerializeField]
        int [,] position;

        [SerializeField]
        int slotIndex;

        //You can put the inspector in debug mode to see this item, or set this as [SerializableField], but the first option is recommended.
        InventoryItem myInventoryItem;

        [SerializeField]
        RawImage image;

        [SerializeField]
        Text quantityText;
        [SerializeField]
        Text quantityShadowText;

        [SerializeField]
        CraftingSystem craftingSystem;

        [NonSerialized]
        VoxelPlayEnvironment env;

        [SerializeField][Tooltip("If true, prevents the swap manager to be called when clicking the item")]
        bool isOutputSlot=false;
        

        void Start(){
            myInventoryItem.item=null;
            image = GetComponent<RawImage>();
            // quantityShadowText=quantityText.transform.Find("QuantityText").GetComponent<Text>();
            if (env == null) {
                env = VoxelPlayEnvironment.instance;
            }

        }

        
        public void OnPointerClick(PointerEventData eventData)
        {
            if(isOutputSlot)return;
            // Debug.Log("Clicked on the crafting inventory slot!");
            bool isRightClick = eventData.button == PointerEventData.InputButton.Right;
            InventorySwapManager.Instance.SelectItem(this, slotIndex, false, isRightClick); 
            
            craftingSystem.PlaceObject();
            RefreshVisual();
        }

        /// <summary>
        /// Called on click of the crafting grid Button.
        /// </summary>
        public void PutItem(){
            // TryPutItem(craftingSystem.currentInventoryItem);
            // InventorySwapManager.Instance.SelectItem(this,slotIndex,false);
            // craftingSystem.PlaceObject();
            // RefreshVisual();

        }
        
        /// <summary>
        /// We try to put the item in the grid if an item is selected from the inventory, otherwise we do nothing.
        /// </summary>
        /// <param name="_inventoryItem"></param>
        public void TryPutItem(InventoryItem _inventoryItem){
            if(_inventoryItem.item==null){ //There is no object selected, we return.
                return;
            }
            if(myInventoryItem.item==null){ //Assign the item to the slot and add one.
                myInventoryItem=_inventoryItem;
                myInventoryItem.quantity=0;
                AddItem();
                return;
            }else if(myInventoryItem.item==_inventoryItem.item){ //Just add one if the item is already assigned.
                AddItem();
                return;
            }
            if(myInventoryItem.item!=_inventoryItem.item){ //If the objects are different, swap items
                
                Debug.Log("Items are different, swapping them");
                craftingSystem.AddItem(myInventoryItem.item,myInventoryItem.quantity, myInventoryItem.durabilityLeft ); //Give back to the player the item used.
                myInventoryItem=_inventoryItem;
                myInventoryItem.quantity=0;
                AddItem();
            }

        }

        /// <summary>
        /// Add item to the grid. Remove the item from the player
        /// </summary>
        void AddItem(){ 
            myInventoryItem.quantity++;
            craftingSystem.RemoveItem(myInventoryItem.item, 1);

            Debug.Log("Adding item to slot " + gameObject.name);
            craftingSystem.PlaceObject();
            RefreshVisual();
        }

        /// <summary>
        /// Reset the slot. Give the player back the item and set the item of the slot to null.
        /// </summary>
        public void Reset(){   
            if(myInventoryItem==null) return;         
            craftingSystem.AddItem(myInventoryItem.item,myInventoryItem.quantity, myInventoryItem.durabilityLeft);
            
            myInventoryItem.item=null;  //Reset the slot
            myInventoryItem.quantity=0;
            RefreshVisual();

        }

        void RefreshVisual(){
            if(myInventoryItem.item!=null){
                image.texture = myInventoryItem.item.icon;  //Set image
                quantityText.enabled=true;                  //Set texts
                quantityShadowText.enabled=true;
                quantityText.text=myInventoryItem.quantity.ToString();
                quantityShadowText.text=myInventoryItem.quantity.ToString();
            }else
            { //Set everything back to null
                image.texture=null;
                quantityText.enabled=false;
                quantityShadowText.enabled=false;
            }
        }
        /// <summary>
        ///  Once we craft, we reduce the objects in the grid by 1.
        /// </summary>
        public void ReduceQuantity(){
            
            myInventoryItem.quantity-=1;
            if(myInventoryItem.quantity<=0){
                myInventoryItem.item=null;
            }
            RefreshVisual();
        }

        public InventoryItem GetItem(int slotIndex)
        {
            return myInventoryItem;
        }

        public void SetItem(int slotIndex, InventoryItem item)
        {
            // Check if the slot itself is null
                // if (myInventoryItem == null)
                // {
                //     // Initialize a new ChestInventorySlot if the slot is empty
                //     myInventoryItem = new ChestInventorySlot();
                // }
                
                // Now that we know chestItems[slotIndex] is not null, set the item
                myInventoryItem = item;
                RefreshVisual();
                // needsSave=true;
        }

        public int GetSlotCount()
        {
            throw new NotImplementedException();
        }


        public InventoryItem MyInventoryItem{get{return myInventoryItem;}}

    }
}