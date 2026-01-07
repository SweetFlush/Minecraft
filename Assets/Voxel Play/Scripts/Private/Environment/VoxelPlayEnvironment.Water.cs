using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {


    public delegate bool VoxelSpreadBeforeEvent(VoxelDefinition voxeltype, Vector3d newPosition, VoxelDefinition voxelOnNewPosition);
    public delegate void VoxelSpreadAfterEvent(VoxelDefinition voxeltype, Vector3d newPosition);

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        public event VoxelSpreadBeforeEvent OnVoxelBeforeSpread;
        public event VoxelSpreadAfterEvent OnVoxelAfterSpread;

        // Max distance from anchor to allow water spread (prevents distant water from spreading and causing lag unnecessarily)
        const int WATER_SPREAD_MAX_DISTANCE = 250;
        const int WATER_SPREAD_MAX_DISTANCE_SQR = WATER_SPREAD_MAX_DISTANCE * WATER_SPREAD_MAX_DISTANCE;
        const int FULL_WATER = 15;

        WaterFloodList waterFloodSources;
        readonly float[] floodOffsets = {
            0, 1, 0,
            -1, 0, 0,
            1, 0, 0,
            0, 0, 1,
            0, 0, -1
        };
        readonly float[] floodHorizontalOffsets = {
            0, -1,
            -1, 0,
            1, 0,
            0, 1
        };

        List<VoxelChunk> tempChunks;

        void InitWater() {
            if (waterFloodSources == null) {
                waterFloodSources = new WaterFloodList();
            } else {
                waterFloodSources.Clear();
            }
            if (tempChunks == null) {
                tempChunks = new List<VoxelChunk>();
            } else {
                tempChunks.Clear();
            }
        }



        /// <summary>
        /// 특정 위치에서 플러딩 시작
        /// </summary>
        /// <param name="position">로컬 공간에 위치합니다.</param>
        void AddWaterFloodInt(ref Vector3d position, VoxelDefinition waterVoxel, int lifeTime = 24) {
            if (enableWaterFlood && lifeTime > 0 && waterVoxel != null) {
                waterFloodSources.Add(ref position, waterVoxel, lifeTime);
            }
        }

        /// <summary>
        /// 주어진 복셀 주변에 물이 있는지 확인합니다.그렇다면 물을 팽창시키세요.
        /// </summary>
        void MakeSurroundingWaterExpand(Vector3d position, int lifeTime = 24) {
            VoxelChunk otherChunk;
            int otherVoxelIndex;

            Vector3d pos;
            int floodOffsetsCount = floodOffsets.Length;
            for (int k = 0; k < floodOffsetsCount; k += 3) {
                pos.x = position.x + floodOffsets[k];
                pos.y = position.y + floodOffsets[k + 1];
                pos.z = position.z + floodOffsets[k + 2];
                if (GetVoxelIndex(pos, out otherChunk, out otherVoxelIndex, false)) {
                    int otherWaterLevel = otherChunk.voxels[otherVoxelIndex].GetWaterLevel();
                    if (otherWaterLevel > 0) {
                        AddWaterFloodInt(ref pos, voxelDefinitions[otherChunk.voxels[otherVoxelIndex].typeIndex], lifeTime);
                    }
                }
            }

        }

        /// <summary>
        /// 홍수 관리
        /// </summary>
        void UpdateWaterFlood() {
            int last = waterFloodSources.last;
            int index = waterFloodSources.root;
            tempChunks.Clear();
            modificationTag++;

            float now = Time.time;

            while (index != -1) {
                if (now >= waterFloodSources.nodes[index].spreadNext) {
                    waterFloodSources.nodes[index].lifeTime--;
                    double distSqr = FastVector.SqrDistanceXZ(ref currentAnchorPos, ref waterFloodSources.nodes[index].position);
                    if (waterFloodSources.nodes[index].lifeTime <= 0 || distSqr > WATER_SPREAD_MAX_DISTANCE_SQR) {
                        index = waterFloodSources.RemoveAt(index);
                        continue;
                    }
                    waterFloodSources.SetNextSpreadTime(index);
                    TryWaterSpread(waterFloodSources.nodes[index].position, index);
                    if (index == last)
                        break;
                }
                index = waterFloodSources.nodes[index].next;
            }

            int chunksToRefreshCount = tempChunks.Count;
            for (int k = 0; k < chunksToRefreshCount; k++) {
                VoxelChunk chunk = tempChunks[k];
                WaterRefreshNineChunksMeshes(chunk);
            }
            // Trigger change event
            if (tempChunks.Count > 0) {
                RegisterChunkChanges(tempChunks);
            }
        }


        void WaterRefreshNineChunksMeshes(VoxelChunk chunk) {
            Vector3d position = chunk.position;
            int chunkX, chunkY, chunkZ;
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);

            VoxelChunk neighbour;
            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {
                    for (int x = -1; x <= 1; x++) {
                        if (y == 0 && x == 0 && z == 0)
                            continue;
                        GetChunkFast(chunkX + x, chunkY + y, chunkZ + z, out neighbour);
                        if ((object)neighbour != null && neighbour.poolIndex != chunk.poolIndex) {
                            ChunkRequestRefresh(neighbour, false, true);
                        }
                    }
                }
            }
            ChunkRequestRefresh(chunk, false, true);
        }

        /// <summary>
        /// Updates water voxel at a given position. 
        /// </summary>
        /// <returns>영향을 받은 청크와 복셀 인덱스를 반환합니다.</returns>
        void WaterUpdateLevelFast(VoxelChunk chunk, int voxelIndex, int waterLevel, VoxelDefinition vd) {
            if (waterLevel > FULL_WATER) {
                waterLevel = FULL_WATER;
            }
            int currentWaterLevel = chunk.voxels[voxelIndex].GetWaterLevel();
            if (currentWaterLevel == waterLevel)
                return;

            if (waterLevel == 0) {
                chunk.voxels[voxelIndex].Clear(chunk.voxels[voxelIndex].light);
            } else {
                if (chunk.voxels[voxelIndex].typeIndex <= Voxel.HoleTypeIndex || vd.spreadReplaceThreshold > chunk.voxels[voxelIndex].opaque) {
                    chunk.voxels[voxelIndex].Set(vd);
                }
                chunk.voxels[voxelIndex].SetWaterLevel(waterLevel);
            }

            if (chunk.SetModified(modificationTag)) {
                tempChunks.Add(chunk);
                RegisterChunkChanges(chunk);
            }
        }

        /// <summary>
        /// 주변 복셀 확인 - 비어 있는 경우 물 복셀 1개를 추가하고 플러드 확장
        /// </summary>
        /// <returns>물이 퍼졌습니다.</returns>
        /// <param name="waterPos">물 위치.</param>
        void TryWaterSpread(Vector3d waterPos, int index) {
            GetVoxelIndex(waterPos, out VoxelChunk chunk, out int voxelIndex, false);
            if ((object)chunk == null || chunk.voxels[voxelIndex].typeIndex <= Voxel.HoleTypeIndex) {
                waterFloodSources.RemoveAt(index);
            }

            VoxelChunk otherChunk;
            int vIndex;
            int waterLevel = chunk.voxels[voxelIndex].GetWaterLevel();
            if (waterLevel == 0) {
                waterFloodSources.RemoveAt(index);
                MakeSurroundingWaterExpand(waterPos);
                return;
            }

            VoxelDefinition waterVoxel = waterFloodSources.nodes[index].waterVoxel;

            // Check first beneath, if empty water falls down
            Vector3d down = new Vector3d(waterPos.x, waterPos.y - 1f, waterPos.z);
            if (GetVoxelIndex(down, out otherChunk, out vIndex)) {
                VoxelDefinition vdOther = voxelDefinitions[otherChunk.voxels[vIndex].typeIndex];
                bool canFillWithWater = otherChunk.voxels[vIndex].typeIndex <= Voxel.HoleTypeIndex || vdOther.renderType == RenderType.CutoutCross || vdOther.renderType == RenderType.Water;
                if (canFillWithWater) {
                    int otherWaterLevel = otherChunk.voxels[vIndex].GetWaterLevel();
                    if (otherWaterLevel < FULL_WATER) {
                        if (OnVoxelBeforeSpread != null && !OnVoxelBeforeSpread(chunk.voxels[voxelIndex].type, down, otherChunk.voxels[vIndex].type)) {
                            return;
                        }
                        otherWaterLevel++;
                        WaterUpdateLevelFast(otherChunk, vIndex, otherWaterLevel, waterVoxel);
                        waterLevel--;
                        WaterUpdateLevelFast(chunk, voxelIndex, waterLevel, waterVoxel);
                        if (waterLevel == 0) {
                            waterFloodSources.RemoveAt(index);
                            MakeSurroundingWaterExpand(waterPos);
                        }
                        AddWaterFloodInt(ref down, waterVoxel);
                        if (OnVoxelAfterSpread != null) {
                            OnVoxelAfterSpread(chunk.voxels[voxelIndex].type, down);
                        }
                        return;
                    }
                }
            }

            // If not, try to flood horizontally
            int leveling = 0;
            Vector3d otherPos = waterPos;
            int prevWaterLevel = waterLevel;
            for (int f = 0; f < floodHorizontalOffsets.Length; f += 2) {
                otherPos.z = waterPos.z + floodHorizontalOffsets[f + 1];
                otherPos.x = waterPos.x + floodHorizontalOffsets[f];
                if (GetVoxelIndex(otherPos, out otherChunk, out vIndex, false)) {
                    VoxelDefinition vdOther = voxelDefinitions[otherChunk.voxels[vIndex].typeIndex];
                    if (otherChunk.voxels[vIndex].typeIndex <= Voxel.HoleTypeIndex || vdOther.renderType == RenderType.CutoutCross || vdOther.renderType == RenderType.Water) {
                        int otherWaterLevel = otherChunk.voxels[vIndex].GetWaterLevel();
                        if (otherWaterLevel < waterLevel) {
                            leveling = 1;
                        }
                        if (otherWaterLevel < waterLevel - 1) {
                            if (waterVoxel.drains && otherWaterLevel < waterLevel - 1) {
                                waterLevel--;
                            }
                            if (waterLevel > 1) {
                                if (OnVoxelBeforeSpread != null && !OnVoxelBeforeSpread(chunk.voxels[voxelIndex].type, otherPos, otherChunk.voxels[vIndex].type)) {
                                    continue;
                                }
                                otherWaterLevel++;
                                AddWaterFloodInt(ref otherPos, waterVoxel, waterFloodSources.nodes[index].lifeTime - 1);
                                WaterUpdateLevelFast(otherChunk, vIndex, otherWaterLevel, waterVoxel);
                                if (OnVoxelAfterSpread != null) {
                                    OnVoxelAfterSpread(chunk.voxels[voxelIndex].type, otherPos);
                                }
                            }
                        }
                    }
                }
            }

            for (int f = 0; f < floodHorizontalOffsets.Length; f += 2) {
                otherPos.z = waterPos.z + floodHorizontalOffsets[f + 1];
                otherPos.x = waterPos.x + floodHorizontalOffsets[f];
                if (GetVoxelIndex(otherPos, out otherChunk, out vIndex, false)) {
                    int otherWaterLevel = otherChunk.voxels[vIndex].GetWaterLevel();
                    if (otherWaterLevel > waterLevel) {
                        VoxelDefinition vdOther = voxelDefinitions[otherChunk.voxels[vIndex].typeIndex];
                        if (waterVoxel.drains) {
                            otherWaterLevel--;
                            WaterUpdateLevelFast(otherChunk, vIndex, otherWaterLevel, vdOther);
                        }
                        AddWaterFloodInt(ref otherPos, vdOther, waterFloodSources.nodes[index].lifeTime - 1);
                        if (otherWaterLevel > waterLevel + leveling) {
                            if (OnVoxelBeforeSpread != null && !OnVoxelBeforeSpread(otherChunk.voxels[voxelIndex].type, otherPos, chunk.voxels[voxelIndex].type)) {
                                continue;
                            }
                            waterLevel++;
                            if (OnVoxelAfterSpread != null) {
                                OnVoxelAfterSpread(otherChunk.voxels[voxelIndex].type, otherPos);
                            }
                        }
                    }
                }
            }


            if (prevWaterLevel != waterLevel) {
                WaterUpdateLevelFast(chunk, voxelIndex, waterLevel, waterVoxel);
            }
            if (waterLevel == 0 || prevWaterLevel == waterLevel) {
                waterFloodSources.RemoveAt(index);
                MakeSurroundingWaterExpand(waterPos, waterFloodSources.nodes[index].lifeTime - 1);
            }
        }


    }

}
