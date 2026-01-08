using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public class WorldEditorToolSmoothTerrain : WorldEditorToolElevateTerrain {

        public override Texture2D icon => Resources.Load<Texture2D>("VoxelPlay/WorldEditorIcons/toolSmooth");
        public override string instructions => "Smooth terrain altitude differences.";
        public override int priority => 3;
        public override WorldEditorToolCategory category => WorldEditorToolCategory.TerrainTool;

        public override void DrawGizmos (VoxelHitInfo hitInfo, List<VoxelIndex> voxelIndices) {
        }

        protected override bool Execute (ref VoxelHitInfo hitInfo, int brushSize, float brushStrength, List<VoxelIndex> indices) {

            List<VoxelChunk> modifiedChunks = BufferPool<VoxelChunk>.Get();

            Vector3d center = hitInfo.center;
            int size = brushSize * 2;
            int count = size * size;
            Vector3d corner = center;
            corner.x -= brushSize;
            corner.z -= brushSize;

            // 평균 고도를 계산합니다.
            float averageElevation = 0;
            float samples = 0;
            for (int k = 0; k < count; k++) {
                int pz = k / size;
                int px = k % size;

                Vector3d pos = corner;
                pos.z += pz;
                pos.x += px;
                float h = env.GetHeight(pos, allowedVoxelDefinitions: terrainVoxelDefinitions);
                pos.y = h - 0.1;

                if (!env.GetVoxelIndex(pos, out VoxelChunk chunk, out int voxelIndex, createChunkIfNotExists: false)) continue;

                UpdateChunkElevation(chunk);

                int z = (voxelIndex / VoxelPlayEnvironment.CHUNK_SIZE) & VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE;
                int x = voxelIndex & VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE;

                int elevationIndex = z * VoxelPlayEnvironment.CHUNK_SIZE + x;

                averageElevation += chunk.terrainInfo[elevationIndex].height;
                samples++;
            }
            if (samples == 0) return false;
            averageElevation /= samples;

            // 지형을 부드럽게 만듭니다.
            for (int k = 0; k < count; k++) {
                int pz = k / size;
                int px = k % size;

                float mask = ComputeMaskFactor(pz, px, size) - ROUNDNESS;
                if (mask <= 0) continue;

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

                float factor = averageElevation - chunk.terrainInfo[elevationIndex].height;
                if (count > 1) {
                    factor *= mask * brushStrength;
                }
                // pos.y가 평균 고도보다 높으면 factor를 반전합니다.

                BiomeDefinition biome = chunk.terrainInfo[elevationIndex].biome;
                if (factor < 0) { // lower terrain
                    undoManager.SaveChunk(chunk);
                    chunk.terrainInfo[elevationIndex].height += factor;
                    if (chunk.terrainInfo[elevationIndex].height < averageElevation) {
                        chunk.terrainInfo[elevationIndex].height = averageElevation;
                    }

                    int ny = chunk.terrainInfo[elevationIndex].groundLevel;
                    if (ny >= pos.y) continue;

                    pos.y++;
                    if (env.GetVoxelIndex(pos, out VoxelChunk aboveChunk, out int aboveIndex, createChunkIfNotExists: false)) {
                        if (aboveChunk.voxels[aboveIndex].type.isVegetation) {
                            undoManager.SaveChunk(aboveChunk);
                            aboveChunk.ClearVoxel(aboveIndex, VoxelPlayEnvironment.FULL_LIGHT);
                        }
                    }

                    Vector3d bottomPos = pos;
                    bottomPos.y = ny;
                    if (!env.GetVoxelIndex(bottomPos, out VoxelChunk bottomChunk, out int bottomIndex, createChunkIfNotExists: true)) continue;

                    chunk.ClearVoxel(voxelIndex, VoxelPlayEnvironment.FULL_LIGHT);
                    if ((object)biome != null) {
                        undoManager.SaveChunk(bottomChunk);
                        bottomChunk.voxels[bottomIndex].Set(biome.voxelTop);

                        // 이 복셀 위에 식생 복셀을 직접 배치합니다.
                        if (env.enableVegetation && pos.y > env.waterLevel) {
                            float rn = WorldRand.GetValue(pos);
                            if (biome.vegetationDensity > 0 && rn < biome.vegetationDensity && biome.vegetation.Length > 0) {
                                chunk.voxels[voxelIndex].Set(env.GetVegetation(biome.vegetation, rn / biome.vegetationDensity));
                            }
                        }
                    }

                } else { // raise terrain
                    undoManager.SaveChunk(chunk);
                    chunk.terrainInfo[elevationIndex].height += factor;
                    if (clampAltitude && chunk.terrainInfo[elevationIndex].height > env.sceneEditorAltitude) {
                        chunk.terrainInfo[elevationIndex].height = env.sceneEditorAltitude;
                    }

                    int ny = chunk.terrainInfo[elevationIndex].groundLevel;
                    if (ny <= pos.y) continue;

                    // 위에 불투명하지 않은 복셀이 있는 경우에만 적용됩니다.
                    Vector3d abovePos = pos;
                    abovePos.y = ny;
                    if (!env.GetVoxelIndex(abovePos, out VoxelChunk aboveChunk, out int aboveIndex, createChunkIfNotExists: true)) continue;
                    if (aboveChunk.voxels[aboveIndex].opaque >= VoxelPlayEnvironment.FULL_OPAQUE) continue;

                    if ((object)biome != null) {
                        undoManager.SaveChunk(aboveChunk);

                        chunk.voxels[voxelIndex].Set(biome.voxelDirt);
                        aboveChunk.voxels[aboveIndex].Set(biome.voxelTop);

                        // 이 복셀 위에 식생 복셀을 직접 배치합니다.
                        if (env.enableVegetation && abovePos.y > env.waterLevel) {
                            float rn = WorldRand.GetValue(abovePos);
                            if (biome.vegetationDensity > 0 && rn < biome.vegetationDensity && biome.vegetation.Length > 0) {
                                if (aboveIndex < VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE * VoxelPlayEnvironment.ONE_Y_ROW) {
                                    aboveChunk.voxels[aboveIndex + VoxelPlayEnvironment.ONE_Y_ROW].Set(env.GetVegetation(biome.vegetation, rn / biome.vegetationDensity));
                                }
                            }
                        }
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

    }

}
