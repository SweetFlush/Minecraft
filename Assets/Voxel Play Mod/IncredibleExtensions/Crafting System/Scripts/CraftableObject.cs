using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons{

    /// <summary>
    /// Classed used to let the class "ItemDefinition" and "VoxelDefinition" inherit from this, in order to be able to use both of them in
    /// the recipe list, and as crafting outputs.
    /// </summary>
    public abstract class CraftableObject : ScriptableObject
    {
        public abstract string GetName();
        public abstract Texture2D GetIcon();
        public abstract ItemDefinition GetItemDefinition(); //Used to get the definition of the item (Useful in the VoxelDefinition, check that for more info)
        
    }

}