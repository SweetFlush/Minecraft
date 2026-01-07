using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;
using System;
using System.Collections;

namespace IncredibleExtensions.VPAddons{


    [System.Serializable]
    public class ChestInventorySlot{
        
        public InventoryItem _inventoryItem;

        public int ChestIndex;
    }
    [Serializable]
    public class ChestInventoryData
    {
        public Vector3d chestPosition;
        public List<ChestInventorySlot> chestItems;

        public ChestInventoryData(List<ChestInventorySlot> items, Vector3d position)
        {
            chestItems = items;
            chestPosition=position;
        }
    }

    public class Chest : VoxelPlayInteractiveObject, IInventoryContainer
    {

        public static event Action<Chest> OnChestOpened;




        [SerializeField]
        public List<ChestInventorySlot> chestItems;
        public virtual List<ChestInventorySlot> GetChestInventorySlots{get{return chestItems;}}

        protected bool shown;

        [SerializeField]
        ItemDefinition testItemDef;

        [SerializeField]
        protected int slots;


        private string chestKey;
        [SerializeField]
        private bool needsSave = false;

        private bool isBroken = false; //  Track if the chest was actually broken



        public override void OnStart(){
            chestKey = GenerateChestKey();

            //For the moment, then load the content if possible
            InitChest();
            VoxelPlayEnvironment.instance.OnVoxelBeforeDestroyed += OnVoxelDestroyed;
            StartCoroutine(LoadData());

        }


        private void OnSave(SaveGameCustomDataWriter writer)
        {
        if (needsSave) // Only save if modified
            {
                OnSaveGame();
            }
        }

        protected virtual void OnDisable(){
            // VoxelPlayEnvironment.instance.OnLoadCustomGameData -= LoadData;
            if(isBroken)
                DropItems();
            VoxelPlayEnvironment.instance.OnVoxelBeforeDestroyed -= OnVoxelDestroyed;
        }
        
        private void OnVoxelDestroyed(VoxelChunk chunk, int voxelIndex)
        {

            Vector3d voxelPosition = env.GetVoxelPosition(chunk, voxelIndex);

            // Check if this chest is at that position
            if (env.GetVoxelPosition(transform.position) == voxelPosition)
            {
                ExternalInventoriesSaveManager.Instance.RemoveChestData(chestKey);
                isBroken=true;
            }
        }


        public void OnSaveGame ()
            {
                if (!needsSave) return; // Double check: No changes, no need to save

                string key = GenerateChestKey();


                // Convert chest inventory data to JSON
                ChestInventoryData chestData = new ChestInventoryData(chestItems, env.GetVoxelPosition(transform.position));
                string json = JsonUtility.ToJson(chestData);
                ExternalInventoriesSaveManager.Instance.RegisterChestData(key, json);

                needsSave = false; // Reset save flag after saving


        }


        public void OnLoadGame (Dictionary<string, string> data)
        {
            // Debug.Log("Chest onloadgame");
            StartCoroutine(LoadData());
            
        }

        IEnumerator LoadData(){
            Debug.Log("Loading data started ");
            
            yield return new WaitUntil(() => ExternalInventoriesSaveManager.Instance.HasLoaded);

            // yield return new WaitForSeconds (1f);
            // string key = GenerateChestKey();

            Debug.Log("ExternalInventoriesManager has loaded, should try to get chest data " );

            // foreach(var valuePair in data.Keys){
            //     Debug.Log("Printing key value pair for chest"+ valuePair);
            // }
            // Debug.Log("finished keys");
            string json = ExternalInventoriesSaveManager.Instance.GetChestData(chestKey);


            if (!string.IsNullOrEmpty(json))
            {
                // Deserialize JSON back into chest data
                ChestInventoryData loadedData = JsonUtility.FromJson<ChestInventoryData>(json);
                
                if (loadedData != null)
                {
                    chestItems = loadedData.chestItems;
                    // chestItems = new List<ChestInventorySlot>(int );
                    Debug.Log("DATA RETRIEVED SUCCESFULLY");
                }
            }
        }



        string GenerateChestKey(){
            return "chest_" + env.GetVoxelPosition(transform.position).ToString();

        }

        


        void InitChest(){
            chestItems=new List<ChestInventorySlot>();

            for (int i=0;i<slots;i++){
                ChestInventorySlot inventorySlot = new ChestInventorySlot();
                inventorySlot._inventoryItem=InventoryItem.Null;
                chestItems.Add(inventorySlot);
            }
        }


        public override void OnPlayerApproach () {
            if (!shown) {
                if (Application.isMobilePlatform) {
                    env.ShowMessage (txt: "<color=green>Press </color><color=yellow>Action</color> button to open/close this door.", allowDuplicatedMessage: true);
                } else {
                    env.ShowMessage (txt: "<color=green>Press </color><color=yellow>T</color> to open/close this door.", allowDuplicatedMessage: true);
                }
                shown = true;
            }
        }

        public override void OnPlayerGoesAway () {
            
        }

        public override void OnPlayerAction () {
            Debug.Log("Open chest");
            OnChestOpened?.Invoke(this);
        }

        protected void InvokeOnChestOpened()
        {
            OnChestOpened?.Invoke(this); // Invokes the event with the current instance
        }


        public virtual void AddItem(InventoryItem _item, int index)
        {
            var newItem = new InventoryItem(); //Create a new item and add it to the slot
            newItem.item=_item.item;
            newItem.quantity=_item.quantity;
            chestItems[index]._inventoryItem=newItem;
        }


        public virtual InventoryItem GetItem(int slotIndex)
        {
            if(slotIndex >= 0 && slotIndex < chestItems.Count){
                if(chestItems[slotIndex]!=null){
                    return chestItems[slotIndex]._inventoryItem;
                }else
                return InventoryItem.Null;
            }
            return InventoryItem.Null; 
        }

        public virtual void SetItem(int slotIndex, InventoryItem item)
        {

            if (slotIndex >= 0 && slotIndex < chestItems.Count)
            {
                // Check if the slot itself is null
                if (chestItems[slotIndex] == null)
                {
                    // Initialize a new ChestInventorySlot if the slot is empty
                    chestItems[slotIndex] = new ChestInventorySlot();
                }
                
                // Now that we know chestItems[slotIndex] is not null, set the item
                chestItems[slotIndex]._inventoryItem = item;
                needsSave=true;
                OnSaveGame();
            }
            else
            {
                Debug.LogWarning($"Invalid slot index {slotIndex} for chestItems.");
            }
        }

        protected virtual void DropItems(){
            Debug.Log("Should drop items");
            var spawnPos= new Vector3(transform.position.x,transform.position.y+1,transform.position.z);
            foreach( var item in chestItems){
                if(item._inventoryItem!=InventoryItem.Null){
                    VoxelPlayEnvironment.instance.ItemSpawn(item._inventoryItem.item.name,spawnPos,(int)item._inventoryItem.quantity);
                    //might be needed the "Voxel Spawn" aswell
                    Debug.Log("Object spawned");
                }
            }
        }

        public int GetSlotCount()
        {
            return chestItems.Count;
        }

        
    }
}