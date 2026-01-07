using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using NUnit;

namespace VoxelPlay {

    public class WorldEditorToolPaint : WorldEditorTool {

        public override Texture2D icon => Resources.Load<Texture2D>("VoxelPlay/WorldEditorIcons/toolPaint");
        public override string instructions => "Paint existing voxels with new voxel definitions.";
        public override int priority => 60;
        public override WorldEditorToolCategory category => WorldEditorToolCategory.SculptTool;
        public override void DrawInspector () {
            env.sceneEditorBrushSize = EditorGUILayout.IntSlider("Brush Size", env.sceneEditorBrushSize, 1, 32);
            env.sceneEditorBrushContinuousMode = EditorGUILayout.Toggle(new GUIContent("Continuous Mode", "Hold left button mouse to operate"), env.sceneEditorBrushContinuousMode);
            if (env.sceneEditorBrushContinuousMode) {
                EditorGUI.indentLevel++;
                env.sceneEditorBrushSpeed = EditorGUILayout.Slider("Speed", env.sceneEditorBrushSpeed, 0, 1);
                EditorGUI.indentLevel--;
            }
            env.sceneEditorVoxelDefinition = (VoxelDefinition)EditorGUILayout.ObjectField("Voxel Definition", env.sceneEditorVoxelDefinition, typeof(VoxelDefinition), false);
            if (env.enableTinting) {
                env.sceneEditorTintColor = EditorGUILayout.ColorField("Tint Color", env.sceneEditorTintColor);
            }
        }

        public override void SelectVoxels (ref VoxelHitInfo hitInfo, int brushSize, List<VoxelIndex> voxelIndices) {
            List<VoxelIndex> tempVoxels = BufferPool<VoxelIndex>.Get();
            env.GetVoxelIndices(hitInfo.center, brushSize, tempVoxels, minOpaque: VoxelPlayEnvironment.FULL_OPAQUE);
            int count = tempVoxels.Count;
            Vector3d camPos = SceneView.lastActiveSceneView.camera.transform.position;
            voxelIndices.Clear();
            for (int k = 0; k < count; k++) {
                VoxelIndex vi = tempVoxels[k];
                Vector3d pos = env.GetVoxelPosition(vi.chunk, vi.voxelIndex);
                pos += hitInfo.normal * 0.48f;
                Vector3d toCam = (camPos - pos).normalized;
                if (!env.IsSolidAtPosition(pos + toCam)) {
                    voxelIndices.Add(vi);
                }
            }
            BufferPool<VoxelIndex>.Release(tempVoxels);
        }

        protected override bool Execute (ref VoxelHitInfo hitInfo, int brushSize, float brushStrength, List<VoxelIndex> indices) {

            if (env.sceneEditorVoxelDefinition == null || indices.Count == 0) return false;

            List<VoxelChunk> modifiedChunks = BufferPool<VoxelChunk>.Get();

            env.AddVoxelDefinition(env.sceneEditorVoxelDefinition);
            foreach (var index in indices) {
                undoManager.SaveChunk(index.chunk);
            }
            env.VoxelPlace(indices, env.sceneEditorVoxelDefinition, env.sceneEditorTintColor, modifiedChunks);
            BufferPool<VoxelChunk>.Release(modifiedChunks);

            return modifiedChunks.Count > 0;

        }

    }

}