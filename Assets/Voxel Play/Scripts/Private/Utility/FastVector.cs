using System;
using UnityEngine;
using System.Collections;

namespace VoxelPlay {
	public static class FastVector {

		/// <summary>
		/// 두 벡터의 평균을 계산하고 결과를 다른 벡터에 씁니다.
		/// </summary>
		public static void Average(ref Vector2 v1, ref Vector2 v2, out Vector2 result) {
			result.x = (v1.x + v2.x) * 0.5f;
			result.y = (v1.y + v2.y) * 0.5f;
		}


		/// <summary>
		/// 두 벡터의 평균을 계산하고 결과를 다른 벡터에 씁니다.
		/// </summary>
		public static void Average(ref Vector3 v1, ref Vector3 v2, out Vector3 result) {
			result.x = (v1.x + v2.x) * 0.5f;
			result.y = (v1.y + v2.y) * 0.5f;
			result.z = (v1.z + v2.z) * 0.5f;
		}

		/// <summary>
		/// 한 벡터를 다른 벡터에서 빼기
		/// </summary>
		public static void Substract(ref Vector3 v1, ref Vector3 v2) {
			v1.x -= v2.x;
			v1.y -= v2.y;
			v1.z -= v2.z;
		}

		/// <summary>
		/// v1에 v2를 추가합니다.
		/// </summary>
		public static void Add(ref Vector3 v1, ref Vector3 v2) {
			v1.x += v2.x;
			v1.y += v2.y;
			v1.z += v2.z;
		}

		/// <summary>
		/// v2에 float 값을 곱하여 v1에 추가합니다.
		/// </summary>
		public static void Add(ref Vector3 v1, ref Vector3 v2, float v) {
			v1.x += v2.x * v;
			v1.y += v2.y * v;
			v1.z += v2.z * v;
		}

		/// <summary>
		/// v2에 float 값을 곱하여 v1에 추가합니다.
		/// </summary>
		public static void Add (ref Vector3d v1, ref Vector3 v2, float v)
		{
			v1.x += v2.x * v;
			v1.y += v2.y * v;
			v1.z += v2.z * v;
		}

		/// <summary>
		/// v1에 v2를 추가합니다.
		/// </summary>
		public static void Add(ref Vector2 v1, ref Vector2 v2) {
			v1.x += v2.x;
			v1.y += v2.y;
		}

		/// <summary>
		/// v2에 float 값을 곱하여 v1에 추가합니다.
		/// </summary>
		public static void Add(ref Vector2 v1, ref Vector2 v2, float v) {
			v1.x += v2.x * v;
			v1.y += v2.y * v;
		}

		/// <summary>
		/// 한 위치에서 다른 위치로 정규화된 방향을 작성합니다.
		/// </summary>
		/// <param name="from">에서.</param>
		/// <param name="to">에게.</param>
		/// <param name="result">결과.</param>
		public static void NormalizedDirection(ref Vector2 from, ref Vector2 to, out Vector2 result) {
			float dx = to.x - from.x;
			float dy = to.y - from.y;
			float length = (float)Math.Sqrt(dx * dx + dy * dy);
			result.x = dx / length;
			result.y = dy / length;
		}


		/// <summary>
		/// 한 위치에서 다른 위치로 정규화된 방향을 작성합니다.
		/// </summary>
		/// <param name="from">에서.</param>
		/// <param name="to">에게.</param>
		/// <param name="result">결과.</param>
		public static void NormalizedDirection(ref Vector3d from, ref Vector3d to, out Vector3 result) {
			double dx = to.x - from.x;
			double dy = to.y - from.y;
			double dz = to.z - from.z;
			double length = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
			result.x = (float)(dx / length);
			result.y = (float)(dy / length);
			result.z = (float)(dz / length);
		}

		/// <summary>
		/// 한 위치에서 다른 위치로 정규화된 방향을 작성합니다.
		/// </summary>
		public static Vector3 NormalizedDirectionByValue(ref Vector3d from, ref Vector3d to) {
			Vector3 result = Misc.vector3zero;
			double dx = to.x - from.x;
			double dy = to.y - from.y;
			double dz = to.z - from.z;
			double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
			result.x = (float) (dx / length);
			result.y = (float) (dy / length);
			result.z = (float) (dz / length);
			return result;
		}

		public static float SqrMinDistanceXZ (Vector3 v1, Vector3 v2)
		{
			float dx = v2.x - v1.x;
			dx *= dx;
			float dz = v2.z - v1.z;
			dz *= dz;
			return dx > dz ? dz : dx;
		}


		public static double SqrMinDistanceXZ (Vector3d v1, Vector3d v2) {
            double dx = v2.x - v1.x;
            dx *= dx;
			double dz = v2.z - v1.z;
            dz *= dz;
            return dx > dz ? dz : dx;
        }

		/// <summary>
		/// 한 위치에서 다른 위치까지의 제곱 거리를 반환합니다.
		/// </summary>
		public static float SqrDistance(ref Vector3 v1, ref Vector3 v2) {
			float dx = v2.x - v1.x;
			float dy = v2.y - v1.y;
			float dz = v2.z - v1.z;
			return dx * dx + dy * dy + dz * dz;
		}


		/// <summary>
		/// 한 위치에서 다른 위치까지의 제곱 거리를 반환합니다.
		/// </summary>
		public static double SqrDistance (ref Vector3d v1, ref Vector3d v2)
		{
			double dx = v2.x - v1.x;
			double dy = v2.y - v1.y;
			double dz = v2.z - v1.z;
			return dx * dx + dy * dy + dz * dz;
		}

		/// <summary>
		/// 한 위치에서 다른 위치까지의 제곱 거리를 반환합니다.
		/// </summary>
		public static double SqrMaxDistanceXorZ (ref Vector3d v1, ref Vector3d v2) {
            double dx = v2.x - v1.x;
            dx *= dx;
			double dz = v2.z - v1.z;
            dz *= dz;
            return dx > dz ? dx : dz;
        }


		/// <summary>
		/// 한 위치에서 다른 위치까지의 제곱 거리를 반환합니다.
		/// </summary>
		public static float SqrMaxDistanceXorZByValue (Vector3 v1, Vector3 v2)
		{
			float dx = v2.x - v1.x;
			dx *= dx;
			float dz = v2.z - v1.z;
			dz *= dz;
			return dx > dz ? dx : dz;
		}


		/// <summary>
		/// 한 위치에서 다른 위치까지의 제곱 거리를 반환합니다.벡터를 값으로 전달하는 대체 버전입니다.
		/// </summary>
		public static float SqrDistanceByValue (Vector3 v1, Vector3 v2)
		{
			float dx = v2.x - v1.x;
			float dy = v2.y - v1.y;
			float dz = v2.z - v1.z;
			return dx * dx + dy * dy + dz * dz;
		}

		/// <summary>
		/// 한 위치에서 다른 위치까지의 제곱 거리를 반환합니다.벡터를 값으로 전달하는 대체 버전입니다.
		/// </summary>
		public static double SqrDistanceByValue(Vector3d v1, Vector3d v2) {
			double dx = v2.x - v1.x;
			double dy = v2.y - v1.y;
			double dz = v2.z - v1.z;
			return dx * dx + dy * dy + dz * dz;
		}

		/// <summary>
		/// 한 위치에서 다른 위치까지의 제곱 거리를 반환합니다.
		/// </summary>
		public static float SqrDistance(ref Vector2 v1, ref Vector2 v2) {
			float dx = v2.x - v1.x;
			float dy = v2.y - v1.y;
			return dx * dx + dy * dy;
		}

        /// <summary>
        /// XZ 평면의 한 위치에서 다른 위치까지의 sqr 거리를 반환합니다.
        /// </summary>
        public static double SqrDistanceXZ (ref Vector3d v1, ref Vector3d v2) {
            double dx = v2.x - v1.x + 1;
			double dz = v2.z - v1.z + 1;
            return dx * dx + dz * dz;
        }


		/// <summary>
		/// XZ 평면의 한 위치에서 다른 위치까지의 sqr 거리를 반환합니다.
		/// </summary>
		public static float SqrDistanceXZByValue(Vector3 v1, Vector3 v2)
		{
			float dx = v2.x - v1.x + 1;
			float dz = v2.z - v1.z + 1;
			return dx * dx + dz * dz;
		}

		/// <summary>
		/// XZ 평면의 한 위치에서 다른 위치까지의 sqr 거리를 반환합니다.
		/// </summary>
        public static int SqrDistanceXZ(ref Vector3Int v1, ref Vector3Int v2) {
            int dx = v2.x - v1.x + 1;
            int dz = v2.z - v1.z + 1;
            return dx * dx + dz * dz;
        }

        /// <summary>
        /// 한 위치에서 다른 위치까지의 제곱 거리를 반환합니다.
        /// </summary>
        public static int SqrMaxDistanceXorZ(ref Vector3Int v1, ref Vector3Int v2) {
            int dx = v2.x - v1.x;
            dx = dx < 0 ? -dx : dx;
            int dz = v2.z - v1.z;
            dz = dz < 0 ? -dz : dz;
            return dx > dz ? dx : dz;
        }
		/// "끝" 위치가 "시작" 위치까지 지정된 범위 내에 있는지 확인합니다.값이 변경된 경우 true를 반환합니다.
		/// </summary>
		/// <param name="from">에서.</param>
		/// <param name="to">에게.</param>
		/// <param name="minDistance">최소 거리.</param>
		/// <param name="maxDistance">최대 거리.</param>
		public static bool ClampDistance(ref Vector3 from, ref Vector3 to, float minDistance, float maxDistance) {
			float dx = to.x - from.x;
			float dy = to.y - from.y;
			float dz = to.z - from.z;
			float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);									
			if (dist < 0.0001f)
				return false;
			if (dist < minDistance) {
				float m = minDistance / dist;
				to.x = from.x + (to.x - from.x) * m;
				to.y = from.y + (to.y - from.y) * m;
				to.z = from.z + (to.z - from.z) * m;
				return true;
            }
            if (dist > maxDistance) {
				float m = maxDistance / dist;
				to.x = from.x + (to.x - from.x) * m;
				to.y = from.y + (to.y - from.y) * m;
				to.z = from.z + (to.z - from.z) * m;
				return true;
			}
			return false;
		}


		/// <summary>
		/// 가치에 척도를 곱하고 그 결과를 다시 가치로 전환합니다.
		/// </summary>
		/// <param name="value">값.</param>
		/// <param name="scale">규모.</param>
		public static void Multiply(ref Vector3 value, ref Vector3 scale, float additionalScale = 1f) {
			value.x *= scale.x * additionalScale;
			value.y *= scale.y * additionalScale;
			value.z *= scale.z * additionalScale;
		}

		/// <summary>
		/// 벡터 값을 정수 반내림으로 변환합니다.
		/// </summary>
		public static void Floor(ref Vector3 v) {
			v.x = (float)Math.Floor (v.x);
			v.y = (float)Math.Floor (v.y);
			v.z = (float)Math.Floor (v.z);
		}

		/// <summary>
		/// 벡터 값을 정수 반올림으로 변환합니다.
		/// </summary>
		public static void Ceiling(ref Vector3 v) {
			v.x = (float)Math.Ceiling (v.x);
			v.y = (float)Math.Ceiling (v.y);
			v.z = (float)Math.Ceiling (v.z);
		}

        /// <summary>
        /// 벡터 값을 복셀 중심으로 변환합니다.
        /// </summary>
        public static void Middling(ref Vector3 v) {
            v.x = (float)Math.Floor(v.x) + 0.5f;
            v.y = (float)Math.Floor(v.y) + 0.5f;
            v.z = (float)Math.Floor(v.z) + 0.5f;
        }

		/// <summary>
		/// 벡터 값을 복셀 중심으로 변환합니다.
		/// </summary>
		public static void Middling(ref Vector3d v) {
			v.x = Math.Floor(v.x) + 0.5;
			v.y = Math.Floor(v.y) + 0.5;
			v.z = Math.Floor(v.z) + 0.5;
		}

		/// 다른 벡터의 각 구성 요소에 대한 부호가 있는 벡터를 반환합니다.
		/// </summary>
		/// <param name="v">다섯.</param>
		public static Vector3 Sign(ref Vector3 v) {
			Vector3 r = Misc.vector3zero;
			r.x = v.x >= 0 ? 1 : -1;
			r.y = v.y >= 0 ? 1 : -1;
			r.z = v.z >= 0 ? 1 : -1;
			return r;
		}


		/// <summary>
		/// 벡터 값을 정수 반내림으로 변환합니다.
		/// </summary>
		public static void Floor (ref Vector3d v)
		{
			v.x = Math.Floor (v.x);
			v.y = Math.Floor (v.y);
			v.z = Math.Floor (v.z);
		}


		/// <summary>
		/// 벡터 값을 정수 반올림으로 변환합니다.
		/// </summary>
		public static void Ceiling (ref Vector3d v)
		{
			v.x = Math.Ceiling (v.x);
			v.y = Math.Ceiling (v.y);
			v.z = Math.Ceiling (v.z);
		}

	}
}