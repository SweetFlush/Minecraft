using System;
using UnityEngine;

namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {

#if UNITY_EDITOR
        public int worldManagementSelectedTool;
        public int sceneEditorBrushSize = 1;
        public float sceneEditorBrushStrength = 0.5f;
        public Texture2D sceneEditorBrushShape;
        public int sceneEditorAltitude = 32;
        public bool sceneEditorUseCenterVoxelAltitude = true;
        public bool sceneEditorBuildIgnoreWater = true;
        public VoxelDefinition sceneEditorVoxelDefinition;
        [ColorUsage(showAlpha: true, hdr: true)]
        public Color sceneEditorTintColor = Misc.colorWhite;
        public int sceneEditorRiverDepth = 3;
        public VoxelDefinition sceneEditorRiverWaterVoxel, sceneEditorRiverShoreVoxel;
        public bool sceneEditorRiverAddShore = true;
        public VoxelDefinition sceneEditorBuildVoxel;
        public bool sceneEditorBuildAutoSelectVoxel;
        public ModelDefinition sceneEditorModelDefinition;
        public int sceneEditorPlacementRotation;
        [Tooltip("아래 지형과의 간격이 없도록 하단 복셀을 반복합니다.")]
        public bool sceneEditorPlacementFitTerrain;
        public bool sceneEditorModelAsGameObject;
        public Vector3 sceneEditorModelGameObjectScale = Misc.vector3one;
        public bool sceneEditorBrushContinuousMode = true;
        public float sceneEditorBrushSpeed = 0.9f;
        public int sceneEditorBuildMaxLength = 10;
        public int sceneEditorBrushMicroVoxelSize;
        public Vector3Int sceneEditorCaptureSize = new Vector3Int(5, 5, 5);
        public Vector3Int sceneEditorCaptureOffset;
        public string sceneEditorCaptureFileName = "capture";
        public string sceneEditorCaptureVoxelDefinitionName = "voxelDefinition";
        public bool sceneEditorAutomaticBackup;
        public Vector3 sceneEditorCameraMainPosition;
        public Vector3 sceneEditorCameraSceneViewPosition;
#endif

    }
}

