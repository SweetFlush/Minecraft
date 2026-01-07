using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public static partial class NoiseTools {

		/// <summary>
		/// 지형 샘플링에 대한 무작위 시드 오프셋입니다.영점 위치를 변환하여 다양한 지형 출력을 제공하는 데 사용됩니다.
		/// </summary>
		public static Vector3 seedOffset;

		/// <summary>
		/// 기타지형 생성기에 유용한 기능
		/// </summary>
		/// <returns>텍스처의 노이즈 값입니다.</returns>
		/// <param name="tex">텍스트.</param>
		/// <param name="textureSize">텍스처 크기.</param>
		public static float[] LoadNoiseTexture(Texture tex, out int textureSize) {
			if (tex == null) {
				textureSize = 0;
				return null;
			}
			textureSize = tex.width;
            Color[] temp;
			if (tex is Texture2D) {
				Texture2D tex2D = (Texture2D)tex;
				temp = tex2D.GetPixels();
			} else if (tex is Texture3D) {
				Texture3D tex3D = (Texture3D)tex;
				temp = tex3D.GetPixels();
			} else return null;
				
			int count = temp.Length;
			float[] values = new float[count];
			for (int k = 0; k < temp.Length; k++) {
				values[k] = temp[k].r;
			}
			return values;
		}

        /// <summary>
        /// 기타지형 생성기에 유용한 기능
        /// </summary>
        /// <returns>하이트맵의 값.</returns>
        public static float[] LoadHeightmapFromTerrainData(TerrainData td, out int size) {
            if (td == null) {
                size = 0;
                return null;
            }
            size = td.heightmapResolution;
            float[,] heightmap = td.GetHeights(0, 0, size, size);

            float[] values = new float[size * size];
            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    values[i * size + j] = heightmap[i, j];
                }
            }
            return values;
        }


        /// <summary>
        /// 주어진 월드 위치에서 2D 노이즈 텍스처를 샘플링합니다(원시 값 반환).
        /// </summary>
        /// <returns>노이즈 값은 하나의 샘플입니다.</returns>
        /// <param name="noiseArray">노이즈 배열.</param>
        /// <param name="textureSize">텍스처 크기.</param>
        /// <param name="x">x 좌표입니다.</param>
        /// <param name="z">z 좌표입니다.</param>
        public static float GetNoiseValue(float[] noiseArray, int textureSize, double x, double z, bool ridgeNoise = false) {

			if (textureSize == 0)
				return 0;

			double zz = z + textureSize * 10000 + seedOffset.z;
			double xx = x + textureSize * 10000 + seedOffset.x;
			int posZInt = (int)zz;
			int posXInt = (int)xx;

			// Texture array position
			int ty0 = posZInt % textureSize;
			int tx0 = posXInt % textureSize;

			float value = noiseArray[ty0 * textureSize + tx0];
			if (ridgeNoise) {
				value = 0.5f - value;
				if (value < 0) {
					value = 2f * (0.5f + value);
				} else {
					value = 2f * (0.5f - value);
				}
			}
			return value;
		}

		/// <summary>
		/// 이중선형 필터링을 사용하여 주어진 월드 위치에서 2D 노이즈 텍스처를 샘플링합니다.
		/// </summary>
		/// <returns>노이즈 값은 이중선형입니다.</returns>
		/// <param name="noiseArray">노이즈 배열.</param>
		/// <param name="textureSize">텍스처 크기.</param>
		/// <param name="x">x 좌표입니다.</param>
		/// <param name="z">z 좌표입니다.</param>
		public static float GetNoiseValueBilinear(float[] noiseArray, int textureSize, double x, double z, bool ridgeNoise = false) {

			if (textureSize == 0)
				return 0;

			double zz = z + textureSize * 10000 + seedOffset.z;
			double xx = x + textureSize * 10000 + seedOffset.x;
			int posZInt = (int)zz;
			int posXInt = (int)xx;
			float fy =(float)(zz - posZInt);
			float fx =(float)(xx - posXInt);

			// Texture array position
			int ty0 = posZInt % textureSize;
			int tx0 = posXInt % textureSize;

			// Get noise for upper/left corner
			int ty, tx;
			ty = (ty0 == textureSize - 1) ? 0 : ty0 + 1;
			float noiseUL = noiseArray[ty * textureSize + tx0];
			// Get noise for upper/right corner
			tx = (tx0 == textureSize - 1) ? 0 : tx0 + 1;
			float noiseUR = noiseArray[ty * textureSize + tx];
			// Get noise for bottom/left corner
			float noiseBL = noiseArray[ty0 * textureSize + tx0];
			// Get noise for bottom/right corner
			float noiseBR = noiseArray[ty0 * textureSize + tx];

			// Bilinear interpolation
			float value =
				(1f - fx) * (fy * noiseUL + (1f - fy) * noiseBL) +
				fx * (fy * noiseUR + (1f - fy) * noiseBR);

			if (ridgeNoise) {
				value = 0.5f - value;
				if (value < 0) {
					value = 2f * (0.5f + value);
				} else {
					value = 2f * (0.5f - value);
				}
			}
			return value;
		}


		/// <summary>
		/// 주어진 월드 위치에서 3D 노이즈 텍스처를 샘플링합니다(원시 값 반환).
		/// </summary>
		/// <returns>노이즈 값은 하나의 샘플입니다.</returns>
		/// <param name="noiseArray">노이즈 배열.</param>
		/// <param name="textureSize">텍스처 크기.</param>
		/// <param name="x">x 좌표입니다.</param>
		/// <param name="y">y 좌표입니다.</param>
		/// <param name="z">z 좌표입니다.</param>
		public static float GetNoiseValue(float[] noiseArray, int textureSize, double x, double y, double z) {

			float f = GetNoiseValue (noiseArray, textureSize, x, z);
			float t = GetNoiseValue (noiseArray, textureSize, 1, y);

			f = f * t * 2.0f;
			if (f > 1f)
				f--;
			return f;

		}


	}

}