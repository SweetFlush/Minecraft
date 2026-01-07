using UnityEngine;
using UnityEngine.UI;
using VoxelPlay;
namespace IncredibleExtensions.VPAddons{
    public class DragIconController : MonoBehaviour
    {

        public static DragIconController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private RawImage dragIcon;      // Assign in inspector
        
        [SerializeField] private Text quantityText;      // Assign in inspector
        [SerializeField] private Text quantityShadowText;      // Assign in inspector

        [Header("Settings")]
        [SerializeField] private Vector2 offset = new Vector2(20f, -20f);

        // Internal state
        private bool isDragging;
        private Texture currentSprite;

        private void Awake() {
            
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start() {
            // Hide icon on start
            dragIcon.gameObject.SetActive(false);
        }

        private void Update() {
            if (!isDragging) return;

            // Move icon to mouse position + offset
            Vector3 mousePos = Input.mousePosition;
            dragIcon.rectTransform.position = mousePos + (Vector3)offset;
        }

        /// <summary>
        /// Called when we pick up an item to drag.
        /// </summary>
        public void StartDragging(InventoryItem item) {

            Texture2D textureToUSe = item.item.icon;
            float quantity = item.quantity;
            
            if (textureToUSe == null) {
                StopDragging();
                return;
            }

            currentSprite = textureToUSe;
            dragIcon.texture = currentSprite;
            
            quantityShadowText.text=quantity.ToString();
            quantityText.text=quantity.ToString();
            dragIcon.gameObject.SetActive(true);
            isDragging = true;
        }

        /// <summary>
        /// Called when we place or clear the dragged item.
        /// </summary>
        public void StopDragging() {
            isDragging = false;
            dragIcon.gameObject.SetActive(false);
            currentSprite = null;
            quantityShadowText.text="0";
            quantityText.text="0";
        }
    }
}