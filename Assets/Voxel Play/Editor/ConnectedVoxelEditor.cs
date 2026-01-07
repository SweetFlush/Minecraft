using UnityEditor;
using UnityEngine;

namespace VoxelPlay {

    [CustomEditor(typeof(ConnectedVoxel))]
    public class ConnectedVoxelEditor : Editor {

        SerializedProperty voxelDefinition;
        SerializedProperty ruleEvent;
        SerializedProperty config;

        void OnEnable() {
            voxelDefinition = serializedObject.FindProperty("voxelDefinition");
            ruleEvent = serializedObject.FindProperty("ruleEvent");
            config = serializedObject.FindProperty("config");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(voxelDefinition, new GUIContent("Placing Voxel", "These rules will be applied when placing this voxel in the world."));
            EditorGUILayout.PropertyField(ruleEvent, new GUIContent("Event", "Choose if these rules are applied when placing a voxel or when rendering. If rules are applied when placing, the voxel will actually be changed in the chunk. However, if you select 'When Rendering', the contents of the chunk won't be modified, only the representation will change."));
            EditorGUILayout.HelpBox("인접한 프리팹이 어떻게 연결되는지와 각 상황에서 사용할 동작 및 프리팹을 지정하세요.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Expand All", GUILayout.Width(100))) {
                ToggleExpand(true);
            }
            if (GUILayout.Button("Collapse All", GUILayout.Width(100))) {
                ToggleExpand(false);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(config, new GUIContent("Configuration"), true);
            serializedObject.ApplyModifiedProperties();
        }

        void ToggleExpand(bool expanded) {
            ConnectedVoxel c = (ConnectedVoxel)target;
            if (c != null && c.config != null) {
                for (int k = 0; k < c.config.Length; k++) {
                    c.config[k].foldout = expanded;
                }
            }
            EditorUtility.SetDirty(config.objectReferenceValue);
        }
    }

}
