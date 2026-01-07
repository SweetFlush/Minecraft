using UnityEngine;
using UnityEngine.UI;
using VoxelPlay;



namespace IncredibleExtensions.VPAddons{
    public class CraftingOutput : MonoBehaviour
    {
        CraftingSystem craftingSystem;
        InventoryItem myInventoryItem;

        [SerializeField]
        RawImage image;


        //at the moment the two texts are inverted (the quantity text in editor is set as the shadow and vice versa)

        [SerializeField]
        Text quantityText;
        [SerializeField]
        Text quantityShadowText;


        void Start(){
            craftingSystem=GetComponentInParent<CraftingSystem>();
            image=GetComponent<RawImage>();
        }

        public void SetItem(InventoryItem item){
            myInventoryItem=item;
            RefreshVisual();
        }
        public void SetItem(CraftingRecipe recipe){
            // myInventoryItem=new InventoryItem();
            // if(craftingResult._CraftingOutput is VoxelDefinition){
            //     myInventoryItem.item=craftingResult._CraftingOutput.GetItemDefinition();
            // }
            if(recipe.MyCraftingOutputResult._CraftingOutput==null){
                // Debug.LogErrorFormat($"RECIPE {recipe.name} IS MATCHING, BUT NO OUTPUT IS SET");
                return;
            }
            ItemDefinition itemDef=CraftingSystemsHelper.GetItemDefFromScriptableObject(recipe.MyCraftingOutputResult._CraftingOutput);
            myInventoryItem.item=itemDef    ;
            myInventoryItem.quantity=recipe.MyCraftingOutputResult._Quantity;
            
            //Added for durability
            if(recipe.MyCraftingOutputResult._CraftingOutput.GetType()==typeof(ItemDefinition)){
                var outputItem = recipe.MyCraftingOutputResult._CraftingOutput as ItemDefinition;
                myInventoryItem.durabilityLeft = outputItem.GetPropertyValue<int>("durability");
                // Debug.Log("Durability inserted in crafted item:" + myInventoryItem.durabilityLeft);
            }else{
                // Debug.Log("Durability not inserted " );
                myInventoryItem.durabilityLeft=0;
            }

            RefreshVisual();
        }


        public void TakeCraftedItem(){
            if(myInventoryItem.item==null){
                return;
            }
            craftingSystem.CraftedObjectTaken(myInventoryItem);
        }


        public void Reset(){
            myInventoryItem.item=null;
            myInventoryItem.quantity=0;
            RefreshVisual();
        }

        void RefreshVisual(){
            if(myInventoryItem.item!=null){
                image.texture = myInventoryItem.item.icon; //Set image
                quantityText.enabled=true; //Set texts
                quantityShadowText.enabled=true;
                quantityText.text=myInventoryItem.quantity.ToString();
                quantityShadowText.text=myInventoryItem.quantity.ToString();
            }else
            { //Set everything back to null
                image.texture=null;
                quantityText.enabled=false;
                quantityShadowText.enabled=false;
            }
        }
        
    }
}