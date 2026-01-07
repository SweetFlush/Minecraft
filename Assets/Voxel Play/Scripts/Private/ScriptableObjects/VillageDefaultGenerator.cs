using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[CreateAssetMenu(menuName = "Voxel Play/Detail Generators/Village Generator", fileName = "VillageGenerator", order = 102)]
	public class VillageDefaultGenerator : VoxelPlayDetailGenerator {

        [Range(0,0.1f)]
		public float spawnProbability = 0.02f;
		public ModelDefinition[] buildings;

		struct BuildingStatus {
			public float height;
			public bool placementStatus;
		}


		VoxelPlayEnvironment env;
		// x,y,z chunk position  w cached terrain height
		Dictionary<Vector3d, BuildingStatus> buildingPositions;

		/// <summary>
		/// 초기화 방법.시작 시 Voxel Play에 의해 호출됩니다.
		/// </summary>
		public override void Init() {
			env = VoxelPlayEnvironment.instance;
			buildingPositions = new Dictionary<Vector3d, BuildingStatus>(100);

			// Fill models with empty blocks so they clear any terrain or vegetation inside them when placing on the world
			if (buildings != null && buildings.Length > 0) {
				for (int k = 0; k < buildings.Length; k++) {
					env.ModelFillInside(buildings[k]);
				}
			}
		}


		/// <summary>
		/// 플레이어가 다른 청크로 이동했음을 알리기 위해 Voxel Play에서 호출되어 새로운 세부 정보 생성이 시작될 수 있습니다.
		/// </summary>
		/// <param name="position">현재 플레이어 위치.</param>
		/// <param name="checkOnlyBorders">True는 플레이어가 다음 청크로 이동했음을 의미합니다.False는 플레이어 위치가 완전히 새로운 것이며 모든 청크가
		/// 이 호출에서 자세한 내용은 범위를 확인해야 합니다.</param>
		/// <param name="endTime">이 프레임을 실행하기 위한 최대 시간 프레임을 제공합니다.이것을 env.stopwatch 밀리초와 비교해 보세요.</param>
		/// <returns><c>진실</c>, if there's more work to be executed, <c>거짓</c> otherwise.</returns>
		public override bool ExploreArea(Vector3d position, bool checkOnlyBorders, long endTime) {
			float prob = Mathf.Clamp01 (1f - spawnProbability);
			int explorationRange = env.visibleChunksDistance + 10;
			int minz = -explorationRange;
			int maxz = +explorationRange;
			int minx = -explorationRange;
			int maxx = +explorationRange;
			position = env.GetChunkPosition (position);
			Vector3d pos = position;
			for (int z = minz; z <= maxz; z++) {
				for (int x = minx; x < maxx; x++) {
					if (checkOnlyBorders && z > minz && z < maxz && x > minx && x < maxx) continue;
					pos.x = position.x + x * VoxelPlayEnvironment.CHUNK_SIZE;
					pos.z = position.z + z * VoxelPlayEnvironment.CHUNK_SIZE;
					if (WorldRand.GetValue(pos) > prob) {
						BuildingStatus bs;
						if (!buildingPositions.TryGetValue(pos, out bs)) {
							float h = env.GetTerrainHeight(pos, false);
							if (h > env.waterLevel) {
								bs.height = h;
								bs.placementStatus = false;

								// No trees on this chunk
								VoxelChunk chunk;
								env.GetChunk(pos, out chunk, false);
								if (chunk != null) {
									chunk.allowTrees = false;
								}
							} else {
								bs.placementStatus = true;
							}
							buildingPositions[pos] = bs;
						}
					}
				}
			}
			return false;
		}


		/// <summary>
		/// 주어진 청크를 세부사항으로 채웁니다.채워진 복셀은 지형 생성기로 대체되지 않습니다.
		/// Voxel.Empty를 사용하여 공백을 채웁니다.
		/// </summary>
		/// <param name="chunk">큰 덩어리.</param>
		public override void AddDetail(VoxelChunk chunk) {

			// if chunk is within distance any village center, render the village
			BuildingStatus bs;
			if (buildingPositions.TryGetValue(chunk.position, out bs)) {
				if (!bs.placementStatus) {
					bs.placementStatus = true;
					buildingPositions[chunk.position] = bs;

					Vector3d pos = chunk.position;
					pos.y = bs.height;

					// if this chunk is marked as modified (has been modified by user), don't place any model here
					if (env.GetChunk(pos, out chunk) && chunk.modified) return;

					// otherwise, place the model
					ModelDefinition buildingModel = buildings[WorldRand.Range(0, buildings.Length, pos)];
					env.ModelPlace(pos, buildingModel, WorldRand.Range(0, 3) * 90, 1f, true);

				}
			}
		}




	}

}