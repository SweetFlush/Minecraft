using System;
using UnityEngine;

namespace VoxelPlay {

	[HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000027332-detail-generators")]
	public abstract class VoxelPlayDetailGenerator : ScriptableObject {

		public bool enabled = true;

        [Tooltip("AddDetail 메서드에 대한 다중 중첩 호출을 허용하려면 true로 설정합니다.이는 AddDetail 메서드가 근처 청크 생성을 트리거하여 세부 정보 생성기를 호출하는 경우 발생할 수 있습니다.")]
		public bool allowNestedExecutions;

		[NonSerialized]
		public int detailGeneratorIndex;

		[NonSerialized]
		public bool busy;

		protected const int ONE_Y_ROW = VoxelPlayEnvironment.CHUNK_SIZE * VoxelPlayEnvironment.CHUNK_SIZE;
		protected const int ONE_Z_ROW = VoxelPlayEnvironment.CHUNK_SIZE;

		/// <summary>
		/// 초기화 방법.시작 시 Voxel Play에 의해 호출됩니다.
		/// </summary>
		public virtual void Init() { }


		/// <summary>
		/// 플레이어가 다른 청크로 이동했음을 알리기 위해 Voxel Play에서 호출되어 새로운 세부 정보 생성이 시작될 수 있습니다.
		/// </summary>
		/// <param name="currentPosition">현재 플레이어 위치.</param>
		/// <param name="checkOnlyBorders">True는 플레이어가 다음 청크로 이동했음을 의미합니다.False는 플레이어 위치가 완전히 새로운 것이며 이 호출에서 범위 내 모든 청크의 세부 사항을 확인해야 함을 의미합니다.</param>
		/// <param name="endTime">이 프레임을 실행하기 위한 최대 시간 프레임을 제공합니다.이것을 env.stopwatch 밀리초와 비교해 보세요.</param>
		/// <returns><c>진실</c>, if there's more work to be executed, <c>거짓</c> otherwise.</returns>
		public virtual bool ExploreArea(Vector3d currentPosition, bool checkOnlyBorders, long endTime) { return false; }

		/// <summary>
		/// 세부 정보를 증분식으로 계산하여 필요할 때 세부 정보를 준비할 수 있도록 Voxel Play에서 호출됩니다(GetDetail 메서드로 검색).
		/// 런타임 시 이 메서드는 특정 스레드에서 호출되므로 Unity API를 사용할 수 없습니다.
		/// This method should not produce spikes nor heavy computation in a single frame.		
		/// </summary>
		/// <param name="endTime">이 프레임을 실행하기 위한 최대 시간 프레임을 제공합니다.이것을 env.stopwatch 밀리초와 비교해 보세요.</param>
		/// <returns><c>진실</c>, if there's more work to be executed, <c>거짓</c> otherwise.</returns>
		public virtual bool DoWork(long endTime) { return false; }

		/// <summary>
		/// 주어진 청크를 세부사항으로 채웁니다.채워진 복셀은 지형 생성기로 대체되지 않습니다.
		/// Voxel.Empty를 사용하여 공백을 채웁니다.
		/// </summary>
		public virtual void AddDetail(VoxelChunk chunk) { return; }


		/// <summary>
		/// 세계가 그에 따라 업데이트되도록 청크 내용을 수정하는 경우 DoWork() / AddDetail() 코드에서 이 메서드를 호출하세요.
		/// </summary>
		public void SetChunkIsDirty(VoxelChunk chunk) {
			if (chunk.isPopulated) {
				// if this detail generator has modified a fully generated chunk, we need to refresh it completely
				VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
				env.ChunkRedraw (chunk, includeNeighbours: true, refreshLightmap: true, refreshMesh: true);
			} else {
				// otherwise, just inform that this chunk has changes so the rest of pipeline (during CreateChunk) ensures the lightmap and mesh is rebuilt
				chunk.isDirty = true;
			}
		}

	}

}