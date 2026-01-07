using Math = System.Math;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        internal readonly Dictionary<ulong, MicroVoxelsPrototype> microVoxelsPrototypes = new Dictionary<ulong, MicroVoxelsPrototype>();

        void InitMicroVoxels () {
            microVoxelsPrototypes.Clear();
        }

        void DisposeMicroVoxels () {
        }

        void DamageMicroVoxelFast (ref VoxelHitInfo hitInfo, int size, float probability, bool addParticles = true) {
            if (MicroVoxelDestroy(ref hitInfo, size, probability) && addParticles) {
                Vector3 camForward = cameraMain.transform.forward;
                int voxelLight = GetVoxelLightPacked(hitInfo.voxelCenter - camForward);
                AddParticlesAtHitPoint(Random.Range(1, size), camForward, false, hitInfo, voxelLight, 0.1f);
            }
        }

        /// <summary>
        /// 주어진 위치에서 마이크로복셀을 파괴합니다.
        /// </summary>
        public bool MicroVoxelDestroy (Vector3d position) {

            // Get the corresponding voxel
            if (!GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) return false;

            // Check if the voxel supports micro voxels
            VoxelDefinition vd = voxelDefinitions[chunk.voxels[voxelIndex].typeIndex];
            if (!vd.supportsMicroVoxels) return false;

            // Undo rotation
            int rotationIndex = chunk.voxels[voxelIndex].GetTextureRotation();
            int microVoxelIndex = GetMicroVoxelIndex(position, rotationIndex);

            MicroVoxels mv = GetMicroVoxels(chunk, voxelIndex, defaultFilled: true);
            mv.SetUnoccupied(microVoxelIndex);
            if (mv.isEmpty) {
                VoxelDestroyFast(chunk, voxelIndex);
                return true;
            }
            ChunkRequestRefresh(chunk, false, true);

            byte newOpaque = mv.GetOpaqueProportional();
            if (newOpaque != chunk.voxels[voxelIndex].opaque) {
                chunk.voxels[voxelIndex].opaque = newOpaque;
                chunk.voxelSignature = -1;
                SpreadLightmapAroundPosition(chunk, voxelIndex);
            }

            // Force rebuild neighbour meshes if destroyed voxel is on a border
            RebuildNeighboursIfNeeded(chunk, voxelIndex);

            // Events
            RegisterChunkChanges(chunk);

            return true;
        }


        /// <summary>
        /// 위치와 크기로 정의된 볼륨 내부의 마이크로복셀을 파괴합니다.
        /// </summary>
        /// <param name="probability">볼륨 내부의 마이크로복셀을 제거할 확률</param>
        /// <returns></returns>
        public bool MicroVoxelDestroy (ref VoxelHitInfo hitInfo, int size, float probability = 1f) {

            size = Mathf.Max(size, 1);
            if (size == 1) {
                return MicroVoxelDestroy(hitInfo.center);
            }

            Boundsd bounds = GetMicroVoxelBounds(ref hitInfo, size);
            Vector3d minBox = bounds.min;
            Vector3d microPos = minBox;

            List<VoxelIndex> updatedVoxels = BufferPool<VoxelIndex>.Get();
            VoxelIndex vi = new VoxelIndex();

            for (int y = 0; y < size; y++) {
                microPos.y = minBox.y + y * MicroVoxels.SIZE;
                for (int z = 0; z < size; z++) {
                    microPos.z = minBox.z + z * MicroVoxels.SIZE;
                    for (int x = 0; x < size; x++) {
                        microPos.x = minBox.x + x * MicroVoxels.SIZE;

                        if (probability < 1f && Random.value > probability) continue;

                        // Get the corresponding voxel
                        if (!GetVoxelIndex(microPos, out VoxelChunk chunk, out int voxelIndex, false)) continue;

                        // Check if the voxel supports micro voxels
                        VoxelDefinition vd = voxelDefinitions[chunk.voxels[voxelIndex].typeIndex];
                        if (!vd.supportsMicroVoxels) continue;

                        // Undo rotation
                        int rotationIndex = chunk.voxels[voxelIndex].GetTextureRotation();
                        int microVoxelIndex = GetMicroVoxelIndex(microPos, rotationIndex);

                        MicroVoxels mv = GetMicroVoxels(chunk, voxelIndex, defaultFilled: true);
                        if (mv.SetUnoccupied(microVoxelIndex)) {
                            vi.chunk = chunk;
                            vi.voxelIndex = voxelIndex;
                            if (!updatedVoxels.Contains(vi)) {
                                updatedVoxels.Add(vi);
                            }
                            if (mv.isEmpty) {
                                VoxelDestroyFast(chunk, voxelIndex);
                            } else {
                                byte newOpaque = mv.GetOpaqueProportional();
                                if (newOpaque != chunk.voxels[voxelIndex].opaque) {
                                    chunk.voxels[voxelIndex].opaque = newOpaque;
                                    chunk.voxelSignature = -1;
                                }
                            }
                        }
                    }
                }
            }

            if (updatedVoxels.Count == 0) {
                BufferPool<VoxelIndex>.Release(updatedVoxels);
                return false;
            }

            foreach (var v in updatedVoxels) {
                ChunkRequestRefresh(v.chunk, false, true);
                SpreadLightmapAroundPosition(v.chunk, v.voxelIndex);

                // Force rebuild neighbour meshes if destroyed voxel is on a border
                RebuildNeighboursIfNeeded(v.chunk, v.voxelIndex);

                // Events
                RegisterChunkChanges(v.chunk);
            }

            BufferPool<VoxelIndex>.Release(updatedVoxels);

            return true;
        }



        /// <summary>
        /// 주어진 위치에 마이크로복셀을 배치합니다.
        /// </summary>
        /// <param name="voxelType">위치에 복셀이 포함되지 않은 경우 마이크로복셀에 사용할 복셀 유형</param>
        public bool MicroVoxelPlace (Vector3d position, VoxelDefinition voxelType, Color tintColor = default, int rotation = 0) {

            // Get the corresponding voxel
            if (!GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) return false;

            // If the voxel is empty, place a voxel of the given type
            VoxelDefinition vd = voxelDefinitions[chunk.voxels[voxelIndex].typeIndex];
            if (chunk.voxels[voxelIndex].isEmpty || vd.renderType == RenderType.Water) {
                if (tintColor == default) {
                    tintColor = Misc.colorWhite;
                }
                int waterLevel = chunk.voxels[voxelIndex].GetWaterLevel();
                VoxelPlace(position, voxelType, playSound: false, tintColor, rotation, refresh: false, placeMicroVoxels: false);
                chunk.voxels[voxelIndex].SetWaterLevel(waterLevel);
                vd = voxelType;
            }

            // Check if the voxel supports micro voxels
            if (vd == null || vd.supportsMicroVoxels) {
                // Undo rotation
                rotation = chunk.voxels[voxelIndex].GetTextureRotation(); // voxel type might not allow texture rotation
                int microVoxelIndex = GetMicroVoxelIndex(position, rotation);

                MicroVoxels mv = GetMicroVoxels(chunk, voxelIndex, defaultFilled: false);
                mv.SetOccupied(microVoxelIndex);
                chunk.voxelSignature = -1;

                byte prevOpaque = chunk.voxels[voxelIndex].opaque;
                if (mv.isFull) {
                    chunk.ClearMicroVoxels(voxelIndex);
                    chunk.voxels[voxelIndex].opaque = chunk.voxels[voxelIndex].type.opaque;
                    chunk.voxelSignature = -1;
                } else {
                    byte newOpaque = mv.GetOpaqueProportional();
                    if (newOpaque != chunk.voxels[voxelIndex].opaque) {
                        chunk.voxels[voxelIndex].opaque = newOpaque;
                        chunk.voxelSignature = -1;
                    }
                }
                if (prevOpaque != chunk.voxels[voxelIndex].opaque) {
                    ClearLightmapAtPosition(chunk, voxelIndex);
                }
            }

            ChunkRequestRefresh(chunk, false, true);

            // Force rebuild neighbour meshes if destroyed voxel is on a border
            RebuildNeighboursIfNeeded(chunk, voxelIndex);

            // Events
            RegisterChunkChanges(chunk);

            return true;
        }


        /// <summary>
        /// 위치와 크기로 정의된 볼륨에 마이크로복셀을 배치합니다.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <param name="voxelType">위치에 복셀이 포함되지 않은 경우 마이크로복셀에 사용할 복셀 유형</param>
        /// <param name="probability">볼륨의 각 위치에 마이크로복셀을 배치할 확률</param>
        public bool MicroVoxelPlace (ref VoxelHitInfo hitInfo, int size, VoxelDefinition voxelType, float probability = 1f, Color tintColor = default, int rotation = 0) {

            size = Mathf.Max(size, 1);
            if (size == 1) {
                return MicroVoxelPlace(hitInfo.voxelCenter + hitInfo.normal * MicroVoxels.SIZE, voxelType, tintColor, rotation);
            }

            Boundsd bounds = GetMicroVoxelBounds(ref hitInfo, size);
            Vector3d minBox = bounds.min;
            minBox += hitInfo.normal * MicroVoxels.SIZE * size;
            Vector3d microPos = minBox;

            List<VoxelIndex> updatedVoxels = BufferPool<VoxelIndex>.Get();
            VoxelIndex vi = new VoxelIndex();

            for (int y = 0; y < size; y++) {
                microPos.y = minBox.y + y * MicroVoxels.SIZE;
                for (int z = 0; z < size; z++) {
                    microPos.z = minBox.z + z * MicroVoxels.SIZE;
                    for (int x = 0; x < size; x++) {
                        microPos.x = minBox.x + x * MicroVoxels.SIZE;

                        // Get the corresponding voxel
                        if (!GetVoxelIndex(microPos, out VoxelChunk chunk, out int voxelIndex)) continue;

                        // If the voxel is empty, place a voxel of the given type
                        VoxelDefinition vd = voxelDefinitions[chunk.voxels[voxelIndex].typeIndex];
                        if (chunk.voxels[voxelIndex].isEmpty || vd.isVegetation || vd.renderType == RenderType.Water) {
                            if (tintColor == default) {
                                tintColor = Misc.colorWhite;
                            }
                            int waterLevel = chunk.voxels[voxelIndex].GetWaterLevel();
                            VoxelPlace(microPos, voxelType, playSound: false, tintColor, rotation, refresh: false, placeMicroVoxels: false);
                            chunk.voxels[voxelIndex].SetWaterLevel(waterLevel);
                            vd = voxelType;

                            vi.chunk = chunk;
                            vi.voxelIndex = voxelIndex;
                            if (!updatedVoxels.Contains(vi)) {
                                updatedVoxels.Add(vi);
                            }
                        } else {
                            // if position is not empty and has no microvoxels on it, then we don't need to add a microvoxel because it's filled
                            if (!IsMicroVoxelAtPosition(chunk, voxelIndex)) continue;
                        }

                        // Check if the voxel supports micro voxels
                        if (vd == null || !vd.supportsMicroVoxels) continue;

                        // Undo rotation
                        rotation = chunk.voxels[voxelIndex].GetTextureRotation(); // voxel type might not allow texture rotation
                        int microVoxelIndex = GetMicroVoxelIndex(microPos, rotation);

                        MicroVoxels mv = GetMicroVoxels(chunk, voxelIndex, defaultFilled: false);
                        if (mv.SetOccupied(microVoxelIndex)) {

                            vi.chunk = chunk;
                            vi.voxelIndex = voxelIndex;
                            if (!updatedVoxels.Contains(vi)) {
                                updatedVoxels.Add(vi);
                            }

                            if (mv.isFull) {
                                chunk.ClearMicroVoxels(voxelIndex);
                                chunk.voxels[voxelIndex].opaque = chunk.voxels[voxelIndex].type.opaque;
                                chunk.voxelSignature = -1;
                            } else {
                                byte newOpaque = mv.GetOpaqueProportional();
                                if (chunk.voxels[voxelIndex].opaque != newOpaque) {
                                    chunk.voxels[voxelIndex].opaque = newOpaque;
                                    chunk.voxelSignature = -1;
                                }
                            }
                        }
                    }
                }
            }

            if (updatedVoxels.Count == 0) {
                BufferPool<VoxelIndex>.Release(updatedVoxels);
                return false;
            }

            foreach (var v in updatedVoxels) {
                v.chunk.voxelSignature = -1;
                ChunkRequestRefresh(v.chunk, false, true);
                ClearLightmapAtPosition(v.chunk, v.voxelIndex);

                // Force rebuild neighbour meshes if destroyed voxel is on a border
                RebuildNeighboursIfNeeded(v.chunk, v.voxelIndex);

                // Events
                RegisterChunkChanges(v.chunk);
            }

            BufferPool<VoxelIndex>.Release(updatedVoxels);

            return true;
        }


        MicroVoxels GetMicroVoxels (VoxelChunk chunk, int voxelIndex, bool defaultFilled = false) {

            if (chunk.microVoxels == null) {
                chunk.microVoxels = new Dictionary<int, MicroVoxels>();
            }
            if (!chunk.usesMicroVoxels) {
                chunk.usesMicroVoxels = true;
                chunk.voxelSignature = -1;
            }

            if (!chunk.microVoxels.TryGetValue(voxelIndex, out MicroVoxels mv)) {
                mv = new MicroVoxels();
                chunk.microVoxels[voxelIndex] = mv;
                if (defaultFilled) {
                    mv.Fill();
                }
            }

            return mv;
        }

        /// <summary>
        /// 복셀의 회전 인덱스를 고려하여 주어진 월드 위치의 마이크로복셀에 해당하는 복셀 내부 인덱스를 반환합니다.
        /// </summary>
        int GetMicroVoxelIndex (Vector3d position, int rotation) {
            GetMicroVoxelCoordinates(position, rotation, out int px, out int py, out int pz);
            return px + pz * MicroVoxels.COUNT_PER_AXIS + py * MicroVoxels.COUNT_PER_FACE;
        }


        /// <summary>
        /// 주어진 세계 위치의 마이크로복셀에 해당하는 복셀 내부의 인덱스를 반환합니다.
        /// </summary>
        int GetMicroVoxelIndex (int px, int py, int pz) {
            return px + pz * MicroVoxels.COUNT_PER_AXIS + py * MicroVoxels.COUNT_PER_FACE;
        }

        /// <summary>
        /// 마이크로복셀에 해당하는 복셀 내부의 좌표를 반환합니다.
        /// </summary>
        void GetMicroVoxelCoordinates (int index, out int px, out int py, out int pz) {
            px = index & MicroVoxels.COUNT_PER_AXIS_MINUS_ONE;
            py = index / MicroVoxels.COUNT_PER_FACE;
            pz = (index / MicroVoxels.COUNT_PER_AXIS) & MicroVoxels.COUNT_PER_AXIS_MINUS_ONE;
        }

        /// <summary>
        /// 주어진 세계 위치의 마이크로복셀에 해당하는 복셀 내부의 좌표를 반환합니다.
        /// </summary>
        void GetMicroVoxelCoordinates (Vector3d position, int rotation, out int px, out int py, out int pz) {
            Vector3d iposition = position;
            FastVector.Floor(ref iposition);

            // Calculate fractional positions within the voxel
            double fracX = position.x - iposition.x;
            double fracZ = position.z - iposition.z;
            double fracY = position.y - iposition.y;

            // Calculate initial cell indices
            int cellIndexX = (int)(fracX * MicroVoxels.COUNT_PER_AXIS);
            int cellIndexZ = (int)(fracZ * MicroVoxels.COUNT_PER_AXIS);

            // Handle rotation using integer operations
            switch (rotation) {
                case 1: // 90 degrees CCW
                    (cellIndexX, cellIndexZ) = (MicroVoxels.COUNT_PER_AXIS - 1 - cellIndexZ, cellIndexX);
                    break;
                case 2: // 180 degrees
                    cellIndexX = MicroVoxels.COUNT_PER_AXIS - 1 - cellIndexX;
                    cellIndexZ = MicroVoxels.COUNT_PER_AXIS - 1 - cellIndexZ;
                    break;
                case 3: // 270 degrees CCW
                    (cellIndexX, cellIndexZ) = (cellIndexZ, MicroVoxels.COUNT_PER_AXIS - 1 - cellIndexX);
                    break;
            }

            // Assign final coordinates
            px = cellIndexX;
            pz = cellIndexZ;
            py = (int)(fracY * MicroVoxels.COUNT_PER_AXIS);
        }

        /// <summary>
        /// 주어진 복셀 위치에서 마이크로복셀의 중심을 반환합니다.
        /// </summary>
        Vector3d GetMicroVoxelPosition (Vector3d position, int px, int py, int pz) {
            FastVector.Floor(ref position);
            position.x += (px + 0.5f) * MicroVoxels.SIZE;
            position.y += (py + 0.5f) * MicroVoxels.SIZE;
            position.z += (pz + 0.5f) * MicroVoxels.SIZE;
            return position;
        }

        /// <summary>
        /// 주어진 복셀 위치에서 마이크로복셀의 중심을 반환합니다.
        /// </summary>
        Vector3d GetMicroVoxelPosition (Vector3d position) {
            position.x = (Math.Floor(position.x / MicroVoxels.SIZE) + 0.5) * MicroVoxels.SIZE;
            position.y = (Math.Floor(position.y / MicroVoxels.SIZE) + 0.5) * MicroVoxels.SIZE;
            position.z = (Math.Floor(position.z / MicroVoxels.SIZE) + 0.5) * MicroVoxels.SIZE;
            return position;
        }

        /// <summary>
        /// 청크의 복셀과 voxelIndex에 마이크로복셀이 포함되어 있으면 true를 반환합니다.
        /// </summary>
        public bool IsMicroVoxelAtPosition (VoxelChunk chunk, int voxelIndex) {
            if ((object)chunk == null) return false;
            if (!chunk.usesMicroVoxels) return false;
            return chunk.microVoxels.ContainsKey(voxelIndex);
        }

        /// <summary>
        /// 청크의 복셀과 voxelIndex에 마이크로복셀이 포함되어 있으면 true를 반환합니다.
        /// </summary>
        public bool IsMicroVoxelAtPosition (ref VoxelHitInfo hitInfo) {
            Vector3d position = hitInfo.voxelCenter + hitInfo.normal * MicroVoxels.SIZE;
            return IsMicroVoxelAtPosition(position);
        }

        /// <summary>
        /// 월드 공간의 한 위치에 있는 복셀에 마이크로복셀이 포함된 경우 true를 반환합니다.
        /// </summary>
        public bool IsMicroVoxelAtPosition (Vector3d position) {
            if (!GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex, false)) return false;
            return IsMicroVoxelAtPosition(chunk, voxelIndex);
        }

        public Boundsd GetMicroVoxelBounds (ref VoxelHitInfo hitInfo, int microVoxelSize, float padding = 0) {
            Vector3d v1 = hitInfo.center;
            double size = MicroVoxels.SIZE * microVoxelSize;
            Vector3d size3d = new Vector3d(size, size, size);
            Vector3d min, max;
            if (microVoxelsSnap) {
                min = v1;
                min.x = Math.Floor(min.x / size) * size;
                min.y = Math.Floor(min.y / size) * size;
                min.z = Math.Floor(min.z / size) * size;
            } else {
                Vector3d midPoint = v1 - hitInfo.normal * (0.5f * MicroVoxels.SIZE * (microVoxelSize - 1));
                min = midPoint - size3d * 0.5;
            }
            min = GetMicroVoxelPosition(min);
            min -= MicroVoxels.SIZE_3D * 0.5;
            max = min + size3d;
            size3d.x += padding;
            size3d.y += padding;
            size3d.z += padding;
            return new Boundsd((min + max) * 0.5, size3d);
        }

    }

}