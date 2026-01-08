using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VoxelPlay {

    public class WorldEditorToolElevateTerrain : WorldEditorTool {

        public override Texture2D icon => Resources.Load<Texture2D>("VoxelPlay/WorldEditorIcons/toolElevation");
        public override string instructions => "Raise or lower the terrain.\nHold shift to lower terrain.";
        public override int priority => 1;
        public override WorldEditorToolCategory category => WorldEditorToolCategory.TerrainTool;

        protected bool clampAltitude = false;


        public override void DrawGizmos (VoxelHitInfo hitInfo, List<VoxelIndex> voxelIndices) {
            if (shift) {
                DrawArrow(hitInfo.center + Vector3.up, Vector3.down, 0.3f);
            } else {
                DrawArrow(hitInfo.center + Vector3.up * 0.5f, Vector3.up, 0.3f);
            }
        }

        public override void SelectVoxels (ref VoxelHitInfo hitInfo, int brushSize, List<VoxelIndex> voxelIndices) {
            VoxelIndex vi = new VoxelIndex();
            voxelIndices.Clear();
            int size = brushSize * 2 - 1;
            int count = size * size;
            Vector3d corner = hitInfo.center;
            corner.x -= brushSize - 1;
            corner.z -= brushSize - 1;

            for (int k = 0; k < count; k++) {
                int pz = k / size;
                int px = k % size;

                if (count > 1) {
                    float mask = ComputeMaskFactor(pz, px, size) - ROUNDNESS;
                    if (mask <= 0) continue;
                }

                Vector3d pos = corner;
                pos.z += pz;
                pos.x += px;
                pos.y = env.GetHeight(pos, allowedVoxelDefinitions: terrainVoxelDefinitions) - 0.1;

                if (env.GetVoxelIndex(pos, out VoxelChunk chunk, out int voxelIndex, createChunkIfNotExists: true)) {
                    if (chunk.voxels[voxelIndex].isSolid) {
                        vi.chunk = chunk;
                        vi.voxelIndex = voxelIndex;
                        voxelIndices.Add(vi);
                    }
                }
            }
        }

        protected override bool Execute (ref VoxelHitInfo hitInfo, int brushSize, float brushStrength, List<VoxelIndex> indices) {

            List<VoxelChunk> modifiedChunks = BufferPool<VoxelChunk>.Get();

            Vector3d center = hitInfo.center;
            int size = brushSize * 2 - 1;
            int count = size * size;
            Vector3d corner = center;
            corner.x -= brushSize - 1;
            corner.z -= brushSize - 1;

            float altitude = 0;
            if (clampAltitude) {
                if (env.sceneEditorUseCenterVoxelAltitude) {
                    altitude = (float)startHitInfo.center.y;
                } else {
                    altitude = env.sceneEditorAltitude;
                }
            }

            for (int k = 0; k < count; k++) {
                int pz = k / size;
                int px = k % size;

                float mask = 1;
                if (count > 1) {
                    mask = ComputeMaskFactor(pz, px, size) - ROUNDNESS;
                    if (mask <= 0) continue;
                }

                Vector3d pos = corner;
                pos.z += pz;
                pos.x += px;
                pos.y = env.GetHeight(pos, allowedVoxelDefinitions: terrainVoxelDefinitions) - 0.1f;

                if (!env.GetVoxelIndex(pos, out VoxelChunk chunk, out int voxelIndex, createChunkIfNotExists: false)) continue;

                UpdateChunkElevation(chunk);

                int z = (voxelIndex / VoxelPlayEnvironment.CHUNK_SIZE) & VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE;
                int x = voxelIndex & VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE;

                int elevationIndex = z * VoxelPlayEnvironment.CHUNK_SIZE + x;

                pos.y = chunk.terrainInfo[elevationIndex].groundLevel;

                float factor = 1;
                if (count > 1) {
                    factor = mask * brushStrength;
                }

                BiomeDefinition biome = chunk.terrainInfo[elevationIndex].biome;
                if (shift) { // lower terrain
                    if (clampAltitude && chunk.terrainInfo[elevationIndex].height <= altitude) continue;

                    undoManager.SaveChunk(chunk);
                    chunk.terrainInfo[elevationIndex].height -= factor;
                    if (clampAltitude && chunk.terrainInfo[elevationIndex].height < altitude) {
                        chunk.terrainInfo[elevationIndex].height = altitude;
                    }

                    int ny = chunk.terrainInfo[elevationIndex].groundLevel;

                    // 지형 위의 식생을 제거합니다.
                    ClearVegetationAbove(pos, undoManager, modifiedChunks);

                    Vector3d bottomPos = pos;
                    bottomPos.y = ny;
                    if (!env.GetVoxelIndex(bottomPos, out VoxelChunk bottomChunk, out int bottomIndex, createChunkIfNotExists: true)) continue;

                    chunk.ClearVoxel(voxelIndex, VoxelPlayEnvironment.FULL_LIGHT);
                    if ((object)biome != null) {
                        undoManager.SaveChunk(bottomChunk);
                        bottomChunk.voxels[bottomIndex].Set(biome.voxelTop);
                        PlaceVegetationAbove(bottomPos, bottomChunk, bottomIndex, biome, modifiedChunks);
                    }
                } else { // raise terrain
                    if (clampAltitude && chunk.terrainInfo[elevationIndex].height >= altitude) continue;

                    undoManager.SaveChunk(chunk);
                    chunk.terrainInfo[elevationIndex].height += factor;
                    if (clampAltitude && chunk.terrainInfo[elevationIndex].height > altitude) {
                        chunk.terrainInfo[elevationIndex].height = altitude;
                    }

                    int ny = chunk.terrainInfo[elevationIndex].groundLevel;

                    // 위에 불투명하지 않은 복셀이 있는 경우에만 적용됩니다.
                    Vector3d abovePos = pos;
                    abovePos.y = ny;
                    if (!env.GetVoxelIndex(abovePos, out VoxelChunk aboveChunk, out int aboveIndex, createChunkIfNotExists: true)) continue;
                    if (aboveChunk.voxels[aboveIndex].opaque >= VoxelPlayEnvironment.FULL_OPAQUE) continue;

                    if ((object)biome != null) {
                        undoManager.SaveChunk(aboveChunk);
                        chunk.voxels[voxelIndex].Set(biome.voxelDirt);
                        aboveChunk.voxels[aboveIndex].Set(biome.voxelTop);
                        PlaceVegetationAbove(abovePos, aboveChunk, aboveIndex, biome, modifiedChunks);
                    } else {
                        // 복셀을 단순히 반복합니다.
                        undoManager.SaveChunk(aboveChunk);
                        aboveChunk.voxels[aboveIndex].Set(chunk.voxels[voxelIndex].type);
                    }
                }
                modifiedChunks.Add(chunk);
            }

            int modifiedCount = modifiedChunks.Count;
            RefreshModifiedChunks(modifiedChunks);

            BufferPool<VoxelChunk>.Release(modifiedChunks);

            return modifiedCount > 0;

        }

        protected void ClearVegetationAbove (Vector3d pos, UndoManager undoManager, List<VoxelChunk> modifiedChunks) {
            for (int k = 0; k < 8; k++) {
                if (!env.GetVoxelIndex(pos, out VoxelChunk chunk, out int voxelIndex, createChunkIfNotExists: false)) return;
                if (chunk.voxels[voxelIndex].isEmpty) return;
                if (chunk.voxels[voxelIndex].type.isVegetation) {
                    undoManager.SaveChunk(chunk);
                    chunk.ClearVoxel(voxelIndex, VoxelPlayEnvironment.FULL_LIGHT);
                    if (!modifiedChunks.Contains(chunk)) {
                        modifiedChunks.Add(chunk);
                    }
                }
            }
            pos.y++;
        }

        protected void PlaceVegetationAbove (Vector3d pos, VoxelChunk chunk, int voxelIndex, BiomeDefinition biome, List<VoxelChunk> modifiedChunks) {
            if (env.enableVegetation && pos.y > env.waterLevel) {
                float rn = WorldRand.GetValue(pos);
                if (biome.vegetationDensity > 0 && rn < biome.vegetationDensity && biome.vegetation.Length > 0) {
                    if (voxelIndex < VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE * VoxelPlayEnvironment.ONE_Y_ROW) {
                        chunk.voxels[voxelIndex + VoxelPlayEnvironment.ONE_Y_ROW].Set(env.GetVegetation(biome.vegetation, rn / biome.vegetationDensity));
                        if (!modifiedChunks.Contains(chunk)) {
                            modifiedChunks.Add(chunk);
                        }
                    }
                }
            }
        }
    }

}
