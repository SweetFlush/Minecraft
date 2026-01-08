using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxelPlay {
	[CustomPropertyDrawer(typeof(StepData))]
	public class TerrainStepDataDrawer : PropertyDrawer {

		// 주어진 사각형 안에 속성을 그립니다.
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			position.height -= 5f;
			Rect box = new Rect(position.x - 2f, position.y - 2f, position.width + 4f, position.height + 4f);
			EditorGUI.DrawRect(box, new Color(0, 0, 0.175f, 0.15f));

			float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
			position.height = EditorGUIUtility.singleLineHeight;

			ITerrainDefaultGenerator tg = (ITerrainDefaultGenerator)property.serializedObject.targetObject;
			if (tg.Steps == null)
				return;
			
			int[] stepIndices = new int[tg.Steps.Length];
			for (int k = 0; k < tg.Steps.Length; k++) {
				stepIndices[k] = k;
			}
			GUIContent[] stepLabels = new GUIContent[tg.Steps.Length];
			for (int k = 0; k < tg.Steps.Length; k++) {
				stepLabels[k] = new GUIContent("Step " + k.ToString());
			}

			EditorGUIUtility.labelWidth = 120;

			SerializedProperty enabled = property.FindPropertyRelative("enabled");
			Rect prevPosition = position;
			position.x -= 14;
			position.width = 35;
			EditorGUI.PropertyField(position, enabled, GUIContent.none);
			position = prevPosition;
			SerializedProperty stepType = property.FindPropertyRelative("operation");
			int index = property.GetArrayIndex ();
			EditorGUI.PropertyField(position, stepType, new GUIContent("Step " + index));
			if (enabled.boolValue) {
				position.y += lineHeight;
				SerializedProperty stepLabel = property.FindPropertyRelative("description");
				EditorGUI.PropertyField(position, stepLabel, new GUIContent("User Description"));
				switch (stepType.intValue) {
					case (int)TerrainStepType.SampleHeightMapTexture:
					case (int)TerrainStepType.SampleRidgeNoiseFromTexture:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseTexture"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("frecuency"), new GUIContent("Frequency", "노이즈 텍스처에 적용되는 스케일입니다."));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, property.FindPropertyRelative("offset"), new GUIContent("Offset", "샘플링 좌표에 적용되는 오프셋입니다."));
                        position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseRangeMin"), new GUIContent("Min", "노이즈 값을 최소-최대 범위로 매핑합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseRangeMax"), new GUIContent("Max", "노이즈 값을 최소-최대 범위로 매핑합니다."));
						break;
					case (int)TerrainStepType.SampleHeightMapFractal:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseTexture"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("frecuency"), new GUIContent("Frequency", "노이즈 텍스처에 적용되는 스케일입니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("octaves"), new GUIContent("Octaves", "결합할 노이즈 샘플 수입니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("persistence"), new GUIContent("Persistence", "이전 옥타브의 진폭 값에 곱하는 배율입니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("lacunarity"), new GUIContent("Lacunarity", "이전 옥타브의 주파수 값에 곱하는 배율입니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseRangeMin"), new GUIContent("Min", "최종 노이즈 값을 최소-최대 범위로 매핑합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseRangeMax"), new GUIContent("Max", "최종 노이즈 값을 최소-최대 범위로 매핑합니다."));
						break;
                    case (int)TerrainStepType.SampleHeightMapUnityTerrain:
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, property.FindPropertyRelative("terrainData"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, property.FindPropertyRelative("frecuency"), new GUIContent("Frequency", "하이트맵에 적용되는 스케일입니다."));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, property.FindPropertyRelative("offset"), new GUIContent("Offset", "샘플링 좌표에 적용되는 오프셋입니다."));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseRangeMin"), new GUIContent("Min", "하이트맵 값을 최소-최대 범위로 매핑합니다."));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseRangeMax"), new GUIContent("Max", "하이트맵 값을 최소-최대 범위로 매핑합니다."));
                        break;
                    case (int)TerrainStepType.Constant:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Constant", "상수 값을 출력합니다."));
						break;		
					case (int)TerrainStepType.Copy:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Copy Output From", "이전 단계의 결과를 복사합니다."));
						break;		
					case (int)TerrainStepType.Random:
						break;
					case (int)TerrainStepType.Invert:
						break;
					case (int)TerrainStepType.Shift:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Add", "이전 결과에 더할 값입니다."));
						break;
					case (int)TerrainStepType.BeachMask:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Mask Source", "마스크 값이 0이고 고도가 해변 레벨이면 고도가 낮아집니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("threshold"), new GUIContent("Threshold", "이 임계값보다 큰 값은 해변 효과를 제거합니다."));
						break;
					case (int)TerrainStepType.AddAndMultiply:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Add", "이전 결과에 더할 값입니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param2"), new GUIContent("Then Multiply", "결과에 이 값을 곱합니다."));
						break;
					case (int)TerrainStepType.MultiplyAndAdd:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Multiply", "값에 이 값을 곱합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param2"), new GUIContent("Then Add", "결과에 이 값을 더합니다."));
						break;
					case (int)TerrainStepType.Exponential:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Exponent", "결과 = exp(0까지의 거리, 지수)"));
						break;
					case (int)TerrainStepType.Island:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Radius", "0,0,0에서 멀어질수록 지형 높이를 낮춥니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param2"), new GUIContent("Slope Multiplier", "반경 밖의 경사 배율입니다(0.01 - 5)."));
						break;
					case (int)TerrainStepType.Threshold:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input", "임계값 연산에 사용할 소스입니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("threshold"), new GUIContent("Threshold", "임계값보다 큰 값만 유지하고 나머지는 0을 출력합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdShift"), new GUIContent("If Greater, Add", "임계값을 넘으면 이전 값에 더해지는 값입니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdParam"), new GUIContent("If Not, Output...", "이전 값이 임계값을 넘지 못할 때 설정되는 값입니다."));
						break;
					case (int)TerrainStepType.FlattenOrRaise:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("threshold"), new GUIContent("Min Elevation", "이 임계값보다 큰 값은 평탄화됩니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdParam"), new GUIContent("Multiplier", "평탄화 배율입니다."));
						break;
					case (int)TerrainStepType.BlendAdditive:
						position.y += lineHeight;
						prevPosition = position;
						position.width = 190;
						float labelWidth = EditorGUIUtility.labelWidth;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input A", "결합할 입력 중 하나입니다."));
						position.x += 190;
						position.width = 120;
						EditorGUIUtility.labelWidth = 60;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("weight0"), new GUIContent("Weight", "입력 A에 가중치를 곱합니다."));
						position = prevPosition;
						position.y += lineHeight;
						prevPosition = position;
						position.width = 190;
						EditorGUIUtility.labelWidth = labelWidth;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex1"), stepLabels, stepIndices, new GUIContent("Input B", "결합할 다른 입력입니다."));
						position.x += 190;
						position.width = 120;
						EditorGUIUtility.labelWidth = 60;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("weight1"), new GUIContent("Weight", "입력 A에 가중치를 곱합니다."));
						position = prevPosition;
						break;
					case (int)TerrainStepType.BlendMultiply:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input A", "결과 = 입력 A * 입력 B"));
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex1"), stepLabels, stepIndices, new GUIContent("Input B", "결과 = 입력 A * 입력 B"));
						break;
					case (int)TerrainStepType.Clamp:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("min"), new GUIContent("Min", "값이 Min보다 작으면 Min을, 아니면 값을 출력합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("max"), new GUIContent("Max", "값이 Max보다 크면 Max를, 아니면 값을 출력합니다."));
						break;
					case (int)TerrainStepType.Select:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input", "소스로 사용할 단계를 선택합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("min"), new GUIContent("Range Min", "값이 Min보다 작으면 0을 출력합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("max"), new GUIContent("Range Max", "값이 Max보다 크면 0을 출력합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdParam"), new GUIContent("Outside Value", "범위를 벗어나면 다른 값을 출력합니다."));
						break;
					case (int)TerrainStepType.Fill:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input", "소스로 사용할 단계를 선택합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("min"), new GUIContent("Range Min", "값이 min과 max 사이면 채움 값을 출력합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("max"), new GUIContent("Range Max", "값이 min과 max 사이면 채움 값을 출력합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdParam"), new GUIContent("Fill Value", "입력 값이 min-max 범위 안이면 그 값으로 대체합니다."));
						break;
					case (int)TerrainStepType.Test:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input", "소스로 사용할 단계를 선택합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("min"), new GUIContent("Range Min", "값이 Min보다 작으면 0을 출력합니다."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("max"), new GUIContent("Range Max", "값이 Max보다 크면 0을 출력합니다."));
						break;
				}

				// 버튼
				position.x += 20;
				position.y += lineHeight;
				const float buttonWidth = 60;
				const float buttonSpacing = 70;
				position.width = buttonWidth;
				bool markSceneChanges = false;

				if (GUI.Button(position, "Add")) {
					List<StepData> od = new List<StepData>(tg.Steps);
					StepData stepData = new StepData();
					stepData.inputIndex0 = index;
					od.Insert(index + 1, stepData);
					tg.Steps = od.ToArray();
					// 입력 참조 인덱스를 이동합니다.
					for (int k = 0; k < tg.Steps.Length; k++) {
						if (tg.Steps[k].inputIndex0 > index)
							tg.Steps[k].inputIndex0++;
						if (tg.Steps[k].inputIndex1 > index)
							tg.Steps[k].inputIndex1++;

					}
					markSceneChanges = true;
				}
				position.x += buttonSpacing;
				if (GUI.Button(position, "Remove")) {
					List<StepData> od = new List<StepData>(tg.Steps);
					od.RemoveAt(index);
					tg.Steps = od.ToArray();
					// 입력 참조 인덱스를 이동합니다.
					for (int k = 0; k < tg.Steps.Length; k++) {
						if (tg.Steps[k].inputIndex0 >= index)
							tg.Steps[k].inputIndex0--;
						if (tg.Steps[k].inputIndex1 >= index)
							tg.Steps[k].inputIndex1--;
					}
					markSceneChanges = true;
				}
				if (index > 0) {
					position.x += buttonSpacing;
					if (GUI.Button(position, "Up")) {
						StepData o = tg.Steps[index - 1];
						tg.Steps[index - 1] = tg.Steps[index];
						tg.Steps[index] = o;
						// 입력 참조 인덱스를 이동합니다.
						for (int k = 0; k < tg.Steps.Length; k++) {
							if (tg.Steps[k].inputIndex0 == index)
								tg.Steps[k].inputIndex0--;
							if (tg.Steps[k].inputIndex1 == index)
								tg.Steps[k].inputIndex1--;
						}
						markSceneChanges = true;
					}
				}
				if (index < tg.Steps.Length - 1) {
					position.x += buttonSpacing;
					if (GUI.Button(position, "Down")) {
						StepData o = tg.Steps[index + 1];
						tg.Steps[index + 1] = tg.Steps[index];
						tg.Steps[index] = o;
						// 입력 참조 인덱스를 이동합니다.
						for (int k = 0; k < tg.Steps.Length; k++) {
							if (tg.Steps[k].inputIndex0 == index)
								tg.Steps[k].inputIndex0++;
							if (tg.Steps[k].inputIndex1 == index)
								tg.Steps[k].inputIndex1++;
						}
						markSceneChanges = true;
					}
				}

				if (markSceneChanges && !Application.isPlaying) {
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
				}

			}

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
			int numLines = 3;
			switch (property.FindPropertyRelative("operation").intValue) {
				case (int)TerrainStepType.SampleHeightMapTexture:
				case (int)TerrainStepType.SampleRidgeNoiseFromTexture:
				case (int)TerrainStepType.SampleHeightMapUnityTerrain:
					numLines += 5;
					break;
				case (int)TerrainStepType.SampleHeightMapFractal:
					numLines += 7;
					break;
				case (int)TerrainStepType.BlendAdditive:
				case (int)TerrainStepType.BlendMultiply:
				case (int)TerrainStepType.Clamp:
				case (int)TerrainStepType.MultiplyAndAdd:
				case (int)TerrainStepType.AddAndMultiply:
				case (int)TerrainStepType.FlattenOrRaise:
				case (int)TerrainStepType.BeachMask:
				case (int)TerrainStepType.Island:
					numLines += 2;
					break;
				case (int)TerrainStepType.Test:
					numLines += 3;
					break;
				case (int)TerrainStepType.Threshold:
				case (int)TerrainStepType.Select:
				case (int)TerrainStepType.Fill:
					numLines += 4;
					break;
				case (int)TerrainStepType.Random:
				case (int)TerrainStepType.Invert:
					break;
				default:
					numLines++;
					break;
			}
			float height = property.FindPropertyRelative("enabled").boolValue ? lineHeight * numLines : lineHeight;
			return height + 5f;
		}
	}
}
