using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxelPlay {

    [Serializable]
    public struct ModelBit {
        public int voxelIndex;
        public VoxelDefinition voxelDefinition;
        [Tooltip("빈 위치를 명시적으로 선언합니다.이 모델 정의가 배치되면 빈 위치는 해당 위치에 있는 세계의 기존 복셀을 모두 지웁니다.")]
        public bool isEmpty;
        public Color32 color;
        [Tooltip("이 복셀의 회전입니다.허용되는 회전은 0, 90, 180 또는 270도입니다.")]
        public float rotation;
        [HideInInspector] public MicroVoxels microVoxels;
        
        /// <summary>
        /// 비트 틴트 컬러와 복셀 정의 틴트 컬러를 결합한 최종 컬러
        /// </summary>
        [NonSerialized]
        public Color32 finalColor;
    }


    [Serializable]
    public struct TorchBit {
        public int voxelIndex;
        public ItemDefinition itemDefinition;
        public Vector3 normal;
    }

    [CreateAssetMenu(menuName = "Voxel Play/Model Definition", fileName = "ModelDefinition", order = 102)]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000033382-model-definitions")]
    public partial class ModelDefinition : ScriptableObject {

        [Tooltip("모델의 크기(X축)")]
        public int sizeX = VoxelPlayEnvironment.CHUNK_SIZE;

        [Tooltip("모델의 크기(Y축)")]
        public int sizeY = VoxelPlayEnvironment.CHUNK_SIZE;

        [Tooltip("모델 크기(Z축)")]
        public int sizeZ = VoxelPlayEnvironment.CHUNK_SIZE;

        [Tooltip("배치 위치(X축)를 기준으로 모델의 오프셋")]
        public int offsetX;

        [Tooltip("배치 위치(Y축)를 기준으로 모델의 오프셋")]
        public int offsetY;

        [Tooltip("배치 위치(Z축)를 기준으로 한 모델의 오프셋")]
        public int offsetZ;

        [Tooltip("빌드 기간(초)")]
        public float buildDuration = 3f;

        [Tooltip("이 모델이 트리인 경우 동일한 청크에 더 이상 트리가 허용되지 않습니다.")]
        public bool exclusiveTree;

        [Tooltip("필요한 경우 모델 아래 빈 공간을 채우고 지형 표면까지 바닥 복셀을 확장합니다.")]
        public bool fitToTerrain;

        /// <summary>
        /// 모델 비트 배열.
        /// </summary>
        public ModelBit[] bits;

        /// <summary>
        /// 토치 데이터 배열
        /// </summary>
        public TorchBit[] torches;

        /// <summary>
        /// 모델 정의에서 생성된 게임 객체를 캐시하는 데 임시로 사용됩니다.
        /// </summary>
        [NonSerialized, HideInInspector]
        public GameObject modelGameObject;

        /// <summary>
        /// 새로운 모델 정의를 반환합니다.
        /// </summary>
        public static ModelDefinition Create(int sizeX, int sizeY, int sizeZ) {
            ModelDefinition md = CreateInstance<ModelDefinition>();
            md.sizeX = sizeX;
            md.sizeY = sizeY;
            md.sizeZ = sizeZ;
            return md;
        }

        /// <summary>
        /// 복셀 정의 목록에서 새로운 모델 정의를 생성하는 유틸리티 방법
        /// </summary>
        public static ModelDefinition Create(int sizeX, int sizeY, int sizeZ, List<VoxelDefinition> voxelDefinitions) {
            if (voxelDefinitions == null) {
                Debug.LogError("ModelDefinition.Create: voxelDefinitions is null");
                return null;
            }
            int totalLength = sizeX * sizeY * sizeZ;
            if (voxelDefinitions.Count < totalLength) {
                Debug.LogError("ModelDefinition.Create: voxelDefinitions does not have enough entries");
                return null;
            }
            ModelDefinition md = Create(sizeX, sizeY, sizeZ);
            md.bits = new ModelBit[totalLength];
            ModelBit bit = new ModelBit();
            int c = 0;
            for (int k = 0; k < totalLength; k++) {
                VoxelDefinition vd = voxelDefinitions[k];
                if (vd == null) continue;
                bit.color = vd.tintColor;
                bit.voxelDefinition = vd;
                bit.voxelIndex = k;
                md.bits[c++] = bit;
            }
            md.ComputeBounds();
            md.ComputeFinalColors();
            return md;
        }

        /// <summary>
        /// 복셀 목록에서 새로운 모델 정의를 생성하는 유틸리티 방법
        /// </summary>
        public static ModelDefinition Create(int sizeX, int sizeY, int sizeZ, List<Voxel> voxels) {
            if (voxels == null) {
                Debug.LogError("ModelDefinition.Create: voxels is null");
                return null;
            }
            int totalLength = sizeX * sizeY * sizeZ;
            if (voxels.Count < totalLength) {
                Debug.LogError("ModelDefinition.Create: voxels does not have enough entries");
                return null;
            }
            ModelDefinition md = Create(sizeX, sizeY, sizeZ);
            md.bits = new ModelBit[totalLength];
            ModelBit bit = new ModelBit();
            int c = 0;
            for (int k = 0; k < totalLength; k++) {
                bit.color = voxels[k].color;
                bit.voxelDefinition = voxels[k].type;
                bit.voxelIndex = k;
                md.bits[c++] = bit;
            }
            md.ComputeBounds();
            md.ComputeFinalColors();
            return md;
        }

        /// <summary>
        /// 완전히 새로운 비트를 할당할 때 사용하는 것이 가장 좋은 방법입니다.이 메서드는 ComputeBounds() 및 ComputeFinalColors()도 호출합니다.
        /// </summary>
        public void SetBits(ModelBit[] bits) {
            this.bits = bits;
            ComputeBounds();
            ComputeFinalColors();
        }

        /// <summary>
        /// 동일한 복셀 정의를 사용하여 색상 목록에서 새 모델 정의를 생성하는 유틸리티 방법입니다.복셀 정의가 제공되지 않으면 기본 복셀 정의가 사용됩니다.
        /// </summary>
        public static ModelDefinition Create(int sizeX, int sizeY, int sizeZ, VoxelDefinition voxelDefinition, List<Color> colors) {
            if (colors == null) {
                Debug.LogError("ModelDefinition.Create: colors is null");
                return null;
            }
            int totalLength = sizeX * sizeY * sizeZ;
            if (colors.Count < totalLength) {
                Debug.LogError("ModelDefinition.Create: colors does not have enough entries");
                return null;
            }
            if (voxelDefinition == null) {
                voxelDefinition = VoxelPlayEnvironment.instance.defaultVoxel;
            }
            ModelDefinition md = Create(sizeX, sizeY, sizeZ);
            md.bits = new ModelBit[totalLength];
            ModelBit bit = new ModelBit();
            int c = 0;
            for (int k = 0; k < totalLength; k++) {
                bit.color = colors[k];
                bit.voxelDefinition = voxelDefinition;
                bit.voxelIndex = k;
                md.bits[c++] = bit;
            }
            md.ComputeBounds();
            md.ComputeFinalColors();
            return md;
        }

        /// <summary>
        /// 모델 크기에 따라 이 모델 정의 내부의 복셀 인덱스를 반환합니다.
        /// </summary>
        public int GetVoxelIndex(int x, int y, int z) {
            return y * (sizeZ * sizeX) + z * sizeX + x;
        }

        public Vector3 size {
            get {
                return new Vector3(sizeX, sizeY, sizeZ);
            }
        }

        public Vector3 offset {
            get {
                return new Vector3(offsetX, offsetY, offsetZ);
            }
            set {
                offsetX = (int)value.x;
                offsetY = (int)value.y;
                offsetZ = (int)value.z;
            }
        }

        Bounds _bounds;

        /// <summary>
        /// 모델 정의 내에서 보이는 복셀의 실제 경계
        /// </summary>
        public Bounds bounds {
            get {
                return _bounds;
            }
        }


        int _xMin, _yMin, _zMin;
        int _xMax, _yMax, _zMax;
        public int xMin { get { return _xMin; } }
        public int xMax { get { return _xMax; } }
        public int yMin { get { return _yMin; } }
        public int yMax { get { return _yMax; } }
        public int zMin { get { return _zMin; } }
        public int zMax { get { return _zMax; } }

        void OnEnable() {
            ComputeFinalColors();
            ComputeBounds();
        }


        void OnDestroy() {
            if (modelGameObject != null) {
                DestroyImmediate(modelGameObject);
            }
        }

        public void ComputeFinalColors() {
            if (bits == null) return;
            int bitsLength = bits.Length;
            for (int k = 0; k < bitsLength; k++) {
                Color32 color = bits[k].color;
                if (color.r == 0 && color.g == 0 && color.b == 0) {
                    color = Misc.color32White;
                }
                if (bits[k].voxelDefinition != null) {
                    color = color.MultiplyRGB(bits[k].voxelDefinition.tintColor);
                }
                bits[k].finalColor = color;
                if (bits[k].microVoxels != null && (bits[k].microVoxels.isEmpty || bits[k].microVoxels.isFull)) {
                    bits[k].microVoxels = null;
                }
            }
        }


        public void ComputeBounds() {
            if (bits == null) return;
            _xMin = _zMin = _yMin = int.MaxValue;
            _xMax = _zMax = _yMax = int.MinValue;

            int modelOneYRow = sizeZ * sizeX;
            int modelOneZRow = sizeX;

            for (int b = 0; b < bits.Length; b++) {
                if (bits[b].isEmpty) continue;
                int bitIndex = bits[b].voxelIndex;
                int py = bitIndex / modelOneYRow;
                int remy = bitIndex - py * modelOneYRow;
                int pz = remy / modelOneZRow;
                int px = remy - pz * modelOneZRow;

                if (px < _xMin) _xMin = px;
                if (px > _xMax) _xMax = px;
                if (py < _yMin) _yMin = py;
                if (py > _yMax) _yMax = py;
                if (pz < _zMin) _zMin = pz;
                if (pz > _zMax) _zMax = pz;
            }

            Vector3 size = new Vector3(_xMax - _xMin + 1, _yMax - _yMin + 1, _zMax - _zMin + 1);
            Vector3 center = new Vector3((_xMax + _xMin) * 0.5f, (_yMax + _yMin) * 0.5f, (_zMax + _zMin) * 0.5f);
            _bounds = new Bounds(center, size);
        }
    }
}