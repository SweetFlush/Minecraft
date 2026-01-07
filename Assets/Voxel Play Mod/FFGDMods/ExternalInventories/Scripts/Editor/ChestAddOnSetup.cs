#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IncredibleExtensions.VPAddons{

    [InitializeOnLoad]
    public static class ChestAddOnSetup
    {
        static ChestAddOnSetup()
        {
            // Add the scripting define symbol for the Chest add-on if it's not already defined
            AddDefineSymbol("HAS_CHEST_ADDON");
        }

        private static void AddDefineSymbol(string symbol)
        {
            var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            if (!currentSymbols.Contains(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, $"{currentSymbols};{symbol}");
                Debug.Log($"Added scripting define symbol: {symbol}");
            }
        }
    }
    #endif
}