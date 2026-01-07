using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[CreateAssetMenu (menuName = "Voxel Play/Terrain Generators/Fast Flat Terrain Generator", fileName = "FastFlatTerrainGenerator", order = 103)]
	public class FastFlatTerrainGenerator : VoxelPlayTerrainGenerator {

		public int altitude = 50;
		public VoxelDefinition terrainVoxel;
		public Color32 voxelColor1 = new Color32 (0, 128, 0, 255);
		public Color32 voxelColor2 = new Color32 (128, 0, 0, 255);


        public override void GetTerrainVoxelDefinitions (List<VoxelDefinition> vds) {
            if (terrainVoxel != null) vds.Add(terrainVoxel);
        }

		/// <summary>
		/// 데이터 구조를 초기화하거나 다시 로드하는 데 사용됩니다.
		/// </summary>
		protected override void Init () {
			if (terrainVoxel == null) {
				terrainVoxel = VoxelPlayEnvironment.instance.defaultVoxel;
			}
			env.AddVoxelDefinition (terrainVoxel);
		}

		/// <summary>
		/// 고도와 습기를 얻습니다.
		/// </summary>
		/// <param name="x">x 좌표입니다.</param>
		/// <param name="z">z 좌표입니다.</param>
		/// <param name="altitude">고도.</param>
		/// <param name="moisture">수분.</param>
		public override void GetHeightAndMoisture (double x, double z, out float altitude, out float moisture) {
			altitude = this.altitude / maxHeight;
			moisture = 0;
		}

		/// <summary>
		/// 중앙 "위치"에 의해 정의된 청크 내부의 지형을 그립니다.
		/// </summary>
		/// <returns><c>진실</c>, if terrain was painted, <c>거짓</c> otherwise.</returns>
		public override bool PaintChunk (VoxelChunk chunk) {
			int chunkBottomPos = FastMath.FloorToInt (chunk.position.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE);
			if (chunkBottomPos >= altitude) {
				return false; // does not have contents
			}

			// Voxel Color (checker board style)
			Color32 voxelColor;
			uint chance = (((uint)chunk.position.x + (uint)chunk.position.z) / VoxelPlayEnvironment.CHUNK_SIZE) % 2;
			if (chance == 1) {
				voxelColor = voxelColor1;
			} else {
				voxelColor = voxelColor2;
			}

			// a chunk is made of 16x16x16 voxels - calculate the last voxel position in the array to be filled
			// constant ONE_Y_ROW equals to the number of voxels in an horizontal slice (ie. 16*16 voxels per row)
			int maxY = altitude - chunkBottomPos;
			int lastVoxel = maxY * ONE_Y_ROW;
			if (lastVoxel >= chunk.voxels.Length)
				lastVoxel = chunk.voxels.Length;

			// Fill the chunk.voxels 3D array with voxels
			for (int k = 0; k < lastVoxel; k++) {
				chunk.voxels [k].Set (terrainVoxel, voxelColor);
			}

			// For correct light illumination, specify if this chunk on surface level
			int chunkTopPos = (int)chunk.position.y + VoxelPlayEnvironment.CHUNK_HALF_SIZE;
			chunk.isAboveSurface = chunkTopPos >= altitude;

			return true; // true = > this chunk has contents
		}

	}
}

