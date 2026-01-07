using UnityEngine;

namespace VoxelPlay {

    public static class WorldRand {

        const int RANDOM_TABLE_SIZE = 8192; // 2^13
        const int RANDOM_TABLE_SIZE_MINUS_ONE = RANDOM_TABLE_SIZE - 1;
        const long MAGIC1 = 2166136261; // 17
        const long MAGIC2 = 16777619;   // 23

        static float[] rnd;
        static uint rndIndex = 0;

        static WorldRand() {
            Randomize(0);
        }

        /// <summary>
        /// 시드로 무작위 테이블을 초기화합니다.
        /// </summary>
        public static void Randomize(int seed) {
            Random.State state = Random.state;
            Random.InitState(seed);
            if (rnd == null || rnd.Length == 0) {
                rnd = new float[RANDOM_TABLE_SIZE];
            }
            int rndLength = rnd.Length;
            for (int k = 0; k < rndLength; k++) {
                do {
                    rnd[k] = Random.value;
                } while (rnd[k] >= 1f);
            }
            Random.state = state;
        }


        /// <summary>
        /// 주어진 위치에 "연결된" 임의의 값 중 하나를 가져옵니다.
        /// </summary>
        public static float GetValue(Vector3 position) {
            long hash = MAGIC1;
            hash = hash * MAGIC2 ^ (long)position.x;
            hash = hash * MAGIC2 ^ (long)position.y;
            hash = hash * MAGIC2 ^ (long)position.z;
            rndIndex = (uint)(hash & RANDOM_TABLE_SIZE_MINUS_ONE);
            return rnd[rndIndex];
        }

        /// <summary>
        /// 주어진 위치에 "연결된" 임의의 값 중 하나를 가져옵니다.
        /// </summary>
        public static float GetValue(Vector3 position, int shift) {
            long hash = MAGIC1;
            hash = hash * MAGIC2 ^ (long)position.x;
            hash = hash * MAGIC2 ^ (long)position.y;
            hash = hash * MAGIC2 ^ (long)position.z;
            hash += shift;
            rndIndex = (uint)(hash & RANDOM_TABLE_SIZE_MINUS_ONE);
            return rnd[rndIndex];
        }        


        /// <summary>
        /// 주어진 위치에 "연결된" 임의의 값 중 하나를 가져옵니다.
        /// </summary>
        public static float GetValue(Vector3d position) {
            long hash = MAGIC1;
            hash = hash * MAGIC2 ^ (long)position.x;
            hash = hash * MAGIC2 ^ (long)position.y;
            hash = hash * MAGIC2 ^ (long)position.z;
            rndIndex = (uint)(hash & RANDOM_TABLE_SIZE_MINUS_ONE);
            return rnd[rndIndex];
        }


        /// <summary>
        /// 주어진 위치에 "연결된" 임의의 값 중 하나를 가져옵니다.
        /// </summary>
        public static float GetValue(double x, double z) {
            long hash = MAGIC1;
            hash = hash * MAGIC2 ^ (long)x;
            hash = hash * MAGIC2 ^ (long)z;
            rndIndex = (uint)(hash & RANDOM_TABLE_SIZE_MINUS_ONE);
            return rnd[rndIndex];
        }


        /// <summary>
        /// 주어진 값에 "연결된" 임의의 값을 가져옵니다.
        /// </summary>
        public static float GetValue(int someValue) {
            rndIndex = (uint)someValue & RANDOM_TABLE_SIZE_MINUS_ONE;
            return rnd[rndIndex];
        }

        /// <summary>
        /// 주어진 위치에 "연결된" min(포함)과 max(제외) 사이의 임의 값을 반환합니다.
        /// </summary>
        public static int Range(int min, int max, Vector3d position) {
            float v = GetValue(position);
            return (int)(min + (max - min) * 0.99999f * v);
        }

        /// <summary>
        /// 주어진 위치에 "연결된" min(포함)과 max(제외) 사이의 임의 값을 반환합니다.
        /// </summary>
        public static int Range(int min, int max, Vector3d position, int randomShift) {
            float v = GetValue(position, randomShift);
            return (int)(min + (max - min) * 0.99999f * v);
        }        

        /// <summary>
        /// min(포함)과 max(제외) 사이의 임의 값을 반환합니다.
        /// </summary>
        public static int Range(int min, int max) {
            float v = GetValue();
            return (int)(min + (max - min) * 0.99999f * v);
        }


        /// <summary>
        /// min(포함)과 max(포함) 사이의 임의 값을 반환합니다.
        /// </summary>
        public static float Range(float min, float max) {
            float v = GetValue();
            return min + (max - min) * v;
        }

        /// 주어진 시드에 "연결된" 최소값(포함)과 최대값(포함) 사이의 임의 값을 반환합니다.
        /// </summary>
        public static float Range(float min, float max, int seed) {
            float v = GetValue(seed);
            return min + (max - min) * v;
        }

        /// <summary>
        /// 0~1 범위의 임의의 값을 반환합니다.
        /// </summary>
		public static float GetValue() {
            rndIndex++;
            rndIndex &= RANDOM_TABLE_SIZE_MINUS_ONE;
            return rnd[rndIndex];
        }

        /// <summary>
        /// 주어진 위치에 "연결된" 임의의 Vector3 값을 반환합니다.
        /// </summary>
        /// <returns>벡터3.</returns>
        /// <param name="position">위치.</param>
        /// <param name="scale">규모.</param>
        /// <param name="shift">임의의 값은 0..1 범위에 있습니다.배율을 곱하기 전에 임의의 값에 Shift가 추가됩니다.</param>
        public static Vector3 GetVector3(Vector3d position, float scale, float shift = 0) {
            float x = (GetValue(position) + shift) * scale;
            float y = (GetValue() + shift) * scale;
            float z = (GetValue() + shift) * scale;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// 주어진 위치에 연결된 스케일 범위에서 임의의 Vector3 값을 반환합니다.
        /// </summary>
        /// <returns>벡터3.</returns>
        /// <param name="position">위치.</param>
        /// <param name="scale">규모.</param>
        /// <param name="shift">임의의 값은 0..1 범위에 있습니다.배율을 곱하기 전에 임의의 값에 Shift가 추가됩니다.</param>
        public static Vector3 GetVector3(Vector3d position, Vector3 scale, float shift = 0) {
            float x = (GetValue(position) + shift) * scale.x;
            float y = (GetValue() + shift) * scale.y;
            float z = (GetValue() + shift) * scale.z;
            return new Vector3(x, y, z);
        }


    }

}