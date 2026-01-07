using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoxelPlay {

    [Serializable]
    public partial class MicroVoxels {

public const int COUNT_PER_AXIS = 4;
        public const int COUNT_PER_FACE = COUNT_PER_AXIS * COUNT_PER_AXIS;
        public const int COUNT_PER_VOXEL = COUNT_PER_AXIS * COUNT_PER_AXIS * COUNT_PER_AXIS;
        public const int COUNT_PER_AXIS_MINUS_ONE = COUNT_PER_AXIS - 1;
        public const int COUNT_PER_AXIS_HALF = COUNT_PER_AXIS / 2;

        public const float SIZE = 1f / COUNT_PER_AXIS;

        public static Vector3d SIZE_3D = new Vector3d(SIZE, SIZE, SIZE);
        public static Vector3d HALF_SIZE_3D = SIZE_3D / 2f;

        const int BITS_PER_LONG = 64; // Número de bits en un ulong

        [NonSerialized]
        public MicroVoxelsPrototype prototype;

        [NonSerialized]
        public bool needsMeshDataUpdate;

        public ulong[] gridData;
        public int count;

        public MicroVoxels () {
            int ulongCount = (COUNT_PER_VOXEL + BITS_PER_LONG - 1) / BITS_PER_LONG;
            gridData = new ulong[ulongCount];
            needsMeshDataUpdate = true;
        }

        public MicroVoxels Clone () {
            MicroVoxels clone = new MicroVoxels();
            clone.gridData = (ulong[])gridData.Clone();
            clone.count = count;
            clone.needsMeshDataUpdate = needsMeshDataUpdate;
            clone.prototype = prototype;
            return clone;
        }

        public ulong GetGridHashCode () {
            unchecked {
                ulong hash = 17; // Iniciar con un valor base en ulong
                int count = gridData.Length;
                for (int i = 0; i < count; i++) {
                    // Multiplica y suma usando ulong para evitar overflow negativo
                    hash = hash * 31 + gridData[i];

                    // Mezcla adicional de bits para mejorar la distribución
                    hash ^= hash >> 32; // XOR con los bits superiores para mezclar
                }
                return hash;
            }
        }

        private int GetIndex (int x, int y, int z) {
            return x + (z * COUNT_PER_AXIS) + (y * COUNT_PER_AXIS * COUNT_PER_AXIS);
        }

        public void Fill () {
            int ulongCount = gridData.Length;
            for (int k = 0; k < ulongCount; k++) {
                gridData[k] = ulong.MaxValue;
            }
            count = COUNT_PER_VOXEL;
        }

        public bool isEmpty => count == 0;

        public bool isFull => count == COUNT_PER_VOXEL;

        public bool SetOccupied (int x, int y, int z) {
            int index = GetIndex(x, y, z);
            return SetOccupied(index);
        }

        public bool SetOccupied (int microVoxelIndex) {
            int ulongIndex = microVoxelIndex / BITS_PER_LONG;
            int bitPosition = microVoxelIndex % BITS_PER_LONG;
            if ((gridData[ulongIndex] & (1UL << bitPosition)) == 0) {
                count++;
                gridData[ulongIndex] |= 1UL << bitPosition;
                needsMeshDataUpdate = true;
                return true;
            }
            return false;
        }

        public bool SetUnoccupied (int x, int y, int z) {
            int index = GetIndex(x, y, z);
            return SetUnoccupied(index);
        }

        public bool SetUnoccupied (int microVoxelIndex) {
            int ulongIndex = microVoxelIndex / BITS_PER_LONG;
            int bitPosition = microVoxelIndex % BITS_PER_LONG;
            if ((gridData[ulongIndex] & (1UL << bitPosition)) != 0) {
                count--;
                gridData[ulongIndex] &= ~(1UL << bitPosition);
                needsMeshDataUpdate = true;
                return true;
            }
            return false;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public bool IsOccupied (int x, int y, int z) {
            int index = GetIndex(x, y, z);
            return IsOccupied(index);
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public bool IsOccupied (int microVoxelIndex) {
            int ulongIndex = microVoxelIndex / BITS_PER_LONG;
            int bitPosition = microVoxelIndex % BITS_PER_LONG;
            return (gridData[ulongIndex] & (1UL << bitPosition)) != 0;
        }

        public byte GetOpaqueProportional () {
            return (byte)Mathf.Max(1, 15 * count / COUNT_PER_VOXEL);
        }

        public int CalculateOccupiedCount () {
            int total = 0;
            foreach (ulong val in gridData) {
                ulong value = val;
                while (value != 0) {
                    total++;
                    value &= value - 1; // Clears the least significant bit set
                }
            }
            return total;
        }

        public void WriteToBinaryWriter (BinaryWriter bw) {
            int gridDataLength = gridData.Length;
            for (int k = 0; k < gridDataLength; k++) {
                bw.Write((UInt64)gridData[k]);
            }
        }

        public void ReadFromBinaryReader (BinaryReader br) {
            int gridDataLength = gridData.Length;
            for (int k = 0; k < gridDataLength; k++) {
                gridData[k] = br.ReadUInt64();
            }
            count = CalculateOccupiedCount();
        }

    }
}

