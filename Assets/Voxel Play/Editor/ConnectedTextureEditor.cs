using UnityEngine;
using UnityEditor;

namespace VoxelPlay
{

    [CustomEditor (typeof (ConnectedTexture))]
    public class ConnectedTextureEditor : UnityEditor.Editor
    {

        SerializedProperty voxelDefinition, neighbourDefinition;
        SerializedProperty config;
        SerializedProperty side;

        void OnEnable ()
        {
            voxelDefinition = serializedObject.FindProperty ("voxelDefinition");
            neighbourDefinition = serializedObject.FindProperty ("neighbourDefinition");
            config = serializedObject.FindProperty ("config");
            side = serializedObject.FindProperty ("side");
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();
            EditorGUILayout.PropertyField (voxelDefinition);
            EditorGUILayout.PropertyField (neighbourDefinition);
            EditorGUILayout.PropertyField (side);
            EditorGUILayout.HelpBox ("어떤 인접 타일이 연결되는지와 각 경우에 사용할 텍스처를 지정하세요. 텍스처를 모자이크 중앙으로 드래그하면 됩니다.", MessageType.Info);
            EditorGUILayout.PropertyField (config, new GUIContent ("Configuration"), true);
            serializedObject.ApplyModifiedProperties ();
        }



    }

}
