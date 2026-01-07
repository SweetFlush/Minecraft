using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;

namespace VoxelPlay.GPULighting {
				
	[CustomEditor (typeof(VoxelPlayLightManager))]
	public class VoxelPlayLightManagerEditor : UnityEditor.Editor {

		public override void OnInspectorGUI () {
			EditorGUILayout.HelpBox ("이 카메라 스크립트는 Voxel Play Environment에서 포인트 라이트 렌더링을 관리합니다.", MessageType.Info);
		}

	}

}
