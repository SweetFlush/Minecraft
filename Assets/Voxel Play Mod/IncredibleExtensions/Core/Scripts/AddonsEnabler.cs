using VoxelPlay;
namespace IncredibleExtensions.VPAddons{
    public partial class CustomGameUI : VoxelPlayUI {

        void EnableAddons(){
            #if HAS_CHEST_ADDON
            EnableChestAddOn();
            #endif
            #if HAS_MACHINERY_ADDON
            EnableMachineryAddOn();
            #endif
            #if HAS_CRAFTING_ADDON
            EnableCraftingAddon();
            #endif
            #if HAS_SURVIVAL_ADDON
            EnableSurvivalAddon();
            #endif
            
        }

        void DisableAddons(){
            #if HAS_CHEST_ADDON
            DisableChestAddOn();
            #endif
            #if HAS_MACHINERY_ADDON
            DisableMachineryAddOn();
            #endif
            #if HAS_CRAFTING_ADDON
            DisableCraftingAddon();
            #endif
            #if HAS_SURVIVAL_ADDON
            DisableSurvivalAddon();
            #endif


            
        }

        void OnStartAddons(){
            #if HAS_CHEST_ADDON
            InitializeChestAddOn();
            #endif
            #if HAS_MACHINERY_ADDON
            InitializeMachineryAddon();
            #endif
            #if HAS_CRAFTING_ADDON
            InitializeCraftingAddon();
            #endif
            #if HAS_SURVIVAL_ADDON
            InitializeSurvivalAddon();
            #endif


        }
    }
}