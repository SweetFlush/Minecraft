using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons
{
    public class CraftingSystem : MonoBehaviour
    {
        [SerializeField]
        private int CRAFTING_GRID_SIZE = 3;
        public InventoryItem currentInventoryItem;

        [SerializeField]
        Button[] ingredientsButtons;
        CraftingItemSlot[,] inventorySlots;
        [SerializeField]
        CraftingRecipe[] recipes;

        [Tooltip("Just as debug to see the recipes possible updating live")]
        List<CraftingRecipe> potentialRecipes;

        [SerializeField]
        CraftingOutput craftingOutput;

        IVoxelPlayPlayer myPlayer;


        [SerializeField]
        Transform craftingGrid;

        [SerializeField]
        HorizontalLayoutGroup rowHolder;
        [SerializeField]
        Button ingredientButtonTemplate;

        [SerializeField][Tooltip("Will rebuild to the new size (CRAFTING_GRID_SIZE) on start")]
        bool rebuildOnStart;
        [SerializeField]
        int newGridSizeOnStart=5;

        void Start()
        {
            myPlayer = VoxelPlayPlayer.instance;
            inventorySlots = new CraftingItemSlot[CRAFTING_GRID_SIZE, CRAFTING_GRID_SIZE];
            currentInventoryItem.item = null;
            int buttonIndex = 0;
            potentialRecipes = new List<CraftingRecipe>();
        
            if(craftingGrid==null){
                craftingGrid=transform.Find("CraftingGrid");
            }
            if(rebuildOnStart){
                SetNewCraftingSizeAndBuild(newGridSizeOnStart);
            }else{
                // Assign 9 ingredient buttons to inventorySlots (row=i, col=j)
                for (int i = 0; i < CRAFTING_GRID_SIZE; i++)
                {
                    for (int j = 0; j < CRAFTING_GRID_SIZE; j++)
                    {
                        inventorySlots[i, j] = ingredientsButtons[buttonIndex].GetComponent<CraftingItemSlot>();
                        buttonIndex++;
                    }
                }
            }
        }

        void OEnable()
        {
            InitRecipes();
            
        }


        #region Player Inventory Handling

        public void AddItem(ItemDefinition newItem, float _quantity = 1)
        {
            myPlayer.AddInventoryItem(newItem, _quantity);
            myPlayer.AddInventoryItem(newItem, 0f); // Forces UI refresh
        }

        public void RemoveItem(ItemDefinition item, int _quantity)
        {
            myPlayer.ConsumeItem(item, _quantity);
        }

        public void AddItem(ItemDefinition newItem, float _quantity = 1, int durability = 1)
        {
            myPlayer.AddInventoryItem(newItem, durability, _quantity);
            myPlayer.AddInventoryItem(newItem, durability, 0f); // Forces UI refresh
        }

        #endregion

        public void SetNewCraftingSizeAndBuild(int newSize){
            // for (int i = 0; i < CRAFTING_GRID_SIZE; i++)
            // {
            //     for (int j = 0; j < CRAFTING_GRID_SIZE; j++)
            //     {
            //         Destroy(ingredientsButtons[buttonIndex].gameObject);
            //         buttonIndex++;
            //     }
            // }

            var gridRows = craftingGrid.GetComponentsInChildren<HorizontalLayoutGroup>();
            foreach(var obj in gridRows){
                Destroy(obj.gameObject);
            }
            CRAFTING_GRID_SIZE=newSize;
            ingredientsButtons= new Button[newSize*newSize];
            inventorySlots = new CraftingItemSlot[CRAFTING_GRID_SIZE, CRAFTING_GRID_SIZE];

            int buttonIndex = 0;
             for (int i = 0; i < CRAFTING_GRID_SIZE; i++)
            {
                var newRow= Instantiate(rowHolder,craftingGrid);
                newRow.gameObject.SetActive(true);
                for (int j = 0; j < CRAFTING_GRID_SIZE; j++)
                {
                    var button=Instantiate(ingredientButtonTemplate,newRow.transform);
                    ingredientsButtons[buttonIndex]=button;
                    inventorySlots[i,j]=button.GetComponent<CraftingItemSlot>();
                    button.gameObject.SetActive(true);
                    buttonIndex++;
                }
            }

        }
        public void SetNewCraftingSize(int newSize, Button[] newButtons){
            CRAFTING_GRID_SIZE=newSize;
            
            ingredientsButtons= new Button[newSize*newSize];
            inventorySlots = new CraftingItemSlot[CRAFTING_GRID_SIZE, CRAFTING_GRID_SIZE];

            int buttonIndex = 0;
            for (int i = 0; i < CRAFTING_GRID_SIZE; i++)
            {
                for (int j = 0; j < CRAFTING_GRID_SIZE; j++)
                {
                    ingredientsButtons=newButtons;
                    inventorySlots[i, j] = newButtons[buttonIndex].GetComponent<CraftingItemSlot>();
                    buttonIndex++;
                }
            }

        }

        /// <summary>
        /// Called when placing an object from your "hand" (currentInventoryItem) into a slot.
        /// Decrements the "hand" quantity, then checks recipes.
        /// </summary>
        public void PlaceObject()
        {
            if (currentInventoryItem != null)
            {
                currentInventoryItem.quantity--;
                if (currentInventoryItem.quantity <= 0)
                {
                    currentInventoryItem.item = null;
                }
                CheckRecipes();
            }
        }

        /// <summary>
        /// Called when the crafted item is taken from the output.
        /// Adds the crafted item to the player's inventory, and removes used items from the 3x3.
        /// Then re-checks recipes.
        /// </summary>
        public void CraftedObjectTaken(InventoryItem inventoryItem)
        {
            // Add the result to player inventory
            AddItem(inventoryItem.item, inventoryItem.quantity, inventoryItem.durabilityLeft);

            // Remove used items from the 3x3
            foreach (CraftingItemSlot inventorySlot in inventorySlots)
            {
                if (inventorySlot.MyInventoryItem.item != null)
                {
                    inventorySlot.ReduceQuantity();
                }
            }
            CheckRecipes();
        }

        /// <summary>
        /// Main method that checks the current 3x3 grid items against all recipes.
        /// If a recipe matches, sets the craftingOutput accordingly.
        /// </summary>
        void CheckRecipes()
        {
            craftingOutput.Reset();
            if (recipes.Length > 0)
            {
                // Build a local 3x3 array of the items in each slot
                ItemDefinition[,] items = new ItemDefinition[CRAFTING_GRID_SIZE, CRAFTING_GRID_SIZE];
                for (int i = 0; i < CRAFTING_GRID_SIZE; i++)
                {
                    for (int j = 0; j < CRAFTING_GRID_SIZE; j++)
                    {
                        items[i, j] = inventorySlots[i, j].MyInventoryItem.item;
                        if (items[i, j] != null)
                        {
                            Debug.Log($"Position {i},{j} is not null!... {inventorySlots[i,j].name}");
                        }
                    }
                }

                // Update which recipes could be crafted based on item counts
                UpdatePotentialRecipes(items);

                // Now check each "potential" recipe for actual pattern match
                foreach (CraftingRecipe recipe in potentialRecipes)
                {
                    if (IsRecipeMatch(recipe, items))
                    {
                        Debug.Log($"Seems like {recipe.name} is matching!");
                        craftingOutput.SetItem(recipe); // show the result
                        return;
                    }
                    else
                        Debug.Log($"{recipe.name} is not matching the current recipe!");
                }
            }
        }
        

        public void LogPatternItems()
        {
            for (int x = 0; x < CRAFTING_GRID_SIZE; x++)
            {
                for (int y = 0; y < CRAFTING_GRID_SIZE; y++)
                {
                    if (inventorySlots[x, y].MyInventoryItem.item != null)
                    {
                        var item = inventorySlots[x, y];
                        Debug.Log("Item found in pattern at (" + x + ", " + y + "): " + item.MyInventoryItem.item.GetTitleOrName());
                    }
                    else
                    {
                        Debug.Log("No item found in pattern at (" + x + ", " + y + ") ");
                    }
                }
            }
        }

        /// <summary>
        /// Clears the 3x3 slots and resets the output
        /// </summary>
        public void Reset()
        {
            if (inventorySlots == null) return;
            foreach (CraftingItemSlot inventorySlot in inventorySlots)
            {
                inventorySlot.Reset();
            }
            craftingOutput.Reset();
        }

        #region Crafting Methods

        /// <summary>
        /// Updates the "potentialRecipes" list by checking if the grid has enough items (count-based).
        /// </summary>
        public void UpdatePotentialRecipes(ItemDefinition[,] grid)
        {
            potentialRecipes.Clear();
            HashSet<ItemDefinition> itemsInGrid = MaterialUsed();

            foreach (var recipe in recipes)
            {
                recipe.InitializeRequiredItems();
                if (CanRecipeBeCraftedWithItems(recipe, grid))
                {
                    potentialRecipes.Add(recipe);
                }
            }
        }

        HashSet<ItemDefinition> MaterialUsed()
        {
            HashSet<ItemDefinition> itemsUsed = new HashSet<ItemDefinition>();
            foreach (CraftingItemSlot inventorySlot in inventorySlots)
            {
                itemsUsed.Add(inventorySlot.MyInventoryItem.item);
            }
            return itemsUsed;
        }

        #endregion

        #region Crafting Checks

        /// <summary>
        /// Checks if a recipe's pattern can match somewhere in the 3x3 grid (no partial out-of-bounds).
        /// </summary>
        private bool IsRecipeMatch(CraftingRecipe recipe, ItemDefinition[,] grid)
        {
            // 1) Normalize the 3x3 grid to bounding box
            ItemDefinition[,] normalizedGrid = NormalizeGrid(grid, out int w, out int h);

            // 2) Compare bounding box size to recipe pattern size
            if (w != recipe.PatternSizeX || h != recipe.PatternSizeY)
            {
                // If bounding box doesn't match the pattern dimension, no match
                Debug.Log($"Recipe {recipe.name} expects {recipe.PatternSizeX}x{recipe.PatternSizeY}, but bounding box is {w}x{h}.");
                return false;
            }

            // 3) Compare each cell in the bounding box to recipe.Pattern[x,y]
            // Be consistent if your Pattern is stored as [x,y] = [col,row].
            for (int row = 0; row < h; row++)
            {   
                for (int col = 0; col < w; col++)
                {
                    ScriptableObject patternSO = recipe.Pattern[col, row]; // col= x, row=y
                    ItemDefinition placedItem = normalizedGrid[row, col];  // row then col

                    if (patternSO != null)
                    {
                        // Pattern expects a specific item
                        ItemDefinition patternItemDef = CraftingSystemsHelper.GetItemDefFromScriptableObject(patternSO);
                        if (placedItem != patternItemDef)
                        {
                            Debug.Log($"Mismatch at normalized[{row},{col}]. Pattern wants {patternItemDef?.name}, found {placedItem?.name}");
                            return false;
                        }
                    }
                    else
                    {
                        // Pattern expects empty
                        if (placedItem != null)
                        {
                            Debug.Log($"Expected empty at normalized[{row},{col}], found {placedItem?.name}");
                            return false;
                        }
                    }
                }
            }

            Debug.Log($"Bounding box matched {recipe.name} exactly!");
            return true;
        }


        /// <summary>
        /// Checks if recipe.Pattern (the 2D array) matches the 3x3 grid at [offsetRow, offsetCol].
        /// NOTE: No partial out-of-bounds – if it extends outside, fails.
        /// </summary>
        private bool IsPatternAtOffset(
            CraftingRecipe recipe,
            ItemDefinition[,] grid,
            int offsetRow, 
            int offsetCol,
            int patternSizeY, 
            int patternSizeX)
        {
            Debug.Log($"Checking pattern {recipe.name} at offsetRow={offsetRow}, offsetCol={offsetCol}");

            for (int y = 0; y < patternSizeY; y++)
            {
                for (int x = 0; x < patternSizeX; x++)
                {
                    int gridRow = offsetRow + y;
                    int gridCol = offsetCol + x;

                    // Out-of-bounds check
                    if (gridRow >= CRAFTING_GRID_SIZE || gridCol >= CRAFTING_GRID_SIZE)
                    {
                        Debug.Log("Grid position is out of index => fail");
                        return false;
                    }

                    // The pattern expects this item (or null)
                    ScriptableObject patternItem = recipe.Pattern[x,y]; 
                    ItemDefinition gridItem = grid[gridRow, gridCol];

                    if (patternItem != null)
                    {
                        // Convert ScriptableObject => ItemDefinition
                        ItemDefinition patternItemDef = CraftingSystemsHelper.GetItemDefFromScriptableObject(patternItem);
                        Debug.Log($"Pattern item is not null: {patternItemDef.GetTitleOrName()}");

                        // If mismatch => fail
                        if (gridItem != patternItemDef)
                        {
                            Debug.Log($"Mismatch: gridItem at [{gridRow},{gridCol}] is {gridItem?.name}, pattern wants {patternItemDef?.name}");
                            return false;
                        }
                    }
                    else
                    {
                        // Pattern expects empty
                        if (gridItem != null)
                        {
                            Debug.Log("Empty slot expected but it's filled => fail");
                            return false;
                        }
                    }
                }
            }

            Debug.Log("No problems detected => pattern matches");
            return true;
        }


        ItemDefinition[,] NormalizeGrid(ItemDefinition[,] originalGrid, out int normWidth, out int normHeight)
        {
            int rows = originalGrid.GetLength(0);
            int cols = originalGrid.GetLength(1);

            int minRow = rows, maxRow = -1, minCol = cols, maxCol = -1;

            // 1) Find bounding box
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (originalGrid[r, c] != null)
                    {
                        if (r < minRow) minRow = r;
                        if (r > maxRow) maxRow = r;
                        if (c < minCol) minCol = c;
                        if (c > maxCol) maxCol = c;
                    }
                }
            }

            // If no items found, bounding box is empty => return a 0x0 or 1x1 array
            if (maxRow == -1) 
            {
                normWidth = normHeight = 0;
                return new ItemDefinition[0,0];
            }

            // 2) Define bounding box size
            normHeight = (maxRow - minRow + 1);
            normWidth = (maxCol - minCol + 1);

            // 3) Create normalized array
            ItemDefinition[,] normalized = new ItemDefinition[normHeight, normWidth]; 
            // Or you can define [normWidth, normHeight], but be consistent

            for (int r = 0; r < normHeight; r++)
            {
                for (int c = 0; c < normWidth; c++)
                {
                    // shift from bounding box down to (0,0)
                    normalized[r,c] = originalGrid[minRow + r, minCol + c];
                }
            }

            return normalized;
        }


        /// <summary>
        /// Checks if the grid has the correct item counts to craft this recipe (i.e., enough wood, etc.).
        /// </summary>
        private bool CanRecipeBeCraftedWithItems(CraftingRecipe recipe, ItemDefinition[,] grid)
        {
            // 1) Build dictionary of required item -> count
            Dictionary<ItemDefinition, int> requiredItems = GetItemCounts(recipe.RequiredItems);

            // 2) Build dictionary of what's in the 3x3
            Dictionary<ItemDefinition, int> itemsInGrid = GetItemCountsFromGrid(grid);

            // 3) Check if we have exactly what's needed, no extras
            foreach (var kvp in requiredItems)
            {
                ItemDefinition requiredItem = kvp.Key;
                int requiredCount = kvp.Value;
                Debug.Log($"{recipe.name} needs {requiredCount} of {requiredItem.GetTitleOrName()}");

                // If the grid doesn't have enough, fail
                if (!itemsInGrid.TryGetValue(requiredItem, out int gridCount) || gridCount != requiredCount)
                {
                    Debug.Log("Required items are not matched for " + recipe.name);
                    return false;
                }
            }

            // 4) Ensure no extra items are in the grid that the recipe doesn't require
            foreach (var kvp in itemsInGrid)
            {
                ItemDefinition gridItem = kvp.Key;
                int gridCount = kvp.Value;

                if (!requiredItems.ContainsKey(gridItem))
                {
                    Debug.Log("Grid has extra item: " + gridItem.name + " => fail for " + recipe.name);
                    return false;
                }
            }

            Debug.Log("Recipe quantities matching! " + recipe.name);
            return true; 
        }

        // We store item->count for all items in "recipe.RequiredItems"
        private Dictionary<ItemDefinition, int> GetItemCounts(IEnumerable<ScriptableObject> items)
        {
            Dictionary<ItemDefinition, int> itemCounts = new Dictionary<ItemDefinition, int>();
            foreach (var so in items)
            {
                if (so == null) continue;
                ItemDefinition def = CraftingSystemsHelper.GetItemDefFromScriptableObject(so);
                if (def == null) continue;

                if (!itemCounts.ContainsKey(def))
                    itemCounts[def] = 0;
                itemCounts[def]++;
            }
            return itemCounts;
        }

        // We store item->count for everything found in "grid"
        private Dictionary<ItemDefinition, int> GetItemCountsFromGrid(ItemDefinition[,] grid)
        {
            Dictionary<ItemDefinition, int> counts = new Dictionary<ItemDefinition, int>();
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    ItemDefinition item = grid[r,c];
                    if (item != null)
                    {
                        if (!counts.ContainsKey(item))
                            counts[item] = 0;
                        counts[item]++;
                    }
                }
            }
            return counts;
        }

        void InitRecipes(){
            foreach (CraftingRecipe recipe in potentialRecipes)
            {
                // Ensure the recipe is initialized before checking it
                recipe.InitializeRequiredItems();

            }
        }

        #endregion


        public void EqualShareContent(){
            //InventoriesHelper.EqualShareContent(inventorySlots);
            // Dictionary<ItemDefinition, List<CraftingItemSlot>> itemGroups = new Dictionary<ItemDefinition, List<CraftingItemSlot>>();

            // // Step 1: Group slots by item type
            // foreach (var slot in inventorySlots)
            // {
            //     if (slot.MyInventoryItem != InventoryItem.Null)
            //     {
            //         ItemDefinition itemType = slot.MyInventoryItem.item;
            //         if (!itemGroups.ContainsKey(itemType))
            //         {
            //             itemGroups[itemType] = new List<CraftingItemSlot>();
            //         }
            //         itemGroups[itemType].Add(slot);
            //     }
            // }

            // // Step 2: Distribute items evenly for each group
            // foreach (var kvp in itemGroups)
            // {
            //     List<CraftingItemSlot> slots = kvp.Value;
            //     int totalQuantity = slots.Sum(slot => (int)slot.MyInventoryItem.quantity);
            //     int equalShare = totalQuantity / slots.Count;
            //     int remainder = totalQuantity % slots.Count;

            //     // Step 3: Redistribute items using SetItem()
            //     for (int i = 0; i < slots.Count; i++)
            //     {
            //         int newQuantity = (i < remainder) ? (equalShare + 1) : equalShare;

            //         // Ensure we're setting a new InventoryItem instance
            //         InventoryItem updatedItem = new InventoryItem
            //         {
            //             item = slots[i].MyInventoryItem.item,
            //             quantity = newQuantity,
            //             durabilityLeft = slots[i].MyInventoryItem.durabilityLeft // Keep durability unchanged
            //         };

            //         slots[i].SetItem(0, updatedItem); // Assuming slotIndex isn't needed for crafting slots
            //         // slots[i].UpdateUI(); // Ensure UI refreshes
            //     }
            // }
        }
    }
    
}
