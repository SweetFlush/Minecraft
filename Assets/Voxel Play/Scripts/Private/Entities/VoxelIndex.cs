using System;

namespace VoxelPlay {

    /// <summary>
    /// 복셀 인덱스는 세계의 복셀 위치를 나타냅니다.
    /// </summary>
    public struct VoxelIndex {

        /// <summary>
        /// 월드 공간에서의 위치.
        /// </summary>
        public Vector3d position;

        /// <summary>
        /// 이 복셀이 속한 청크
        /// </summary>
        public VoxelChunk chunk;

        /// <summary>
        /// Chunk.voxels[] 배열에 있는 이 복셀의 인덱스
        /// </summary>
        public int voxelIndex;

        /// <summary>
        /// GetVoxel Indices 호출에 지정된 위치의 중심까지의 거리(제곱 거리)입니다.
        /// </summary>
        public float sqrDistance;

        /// <summary>
        /// 복셀에 적용되는 피해입니다.VoxelDamage 메서드에만 사용됩니다.
        /// </summary>
        public int damageTaken;

        /// <summary>
        /// 이 복셀의 복셀 정의를 반환합니다.
        /// </summary>
        public VoxelDefinition type {
            get {
                if ((object)chunk != null && voxelIndex >= 0) {
                    return chunk.voxels[voxelIndex].type;
                }
                return null;
            }
            set {
                if (value != null) {
                    chunk.voxels[voxelIndex].typeIndex = value.index;
                }
            }
        }

        /// <summary>
        /// 이 복셀에 대한 env.voxelDefinitions 배열의 복셀 정의(정수 인덱스)를 반환합니다.
        /// </summary>
        /// <value>유형의 인덱스입니다.</value>
        public int typeIndex {
            get {
                if ((object)chunk != null && voxelIndex >= 0) {
                    return chunk.voxels[voxelIndex].typeIndex;
                }
                return 0;
            }
        }

        public static VoxelIndex Null = new VoxelIndex();

        public override bool Equals(object obj) {
            if (obj is VoxelIndex other) {
                return chunk == other.chunk && voxelIndex == other.voxelIndex;
            }
            return false;
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 23 + (chunk != null ? chunk.GetHashCode() : 0);
            hash = hash * 23 + voxelIndex;
            return hash;
        }
    }
}