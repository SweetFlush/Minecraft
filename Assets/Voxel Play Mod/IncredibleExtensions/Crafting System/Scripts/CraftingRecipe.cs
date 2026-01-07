using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;
using System;


namespace IncredibleExtensions.VPAddons{
    [CreateAssetMenu(fileName = "CraftingRecipe", menuName = "Crafting/CraftingRecipe", order = 2)]
    // public class CraftingRecipe : ScriptableObject
    // {
    //     // public CraftingOutputResult MyCraftingOutputResult; 
    //     // // public ScriptableObject [,] Pattern = new ScriptableObject [3, 3]; 
    //     // [CraftingItem]
    //     // public ScriptableObject [] PatternCraftableObj = new ScriptableObject [9];

    //     // public List<ItemDefinition > RequiredItems;
    //     // [CraftingItem]
    //     // public List<ScriptableObject > RequiredItems;
    //     // // [Tooltip("Declare if it's 1x1/2x2/3x3")]
    //     // public int PatternSizeX=3;
    //     // public int PatternSizeY=3;

        
    //     // // This method will initialize requiredItems when the recipe is created or modified
    //     public void InitializeRequiredItems()
    //     {
    //         // RequiredItems = new List<ItemDefinition >();
    //         RequiredItems = new List<ScriptableObject>();
    //         // HashSet<ItemDefinition > uniqueItems = new HashSet<ItemDefinition >();
    //         // foreach (var item in PatternTest)
    //         foreach (var item in PatternCraftableObj)
    //         {
    //             if (item != null)
    //             {
    //                 RequiredItems.Add(item);
    //                 Debug.Log("Unique item found, adding it");
    //             }
    //         }
    //         int index=0;
    //         for(int y = 0;y<3;y++){
    //             for(int x = 0;x<3;x++){
    //                 if(PatternCraftableObj[index]!=null){
    //                     Pattern[x,y]=PatternCraftableObj[index];
    //                 }else{
    //                     Debug.Log($"spot at {x},{y} is empty");
    //                 }
    //                 index++;
    //             }
    //         }
    //         // LogPatternItems();
    //         // RequiredItems = new List<ItemDefinition >(uniqueItems);
    //     }

    //     // public void LogPatternItems()
    //     // {
    //     //     for (int y = 0; y < Pattern.GetLength(1); y++)
    //     //     {
    //     //         for (int x = 0; x < Pattern.GetLength(0); x++)
    //     //         {
    //     //             var item = Pattern[y, x];
    //     //             if (item != null)
    //     //             {
    //     //                 if(item.GetType()==typeof(ItemDefinition)|| item.GetType()==typeof(VoxelDefinition)){
                            
    //     //                     Debug.Log("Item found in pattern at (" + x + ", " + y + "): " + item.name);
    //     //                 }else
    //     //                     Debug.Log("Item found in pattern at (" + x + ", " + y + "): " + " BUT IT'S NOT AN ITEM DEFINITION/VOXEL DEFINITTION");
                        
    //     //             }
    //     //             {
    //     //                 Debug.Log($"-LogPatternItems- Spot at {x},{y} is empty");
    //     //             }
    //     //         }
    //     //     }
    //     // }

    // public CraftingOutputResult MyCraftingOutputResult;
    

    // [Tooltip("Recipe Grid - Fixed 3x3 Storage")]
    // public ScriptableObject[,] Pattern = new ScriptableObject[3, 3]; // Always 3x3

    // [CraftingItem]
    // public List<ScriptableObject> RequiredItems; // Auto-filled for fast checks
    
    // [CraftingItem]
    // public ScriptableObject [] PatternCraftableObj = new ScriptableObject [9]; 
    
    
    // [Tooltip("Pattern Size X (Width)")]
    // public int PatternSizeX = 2;

    // [Tooltip("Pattern Size Y (Height)")]
    // public int PatternSizeY = 2;

    // // Correctly initialize the pattern from PatternCraftableObj
    // public void InitializePattern(ScriptableObject[] flatPattern)
    // {
    //     RequiredItems = new List<ScriptableObject>();

    //     for (int y = 0; y < PatternSizeY; y++) 
    //     {
    //         for (int x = 0; x < PatternSizeX; x++) 
    //         {
    //             int index = y * PatternSizeX + x;
    //             if (index < flatPattern.Length && flatPattern[index] != null)
    //             {
    //                 Pattern[x, y] = flatPattern[index]; // ✅ Store correctly
    //                 RequiredItems.Add(flatPattern[index]); // ✅ Collect required items
    //             }
    //         }
    //     }
    // }

    // }

    public class CraftingRecipe : ScriptableObject
    {
        [Tooltip("Final item or result produced by this recipe.")]
        public CraftingOutputResult MyCraftingOutputResult;

        [Tooltip("Fixed storage for the 3x3 pattern. Only the region up to (PatternSizeX, PatternSizeY) is meaningful.")]
        public ScriptableObject[,] Pattern = new ScriptableObject[3, 3]; 

        [CraftingItem, Tooltip("List of all items involved in this recipe (populated automatically).")]
        public List<ScriptableObject> RequiredItems;

        [CraftingItem, Tooltip("A flat array (up to 9 slots) used in the Inspector to fill the pattern easily.")]
        public ScriptableObject[] PatternCraftableObj = new ScriptableObject[9]; 

        [Tooltip("Recipe width (X dimension). 1 to 3.")]
        public int PatternSizeX = 2;

        [Tooltip("Recipe height (Y dimension). 1 to 3.")]
        public int PatternSizeY = 2;


        /// <summary>
        /// Called to fill both the 2D Pattern array and the RequiredItems list from PatternCraftableObj.
        /// If your pattern is smaller than 3x3, only the first PatternSizeX*PatternSizeY slots in PatternCraftableObj are used.
        /// </summary>
        // public void InitializeRequiredItems() //New, WORKS
        // {
        //     // Clear old references
        //     RequiredItems = new List<ScriptableObject>();

        //     // Safety check: clamp pattern sizes so we don't overrun the 3x3 array
        //     PatternSizeX = Mathf.Clamp(PatternSizeX, 1, 3);
        //     PatternSizeY = Mathf.Clamp(PatternSizeY, 1, 3);

        //     // Fill Pattern[x,y] from PatternCraftableObj
        //     int index = 0;
        //     int gridValue =0;
        //     for(int y = 0; y < PatternSizeY; y++)
        //     {
        //         for(int x = 0; x < PatternSizeX; x++)
        //         {
        //             gridValue=y*3+x; //Formula is Y*Gridsize_Y+X
        //             if (index < PatternCraftableObj.Length && PatternCraftableObj[gridValue] != null)
        //             {
        //                 Pattern[x, y] = PatternCraftableObj[gridValue];
        //                 RequiredItems.Add(PatternCraftableObj[gridValue]);
        //                 Debug.Log($"Adding object to the required items of{name}  Count is now {RequiredItems.Count}");
                        
        //             }
        //             else
        //             {
        //                 Debug.Log($"Slot of {name} is either null or out of index. Count is {RequiredItems.Count}");
        //                 Pattern[x, y] = null; // Ensure it's cleared if not used
        //             }
        //             index++;
        //         }
        //     }

        //     // If the recipe is smaller than 3x3, clear out unused cells
        //     for(int y = PatternSizeY; y < 3; y++)
        //     {
        //         for(int x = 0; x < 3; x++)
        //         {
        //             Pattern[x, y] = null;
        //         }
        //     }
        //     for(int x = PatternSizeX; x < 3; x++)
        //     {
        //         for(int y = 0; y < PatternSizeY; y++)
        //         {
        //             Pattern[x, y] = null;
        //         }
        //     }

        //     Debug.Log($"InitializeRequiredItems finished. Found {RequiredItems.Count} items in the pattern.");
        // }

        public void InitializeRequiredItems() //new ,test
        {
            // Clear old references
            RequiredItems = new List<ScriptableObject>();

            // Safety check: clamp pattern sizes so we don't overrun the 10x10 array, change if you need larger or smaller
            PatternSizeX = Mathf.Clamp(PatternSizeX, 1, 10);
            PatternSizeY = Mathf.Clamp(PatternSizeY, 1, 10);


            Pattern = new ScriptableObject[PatternSizeX, PatternSizeY];

            // Fill Pattern[x,y] from PatternCraftableObj
            int index = 0;
            int gridValue =0;

            int expectedSize = PatternSizeX * PatternSizeY;
            if (PatternCraftableObj == null || PatternCraftableObj.Length != expectedSize)
            {
                PatternCraftableObj = new ScriptableObject[expectedSize];
            }
            
            for(int y = 0; y < PatternSizeY; y++)
            {
                for(int x = 0; x < PatternSizeX; x++)
                {
                    gridValue=y * PatternSizeX + x; //Formula is Y*Gridsize_Y+X
                    if (index < PatternCraftableObj.Length && PatternCraftableObj[gridValue] != null)
                    {
                        Pattern[x, y] = PatternCraftableObj[gridValue];
                        RequiredItems.Add(PatternCraftableObj[gridValue]);
                        Debug.Log($"Adding object to the required items of{name}  Count is now {RequiredItems.Count}");
                        
                    }
                    else
                    {
                        Debug.Log($"Slot of {name} is either null or out of index. Count is {RequiredItems.Count}");
                        Pattern[x, y] = null; // Ensure it's cleared if not used
                    }
                    index++;
                }
            }

            // If the recipe is smaller than 3x3, clear out unused cells
            // for(int y = PatternSizeY; y < 3; y++)
            // {
            //     for(int x = 0; x < 3; x++)
            //     {
            //         Pattern[x, y] = null;
            //     }
            // }
            // for(int x = PatternSizeX; x < 3; x++)
            // {
            //     for(int y = 0; y < PatternSizeY; y++)
            //     {
            //         Pattern[x, y] = null;
            //     }
            // }

            Debug.Log($"InitializeRequiredItems finished. Found {RequiredItems.Count} items in the pattern.");
        }

        // public void InitializeRequiredItems()
        // {
        //     // RequiredItems = new List<ItemDefinition >();
        //     RequiredItems = new List<ScriptableObject>();
        //     // HashSet<ItemDefinition > uniqueItems = new HashSet<ItemDefinition >();
        //     // foreach (var item in PatternTest)
        //     foreach (var item in PatternCraftableObj)
        //     {
        //         if (item != null)
        //         {
        //             RequiredItems.Add(item);
        //             Debug.Log("Unique item found, adding it");
        //         }
        //     }
        //     int index=0;
        //     for(int y = 0;y<3;y++){
        //         for(int x = 0;x<3;x++){
        //             if(PatternCraftableObj[index]!=null){
        //                 Pattern[x,y]=PatternCraftableObj[index];
        //             }else{
        //                 Debug.Log($"spot at {x},{y} is empty");
        //             }
        //             index++;
        //         }
        //     }
        //     // LogPatternItems();
        //     // RequiredItems = new List<ItemDefinition >(uniqueItems);
        // }


        /// <summary>
        /// (Optional) Logs what's in the Pattern array, up to PatternSizeX,PatternSizeY.
        /// Helps debug orientation / indexing issues.
        /// </summary>
        public void LogPatternItems()
        {
            Debug.Log($"Logging Pattern items for a {PatternSizeX}x{PatternSizeY} recipe:");
            for (int y = 0; y < PatternSizeY; y++)
            {
                for (int x = 0; x < PatternSizeX; x++)
                {
                    var item = Pattern[x, y];
                    if (item != null)
                        Debug.Log($"  Slot ({x},{y}): {item.name}");
                    else
                        Debug.Log($"  Slot ({x},{y}): [Empty]");
                }
            }
        }
    }

    

    
}