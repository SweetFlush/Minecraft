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
            EditorGUILayout.PropertyField(voxelDefinition, new GUIContent("Placing Voxel", "월드에 이 복셀을 배치할 때 이 규칙이 적용됩니다."));
            EditorGUILayout.PropertyField(ruleEvent, new GUIContent("Event", "복셀을 배치할 때 적용할지 렌더링할 때 적용할지 선택합니다. 배치 시 적용하면 청크의 복셀 데이터가 실제로 변경됩니다. 하지만 'When Rendering'을 선택하면 청크 내용은 변경되지 않고 표시만 바뀝니다."));
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
