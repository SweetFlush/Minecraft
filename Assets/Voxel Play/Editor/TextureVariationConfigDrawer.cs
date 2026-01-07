using UnityEditor;
using UnityEngine;

namespace VoxelPlay {

    [CustomPropertyDrawer(typeof(TextureVariationConfig))]
    public class TextureVariationConfigDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

            const float w = 110f;
            const float sw = 120f;

            position.y += 6;
            position.height = lineHeight;
            position.width = w;

            int index = property.GetArrayIndex();
            if (index == 0) {
                Rect prevPosition = position;

                GUI.Label(position, "Texture");
                position.x += sw;
                GUI.Label(position, "Normal Map");
                position.x += sw;
                GUI.Label(position, "Probability");
                position.x += sw;

                position = prevPosition;
                position.y += lineHeight;
            }

            EditorGUI.BeginChangeCheck();

            SerializedProperty texture = property.FindPropertyRelative("texture");
            EditorGUI.ObjectField(position, texture, GUIContent.none);
            position.x += sw;
            SerializedProperty normalMap = property.FindPropertyRelative("normalMap");
            EditorGUI.ObjectField(position, normalMap, GUIContent.none);
            position.x += sw;
            position.width = EditorGUIUtility.currentViewWidth - position.x - 15;
            SerializedProperty probability = property.FindPropertyRelative("probability");
            EditorGUI.Slider(position, probability, 0, 1, GUIContent.none);

            if ((EditorGUI.EndChangeCheck() || GUI.enabled) && !Application.isPlaying) {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) {
            int lines = prop.GetArrayIndex() == 0 ? 2 : 1;
            return lines * EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + 4f;
        }

    }
}