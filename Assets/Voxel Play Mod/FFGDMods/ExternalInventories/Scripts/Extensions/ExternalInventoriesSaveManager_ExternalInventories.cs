using UnityEngine;
using System.Text;
using System.Collections.Generic;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons{
    public partial class ExternalInventoriesSaveManager : MonoBehaviour
    {   

        private Dictionary<string, string> chestDataDictionary = new Dictionary<string, string>();

        private HashSet<string> chestsToSave = new HashSet<string>(); // Only save modified chests



        #region save/load

        partial void SaveChestData(SaveGameCustomDataWriter writer)
        {
            string chestJson = JsonUtility.ToJson(new SerializableDictionary(chestDataDictionary));

            // Convert JSON to byte array
            byte[] chestData = Encoding.UTF8.GetBytes(chestJson);

            // Save data with a specific tag (e.g., "ChestData")
            writer.Add("ChestData", chestData);
        }

        partial void LoadChestData(string tag,byte[] contents)
        {
            if (tag == "ChestData"){

                chestDataDictionary.Clear();

                // Convert byte array back to JSON string
                string json = System.Text.Encoding.UTF8.GetString(contents);

                // Deserialize JSON back into chestDataDictionary
                SerializableDictionary loadedData = JsonUtility.FromJson<SerializableDictionary>(json);

                if (loadedData != null)
                {
                    chestDataDictionary = loadedData.ToDictionary();
                }
                // else
                // {
                //     Debug.LogWarning("Failed to load chest data.");
                // }
            }
        }

        #endregion

        #region  ChestData

        public void RegisterChestData(string key, string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                chestDataDictionary[key] = json;
                chestsToSave.Add(key); // Mark this chest for saving
            }
        }

        public void RemoveChestData(string key)
        {
            if (chestDataDictionary.ContainsKey(key))
            {
                chestDataDictionary.Remove(key);
                Debug.Log($"Removed chest data for key: {key}");
            }

            chestsToSave.Remove(key); // Prevent saving if the chest was removed
        }

        public string GetChestData(string key)
        {
            chestDataDictionary.TryGetValue(key, out string json);
            // if(json==null){
            //     Debug.LogWarningFormat($"key {key} not existent in manager ");
            // }
            return json;
        }


        
        private void SaveModifiedChests()
        {
            if (chestsToSave.Count == 0) return; // No chests need saving

            foreach (var key in chestsToSave)
            {
                if (!chestDataDictionary.ContainsKey(key)) continue;
                Debug.Log($"Saving modified chest data for key: {key}");
            }

            chestsToSave.Clear(); // Clear the list after saving
        }


        #endregion
    }
}
