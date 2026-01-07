using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public abstract partial class VoxelPlayTerrainGenerator : ScriptableObject {

		protected const int ONE_Y_ROW = VoxelPlayEnvironment.ONE_Y_ROW;
		protected const int ONE_Z_ROW = VoxelPlayEnvironment.ONE_Z_ROW;

		[Header("Terrain Parameters")]
		[Tooltip("지형 생성기가 허용하는 최대 높이입니다(일반적으로 255와 동일).지형 생성기에 의해 반환된 고도는 0-1 범위에 있으며 이 값을 곱하여 세계의 각 위치에 대한 실제 지형 고도를 생성합니다.")]
		public float maxHeight = 255;

		[Tooltip("세계의 최소 높이.이 값은 일부 지형 생성기에서 지형의 깊이를 제한하거나 기반암 복셀을 배치하는 데 사용됩니다.")]
		public float minHeight = -32;

		[Tooltip("물 렌더링을 방지하려면 비활성화하세요.")]
		public bool addWater = true;

		[Tooltip("수위(수위).세계에서 호수나 바다와 같은 물을 사용하지 않는 경우 이 값을 0으로 설정하세요.")]
		public int waterLevel = 25;

		/// <summary>
		/// 지형 생성기가 하이트맵이나 습기를 사용하지 않는 경우 false로 설정합니다.이는 지형 생성기에 의해 노출된 하이트맵에 의존하지 않는 PaintChunk 메서드에 사용자 정의 콘텐츠를 작성하는 경우 유용합니다.
		/// </summary>
		[NonSerialized]
		public bool usesHeightAndMoisture = true;

		[NonSerialized]
		protected VoxelPlayEnvironment env;

		[NonSerialized]
		protected WorldDefinition world;

		/// <summary>
		/// 캐시된 데이터를 재설정하고 정보를 다시 로드합니다.이 방법은 선택 사항입니다.
		/// </summary>
		protected virtual void Init () { }

		/// <summary>
		/// 고도와 습도(0..1 범위)를 가져옵니다.이 방법은 선택 사항입니다.
		/// </summary>
		/// <param name="x">x 좌표입니다.</param>
		/// <param name="z">z 좌표입니다.</param>
		/// <param name="altitude">고도(0..1 범위).</param>
		/// <param name="moisture">수분(0..1 범위).</param>
		public virtual void GetHeightAndMoisture (double x, double z, out float altitude, out float moisture) {
			usesHeightAndMoisture = false;
			altitude = 0;
			moisture = 0;
		}

		/// <summary>
		/// 중앙 "위치"에 의해 정의된 청크 내부의 지형을 그립니다.
		/// </summary>
		/// <returns><c>진실</c>, if terrain was painted, <c>거짓</c> otherwise.</returns>
		public abstract bool PaintChunk (VoxelChunk chunk);


		/// <summary>
		/// 이 지형 생성기가 지형을 구축할 때 사용할 수 있는 복셀 정의 목록을 반환합니다(물이나 초목, 나무, .. 땅만 해당).
		/// </summary>
		/// <param name="voxelDefinitions"></param>
		public virtual void GetTerrainVoxelDefinitions (List<VoxelDefinition> voxelDefinitions) {
			Debug.LogWarning($"The terrain generator { name } does not implement the GetTerrainVoxelDefinitions method. It's recommended, especially if you plan to use the world editor tools.");
		}

		/// <summary>
		/// 지형 생성기를 사용할 준비가 되면 true를 반환합니다.그렇지 않으면 초기화()를 호출하십시오.
		/// </summary>
		[NonSerialized]
		public bool isInitialized;



		/// <summary>
		/// 지형 생성기를 초기화하려면 이 방법을 사용하세요.
		/// </summary>
		public void Initialize () {
			env = VoxelPlayEnvironment.instance;
			if (env == null)
				return;
			world = env.world;
			if (addWater) {
				if (waterLevel > maxHeight) {
					Debug.LogWarning("Water level is higher than terrain maximum height. Check terrain settings.");
				}
				env.waterLevel = waterLevel;
				env.hasWater = true;
			} else {
				env.hasWater = false;
			}
			Init();
			if (world == null)
				return;
			isInitialized = true;
		}

	}

}