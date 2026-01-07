using UnityEngine;
using VoxelPlay;
using System;

namespace IncredibleExtensions.VPAddons{
    [Serializable]
    public struct CraftingOutputResult{
        [CraftingItem]
        public ScriptableObject _CraftingOutput;
        public int _Quantity;
        
    }

    public class CraftingSystemsHelper 
    {
        public static ItemDefinition GetItemDefFromScriptableObject(ScriptableObject _scriptableObject){
            ItemDefinition patternItemDef=null;
            //If the pattenr item is an item definition
            if(_scriptableObject.GetType()==typeof(ItemDefinition)){
                patternItemDef = _scriptableObject as ItemDefinition;
            }

            if(_scriptableObject.GetType()==typeof(VoxelDefinition)){
                VoxelDefinition voxelDefinition = _scriptableObject as VoxelDefinition;

                if(voxelDefinition.dropItem==null){ //If you did not assign a specific definition to this voxel we'll get the default one.
                    ItemDefinition def = VoxelPlayEnvironment.instance.GetItemDefinition(ItemCategory.Voxel, voxelDefinition);
                    if(def!=null){
                        patternItemDef=def;
                    }
                }
            }
            return patternItemDef;
        }
    }
}