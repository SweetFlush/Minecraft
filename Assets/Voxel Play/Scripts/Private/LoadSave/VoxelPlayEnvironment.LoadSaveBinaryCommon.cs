using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Data.Common;

namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        List<VoxelDefinition> saveVoxelDefinitionsList;
        Dictionary<VoxelDefinition, int> saveVoxelDefinitionsDict;
        List<string> saveItemDefinitionsList;
        Dictionary<ItemDefinition, int> saveItemDefinitionsDict;
        static byte[] zeroInt = { 0, 0, 0, 0 };

        void InitSaveGameStructs () {
            if (saveVoxelDefinitionsList == null) {
                saveVoxelDefinitionsList = new List<VoxelDefinition>(100);
            } else {
                saveVoxelDefinitionsList.Clear();
            }
            if (saveVoxelDefinitionsDict == null) {
                saveVoxelDefinitionsDict = new Dictionary<VoxelDefinition, int>(100);
            } else {
                saveVoxelDefinitionsDict.Clear();
            }
            if (saveItemDefinitionsList == null) {
                saveItemDefinitionsList = new List<string>(100);
            } else {
                saveItemDefinitionsList.Clear();
            }
            if (saveItemDefinitionsDict == null) {
                saveItemDefinitionsDict = new Dictionary<ItemDefinition, int>(100);
            } else {
                saveItemDefinitionsDict.Clear();
            }
        }


        Vector3 DecodeVector3Binary (BinaryReader br) {
            Vector3 v = new Vector3();
            v.x = br.ReadSingle();
            v.y = br.ReadSingle();
            v.z = br.ReadSingle();
            return v;
        }

        void EncodeVector3Binary (BinaryWriter bw, Vector3 v) {
            bw.Write(v.x);
            bw.Write(v.y);
            bw.Write(v.z);
        }


        /// <summary>
        /// 청크의 내용을 담기에 충분한 용량을 가진 새 바이트 배열을 반환합니다.GetChunkRawData()를 호출하기 전에 이를 사용하십시오.
        /// </summary>
        /// <returns></returns>
        public byte[] GetChunkRawBuffer () {
            int bufferSize = GetChunkBufferMinimumSize();
            return new byte[bufferSize];
        }

        void WriteByte (byte source, byte[] destination, ref int baseIndex) {
            destination[baseIndex] = source;
            baseIndex++;
        }

        void WriteBytes (byte[] source, byte[] destination, ref int baseIndex) {
            source.CopyTo(destination, baseIndex);
            baseIndex += source.Length;
        }

        void ReadByte (byte[] source, ref int baseIndex, out int value) {
            value = source[baseIndex];
            baseIndex++;
        }


        void ReadInt16 (byte[] source, ref int baseIndex, out int value) {
            value = BitConverter.ToInt16(source, baseIndex);
            baseIndex += 2;
        }

        void ReadInt32 (byte[] source, ref int baseIndex, out int value) {
            value = BitConverter.ToInt32(source, baseIndex);
            baseIndex += 4;
        }

        void ReadFloat (byte[] source, ref int baseIndex, out float value) {
            value = BitConverter.ToSingle(source, baseIndex);
            baseIndex += 4;
        }

        void ReadUlong (byte[] source, ref int baseIndex, out ulong value) {
            value = BitConverter.ToUInt64(source, baseIndex);
            baseIndex += 8;
        }

        void ReadString (byte[] source, ref int baseIndex, out string value) {
            int length = BitConverter.ToInt32(source, baseIndex);
            baseIndex += 4;
            if (length > 0) {
                value = Encoding.ASCII.GetString(source, baseIndex, length);
                baseIndex += length;
            } else {
                value = "";
            }
        }


        int GetChunkRawProperties (VoxelChunk chunk, byte[] contents, int baseIndex) {

            List<KeyValuePair<int, FastHashSet<VoxelProperty>>> voxelsProperties = BufferPool<KeyValuePair<int, FastHashSet<VoxelProperty>>>.Get();
            List<KeyValuePair<int, VoxelProperty>> voxelProperties = BufferPool<KeyValuePair<int, VoxelProperty>>.Get();
            chunk.voxelsProperties.GetValues(voxelsProperties);
            int voxelsPropertiesCount = chunk.voxelsProperties.Count;
            WriteBytes(BitConverter.GetBytes((Int16)voxelsPropertiesCount), contents, ref baseIndex);
            for (int j = 0; j < voxelsPropertiesCount; j++) {
                KeyValuePair<int, FastHashSet<VoxelProperty>> kvp = voxelsProperties[j];
                WriteBytes(BitConverter.GetBytes((Int16)kvp.Key), contents, ref baseIndex); // voxel index

                kvp.Value.GetValues(voxelProperties);
                int voxelPropertiesCount = voxelProperties.Count;

                WriteBytes(BitConverter.GetBytes((Int16)voxelPropertiesCount), contents, ref baseIndex); // properties count for this voxel
                for (int i = 0; i < voxelPropertiesCount; i++) {
                    KeyValuePair<int, VoxelProperty> prop = voxelProperties[i];
                    WriteBytes(BitConverter.GetBytes(prop.Key), contents, ref baseIndex); // property id
                    WriteBytes(BitConverter.GetBytes(prop.Value.floatValue), contents, ref baseIndex); // float value
                    if (!string.IsNullOrEmpty(prop.Value.stringValue)) {
                        WriteBytes(BitConverter.GetBytes(prop.Value.stringValue.Length), contents, ref baseIndex); // string length
                        WriteBytes(Encoding.ASCII.GetBytes(prop.Value.stringValue), contents, ref baseIndex); // string value
                    } else {
                        WriteBytes(zeroInt, contents, ref baseIndex); // 0-length string
                    }
                }
            }
            BufferPool<KeyValuePair<int, VoxelProperty>>.Release(voxelProperties);
            BufferPool<KeyValuePair<int, FastHashSet<VoxelProperty>>>.Release(voxelsProperties);

            return baseIndex;
        }

        void SetChunkRawProperties (VoxelChunk chunk, byte[] contents, int baseIndex) {
            if (chunk.voxelsProperties == null) {
                chunk.voxelsProperties = new FastHashSet<FastHashSet<VoxelProperty>>();
            } else {
                chunk.voxelsProperties.Clear();
            }
            ReadInt16(contents, ref baseIndex, out int voxelsPropertiesCount);
            for (int k = 0; k < voxelsPropertiesCount; k++) {
                ReadInt16(contents, ref baseIndex, out int voxelIndex);
                ReadInt16(contents, ref baseIndex, out int voxelPropertiesCount);
                if (!chunk.voxelsProperties.TryGetValue(voxelIndex, out FastHashSet<VoxelProperty> voxelProperties)) {
                    voxelProperties = new FastHashSet<VoxelProperty>();
                    chunk.voxelsProperties[voxelIndex] = voxelProperties;
                }
                for (int i = 0; i < voxelPropertiesCount; i++) {
                    VoxelProperty prop;
                    ReadInt32(contents, ref baseIndex, out int propId);
                    ReadFloat(contents, ref baseIndex, out float floatValue);
                    prop.floatValue = floatValue;
                    ReadInt32(contents, ref baseIndex, out int stringLength);
                    if (stringLength > 0) {
                        ReadString(contents, ref baseIndex, out string stringValue);
                        prop.stringValue = stringValue;
                    } else {
                        prop.stringValue = "";
                    }
                    voxelProperties[propId] = prop;
                }
            }
        }

        int GetChunkRawMicroVoxels (VoxelChunk chunk, byte[] contents, int baseIndex) {
            int microVoxelsCount = chunk.usesMicroVoxels ? chunk.microVoxels.Count : 0;
            WriteBytes(BitConverter.GetBytes((Int16)microVoxelsCount), contents, ref baseIndex); // number of microvoxels
            foreach (var kvp in chunk.microVoxels) {
                WriteBytes(BitConverter.GetBytes((Int16)kvp.Key), contents, ref baseIndex); // voxel index
                ulong[] data = kvp.Value.gridData;
                int dataCount = data.Length;
                WriteByte((byte)dataCount, contents, ref baseIndex); // microvoxels data count
                for (int i = 0; i < dataCount; i++) {
                    WriteBytes(BitConverter.GetBytes(data[i]), contents, ref baseIndex); // microvoxels data
                }
            }
            return baseIndex;
        }

        void SetChunkRawMicroVoxels (VoxelChunk chunk, byte[] contents, int baseIndex) {
            ReadInt16(contents, ref baseIndex, out int microVoxelsCount);  // number of microvoxels
            if (microVoxelsCount == 0) return;
            if (chunk.microVoxels == null) {
                chunk.microVoxels = new Dictionary<int, MicroVoxels>();
            } else {
                chunk.microVoxels.Clear();
            }
            for (int k = 0; k < microVoxelsCount; k++) {
                ReadInt16(contents, ref baseIndex, out int voxelIndex); // voxel index
                ReadByte(contents, ref baseIndex, out int dataCount); // microvoxels data count
                MicroVoxels mv = new MicroVoxels();
                for (int i = 0; i < dataCount; i++) {
                    ReadUlong(contents, ref baseIndex, out mv.gridData[i]); // microvoxels data
                }
                chunk.microVoxels[voxelIndex] = mv;
            }
        }


        int GetChunkBufferMinimumSize () {
            return CHUNK_VOXEL_COUNT * (Voxel.memorySize + 2);
        }

        /// <summary>
        /// 압축된 이진 형식(RLE)으로 지정된 청크의 복셀을 반환합니다.
        /// </summary>
        /// <param name="contents">데이터를 쓸 바이트 배열입니다.새로운 바이트 배열을 얻으려면 GetChunkRawBuffer()를 사용하십시오.</param>
        /// <param name="includeVoxelProperties">true인 경우 사용자 지정 복셀 속성도 포함됩니다.</param>
        /// <param name="includeVoxelProperties">true인 경우 사용자 지정 복셀 속성도 포함됩니다.</param>
        /// <param name="includeMicroVoxels">true인 경우 microVoxels 데이터도 포함됩니다.</param>
        /// <returns>컨텐츠 배열 버퍼 내부 데이터의 실제 길이</returns>
        public int GetChunkRawData (VoxelChunk chunk, byte[] contents, bool includeVoxelProperties = false, bool includeMicroVoxels = false) {
            if ((object)chunk == null || contents == null) return 0;

            int baseIndex = 0;
            int minimumLength = GetChunkBufferMinimumSize();
            if (contents.Length < minimumLength) {
                Debug.Log("Contents length must be at least of " + minimumLength);
            }
            int i, k = 0, count;
            Voxel voxel = Voxel.Empty;

            for (i = 0; i < CHUNK_VOXEL_COUNT; i++) {
                if (chunk.voxels[i] == voxel) continue;
                count = i - k;
                if (count > 0) {
                    if (count >= 255) {
                        contents[baseIndex++] = 255;
                        contents[baseIndex++] = (byte)(count >> 8);
                    }
                    contents[baseIndex++] = (byte)(count & 0xFF);
                    baseIndex = chunk.voxels[k].WriteRawData(contents, baseIndex);
                }
                k = i;
                voxel = chunk.voxels[i];
            }
            count = i - k;
            if (count > 0) {
                if (count >= 255) {
                    contents[baseIndex++] = 255;
                    contents[baseIndex++] = (byte)(count >> 8);
                }
                contents[baseIndex++] = (byte)(count & 0xFF);
                baseIndex = chunk.voxels[k].WriteRawData(contents, baseIndex);
            }

            if (includeVoxelProperties) {
                baseIndex = GetChunkRawProperties(chunk, contents, baseIndex);
            }
            if (includeMicroVoxels) {
                baseIndex = GetChunkRawMicroVoxels(chunk, contents, baseIndex);
            }

            return baseIndex;
        }


        /// <summary>
        /// 청크의 내용을 내용 배열에서 제공하는 새로운 복셀 내용으로 바꿉니다.GetChunkRawData()를 사용하여 청크에서 광선 바이너리 데이터를 가져옵니다.
        /// </summary>
        /// <param name="contents">원시 바이트 형식의 청크 내용</param>
        /// <param name="length">콘텐츠 버퍼의 데이터 길이</param>
        /// <param name="validate">복셀 유형이 모든 복셀 정의와 일치하는지 확인합니다.</param>
        /// <param name="includeVoxelProperties">true인 경우 사용자 지정 복셀 속성도 포함됩니다.</param>
        /// <param name="includeMicroVoxels">true인 경우 microVoxels 데이터도 포함됩니다.</param>
        public void SetChunkRawData (VoxelChunk chunk, byte[] contents, int length, bool validate = true, bool includeVoxelProperties = false, bool includeMicroVoxels = false) {
            if ((object)chunk == null || contents == null) return;
            int baseIndex = 0;
            Voxel voxel = new Voxel();
            for (int i = 0; i < length;) {
                int count = contents[i++];
                if (count == 255) {
                    count = (contents[i] << 8) + contents[i + 1];
                    i += 2;
                }
                i = voxel.ReadRawData(contents, i);
                for (int k = 0; k < count; k++) {
                    chunk.voxels[baseIndex++] = voxel;
                }
            }
            if (validate) {
                for (int k = 0; k < CHUNK_VOXEL_COUNT; k++) {
                    if (chunk.voxels[k].typeIndex < 0 || chunk.voxels[k].typeIndex >= voxelDefinitionsCount) {
                        chunk.voxels[k].typeIndex = 0;
                        ShowError("SetChunkRawData: unknown voxel definition at chunk pos=" + chunk.position + " index=" + k + ". Make sure all voxel definitions are loaded/added to Voxel Play during start up in order to ensure proper synchronization.");
                    }
                }
            }

            if (includeVoxelProperties) {
                SetChunkRawProperties(chunk, contents, baseIndex);
            }
            if (includeMicroVoxels) {
                SetChunkRawMicroVoxels(chunk, contents, baseIndex);
            }
        }
    }



}
