using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoxelPlay {

    public enum RenderType : byte {
        Opaque = 0,
        Cutout = 1,
        Water = 2,
        CutoutCross = 3,
        Cloud = 4,
        Custom = 5,
        Opaque6tex = 6,
        Transp6tex = 7,
        Invisible = 8,
        OpaqueNoAO = 9,
        OpaqueAnimated = 10
    }

    public enum CustomVoxelMaterial {
        PrefabMaterial,
        VertexLit,
        Texture,
        TextureAlpha,
        TextureAlphaDoubleSided,
        TextureTriplanar,
        TextureCutout,
        TextureBumpMap
    }

    public enum SeeThroughMode {
        NotSupported = 0,
        Transparency = 1,
        ReplaceVoxel = 2,
        FullyInvisible = 3
    }

    public struct TextureRotationIndices {
        public int forward, right, back, left;
    }


    [CreateAssetMenu(menuName = "Voxel Play/Voxel Definition", fileName = "VoxelDefinition", order = 101)]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001917-voxels-and-voxel-definitions")]
    public partial class VoxelDefinition : ScriptableObject {

        [Tooltip("UI에 표시할 이름")]
        public string title;

        public RenderType renderType = RenderType.Opaque;

        [Tooltip("이 복셀 유형을 렌더링하기 위해 다른 재료를 사용해야 하는 경우.")]
        public bool overrideMaterial;

        [Tooltip("사용자 정의 재질을 할당합니다.재료는 적절한 렌더 유형에 대한 VP 기본 재료에서 파생되어야 합니다.")]
        public Material overrideMaterialNonGeo;

        [Tooltip("이 재정의 재질에 그리디 메싱을 사용할 수 있는 경우 활성화합니다.")]
        public bool overrideMaterialGreedyMeshing;

        [Tooltip("텍스처는 재료 자체에 지정됩니다.")]
        public bool texturesByMaterial;

        [Tooltip("사용자 정의 설정을 사용하여 아래에 지정된 텍스처를 다른 텍스처 배열로 압축합니다.셰이더는 텍스처 배열과 호환되어야 합니다.")]
        public bool texturesCustomPacking;

        [Tooltip("텍스처의 크기.")]
        public int texturesPackingSize = 256;

        [Tooltip("텍스처 UV 승수")]
        public float texturesPackingScale = 1;

        [Tooltip("텍스처 패킹을 사용할 때 노멀 맵 지원을 활성화합니다.")]
        public bool texturesPackingNormalMap;

        [Tooltip("텍스처 패킹을 사용할 때 릴리프 맵 지원을 활성화합니다.")]
        public bool texturesPackingReliefMap;

        [NonSerialized]
        public TextureArrayPacker textureArrayPacker; // the texture array packer for this voxel

        [Tooltip("월드 공간 UV를 활성화합니다.텍스처가 여러 복셀에 걸쳐 펼쳐져 있을 때 유용합니다.")]
        public bool worldSpaceUVs;

        [Tooltip("이 복셀에 의해 차단되는 빛의 양입니다.다른 인접한 복셀을 가리는 완전 솔리드 개체임을 지정하려면 이 값을 15로 설정합니다.사용자 정의 복셀은 불투명도가 15 미만이어야 하며 그렇지 않으면 검은색으로 표시됩니다.")]
        [Range(0, 15)]
        public byte opaque;

        [Tooltip("이 사용자 정의 복셀에 이 방향으로 폐색면이 있는 경우.")]
        public bool occludesTop, occludesBottom, occludesLeft, occludesRight, occludesForward, occludesBack;

        [Tooltip("복셀 윗면의 질감")]
        public Texture2D textureTop;
        [Tooltip("배출 지도")]
        public Texture2D textureTopEmission;
        [Tooltip("노멀 맵")]
        public Texture2D textureTopNRM;
        [Tooltip("배수량")]
        public Texture2D textureTopDISP;

        [Tooltip("모든 복셀 측면의 텍스처 또는 복셀에 6개의 텍스처가 있는 경우 후면 텍스처")]
        public Texture2D textureSide;
        [Tooltip("배출 지도")]
        public Texture2D textureSideEmission;
        [Tooltip("노멀 맵")]
        public Texture2D textureSideNRM;
        [Tooltip("변위 맵")]
        public Texture2D textureSideDISP;

        [Tooltip("복셀의 오른쪽 면 텍스처")]
        public Texture2D textureRight;
        [Tooltip("배출 지도")]
        public Texture2D textureRightEmission;
        [Tooltip("노멀 맵")]
        public Texture2D textureRightNRM;
        [Tooltip("변위 맵")]
        public Texture2D textureRightDISP;

        [Tooltip("복셀의 앞쪽 면에 대한 텍스처")]
        public Texture2D textureForward;
        [Tooltip("배출 지도")]
        public Texture2D textureForwardEmission;
        [Tooltip("노멀 맵")]
        public Texture2D textureForwardNRM;
        [Tooltip("변위 맵")]
        public Texture2D textureForwardDISP;

        [Tooltip("복셀의 왼쪽 면 텍스처")]
        public Texture2D textureLeft;
        [Tooltip("배출 지도")]
        public Texture2D textureLeftEmission;
        [Tooltip("노멀 맵")]
        public Texture2D textureLeftNRM;
        [Tooltip("변위 맵")]
        public Texture2D textureLeftDISP;

        [Tooltip("복셀 하단의 텍스처")]
        public Texture2D textureBottom;
        [Tooltip("배출 지도")]
        public Texture2D textureBottomEmission;
        [Tooltip("노멀 맵")]
        public Texture2D textureBottomNRM;
        [Tooltip("변위 맵")]
        public Texture2D textureBottomDISP;

        public AnimationTextureSet[] animationTextures;

        [Tooltip("기본 텍스처 외에 프레임 수")]
        [Range(1, 16)]
        public int animationSpeed = 4;

        [Tooltip("기본 색조 색상")]
        public Color32 tintColor = Misc.color32White;

        [Tooltip("풀/나무 잎의 색상 변화")]
        [Range(0, 2f)]
        public float colorVariation = 0.5f;

        [Tooltip("투명한 복셀을 위한 맞춤형 알파")]
        [Range(0, 1f)]
        public float alpha = 1f;

        [Tooltip("주변 조명과 AO를 계산하고 정점 색상으로 굽습니다.")]
        public bool computeLighting;

        [Range(0, 15), Tooltip("Amount of emitting light")]
        public byte lightIntensity;

        [Tooltip("복셀이 선택될 때 재생되는 사운드")]
        public AudioClip pickupSound;

        [Tooltip("이 복셀이 장면에 배치될 때 생성되는 소리")]
        public AudioClip buildSound;

        [Tooltip("발자국 소리")]
        public AudioClip[] footfalls;

        [Tooltip("플레이어가 이 복셀에서 점프할 때 생성되는 소리")]
        public AudioClip jumpSound;

        [Tooltip("이 복셀 위로 점프하고 착지한 후 생성되는 소리")]
        public AudioClip landingSound;

        [Tooltip("복셀 히트 사운드")]
        public AudioClip impactSound;

        [Tooltip("복셀 파괴음")]
        public AudioClip destructionSound;

        [Range(0, 255)]
        [Tooltip("저항 포인트.0은 적중할 수 없음을 의미합니다.255는 파괴불가라는 뜻입니다.")]
        public byte resistancePoints = 15;

        public bool showDamageCracks = true;

        // used for vegetation particles; extracted dynamically from texture
        [NonSerialized, HideInInspector]
        public Color sampleColor;

        /// <summary>
        /// 이 복셀 유형을 인벤토리에 표시할 수 없는 경우.
        /// </summary>
        [Tooltip("이 복셀 유형을 인벤토리에 표시할 수 없는 경우.")]
        public bool hidden;

        /// <summary>
        /// 이 복셀 유형을 위반한 후 사용자가 수집할 수 있는 경우.
        /// </summary>
        [Tooltip("이 복셀 유형을 위반한 후 사용자가 수집할 수 있는 경우.")]
        public bool canBeCollected = true;

        [Tooltip("이 복셀이 파괴될 때 아이템이 드롭될 확률(0-1)입니다.")]
        [Range(0, 1)]
        public float dropProbability = 1f;

        [Tooltip("이 복셀이 파괴되면 드랍되는 아이템입니다.null인 경우 동일한 복셀 유형의 기본 항목이 사용됩니다.")]
        public ItemDefinition dropItem;

        [Tooltip("항목 수명을 초 단위로 삭제합니다.")]
        public float dropItemLifeTime = 10f;

        [Tooltip("아이템 스케일을 떨어뜨립니다.")]
        public float dropItemScale = 0.25f;

        [Tooltip("인벤토리 패널에 사용되는 텍스처입니다.생략하면 측면 텍스처가 사용됩니다.")]
        public Texture2D icon;

        [Tooltip("입자 효과를 위해 색상을 샘플링하는 데 사용되는 텍스처입니다.")]
        public Texture2D textureSample;

        [Tooltip("조립식 메인 텍스처를 재정의합니다.활성화되면 텍스처 샘플이 이 프리팹의 기본 텍스처로 사용됩니다.")]
        public bool overrideMainTexture;

        [Tooltip("프리팹 메인 텍스처 오프셋을 재정의합니다.")]
        public Vector2 overrideMainTextureOffset;

        [Tooltip("이 복셀 유형이 NavMesh 탐색에 포함될 수 있는 경우.모든 불투명 복셀은 기본적으로 생성된 모든 NavMesh에 포함되지만 이 속성을 false로 설정하여 이 복셀 유형을 제외할 수 있습니다.")]
        public bool navigatable = true;

        [Tooltip("더 조밀한 효과를 추가하려면 추가 컷아웃 크로스 메시를 렌더링합니다(나무 잎에만 사용해야 함).")]
        public bool denseLeaves = true;

        [Tooltip("이 속성이 true로 설정된 경우 컷아웃 복셀 유형에 애니메이션을 적용할 수 있습니다.")]
        public bool windAnimation = true;

        [Tooltip("이 복셀이 경사 효과를 지원하는 경우.")]
        public bool supportsBevel = true;

        [Tooltip("이 복셀을 렌더링할 때 사용할 프리팹입니다.프리팹이 유효한 재료를 사용하는지 확인하십시오(Voxel Play와 함께 제공되는 VP 모델 * 재료 중 하나를 복사할 수 있음).자세한 내용은 설명서를 확인하세요.")]
        public GameObject model;

        [Tooltip("이 프리팹에 대해 충돌기 데이터가 생성되고 청크 충돌기와 병합됩니다.")]
        public bool generateCollider;

        [Tooltip("NavMesh 데이터는 이 프리팹에 대해 생성되고 청크 NavMesh와 병합됩니다.")]
        public bool generateNavMesh;

        /// <summary>
        /// 이는 복셀 정의 구성이 재질을 재정의하려는 경우 모델 또는 인스턴스 모델에 대한 참조입니다.
        /// </summary>
        [NonSerialized]
        public GameObject prefab;

        [Tooltip("사용자 정의 복셀을 렌더링할 때 사용할 재질입니다.Prefab에서 제공하는 재질을 사용하거나 Voxel Play에서 제공하는 최적화된 재질 중 하나를 사용할 수 있습니다.")]
        public CustomVoxelMaterial prefabMaterial = CustomVoxelMaterial.PrefabMaterial;

        [Tooltip("이 복셀 유형을 렌더링하려면 GPU 인스턴싱을 사용하세요.")]
        public bool gpuInstancing;

        [Tooltip("이 인스턴스화된 복셀이 그림자를 투사할 수 있는 경우.")]
        public bool castShadows = true;

        [Tooltip("인스턴스화된 복셀이 그림자를 받을 수 있는 경우.")]
        public bool receiveShadows = true;

        [Tooltip("GPU 인스턴싱이 활성화되면 렌더링은 GPU에서 수행되지만 충돌체 또는 사용자 정의 스크립트를 보유할 수 있는 게임 개체를 강제로 생성할 수 있습니다.")]
        public bool createGameObject;

        [Tooltip("복셀의 선택적 변위.")]
        public Vector3 offset;

        [Tooltip("변형을 생성하기 위해 오프셋을 무작위로 수정해야 하는 경우")]
        public bool offsetRandom;

        [Tooltip("X/Y/Z의 임의 오프셋 범위")]
        public Vector3 offsetRandomRange;

        [Tooltip("복셀의 선택적 스케일입니다.이 스케일에 게임오브젝트 변환 스케일이 곱해집니다.")]
        public Vector3 scale = Misc.vector3one;

        [Tooltip("복셀의 선택적 회전(도)입니다.")]
        public Vector3 rotation;

        [Tooltip("Y축을 중심으로 무작위 회전")]
        public bool rotationRandomY;

        [Tooltip("이 복셀이 동일한 위치에 두 번 배치될 때 사용할 복셀 정의입니다.")]
        public VoxelDefinition promotesTo;

        [Tooltip("이 복셀이 파괴될 때 사용할 복셀 정의입니다.")]
        public VoxelDefinition replacedBy;

        [Tooltip("이 복셀이 전파되는 경우(예: 물).")]
        public bool spreads;

        [Tooltip("이 복셀이 전파될 때 양이 감소하는 경우(예: 바닷물이 배수되지 않고 무한한 물 공급원처럼 작동하지만 플레이어의 인벤토리에 있는 물은 확산되면서 배수되어야 합니다)")]
        public bool drains = true;

        [Tooltip("전파 지연 시간(초)입니다.")]
        public float spreadDelay;

        [Tooltip("전파를 위한 추가 무작위 지연입니다.")]
        public float spreadDelayRandom;

        [Tooltip("이 임계값보다 작은 불투명 값을 가진 복셀은 대체됩니다.")]
        [Range(0, 15)]
        public byte spreadReplaceThreshold;

        [ColorUsage(true)]
        public Color diveColor = new Color(0, 0.41f, 0.63f, 0.83f);

        [Range(0, 15), Tooltip("Default block level")]
        public byte height = 13;

        [Range(0, 255), Tooltip("Damage caused to the player.")]
        public byte playerDamage;

        [Range(0, 255), Tooltip("Time interval in seconds between damage caused to the player.")]
        public float playerDamageDelay = 2f;

        [Tooltip("이 복셀이 파괴되면, TriggerCollapse 플래그를 사용하여 동일한 레벨 또는 상위 레벨에 연결된 복셀이 떨어지게 됩니다.")]
        public bool triggerCollapse;

        [Tooltip("근처의 복셀이 파괴되면 이 복셀이 무너져 떨어지게 됩니다.")]
        public bool willCollapse;

        [Tooltip("이 복셀 유형(예: 공기 복셀)에서 레이캐스트를 무시합니다.")]
        public bool ignoresRayCast;

        [Tooltip("이 복셀 유형에 대한 충돌체 데이터를 강제로 생성합니다.")]
        public bool generateColliders;

        [Tooltip("강조 상자에 대한 선택적 변위입니다.")]
        public Vector3 highlightOffset;

        [Tooltip("이 복셀 유형에 대해 텍스처 회전이 허용되는 경우입니다.")]
        public bool allowsTextureRotation = true;

        [Tooltip("이 복셀을 배치할 때는 벽에 부착하세요.")]
        public bool placeOnWall;

        [Tooltip("이 복셀을 배치할 때 플레이어가 향하는 방향으로 방향을 지정합니다.6가지 텍스처 또는 사용자 정의 복셀 유형이 있는 복셀에만 적용됩니다.")]
        public bool placeFacingPlayer = true;

        [Tooltip("이 복셀을 입력하면 FPS 컨트롤러에 의해 OnVoxelEnter 이벤트가 발생합니다.")]
        public bool triggerEnterEvent;

        [Tooltip("이 복셀 위를 걷는 경우 FPS 컨트롤러에 의해 OnVoxelWalk 이벤트가 발생합니다.")]
        public bool triggerWalkEvent;

        public bool showFoam = true;

        [Tooltip("이 복셀은 보이지 않게 설정할 수 있습니다.특정 상황에서는 복셀 플레이가 특정 복셀을 렌더링하지 않습니다. 예를 들어 플레이어가 3인칭 시점에서 건물에 들어갈 때 건물 지붕을 숨기는 경우입니다.")]
        public SeeThroughMode seeThroughMode = SeeThroughMode.Transparency;

        [Tooltip("투명 효과가 발생할 때 렌더링하는 데 사용되는 복셀입니다.이 복셀은 다른 유형의 복셀의 투명도를 갖는 이 복셀의 변형일 수 있습니다.")]
        public VoxelDefinition seeThroughVoxel;

        public bool allowUpsideDownVoxel;
        public VoxelDefinition upsideDownVoxel;
        public bool isUpsideDown;

        /// ************************ 임시/세션 관련 데이터 *****************************************
        /// <summary>
        /// 이 복셀이 충돌을 일으키는 경우 true를 반환합니다.
        /// </summary>
        [NonSerialized]
        public bool isSolid;

        // If stuff like pictures or carpets can be attached to this voxel
        public bool supportsDecorations => !placeOnWall && renderType != RenderType.CutoutCross && renderType != RenderType.Water && renderType != RenderType.Invisible && renderType != RenderType.Cloud;

        [NonSerialized]
        public int textureIndexSide, textureIndexTop, textureIndexBottom, textureIndexLeft, textureIndexForward, textureIndexRight;

        // indices for rotated textures
        [NonSerialized]
        public TextureRotationIndices[] textureSideIndices;

        // index in voxelDefinitions list when it's added
        [NonSerialized]
        public ushort index;

        public bool hasContent { get { return index > Voxel.HoleTypeIndex; } }

        // The related dynamic voxel definition. This field is only set when a static voxel is converted to dynamic (only set once per type)
        [NonSerialized]
        public VoxelDefinition dynamicDefinition;

        // The related static voxel definition. Thsi field is set so when a dynamic voxel is converted back to static, it can know which static type belongs to
        [NonSerialized]
        public VoxelDefinition staticDefinition;

        // if this voxel definition is dynamic
        [NonSerialized]
        public bool isDynamic;

        // if this voxel is used for vegetation
        public bool isVegetation => renderType == RenderType.CutoutCross;

        /// <summary>
        /// 이 복셀이 트리 모델 정의의 일부인 경우 true를 반환합니다.
        /// </summary>
        [NonSerialized]
        public bool isTree;

        // Used to cache dynamic voxel meshes
        [NonSerialized]
        public Dictionary<Color, Mesh> dynamicMeshes;

        // Used to texture dropped voxels
        [NonSerialized]
        public Texture2D textureThumbnailTop, textureThumbnailSide, textureThumbnailBottom;

        // Annotated if the thumbnail textures are instanced/scaled versions so they should be disposed properly
        [NonSerialized]
        public bool textureThumbnailTopInstanced, textureThumbnailSideInstanced, textureThumbnailBottomInstanced;

        [Tooltip("이 복셀이 생물 군계에서 표면 복셀로 사용되는 경우 이 필드는 먼지 복셀을 가리킵니다.기본적으로 복셀 재생은 초기화 중에 이 필드를 자동으로 할당하지만 여기에서 다른 복셀을 지정할 수 있습니다.")]
        public VoxelDefinition biomeDirtCounterpart;

        // mesh used by the prefab
        [NonSerialized]
        Mesh _mesh;

        public Mesh mesh {
            get {
                if (_mesh == null && prefab != null) {
                    MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
                    if (mf != null)
                        _mesh = mf.sharedMesh;
                }
                return _mesh;
            }
        }

        [NonSerialized]
        public Color32[][] meshColors32; // for multi-part prefabs, a different color array per each object

        // material used by the prefab
        [NonSerialized]
        Material[] _materials;

        void FetchMaterials () {
            if (prefab == null) return;
            MeshRenderer mr = prefab.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
                _materials = mr.sharedMaterials;
        }

        /// <summary>
        /// 프리팹에서 사용되는 재료를 가져오거나 설정합니다.
        /// </summary>
        public Material[] materials {
            get {
                if (_materials.IsNullOrEmpty()) {
                    FetchMaterials();
                }
                return _materials;
            }
            set {
                _materials = value;
            }
        }

        /// <summary>
        /// 프리팹에서 사용되는 첫 번째 재료를 가져오거나 설정합니다.
        /// </summary>
        public Material material {
            get {
                if (_materials.IsNullOrEmpty()) {
                    FetchMaterials();

                    if (_materials.IsNullOrEmpty()) {
                        return null;
                    }
                }
                return _materials[0];
            }

            set {
                if (_materials.IsNullOrEmpty()) {
                    _materials = new Material[1];
                }
                _materials[0] = value;
            }
        }

        // if the model has collider and the gameobject is being created.
        [NonSerialized]
        public bool prefabUsesCollider;


        Bounds _bakedBoxColliderBounds;

        // The computed collider prefab bounds
        public Bounds prefabColliderBounds {
            get {
                if (_bakedBoxColliderBounds.size.x != 0) {
                    return _bakedBoxColliderBounds;
                }
                return mesh.bounds;
            }
            set {
                _bakedBoxColliderBounds = value;
            }
        }

        // if the model has a rigid body.
        [NonSerialized]
        public bool prefabUsesRigidbody;


        /// <summary>
        /// 각 머티리얼에는 메시 작업 구조에 정점 인덱스를 저장하기 위한 인덱스가 있습니다.
        /// </summary>
        [NonSerialized]
        public int materialBufferIndex;

        /// <summary>
        /// 이 복셀 정의와 관련된 렌더링 자료에 해당하는 그리디메셔에 대한 편리한 접근자
        /// </summary>
        [NonSerialized]
        public VoxelPlayGreedyMesherLit greedyMesherLit;
        [NonSerialized]
        public VoxelPlayGreedyMesherLitAO greedyMesherLitAO;


        public Quaternion GetRotation (Vector3d position) {
            Vector3 rot = rotation;
            if (rotationRandomY) {
                rot.y += WorldRand.GetValue(position) * 360f;
            }
            return Quaternion.Euler(rot);
        }

        public Vector3 GetOffset (Vector3d position) {
            Vector3 shiftedOffset = offset;
            if (offsetRandom) {
                shiftedOffset += WorldRand.GetVector3(position, offsetRandomRange, -0.5f);
            }
            return shiftedOffset;
        }

        /// <summary>
        /// 인벤토리에 사용되는 텍스처
        /// </summary>
        /// <value>아이콘.</value>
        public Texture2D GetIcon () {
            if (icon != null)
                return icon;
            if (textureThumbnailSide != null) {
                return textureThumbnailSide;
            }
            return textureSide;
        }

        public Material GetOverrideMaterial () {
            if (!overrideMaterial)
                return null;
            return overrideMaterialNonGeo;
        }

        /// <summary>
        /// 투명 효과를 위해 생성된 임시 투명 복셀
        /// </summary>
        [NonSerialized]
        public int seeThroughVoxelTempTransp;

        /// <summary>
        /// 이 복셀을 저장 게임에서 저장할 수 있는 경우
        /// </summary>
        [NonSerialized]
        public bool doNotSave;

        /// <summary>
        /// 런타임에 설정합니다.densityLeves를 전역 빽빽한 나뭇잎 설정과 결합합니다.
        /// </summary>
        [NonSerialized]
        public bool usesDenseLeaves;


        // ********************************
        // Static methods *****************
        // ********************************

        /// <returns>복셀이 null이거나 null 복셀 정의를 나타내는 경우 true입니다.</returns>
        public static bool IsNull (VoxelDefinition vd) {
            return vd == null || vd.index == 0;
        }


        // ********************************
        // Events *************************
        // ********************************

        /// <summary>
        /// 직렬화할 수 없는 임시/세션 필드를 지웁니다.
        /// </summary>
        public void Reset () {
            index = 0;
            dynamicDefinition = null;
            batchedIndex = -1;
            doNotSave = false;
            textureIndexBottom = textureIndexSide = textureIndexTop = 0;
            isDynamic = false;
            dynamicDefinition = null;
            staticDefinition = null;
            seeThroughVoxelTempTransp = 0;
            materialBufferIndex = 0;
            if (dynamicMeshes != null) {
                dynamicMeshes.Clear();
                dynamicMeshes = null;
            }
            _mesh = null;
            _materials = null;
        }
    }

    [Serializable]
    public struct AnimationTextureSet {
        public Texture2D textureTop;
        public Texture2D textureSide;
        public Texture2D textureBottom;
    }

    public static partial class RenderTypeExtensions {

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsNavigation (this RenderType o) {
            return o == RenderType.Opaque || o == RenderType.Cutout || o == RenderType.OpaqueNoAO || o == RenderType.Opaque6tex || o == RenderType.Transp6tex || o == RenderType.Invisible || o == RenderType.OpaqueAnimated;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsWindAnimation (this RenderType o) {
            return o == RenderType.Cutout || o == RenderType.CutoutCross;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsBevel (this RenderType o) {
            return o == RenderType.Opaque || o == RenderType.Opaque6tex;
        }

        public static int numberOfTextures (this RenderType o) {
            if (o == RenderType.Invisible)
                return 0;
            return (o == RenderType.Opaque6tex || o == RenderType.Transp6tex) ? 6 : 3;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsTextureRotation (this RenderType o) {
            // custom types can always be rotated
            return o == RenderType.Opaque6tex || o == RenderType.Transp6tex || o == RenderType.Custom || o == RenderType.Opaque || o == RenderType.Cutout;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsEmission (this RenderType o) {
            return o == RenderType.Opaque || o == RenderType.Opaque6tex || o == RenderType.OpaqueAnimated;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsDynamic (this RenderType o) {
            return o == RenderType.Opaque || o == RenderType.Opaque6tex || o == RenderType.OpaqueNoAO || o == RenderType.Cutout || o == RenderType.Custom || o == RenderType.Transp6tex;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static byte hasContent (this RenderType o) {
            return o == RenderType.Invisible ? (byte)0 : (byte)1;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsAlphaSeeThrough (this RenderType o) {
            return o == RenderType.Opaque || o == RenderType.Cutout || o == RenderType.Opaque6tex || o == RenderType.OpaqueNoAO;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsTextureAnimation (this RenderType o) {
            return o == RenderType.OpaqueAnimated;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsTintColor (this RenderType o) {
            return o == RenderType.Opaque || o == RenderType.Opaque6tex || o == RenderType.OpaqueAnimated || o == RenderType.Transp6tex;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool supportsOptionalColliders (this RenderType o) {
            return o == RenderType.Cutout;
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public static bool isOpaque (this RenderType o) {
            return o == RenderType.Opaque || o == RenderType.Opaque6tex || o == RenderType.OpaqueAnimated || o == RenderType.OpaqueNoAO;
        }

        public static Material GetDefaultMaterial (this RenderType o, VoxelPlayEnvironment context) {
            string name;
            bool shadowsOnWater = context.shadowsOnWater && !VoxelPlayEnvironment.supportsURP;

            switch (o) {
                case RenderType.Opaque:
                case RenderType.Opaque6tex:
                    name = "VP Voxel Triangle Opaque";
                    break;
                case RenderType.OpaqueNoAO:
                    name = "VP Voxel Triangle Opaque No AO";
                    break;
                case RenderType.OpaqueAnimated:
                    name = "VP Voxel Triangle Opaque Animated";
                    break;
                case RenderType.Cutout:
                    name = "VP Voxel Triangle Cutout";
                    break;
                case RenderType.CutoutCross:
                    name = "VP Voxel Triangle Cutout Cross";
                    break;
                case RenderType.Water:
                    if (context.realisticWater) {
                        if (shadowsOnWater) {
                            name = "VP Voxel Triangle Water Realistic";
                        } else {
                            name = "VP Voxel Triangle Water Realistic No Shadows";
                        }
                    } else {
                        if (shadowsOnWater) {
                            name = "VP Voxel Triangle Water";
                        } else {
                            name = "VP Voxel Triangle Water No Shadows";
                        }
                    }
                    break;
                case RenderType.Transp6tex:
                    if (context.doubleSidedGlass) {
                        name = "VP Voxel Triangle Transp Double Sided";
                    } else {
                        name = "VP Voxel Triangle Transp";
                    }
                    break;
                case RenderType.Cloud:
                    name = "VP Voxel Triangle Cloud";
                    break;
                case RenderType.Invisible:
                    return null;
                default:
                    Debug.LogError("Unknown Render type?");
                    return null;
            }
            return Resources.Load<Material>("VoxelPlay/Materials/" + name);
        }

    }
}