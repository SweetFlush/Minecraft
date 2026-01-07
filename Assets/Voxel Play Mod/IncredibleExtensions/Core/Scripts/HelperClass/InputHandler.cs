using UnityEngine;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons{
    public static class InputHandler 
    {
        /// <summary>
        /// Back to the game, get back controls
        /// </summary>
        /// <param name="hideCursor">if for any reason you need to show the cursor: set to false</param>
        public static void EnableAllInputs(bool hideCursor=true){
            VoxelPlayEnvironment.instance.input.enabled = true;  
            if(!hideCursor) return;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

        }
        
        /// <summary>
        /// Disable all the controls and show the cursor when needed
        /// </summary>
        /// <param name="showCursor">set to false if don't want to show the cursor</param>
        public static void DisableAllInputs(bool showCursor=true){
            VoxelPlayEnvironment.instance.input.enabled = false;  
            if(!showCursor) return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

        }

    }
}
