using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		struct VegetationRequest {
			public Vector3d position;
			public VoxelDefinition vd;
		}

		const int VEGETATION_CREATION_BUFFER_SIZE = 20000;

		VegetationRequest[] vegetationRequests;
		int vegetationRequestLast, vegetationRequestFirst;

		void InitVegetation () {
			if (vegetationRequests == null || vegetationRequests.Length != VEGETATION_CREATION_BUFFER_SIZE) {
				vegetationRequests = new VegetationRequest[VEGETATION_CREATION_BUFFER_SIZE];
			}
			vegetationRequestLast = -1;
			vegetationRequestFirst = -1;
		}

		/// <summary>
		/// 식물 생성을 요청합니다.
		/// </summary>
		public void RequestVegetationCreation (Vector3d position, VoxelDefinition vd) { 
			if (!enableVegetation) {
				return;
			}

			vegetationRequestLast++;
			if (vegetationRequestLast >= vegetationRequests.Length) {
				vegetationRequestLast = 0;
			}
			if (vegetationRequestLast != vegetationRequestFirst) {
				vegetationRequests[vegetationRequestLast].position = position;
				vegetationRequests [vegetationRequestLast].vd = vd;
				vegetationInCreationQueueCount++;
			}
		}

		/// <summary>
		/// 새로운 식생 요청 대기열을 모니터링합니다.이 함수는 Createvegetation을 호출하여 식생 데이터를 생성하고 청크 새로 고침을 푸시합니다.
		/// </summary>
		void CheckVegetationRequests (long endTime) {
			int max = maxBushesPerFrame > 0 ? maxBushesPerFrame : 10000;
			for (int k = 0; k < max; k++) {
				if (vegetationRequestFirst == vegetationRequestLast)
					return;
				vegetationRequestFirst++;
				if (vegetationRequestFirst >= vegetationRequests.Length) {
					vegetationRequestFirst = 0;
				}
				vegetationInCreationQueueCount--;

				if (GetVoxelIndex(vegetationRequests[vegetationRequestFirst].position, out VoxelChunk chunk, out int voxelIndex)) {
					if (!chunk.modified && chunk.voxels[voxelIndex].opaque < FULL_OPAQUE) {
						CreateVegetation(chunk, voxelIndex, vegetationRequests[vegetationRequestFirst].vd);
                    }
                }
                long elapsed = stopWatch.ElapsedMilliseconds;
                if (elapsed >= endTime)
                    break;
            }
        }

		/// <summary>
		/// 위치, 생물 군계 및 임의 값을 기반으로 식물 복셀을 가져옵니다.
		/// </summary>
		/// <returns>식물.</returns>
		/// <param name="biome">생물 군계.</param>
		/// <param name="random">무작위의.</param>
		public VoxelDefinition GetVegetation (BiomeVegetation[] vegetation, float random) {
			float acumProb = 0;
			int index = 0;
			for (int t = 0; t < vegetation.Length; t++) {
				acumProb += vegetation [t].probability;
				if (random < acumProb) {
					index = t;
					break;
				}
			}
			return vegetation [index].vegetation;
		}


		/// <summary>
		/// 식물을 생성합니다.
		/// </summary>
		void CreateVegetation (VoxelChunk chunk, int voxelIndex, VoxelDefinition vd) {

			if ((object)chunk != null) {
				// Updates current chunk
				if (chunk.allowTrees && chunk.voxels [voxelIndex].opaque < FULL_OPAQUE) {
					chunk.voxels [voxelIndex].Set (vd);
					vegetationCreated++;
					ChunkRequestRefresh(chunk, false, true);
				}
			}
		}


	}



}
