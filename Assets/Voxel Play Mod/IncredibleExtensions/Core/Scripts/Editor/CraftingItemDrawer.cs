using UnityEditor;
using UnityEngine;
using VoxelPlay;

namespace IncredibleExtensions.VPAddons{
    [CustomPropertyDrawer(typeof(CraftingItemAttribute))]
    public class CraftingItemDrawer : PropertyDrawer
    {
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Rect buttonRect = new Rect(position.x + position.width - 25, position.y, 25, position.height);
            Rect fieldRect = new Rect(position.x, position.y, position.width - 30, position.height);
            // Rect clearButtonRect = new Rect(position.x + position.width - 25, position.y, 25, position.height);  // Clear "X" button
            
            Rect labelRect = new Rect(position.x, position.y, position.width - 55, position.height);  // Label to show the selected object
            Rect buttonRect = new Rect(position.x + position.width - 50, position.y, 25, position.height); // Custom "..." button
            Rect clearButtonRect = new Rect(position.x + position.width - 25, position.y, 25, position.height);  // Clear "X" button

            

            
            // Display the object name (without the circle button)
            if (property.objectReferenceValue != null)
            {
                EditorGUI.LabelField(fieldRect, property.objectReferenceValue.name);  // Display the name of the selected object
            }
            else
            {
                EditorGUI.LabelField(fieldRect, "None (ScriptableObject)");  // Default display when no object is selected
            }

            // Custom button with the three dots
            if (GUI.Button(buttonRect, new GUIContent("...")))
            {
                CraftingPickerWindow.Show(property);
            }
            
            if (GUI.Button(clearButtonRect, new GUIContent("X")))
            {
                property.objectReferenceValue = null;  // Clear the selection
                property.serializedObject.ApplyModifiedProperties();  // Apply changes
            }

            EditorGUI.EndProperty();
        }

    }

    public class CraftingPickerWindow : EditorWindow
    {
        SerializedProperty property;
        
        
        private Vector2 scrollPosition;  // For scrollable area
        private string filterText = "";  // For filtering
        private string searchPlaceholder = "Search by name...";  

        public static void Show(SerializedProperty property)
        {
            var window = GetWindow<CraftingPickerWindow>("Select Item");
            window.property = property;
            window.Show();
        }

        
        private void OnGUI()
        {
            // Search bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search: ", GUILayout.Width(50));
            filterText = GUILayout.TextField(filterText, GUILayout.Width(position.width - 60)); // Search input field
            GUILayout.EndHorizontal();

            // Scrollable list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Filtering logic
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is ItemDefinition || asset is VoxelDefinition)
                {
                    // Apply filter: show only items that match the search text
                    if (string.IsNullOrEmpty(filterText) || asset.name.ToLower().Contains(filterText.ToLower()))
                    {
                        if (GUILayout.Button(asset.name))
                        {
                            property.objectReferenceValue = asset;
                            property.serializedObject.ApplyModifiedProperties();
                            Close();
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView(); // End scrollable area
        }

    }
}