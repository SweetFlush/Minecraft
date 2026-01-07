using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		// Chunk buffer & cache
		VoxelChunk[] chunksPool;

		/// <summary>
		/// 생성 중인 풀의 청크 인덱스
		/// </summary>
		int chunksPoolCurrentIndex;

		/// <summary>
		/// 마지막 청크가 내용이 포함된 청크를 생성했는지 여부를 주석으로 표시하는 데 사용됩니다.그렇지 않으면 복셀 메모리 공간을 사용하십시오.
		/// </summary>
		bool chunksPoolFetchNew;

		/// <summary>
		/// 청크 슬롯의 증분 비차단 사전 로드에 사용됩니다.
		/// </summary>
		int chunksPoolLoadIndex;

		/// <summary>
		/// 버퍼 시작 시 재사용 불가능한 것으로 표시된 많은 클라우드 청크로 인해 재사용 가능한 청크의 조회를 최적화하는 데 사용됩니다.
		/// </summary>
		int chunksPoolFirstReusableIndex;

		#region Chunks pool functions

		void ReserveChunkMemory () {
			if (chunksPoolLoadIndex < maxChunks) {
				chunksPool[chunksPoolLoadIndex] = CreateChunkPoolEntry();
				chunksPool[chunksPoolLoadIndex].poolIndex = chunksPoolLoadIndex;
				chunksPoolLoadIndex++;
			}
		}


		void FetchNewChunkIndex (Vector3d position) {
			if (chunksUsed >= chunksPool.Length) {
				ReuseChunkEntry(position);
			} else {
				if (chunksPoolCurrentIndex >= chunksPoolLoadIndex - 1) {
					for (int k = 0; k < 1000; k++) {
						ReserveChunkMemory();
					}
				}
				chunksPoolCurrentIndex++;
				chunksUsed++;
			}
		}

		void ComputeFirstReusableChunk () {
			chunksPoolFirstReusableIndex = 0;
			if (chunksPool == null)
				return;
			for (int k = 0; k < chunksPool.Length; k++) {
				if (!chunksPool[k].cannotBeReused) {
					chunksPoolFirstReusableIndex = k;
					return;
				}
			}
		}

		/// <summary>
		/// 현재 원하는 위치에 가깝지 않고 가시 거리에서 첫 번째 청크를 선택합니다.
		/// </summary>
		void ReuseChunkEntry (Vector3d position) {
			bool valid = false;
			float visibleDistance = (_visibleChunksDistance + 1) * CHUNK_SIZE; // adds one to avoid chunks appearing and disappearing on the border of visible distance
			float minDistance = 8 * CHUNK_SIZE;
			int lastGood = -1;
			bool notifyChunkReuse = captureEvents && OnChunkReuse != null;
			for (int i = 0; i < chunksPool.Length; i++) {
				if (++chunksPoolCurrentIndex >= chunksPool.Length) {
					chunksPoolCurrentIndex = chunksPoolFirstReusableIndex;
				}

				VoxelChunk chunk = chunksPool[chunksPoolCurrentIndex];

				// if chunk has been modified or chunk is marked as non reusable, skip it
				if (chunk.modified || chunk.cannotBeReused)
					continue;

				// if chunk is too near from desired position, skip it
				double dx = position.x - chunk.position.x;
				double dy = position.y - chunk.position.y;
				double dz = position.y - chunk.position.z;
				if (dx >= -minDistance && dx <= minDistance && dy >= -minDistance && dy <= minDistance && dz >= -minDistance && dz <= minDistance)
					continue;
				lastGood = chunksPoolCurrentIndex;

				// if chunk is within visible distance, skip it
				dx = currentAnchorPos.x - chunk.position.x;
				dz = currentAnchorPos.z - chunk.position.z;
				if (dx >= -visibleDistance && dx <= visibleDistance && dz >= -visibleDistance && dz <= visibleDistance)
					continue;

				// check event confirmation
				if (notifyChunkReuse) {
					bool canReuse;
					OnChunkReuse(chunk, out canReuse);
					if (!canReuse)
						continue;
				}

				// chunk seems good, pick it up!
				valid = true;
				break;
			}
			if (lastGood >= 0)
				chunksPoolCurrentIndex = lastGood;
			if (!valid) {
				ShowMessage("Reusing visible chunks (Chunks Pool Size value must be increased)");
			}
			VoxelChunk bestChunk = chunksPool[chunksPoolCurrentIndex];
			lock (lockLastChunkFetch) {
				if (bestChunk == lastChunkFetch)
					lastChunkFetch = null;
			}
			// Remove from render queue
			if (bestChunk.inqueue) {
				RemoveFromRenderQueue(bestChunk);
			}
			// Remove from NavMesh
			ReleaseChunkNavMesh(bestChunk);
			// Reset data
			bestChunk.PrepareForReuse(effectiveGlobalIllumination ? FULL_DARK : FULL_LIGHT);
			// Remove cached chunk at old position
			SetChunkOctreeIsDirty(bestChunk.position, true);
			// Force update chunk visible distance status
			TriggerFarChunksUnloadCheck();
		}

		/// <summary>
		/// 위치에서 옥트리를 가져오고 탐색된 플래그를 지워 해당 영역을 다시 새로 고칠 수 있도록 합니다.
		/// 선택적으로 캐시에서 청크를 제거합니다.
		/// </summary>
		void SetChunkOctreeIsDirty (Vector3d position, bool removeFromCache) {
			FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
			int existingChunkHash = GetChunkHash(chunkX, chunkY, chunkZ);
			if (cachedChunks.TryGetValue(existingChunkHash, out CachedChunk cachedChunk)) {
				Octree octree = cachedChunk.octree;
				while (octree != null) {
					octree.explored = false;
					if (octree.parent != null) {
						octree.parent.exploredChildren--;
					}
					octree = octree.parent;
				}
				if (removeFromCache) {
					cachedChunks.Remove(existingChunkHash);
				}
			}
		}


		VoxelChunk CreateChunkPoolEntry () {

			GameObject chunkGO = Instantiate(chunkPlaceholderPrefab, Misc.vector3far, Misc.quaternionZero, chunksRoot);
			chunkGO.layer = layerVoxels;
			chunkGO.hideFlags = HideFlags.DontSave;
#if UNITY_EDITOR
			if (hideChunksInHierarchy) {
				chunkGO.hideFlags |= HideFlags.HideInHierarchy;
			}
#endif
			chunkGO.name = "Chunk";
			chunkGO.TryGetComponent(out VoxelChunk chunk);
			chunk.voxels = new Voxel[CHUNK_VOXEL_COUNT];
			chunkGO.TryGetComponent(out chunk.mf);
			chunk.mf.sharedMesh = null;
			chunkGO.TryGetComponent(out chunk.mr);
			chunk.mr.enabled = false;
			chunk.mr.receiveShadows = enableShadows;
			if (enableShadows) {
				chunk.mr.shadowCastingMode = ShadowCastingMode.On;
			} else {
				chunk.mr.shadowCastingMode = ShadowCastingMode.Off;
			}
			chunkGO.TryGetComponent(out chunk.mc);
			return chunk;
		}

		#endregion

	}



}
