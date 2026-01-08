using System;
using System.IO;

using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
				
	public class VoxelPlayImportTools : UnityEditor.EditorWindow {

		enum ImportFormat {
			QubicleBinary
		}


		// 모델 임포트 도구
		ImportFormat importFormat;
		bool importIgnoreOffset = true;
		bool importIgnoreTransparency = true;

		string importFilename;
		Vector3 scale = Misc.vector3one;
		ColorToVoxelMap mapping;

		[MenuItem ("Assets/Create/Voxel Play/Import Tools...", false, 151)]
		public static void ShowWindow () {
			VoxelPlayImportTools window = GetWindow<VoxelPlayImportTools> ("Import Tools", true);
			window.minSize = new Vector2 (400, 140);
			window.Show ();
		}

		void OnGUI () {
			EditorGUIUtility.wideMode = true;

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.HelpBox ("다른 애플리케이션에서 복셀 모델을 가져옵니다.", MessageType.Info);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Separator ();

			importFormat = (ImportFormat)EditorGUILayout.EnumPopup (new GUIContent("Format"), importFormat);

			EditorGUILayout.BeginHorizontal ();
			importFilename = EditorGUILayout.TextField (new GUIContent("File name"), importFilename);
			if (GUILayout.Button ("Open...", GUILayout.Width (80))) {
				importFilename = EditorUtility.OpenFilePanel ("Select model File (*.qb)", "", "qb");
			}
			EditorGUILayout.EndHorizontal ();

			mapping = (ColorToVoxelMap) EditorGUILayout.ObjectField (new GUIContent ("Color-Voxel Map", "Optional color to voxel mapping."), mapping, typeof(ColorToVoxelMap), false);
			importIgnoreOffset = EditorGUILayout.Toggle (new GUIContent ("Ignore Offset", "Model can specify an offset for the center."), importIgnoreOffset);
			scale = EditorGUILayout.Vector3Field (new GUIContent ("Scale", "Scale applied to the model."), scale);
			importIgnoreTransparency = EditorGUILayout.Toggle (new GUIContent ("Ignore Transparency", "Ignore alpha values when determining unique colors."), importIgnoreTransparency);

			EditorGUILayout.Separator ();
			GUI.enabled = !string.IsNullOrEmpty (importFilename);
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Generate ColorMap Asset")) {
				GenerateColorMapAsset ();
				GUIUtility.ExitGUI ();
			}
			if (GUILayout.Button ("Generate Model Asset")) {
				GenerateModelAsset ();
				GUIUtility.ExitGUI ();
			}
			if (GUILayout.Button ("Generate Prefab")) {
				GeneratePrefab ();
				GUIUtility.ExitGUI ();
			}
			GUI.enabled = false;
			EditorGUILayout.EndHorizontal ();
		}


		void GenerateColorMapAsset () {
			ColorBasedModelDefinition baseModel = QubicleBinaryToColorBasedModelDefinition ();
			if (baseModel.colors == null)
				return;
			ColorToVoxelMap colorMap = VoxelPlayConverter.GetColorToVoxelMapDefinition (baseModel, importIgnoreTransparency);
			colorMap.name = string.IsNullOrEmpty(baseModel.name) ? "ColorMap" : baseModel.name + " ColorMap";

			// 적절한 파일 경로를 생성합니다.
			string path = GetPathForNewAsset ();
			AssetDatabase.CreateAsset (colorMap, path + "/" + GetFilenameForNewModel (colorMap.name) + ".asset");
			AssetDatabase.SaveAssets ();
			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = colorMap;
			EditorGUIUtility.PingObject (colorMap);
		}


		void GenerateModelAsset () {
			ColorBasedModelDefinition baseModel = QubicleBinaryToColorBasedModelDefinition ();
			if (baseModel.colors == null)
				return;
			ModelDefinition newModel = VoxelPlayConverter.GetModelDefinition (null, baseModel, importIgnoreOffset, mapping);
			if (!string.IsNullOrEmpty (baseModel.name)) {
				newModel.name = baseModel.name;
			}

			// 적절한 파일 경로를 생성합니다.
			string path = GetPathForNewAsset ();
			AssetDatabase.CreateAsset (newModel, path + "/" + GetFilenameForNewModel (newModel.name) + ".asset");
			AssetDatabase.SaveAssets ();
			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = newModel;
			EditorGUIUtility.PingObject (newModel);
		}


		void GeneratePrefab () {
			ColorBasedModelDefinition baseModel = QubicleBinaryToColorBasedModelDefinition ();
			if (baseModel.colors == null)
				return;

			// 보이는 복셀마다 직육면체를 생성합니다.
			int sizeX = baseModel.sizeX;
			int sizeY = baseModel.sizeY;
			int sizeZ = baseModel.sizeZ;
			float offsetX = 0, offsetY = 0, offsetZ = 0;
			if (!importIgnoreOffset) {
				offsetX += baseModel.offsetX;
				offsetY += baseModel.offsetY;
				offsetZ += baseModel.offsetZ;
			}
			Color32[] colors = baseModel.colors;

			GameObject obj = VoxelPlayConverter.GenerateVoxelObject (colors, sizeX, sizeY, sizeZ, new Vector3(offsetX, offsetY, offsetZ), scale, true, 1);

			string path = GetPathForNewAsset ();
			path += "/" + GetFilenameForNewModel (baseModel.name) + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);

			// 메쉬를 프리팹 안에 저장합니다.
			Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
			AssetDatabase.AddObjectToAsset (mesh, prefab);
			prefab.GetComponent<MeshFilter> ().sharedMesh = mesh;
			Material mat = obj.GetComponent<MeshRenderer> ().sharedMaterial;
			AssetDatabase.AddObjectToAsset(mat, prefab);
			prefab.GetComponent<MeshRenderer> ().sharedMaterial = mat;
			MeshCollider mc = prefab.AddComponent<MeshCollider> ();
			mc.sharedMesh = mesh;
			mc.convex = true;
			Rigidbody rb = prefab.AddComponent<Rigidbody> ();
			rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
			AssetDatabase.SaveAssets ();
			DestroyImmediate (obj);

			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = prefab;
			EditorGUIUtility.PingObject (prefab);
		}


		ColorBasedModelDefinition QubicleBinaryToColorBasedModelDefinition () {
			ColorBasedModelDefinition baseModel = ColorBasedModelDefinition.Null;
			Stream file = File.Open (importFilename, FileMode.Open);
			try {
				baseModel = QubicleImporter.ImportBinary (file, System.Text.Encoding.UTF8);
			} catch {
			} finally {
				file.Close ();
			}
			return baseModel;
		}

		string GetPathForNewAsset () {
			string path;
			if (VoxelPlayEnvironment.instance != null) {
				path = AssetDatabase.GetAssetPath (VoxelPlayEnvironment.instance.world);
				path = System.IO.Path.GetDirectoryName (path) + "/Models";
			} else {
				path = "Assets/ImportedModels";
			}
			System.IO.Directory.CreateDirectory (path);
			return path;
		}

		string GetFilenameForNewModel (string proposed) {
			if (string.IsNullOrEmpty (proposed)) {
				return "NewModel";
			}
            return String.Concat (proposed.Split (Path.GetInvalidFileNameChars ()));
		}



	}

}
