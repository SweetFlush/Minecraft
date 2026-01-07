using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons{
    public interface ICraftingItem 
    {
        public string GetName();
        public Sprite GetIcon();

        public ItemDefinition GetItemDef();
    }
    }