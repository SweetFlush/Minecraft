using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        struct LightmapAddNode {
            public VoxelChunk chunk;
            public int voxelIndex;
        }

        struct LightmapRemovalNode {
            public VoxelChunk chunk;
            public int voxelIndex;
            public int light;
        }

        bool effectiveGlobalIllumination {
            [MethodImpl(256)]
            get {
#if UNITY_EDITOR
                if (!applicationIsPlaying)
                    return false;
#endif
                return globalIllumination;
            }
        }


        void InitLightmap() {
            sunLightmapRemovalQueue = new FastList<LightmapRemovalNode>(128);
            sunLightmapSpreadQueue = new FastList<LightmapAddNode>(128);
            torchLightmapRemovalQueue = new FastList<LightmapRemovalNode>(128);
            torchLightmapSpreadQueue = new FastList<LightmapAddNode>(128);
            if (!effectiveGlobalIllumination) {
                Voxel.Hole.light = FULL_LIGHT;
            }
        }

        /// <summary>
        /// 빛의 전파를 계산합니다.오직 태양빛.횃불과 같은 다른 광원은 셰이더 자체에서 처리됩니다.
        /// </summary>이자형
        void ComputeLightmap(VoxelChunk chunk) {
            chunk.lightmapIsClear = false;
            chunk.needsLightmapRebuild = false;

            if (!effectiveGlobalIllumination) {
                return;
            }

            int lightSourcesCount = chunk.lightSources != null ? chunk.lightSources.Count : 0;
            for (int k = 0; k < lightSourcesCount; k++) {
                LightSource ls = chunk.lightSources[k];
                SetTorchLightmap(chunk, ls.voxelIndex, ls.lightIntensity);
            }
            ComputeSunLightmap(chunk);
            ComputeTorchLightmap(chunk);
        }

        /// <summary>
        /// 빠른 라이트맵 변경을 계산합니다.
        /// </summary>
        void ProcessLightmapUpdates() {
            if (!effectiveGlobalIllumination) {
                return;
            }

            ProcessSunLightmapRemoval();
            ProcessSunLightmapSpread();
            ProcessTorchLightmapRemoval();
            ProcessTorchLightmapSpread();
        }

        /// <summary>
        /// 해당 위치의 태양 및 토치 라이트맵 지우기
        /// </summary>
        void ClearLightmapAtPosition(VoxelChunk chunk, int voxelIndex) {
            ClearSunLightmap(chunk, voxelIndex);
            ClearTorchLightmap(chunk, voxelIndex);
        }

        /// <summary>
        /// 하나의 복셀이 파괴되면 라이트맵이 확산됩니다.
        /// </summary>
        void SpreadLightmapAroundPosition(VoxelChunk chunk, int voxelIndex) {
            SpreadSunLightmapAroundVoxel(chunk, voxelIndex);
            SpreadTorchLightmapAroundVoxel(chunk, voxelIndex);
        }

    }



}

