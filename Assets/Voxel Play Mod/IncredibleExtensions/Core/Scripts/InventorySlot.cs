using UnityEngine;
using UnityEngine.EventSystems;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons{
    public class InventorySlot : MonoBehaviour, IPointerClickHandler
    {
        private InventorySwapManager swapManager; 
        [SerializeField] private IInventoryContainer container;  // e.g. PlayerInventoryContainer, ChestContainer, etc.

        [SerializeField] private int slotIndex;

        [SerializeField] private bool isPlayerSlot;

        public InventoryItem _inventoryItem;

        public int PlayerInventoryIndex;

        public void OnPointerClick(PointerEventData eventData)
        {
            bool isRightClick = eventData.button == PointerEventData.InputButton.Right;
            
            // Now pass along that info to your swap manager
            swapManager.SelectItem(container, slotIndex, isPlayerSlot, isRightClick);
        }

        public void SetInventorySlot(int slot, IInventoryContainer _container, bool _isPlayerSlot){
            swapManager=InventorySwapManager.Instance;
            slotIndex=slot;
            container=_container;
            isPlayerSlot=_isPlayerSlot;
        }
    }
}
