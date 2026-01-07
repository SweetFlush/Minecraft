using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;
using Autohand;

namespace VR_Minecraft
{
    public class VR_VoxelPlayer : MonoBehaviour, IVoxelPlayPlayer
    {
        public List<InventoryItem> items => throw new System.NotImplementedException();

        public int selectedItemIndex { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string playerName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public float hitDelay { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public int hitDamage { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public float hitRange { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public int hitDamageRadius { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public Color selectedItemTintColor { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public event OnPlayerInventoryEvent OnItemSelectedChanged;
        public event OnPlayerGetDamageEvent OnPlayerGetDamage;
        public event OnPlayerIsKilledEvent OnPlayerIsKilled;
        public event OnPlayerInventoryItemQuantityChange OnItemAdded;
        public event OnPlayerInventoryItemQuantityChange OnItemConsumed;
        public event OnPlayerInventoryClear OnItemsClear;

        public void AddInventoryItem(ItemDefinition[] newItems)
        {
            throw new System.NotImplementedException();
        }

        public bool AddInventoryItem(ItemDefinition newItem, float quantity = 1)
        {
            throw new System.NotImplementedException();
        }

        public bool AddInventoryItem(ItemDefinition newItem, int durability, float _quantity)
        {
            throw new System.NotImplementedException();
        }

        public void ConsumeAllItems()
        {
            throw new System.NotImplementedException();
        }

        public InventoryItem ConsumeItem()
        {
            throw new System.NotImplementedException();
        }

        public void ConsumeItem(ItemDefinition item)
        {
            throw new System.NotImplementedException();
        }

        public void ConsumeItem(ItemDefinition item, float amount)
        {
            throw new System.NotImplementedException();
        }

        public void DamageToPlayer(int damagePoints)
        {
            throw new System.NotImplementedException();
        }

        public void DropAllItems()
        {
            throw new System.NotImplementedException();
        }

        public int GetHitDamage()
        {
            throw new System.NotImplementedException();
        }

        public int GetHitDamageRadius()
        {
            throw new System.NotImplementedException();
        }

        public float GetHitDelay()
        {
            throw new System.NotImplementedException();
        }

        public float GetHitRange()
        {
            throw new System.NotImplementedException();
        }

        public InventoryItem GetInventoryItem(VoxelDefinition voxelDefinition)
        {
            throw new System.NotImplementedException();
        }

        public float GetItemQuantity(ItemDefinition item)
        {
            throw new System.NotImplementedException();
        }

        public List<InventoryItem> GetPlayerItems()
        {
            throw new System.NotImplementedException();
        }

        public InventoryItem GetSelectedItem()
        {
            throw new System.NotImplementedException();
        }

        public Transform GetTransform()
        {
            throw new System.NotImplementedException();
        }

        public bool HasItem(ItemDefinition item)
        {
            throw new System.NotImplementedException();
        }

        public void PickUpItem(ItemDefinition newItem, float quantity = 1)
        {
            throw new System.NotImplementedException();
        }

        public bool SetSelectedItem(int itemIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool SetSelectedItem(InventoryItem item)
        {
            throw new System.NotImplementedException();
        }

        public bool SetSelectedItem(VoxelDefinition v)
        {
            throw new System.NotImplementedException();
        }

        public void UnSelectItem()
        {
            throw new System.NotImplementedException();
        }
    }
}
