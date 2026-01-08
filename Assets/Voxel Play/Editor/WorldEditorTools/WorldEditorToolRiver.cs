using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace VoxelPlay {

    public class WorldEditorToolRiver : WorldEditorTool {

        public override Texture2D icon => Resources.Load<Texture2D>("VoxelPlay/WorldEditorIcons/toolRiver");
        public override string instructions => "Paint a river.";
        public override int priority => 70;
        public override int minOpaque => 1;
        public override bool supportsContinuousMode => true;
        public override WorldEditorToolCategory category => WorldEditorToolCategory.TerrainTool;

        public WorldEditorToolRiver () : base() {
            if (env == null) return;
            if (env.sceneEditorRiverWaterVoxel == null) {
                env.sceneEditorRiverWaterVoxel = env.currentWaterVoxelDefinition;
            }
            if (env.sceneEditorRiverShoreVoxel == null && env.world != null && env.world.terrainGenerator != null && env.world.terrainGenerator is TerrainDefaultGenerator) {
                TerrainDefaultGenerator tg = (TerrainDefaultGenerator)env.world.terrainGenerator;
                env.sceneEditorRiverShoreVoxel = tg.shoreVoxel;
            }
        }

        public override void DrawInspector () {
            env.sceneEditorBrushSize = EditorGUILayout.IntSlider("Brush Size", env.sceneEditorBrushSize, 1, 16);
            env.sceneEditorBrushContinuousMode = EditorGUILayout.Toggle(new GUIContent("Continuous Mode", "왼쪽 마우스 버튼을 누른 채로 작업합니다."), env.sceneEditorBrushContinuousMode);
            if (env.sceneEditorBrushContinuousMode) {
                EditorGUI.indentLevel++;
                env.sceneEditorBrushSpeed = EditorGUILayout.Slider("Speed", env.sceneEditorBrushSpeed, 0, 1);
                EditorGUI.indentLevel--;
            }
            env.sceneEditorRiverDepth = EditorGUILayout.IntSlider("River Depth", env.sceneEditorRiverDepth, 1, 10);
            env.sceneEditorRiverWaterVoxel = (VoxelDefinition)EditorGUILayout.ObjectField("Water Voxel", env.sceneEditorRiverWaterVoxel, typeof(VoxelDefinition), false);
            env.sceneEditorRiverAddShore = EditorGUILayout.Toggle("Add Shore", env.sceneEditorRiverAddShore);
            if (env.sceneEditorRiverAddShore) {
                env.sceneEditorRiverShoreVoxel = (VoxelDefinition)EditorGUILayout.ObjectField("Shore Voxel", env.sceneEditorRiverShoreVoxel, typeof(VoxelDefinition), false);
            }
        }

        public override void SelectVoxels (ref VoxelHitInfo hitInfo, int brushSize, List<VoxelIndex> voxelIndices) {

            VoxelDefinition shoreVoxel = env.sceneEditorRiverAddShore ? env.sceneEditorRiverShoreVoxel : env.sceneEditorRiverWaterVoxel;
            bool usesShore = shoreVoxel != null;

            voxelIndices.Clear();
            if (env.currentWaterVoxelDefinition == null) {
                return;
            }

            Vector3d center = hitInfo.center;

            List<VoxelIndex> tempVoxels = BufferPool<VoxelIndex>.Get();
            env.GetVoxelIndices(center, brushSize, env.sceneEditorRiverDepth, brushSize, tempVoxels, VoxelPlayEnvironment.FULL_OPAQUE);

            // 임시 처리: damageTaken 필드를 배치할 복셀 타입을 임시 저장하는 데 사용합니다.
            int count = tempVoxels.Count;
            for (int k = 0; k < count; k++) {
                VoxelIndex vi = tempVoxels[k];
                Vector3d pos = env.GetVoxelPosition(vi.chunk, vi.voxelIndex);
                if (pos.y < center.y - env.sceneEditorRiverDepth) continue;
                vi.damageTaken = env.currentWaterVoxelDefinition.index;
                if (pos.y >= center.y) {
                    double dz = center.z - pos.z;
                    double dx = center.x - pos.x;
                    if (usesShore && brushSize > 0 && dz * dz + dx * dx > (brushSize - 1) * (brushSize - 1)) {
                        vi.damageTaken = shoreVoxel.index;
                    }
                }
                voxelIndices.Add(vi);
            }
            BufferPool<VoxelIndex>.Release(tempVoxels);
        }

        protected override bool Execute (ref VoxelHitInfo hitInfo, int brushSize, float brushStrength, List<VoxelIndex> indices) {

            List<VoxelChunk> modifiedChunks = BufferPool<VoxelChunk>.Get();
            int count = indices.Count;

            for (int k = 0; k < count; k++) {
                VoxelIndex vi = indices[k];
                VoxelDefinition vd = env.GetVoxelDefinition(vi.damageTaken);
                undoManager.SaveChunk(vi.chunk);
                vi.chunk.voxels[vi.voxelIndex].Set(vd);
                modifiedChunks.Add(vi.chunk);
                // 위에 있는 식물을 정리합니다.
                Vector3d pos = env.GetVoxelPosition(vi);
                pos.y++;
                if (env.GetVoxelIndex(pos, out VoxelChunk aboveChunk, out int aboveIndex)) {
                    if (aboveChunk.voxels[aboveIndex].type.isVegetation) {
                        undoManager.SaveChunk(aboveChunk);
                        aboveChunk.ClearVoxel(aboveIndex, VoxelPlayEnvironment.FULL_LIGHT);
                        modifiedChunks.Add(aboveChunk);
                    }
                }
            }

            int modifiedCount = modifiedChunks.Count;
            RefreshModifiedChunks(modifiedChunks);

            BufferPool<VoxelChunk>.Release(modifiedChunks);

            return modifiedCount > 0;

        }

    }

}
