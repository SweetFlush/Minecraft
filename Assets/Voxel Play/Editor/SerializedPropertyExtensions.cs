using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace VoxelPlay {
	
	public static class SerializedPropertyExtensions {

		/// <summary>
		/// 배열에서 이 속성의 인덱스를 반환합니다.
		/// </summary>
		public static int GetArrayIndex (this SerializedProperty property) {
			string s = property.propertyPath;
			int bracket = s.LastIndexOf ("[");
			if (bracket >= 0) {
				string indexStr = s.Substring (bracket + 1, s.Length - bracket - 2);
				int index;
				if (int.TryParse (indexStr, out index)) {
					return index;
				}
			}
			return 0;
		}
	}
}