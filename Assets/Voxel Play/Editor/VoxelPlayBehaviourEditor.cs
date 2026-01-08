using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
				
	[CustomEditor (typeof(VoxelPlayBehaviour))]
	public class VoxelPlayBehaviourEditor : UnityEditor.Editor {

        SerializedProperty enableVoxelLight, useVoxelPlayMaterials;
        SerializedProperty forceUnstuck, unstuckOffsetY;
		SerializedProperty checkNearChunks, chunkExtents, renderChunks;
		SerializedProperty useOriginShift;

		void OnEnable () {
			enableVoxelLight = serializedObject.FindProperty ("enableVoxelLight");
			useVoxelPlayMaterials = serializedObject.FindProperty("useVoxelPlayMaterials");
			forceUnstuck = serializedObject.FindProperty ("forceUnstuck");
            unstuckOffsetY = serializedObject.FindProperty("unstuckOffsetY");
			checkNearChunks = serializedObject.FindProperty ("checkNearChunks");
			chunkExtents = serializedObject.FindProperty ("chunkExtents");
			renderChunks = serializedObject.FindProperty ("renderChunks");
			useOriginShift = serializedObject.FindProperty ("useOriginShift");
		}


		public override void OnInspectorGUI () {
			serializedObject.Update ();
			EditorGUILayout.Separator ();
			EditorGUI.BeginChangeCheck ();
			EditorGUILayout.PropertyField (enableVoxelLight, new GUIContent("Enable Voxel Light", "복셀 전역 조명을 기반으로 머티리얼 조명을 조정하려면 이 옵션을 활성화합니다."));
			EditorGUILayout.PropertyField(useVoxelPlayMaterials, new GUIContent("Use Voxel Play Materials", "이 게임오브젝트의 머티리얼을 Voxel Play의 최적화 머티리얼로 교체합니다."));
			EditorGUILayout.PropertyField (forceUnstuck, new GUIContent("Force Unstuck", "이 게임오브젝트가 단단한 복셀 아래로 떨어지거나 관통하면 지형 표면으로 이동합니다."));
            if (forceUnstuck.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(unstuckOffsetY, new GUIContent("Offset Y"));
                EditorGUI.indentLevel--;
            }
			EditorGUILayout.PropertyField (checkNearChunks, new GUIContent("Chunk Area", "주변의 모든 청크가 생성되도록 보장합니다."));
			if (checkNearChunks.boolValue) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField (chunkExtents, new GUIContent("Extents", "트랜스폼 위치 주변의 청크 거리입니다(기본값 기준 1 청크 = 16 월드 유닛)."));
				EditorGUILayout.PropertyField (renderChunks, new GUIContent("Render Chunks", "활성화하면 영역 내 청크도 렌더링됩니다. 비활성화하면 청크는 생성되지만 메쉬/콜라이더/네브메시는 생성되지 않습니다."));
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.PropertyField (useOriginShift);
			serializedObject.ApplyModifiedProperties ();
			VoxelPlayBehaviour b = (VoxelPlayBehaviour)target;
			if (EditorGUI.EndChangeCheck ()) {
				b.Refresh ();
			}
            if (GUILayout.Button("Select Chunk")) {
				VoxelChunk chunk = VoxelPlayEnvironment.instance.GetChunk(b.transform.position);
                if (chunk != null) {
					chunk.gameObject.hideFlags = 0;
					Selection.activeGameObject = chunk.gameObject;
					EditorGUIUtility.PingObject(chunk.gameObject);
                }
            }
		}
	}

}
