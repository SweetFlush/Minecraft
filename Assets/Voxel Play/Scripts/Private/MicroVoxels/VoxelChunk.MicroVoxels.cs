using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public partial class VoxelChunk : MonoBehaviour {

        public bool usesMicroVoxels;
        public Dictionary<int, MicroVoxels> microVoxels;

#if UNITY_EDITOR
        [NonSerialized]
        public HeightMapInfo[] terrainInfo;
#endif

        public void SetMicroVoxels (int voxelIndex, MicroVoxels microVoxels) {
            if (microVoxels == null || microVoxels.isEmpty || microVoxels.isFull) {
                ClearMicroVoxels(voxelIndex);
                return;
            }
            if (this.microVoxels == null) {
                this.microVoxels = new Dictionary<int, MicroVoxels>();
            }
            this.microVoxels[voxelIndex] = microVoxels;
            usesMicroVoxels = true;
        }

        public void ClearMicroVoxels (int voxelIndex) {
            if (microVoxels != null) {
                microVoxels.Remove(voxelIndex);
                usesMicroVoxels = microVoxels.Count > 0;
                return;
            }
            usesMicroVoxels = false;
        }
    }

}