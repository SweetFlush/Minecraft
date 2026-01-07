using UnityEditor;
using UnityEngine;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons{


    [CustomEditor(typeof(CraftingRecipe)),CanEditMultipleObjects]
    public class CraftingRecipeEditor : Editor
    {
        // public override void OnInspectorGUI() //Old, functioning
        // {
        //     DrawDefaultInspector();

        //     CraftingRecipe recipe = (CraftingRecipe)target;
        //     if (GUILayout.Button("Update Required Items"))
        //     {
        //         recipe.InitializeRequiredItems();
        //         EditorUtility.SetDirty(recipe); // Marks the recipe as modified
        //     }
        // }


        //New

        private SerializedProperty patternCraftableObj;
        private SerializedProperty patternSizeX;
        private SerializedProperty patternSizeY;
        private SerializedProperty outputResult;
        private Texture2D defaultIcon;

        private void OnEnable()
        {
            patternCraftableObj = serializedObject.FindProperty("PatternCraftableObj");
            patternSizeX = serializedObject.FindProperty("PatternSizeX");
            patternSizeY = serializedObject.FindProperty("PatternSizeY");
            outputResult = serializedObject.FindProperty("MyCraftingOutputResult");

            // Default empty square texture
            defaultIcon = Texture2D.grayTexture;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Visual Grid Drawing with Clickable Icons
            DrawGridEditor();

            EditorGUILayout.Space(10);

            // Output Preview (Also as an icon button)
            EditorGUILayout.LabelField("Output Result", EditorStyles.boldLabel);
            DrawCraftingItemIcon(outputResult);

            EditorGUILayout.Space(10);

            CraftingRecipe recipe = (CraftingRecipe)target;

            if (GUILayout.Button("Force Update Required Items"))
            {
                ((CraftingRecipe)target).InitializeRequiredItems();
                // ((CraftingRecipe)target).InitializePattern(recipe.PatternCraftableObj);
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawGridEditor()
        {
            int sizeX = patternSizeX.intValue;
            int sizeY = patternSizeY.intValue;


            EditorGUILayout.LabelField($"Recipe Grid ({sizeX}x{sizeY})", EditorStyles.boldLabel);

            for (int y = 0; y < sizeY; y++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < sizeX; x++)
                {
                    // int index = y * 3 + x; // Convert 2D index to 1D
                    int index = y * sizeX + x; // CHANGED FOR THE NEW _TEST_ SYSTEM. The *3 was sending it out of index for smaller cases, and weird for larger cases
                    if (index < patternCraftableObj.arraySize)
                    {
                        SerializedProperty item = patternCraftableObj.GetArrayElementAtIndex(index);
                        DrawCraftingItemIcon(item);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.HelpBox("💡 Right-click on a slot to clear it.", MessageType.Info);
        }

        void DrawCraftingItemIcon(SerializedProperty property)
        {

            if (property == null)
            {
                Debug.LogError("DrawCraftingItemIcon received a null property!");
                return;
            }

            // if (property.propertyType != SerializedPropertyType.ObjectReference)
            // {
            //     Debug.LogError($"DrawCraftingItemIcon: {property.name} is not an ObjectReference! It's {property.propertyType} instead.");
            //     return;
            // }

            SerializedProperty itemReference = property;
    
            // **Check if this is actually the Crafting Output (Struct, Not Object Reference)**
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                // If it's not an object reference, try getting the `_CraftingOutput` field inside the struct
                SerializedProperty craftingOutputProp = property.FindPropertyRelative("_CraftingOutput");
                
                if (craftingOutputProp != null && craftingOutputProp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    itemReference = craftingOutputProp;  // Use the correct reference
                }
                else
                {
                    Debug.LogError($"DrawCraftingItemIcon: {property.name} is not an ObjectReference and does not have a valid _CraftingOutput.");
                    return;
                }
            }
            // Rect slotRect = GUILayoutUtility.GetRect(64, 64);
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };


            // Ensure it's either a VoxelDefinition or ItemDefinition
            // ScriptableObject selectedItem = property.objectReferenceValue as ScriptableObject;
            // ScriptableObject selectedItem = null;

            ScriptableObject selectedItem = itemReference.objectReferenceValue as ScriptableObject;


            // if (selectedItem == null && property.objectReferenceValue != null)
            // {
            //     Debug.LogError($"DrawCraftingItemIcon: objectReferenceValue is not a ScriptableObject! It's {property.objectReferenceValue.GetType()} instead.");
            //     return;
            // }


            // if (property.objectReferenceValue != null && property.objectReferenceValue is ScriptableObject)
            // {
            //     selectedItem = property.objectReferenceValue as ScriptableObject;
            // }

            if (selectedItem != null && !(selectedItem is VoxelDefinition || selectedItem is ItemDefinition))
            {
                Debug.LogError($"Invalid object reference in {property.propertyPath}. Only VoxelDefinition or ItemDefinition are allowed.");
                property.objectReferenceValue = null;
            }

            // Attempt to get an icon if available
            Texture2D icon = defaultIcon;
            if (selectedItem != null)
            {
                if (selectedItem is ItemDefinition itemDef)
                    icon = itemDef.icon;
                else if (selectedItem is VoxelDefinition voxelDef)
                    icon = voxelDef.GetIcon() ?? defaultIcon;
            }

            // Draw clickable button with the item's icon
            // if (GUI.Button(slotRect, icon, buttonStyle))
            // {
            //     CraftingPickerWindow.Show(property);
            // }

            // // Draw "X" button inside the slot to clear it
            // Rect clearButtonRect = new Rect(slotRect.xMax - 25, slotRect.yMin, 35, 35);
            // if (GUI.Button(clearButtonRect, "X", EditorStyles.miniButton))
            // {
            //     property.objectReferenceValue = null;
            //     property.serializedObject.ApplyModifiedProperties();
            //     Event.current.Use();
            // }

            // // Handle Drag & Drop
            // Event evt = Event.current;
            // if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            // {
            //     if (slotRect.Contains(evt.mousePosition))
            //     {
            //         DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            //         if (evt.type == EventType.DragPerform)
            //         {
            //             DragAndDrop.AcceptDrag();
            //             foreach (var dragged in DragAndDrop.objectReferences)
            //             {
            //                 if (dragged is VoxelDefinition || dragged is ItemDefinition)
            //                 {
            //                     property.objectReferenceValue = dragged;
            //                     property.serializedObject.ApplyModifiedProperties();
            //                     break; // Stop after assigning first valid item
            //                 }
            //             }
            //         }
            //         Event.current.Use();
            //     }
            // }

            // **Draw Slot Button**
            Rect slotRect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
            GUI.Box(slotRect, icon, buttonStyle);


            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && slotRect.Contains(evt.mousePosition))
            {
                if (evt.button == 0)  // Left-click → Open Picker
                {
                    CraftingPickerWindow.Show(itemReference);
                    evt.Use();
                }
                else if (evt.button == 1)  // Right-click → Clear Slot
                {
                    Debug.Log("Right-clicked! Clearing slot...");
                    Undo.RecordObject(itemReference.serializedObject.targetObject, "Clear Crafting Slot");
                    itemReference.objectReferenceValue = null;
                    itemReference.serializedObject.ApplyModifiedProperties();
                    GUI.FocusControl(null);
                    EditorUtility.SetDirty(itemReference.serializedObject.targetObject);
                    evt.Use();
                }
            }

            // **Handle Drag & Drop**
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && slotRect.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var dragged in DragAndDrop.objectReferences)
                    {
                        if (dragged is VoxelDefinition || dragged is ItemDefinition)
                        {
                            Undo.RecordObject(itemReference.serializedObject.targetObject, "Drag Crafting Item");
                            itemReference.objectReferenceValue = dragged;
                            itemReference.serializedObject.ApplyModifiedProperties();
                            break;
                        }
                    }
                }
                evt.Use();
            }
            
        }
    }

}