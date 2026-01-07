//#define USES_TINTING

using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoxelPlay {


    public partial struct Voxel {

        public const ushort EmptyTypeIndex = 0;
        public const ushort HoleTypeIndex = 2;

        /// <summary>
        /// voxelDefinitions 목록의 복셀 정의 인덱스
        /// 몇 가지 예약된 값이 있습니다: 0 = 절대 비어 있음, 1 = 비어 있음(사용되지 않음, 예약됨), 2 = 빈 유형(구멍), 3 = 완전 불투명 더미, 내용이 있는 4+ 복셀 유형
        /// </summary>
        public ushort typeIndex;

        /// <summary>
        /// 이 복셀이 빛을 통과시키면 불투명 = 0이고, 그렇지 않으면 빛의 강도 감소 인자입니다.
        /// </summary>
        public byte opaque;

        /// <summary>
        /// 이 복셀의 현재 태양광 값입니다.빛은 복셀을 통과하는 빛의 강도입니다.
        /// </summary>
        public byte light;

        /// <summary>
        /// 이 복셀의 토치 조명 강도입니다.
        /// </summary>
        public byte torchLight;

        /// <summary>
        /// 압축광 강도(토치 + 태양광)
        /// </summary>
        public int packedLight {
            get { return (torchLight << 12) | light; }
        }

#if USES_TINTING
		public byte red, green, blue;
#else
        public byte red { get { return 255; } }
        public byte green { get { return 255; } }
        public byte blue { get { return 255; } }
#endif

        /// <summary>
        /// Returns the 
        /// </summary>
        /// <value>틴트의 색상</value>
        public Color32 color {
            get {
#if USES_TINTING
				return new Color32 (red, green, blue, 255);
#else
                return Misc.color32White;
#endif
            }
            set {
#if USES_TINTING
				red = value.r;
				green = value.g;
				blue = value.b;
#endif
            }
        }

        /// <summary>
        /// 이 복셀이 비어 있으면 true를 반환합니다.
        /// </summary>
        public bool isEmpty {
            get { return typeIndex <= HoleTypeIndex; }
            set {
                bool isCurrentlyEmpty = typeIndex <= HoleTypeIndex;
                if (isCurrentlyEmpty != value) {
                    if (value) {
                        typeIndex = 0;
                    } else {
                        typeIndex = HoleTypeIndex + 1;
                    }
                }
            }
        }

        /// <summary>
        /// 이 복셀이 비어 있으면 true를 반환합니다.
        /// </summary>
        public bool hasContent {
            get { return typeIndex > HoleTypeIndex; }
        }


        /// <summary>
        /// 이 복셀이 구멍이면 true를 반환합니다.
        /// </summary>
        public bool isHole {
            get { return typeIndex == HoleTypeIndex; }
            set {
                if (value) {
                    typeIndex = HoleTypeIndex;
                }
            }
        }

        /// <summary>
        /// 이 복셀의 복셀 정의 객체를 반환합니다.
        /// </summary>
        public VoxelDefinition type {
            get {
                return VoxelPlayEnvironment.instance.voxelDefinitions[typeIndex];
            }
        }

        /// <summary>
        /// 단일 정수로 압축된 태양 및 손전등 강도를 반환합니다.
        /// </summary>
        /// <param name="variation">선택적으로 광도에 작은 변화를 제공합니다.</param>
        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public int GetPackedLight(float variation) {
            return (torchLight << 12) + (int)(light * variation);
        }

        /// <summary>
        /// 이 복셀에 사용자 정의 색상이 있으면 true를 반환합니다.
        /// </summary>
        public bool isColored {
            get {
                return red != 255 || green != 255 || blue != 255;
            }
        }

        /// <summary>
        /// 이 복셀에 대해 추가로 포함된 정보
        /// 하위 4비트 = 수위
        /// 상위 4비트 = 텍스처 회전
        /// </summary>
        byte _flags;

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public byte GetFlags() {
            return _flags;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public void SetFlags(byte value) {
            _flags = value;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public int GetWaterLevel() {
            return _flags & 0xF;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public void SetWaterLevel(int value) {
            _flags = (byte)((_flags & 0xF0) | value);
        }

        /// <summary>
        /// 이 복셀의 텍스처 회전 비트를 반환합니다(0=0, 1=90, 2=180, 3=270).
        /// </summary>
        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public int GetTextureRotation() {
            return _flags >> 4;
        }


        /// <summary>
        /// 이 복셀의 회전 각도(0-360)를 반환합니다.
        /// </summary>
        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public float GetTextureRotationDegrees() {
            int rot = _flags >> 4;
            switch (rot) {
                case 1:
                    return 90;
                case 2:
                    return 180;
                case 3:
                    return 270;
                default:
                    return 0;
            }
        }


        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static float GetTextureRotationDegrees(int rotation) {
            switch (rotation) {
                case 1:
                    return 90;
                case 2:
                    return 180;
                case 3:
                    return 270;
                default:
                    return 0;
            }
        }


        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static int GetTextureRotationFromDegrees(float rotation) {
            switch ((int)rotation) {
                case 90:
                    return 1;
                case 180:
                    return 2;
                case 270:
                    return 3;
                default:
                    return 0;
            }
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public void SetTextureRotation(int value) {
            _flags = (byte)((_flags & 0xF) | (value << 4));
        }

        /// <summary>
        /// 마지막 빛이 계산되었습니다.
        /// </summary>
        public byte lightOrTorch {
            get { return light > torchLight ? light : torchLight; }
        }

        /// <summary>
        /// 이 복셀에 물이 있는지 여부
        /// </summary>
        /// <value><c>진실</c> if has water; otherwise, <c>거짓</c>.</value>
        public bool hasWater {
            get {
                return (_flags & 0xF) > 0;
            }
        }

        /// <summary>
        /// 이 복셀이 고체 블록인지 여부
        /// </summary>
        /// <value><c>진실</c> if is solid; otherwise, <c>거짓</c>.</value>
        public bool isSolid {
            get {
                return opaque >= VoxelPlayEnvironment.FULL_OPAQUE;
            }
        }

        public void Clear(byte light) {
            typeIndex = 0;
            opaque = 0;
            this.light = light;
            torchLight = 0;
            _flags = 0;
#if USES_TINTING
                this.red = this.green = this.blue = 255;
#endif
        }

        public static void Clear(Voxel[] voxels, byte light) {
            // Faster method
            Voxel emptyVoxel = new Voxel();
            emptyVoxel.light = light;
            voxels.Fill(emptyVoxel);
        }

        /// <summary>
        /// 복셀 유형을 설정합니다.이 방법은 라이트맵을 업데이트하지 않습니다.복셀이 빛을 방출하는 경우에는 Chunk.SetVoxel을 사용하세요.
        /// </summary>
        public void Set(VoxelDefinition type) {
#if USES_TINTING
			this.red = type.tintColor.r;
			this.green = type.tintColor.g;
			this.blue = type.tintColor.b;
#endif

            this.typeIndex = type.index;
            switch (type.renderType) {
                case RenderType.Opaque:
                case RenderType.Opaque6tex:
                case RenderType.OpaqueAnimated:
                    this.opaque = VoxelPlayEnvironment.FULL_OPAQUE;
                    this._flags = 0;
                    break;
                case RenderType.Transp6tex:
                    this.opaque = 2;
                    this._flags = 0;
                    break;
                case RenderType.Cutout:
                    this.opaque = 3;
                    this._flags &= 15; // keeps any water amount
                    break;
                case RenderType.Water:
                    this.opaque = 2;
                    this._flags = type.height;
                    break;
                case RenderType.OpaqueNoAO:
                case RenderType.Cloud:
                    this.opaque = VoxelPlayEnvironment.FULL_OPAQUE;
                    this._flags = 0;
                    break;
                default:
                    this.opaque = type.opaque;
                    this._flags &= 15; // keeps any water amount
                    break;
            }
        }

        /// <summary>
        /// 복셀 유형을 설정합니다.이 방법은 라이트맵을 업데이트하지 않습니다.복셀이 빛을 방출하는 경우에는 Chunk.SetVoxel을 사용하세요.
        /// </summary>
        public void Set(VoxelDefinition type, Color32 tintColor) {
#if USES_TINTING
			this.red = tintColor.r;
			this.green = tintColor.g;
			this.blue = tintColor.b;
#endif

            this.typeIndex = type.index;
            switch (type.renderType) {
                case RenderType.Opaque:
                case RenderType.Opaque6tex:
                case RenderType.OpaqueAnimated:
                    this.opaque = VoxelPlayEnvironment.FULL_OPAQUE;
                    this._flags = 0;
                    break;
                case RenderType.Transp6tex:
                    this.opaque = 2;
                    this._flags = 0;
                    break;
                case RenderType.Cutout:
                    this.opaque = 3;
                    this._flags &= 15;  // keeps any water amount
                    break;
                case RenderType.Water:
                    this.opaque = 2;
                    this._flags = type.height;
                    break;
                case RenderType.OpaqueNoAO:
                case RenderType.Cloud:
                    this.opaque = VoxelPlayEnvironment.FULL_OPAQUE;
                    this._flags = 0;
                    break;
                default:
                    this.opaque = type.opaque;
                    this._flags &= 15;  // keeps any water amount
                    break;
            }
        }

        /// <summary>
        /// 복셀 유형을 설정합니다.이 방법은 라이트맵을 업데이트하지 않습니다.복셀이 빛을 방출하는 경우에는 Chunk.SetVoxel을 사용하세요.
        /// </summary>
        [MethodImpl(256)]
        public void SetFastOpaque(VoxelDefinition type) {
#if USES_TINTING
			this.red = type.tintColor.r;
			this.green = type.tintColor.g;
			this.blue = type.tintColor.b;
#endif
            this.typeIndex = type.index;
            this.opaque = VoxelPlayEnvironment.FULL_OPAQUE;
            this._flags = 0;
        }

        /// <summary>
        /// 복셀 유형을 설정합니다.이 방법은 라이트맵을 업데이트하지 않습니다.복셀이 빛을 방출하는 경우에는 Chunk.SetVoxel을 사용하세요.
        /// </summary>
        [MethodImpl(256)]
        public void SetFastWater(VoxelDefinition type) {
            this.typeIndex = type.index;
            this.opaque = 2;
            this._flags = type.height;
        }

        /// <summary>
        /// 아무것도 나타내지 않음
        /// </summary>
        public static Voxel Empty = GetEmptyVoxel();

        static Voxel GetEmptyVoxel() {
            Voxel empty = new Voxel();
            empty.Clear(0);
            return empty;
        }

        /// <summary>
        /// 구멍(지형 생성기가 청크를 채우기 전에 이 복셀이 배치된 경우 지형 생성기로 채워지지 않는 빈 복셀)을 나타냅니다.
        /// </summary>
        public static Voxel Hole = new Voxel() { typeIndex = HoleTypeIndex };


        public static bool supportsTinting {
            get {
#if USES_TINTING
				return true;
#else
                return false;
#endif
            }
        }

        public int WriteRawData(byte[] buffer, int index) {
            if (typeIndex >= 255) {
                buffer[index++] = 255;
                buffer[index++] = (byte)(typeIndex >> 8);
            }
            buffer[index++] = (byte)(typeIndex & 0xFF);
            buffer[index++] = (byte)opaque;
            buffer[index++] = (byte)(light + (torchLight << 4));
            buffer[index++] = _flags;
#if USES_TINTING
            buffer [index++] = red;
            buffer [index++] = green;
            buffer [index++] = blue;
#endif
            return index;
        }

        public int ReadRawData(byte[] buffer, int index) {
            typeIndex = buffer[index++];
            if (typeIndex == 255) {
                typeIndex = (ushort)((buffer[index] << 8) + buffer[index + 1]);
                index += 2;
            }

            int packed = buffer[index++];
            opaque = (byte)(packed & 0xF);

            int packedLight = buffer[index++];
            light = (byte)(packedLight & 0xF);
            torchLight = (byte)(packedLight >> 4);

            _flags = buffer[index++];

#if USES_TINTING
            red = buffer [index++];
            green = buffer [index++];
            blue = buffer [index++];
#endif
            return index;

        }


        public static bool operator ==(Voxel c1, Voxel c2) {
            return (c1.typeIndex == c2.typeIndex && c1.opaque == c2.opaque && c1.light == c2.light && c1.torchLight == c2.torchLight && c1.hasContent == c2.hasContent && c1.red == c2.red && c1.green == c2.green && c1.blue == c2.blue);
        }

        public static bool operator !=(Voxel c1, Voxel c2) {
            return !(c1.typeIndex == c2.typeIndex && c1.opaque == c2.opaque && c1.light == c2.light && c1.torchLight == c2.torchLight && c1.hasContent == c2.hasContent && c1.red == c2.red && c1.green == c2.green && c1.blue == c2.blue);
        }

        public override bool Equals(object obj) {
            if ((obj == null) || !GetType().Equals(obj.GetType())) {
                return false;
            }
            Voxel c2 = (Voxel)obj;
            return (typeIndex == c2.typeIndex && opaque == c2.opaque && light == c2.light && torchLight == c2.torchLight && hasContent == c2.hasContent && red == c2.red && green == c2.green && blue == c2.blue);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override string ToString() {
            return "Voxel type: " + type;
        }

        public static int memorySize {
            get {
#if USES_TINTING
                return 10;
#else
                return 7;
#endif
            }
        }



    }

}
