using UnityEngine;
using VoxelPlay;


namespace IncredibleExtensions.VPAddons{
    /// <summary>
    /// TODO: Modify the save/load depending on the save (world name)
    /// Saves any object that has an inventory and loads the data on load
    /// </summary>
    public partial class ExternalInventoriesSaveManager : MonoBehaviour
    {
        public static ExternalInventoriesSaveManager Instance;



        public bool HasLoaded{get;private set;}

        void Awake(){
            if(Instance==null){
                Instance=this;
            }else
                Destroy(this.gameObject);
            VoxelPlayEnvironment.instance.OnLoadCustomGameData += OnLoad;
            VoxelPlayEnvironment.instance.OnSaveCustomGameData += OnSave;
        }
        
        
        partial void SaveChestData(SaveGameCustomDataWriter writer);
        partial void SaveMachineData(SaveGameCustomDataWriter writer);
        private void OnSave(SaveGameCustomDataWriter writer)
        {
            
            SaveChestData(writer);


            SaveMachineData(writer);
        }


        partial void LoadChestData(string tag, byte[] contents);
        partial void LoadMachinetData(string tag, byte[] contents);
        private void OnLoad(string tag, byte[] contents)
        {
            LoadChestData(tag, contents);
            LoadMachinetData(tag, contents);
            HasLoaded=true;
        }




    }


    

}
