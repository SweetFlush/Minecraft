using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace VoxelPlay {

    public delegate float SDF (Vector3d position);

    public enum ChunkModifiedFilter {
        All,
        OnlyModified = 1,
        NonModified = 2,
        ModifiedThisSession = 3
    }

    public enum EditorRenderDetail {
        Standard = 0,
        StandardPlusColliders = 1,
        StandardNoDetailGenerators = 2
    }

    public enum ObscuranceMode {
        Faster = 0,
        Custom = 1
    }

    public enum LogLevel {
        Default = 0,
        Verbose = 1
    }

    public enum UnloadChunkMode {
        ToggleVisibility = 0,
        DestroyIfNotModified = 1,
        Destroy
    }

    public enum InstancingCullingMode {
        Aggresive,
        Gentle,
        Disabled
    }

    public enum NavMeshResolution {
        Default,
        High = 10
    }

    public enum ModelPlacementAlignment {
        Centered,
        NonCentered,
    }

    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001712-voxel-play-environment")]
    public partial class VoxelPlayEnvironment : MonoBehaviour {


        public const int CHUNK_SIZE = 16;
        public const int CHUNK_HALF_SIZE = CHUNK_SIZE / 2;
        public const int CHUNK_SIZE_PLUS_2 = CHUNK_SIZE + 2;
        public const int ONE_Y_ROW = CHUNK_SIZE * CHUNK_SIZE;
        public const int ONE_Z_ROW = CHUNK_SIZE;
        public const int CHUNK_VOXEL_COUNT = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;
        public const int CHUNK_SIZE_MINUS_ONE = CHUNK_SIZE - 1;
        public const int VOXELINDEX_X_EDGE_BITWISE = CHUNK_SIZE - 1;
        public const int VOXELINDEX_Z_EDGE_BITWISE = (CHUNK_SIZE - 1) * ONE_Z_ROW;
        public const int VOXELINDEX_Y_EDGE_BITWISE = (CHUNK_SIZE - 1) * ONE_Y_ROW;
        public static Vector3 CHUNK_HALF_SIZE_VECTOR = new Vector3(CHUNK_HALF_SIZE, CHUNK_HALF_SIZE, CHUNK_HALF_SIZE);

        /// <summary>
        /// 디버그 로깅의 상세 수준입니다.최대 디버그 정보를 얻으려면 Verbose로 설정하십시오.
        /// </summary>
        public LogLevel debugLevel = LogLevel.Default;

        /// <summary>
        /// 세계 생성 매개변수와 생물군계 구성이 포함되어 있습니다.
        /// </summary>
        public WorldDefinition world;

        /// <summary>
        /// 사용 가능한 모든 복셀 정의를 사용하고 복셀을 파괴하는 플레이어의 능력을 활성화/비활성화합니다(빌드 모드 토글)
        /// </summary>
        public bool enableBuildMode = true;

        /// <summary>
        /// 빌드 모드의 현재 상태입니다.
        /// </summary>
        public bool buildMode;

        public bool enableGeneration = true;
        public bool constructorMode;
        public bool renderInEditor;
        public bool renderInEditorLowPriority = true;
        public bool generateAroundCamera = true;
        public EditorRenderDetail renderInEditorDetail = EditorRenderDetail.Standard;
        public Vector3 renderInEditorAreaCenter;
        public Vector3 renderInEditorAreaSize = new Vector3(512, 64, 512);
        public IVoxelPlayCharacterController characterController;
        public bool enableConsole = true;
        public bool showConsole;
        public bool enableInventory = true;
        public bool enableDebugWindow = true;
        public bool showFPS;
        public string welcomeMessage = "<color=green>Welcome to <color=white>Voxel Play</color>! Press<color=yellow> F1 </color>for console commands.</color>";
        public float welcomeMessageDuration = 5f;
        public GameObject UICanvasPrefab;
        public GameObject inputControllerPCPrefab, inputControllerMobilePrefab;
        public GameObject crosshairPrefab;
        public Texture2D crosshairTexture;
        public Color consoleBackgroundColor = new Color(0, 0, 0, 82f / 255f);
        public bool enableStatusBar = true;
        public Color statusBarBackgroundColor = new Color(0, 0, 0, 192 / 255f);
        public int layerParticles = 2;
        public int layerVoxels = 1;
        public int layerClouds = 1;
        public bool enableLoadingPanel = true;
        public string loadingText = "Initializing...";
        public float initialWaitTime;
        public string initialWaitText = "Loading World...";

        public bool loadSavedGame;
        public string saveFilename = "save0001";

        /// <summary>
        /// 빛은 복셀에서 복셀로 퍼지고 지하로 갈수록 감쇠됩니다.
        /// </summary>
        public bool globalIllumination = true;

        /// <summary>
        /// 기본 주변광 수준(0-1).복셀의 최소 밝기에 영향을 줍니다.
        /// </summary>
        [Range(0f, 1f)]
        public float ambientLight = 0.2f;

        [Range(0f, 1f)]
        public float daylightShadowAtten = 0.65f;

        /// <summary>
        /// 복셀 정점에 AO + 조명 적용
        /// </summary>
        public bool enableSmoothLighting = true;

        public ObscuranceMode obscuranceMode = ObscuranceMode.Faster;
        [Range(0f, 3f)]
        public float obscuranceIntensity = 0.5f;

        public bool enableReliefMapping;

        [Range(0f, 0.2f)]
        public float reliefStrength = 0.05f;
        [Range(2, 100)]
        public int reliefIterations = 10;
        [Range(0, 10)]
        public int reliefIterationsBinarySearch = 5;
        public float reliefMaxDistance = 25;

        public bool enableNormalMap;

        public bool enableFogSkyBlending = true;

        public int textureSize = 64;

        public int maxChunks = 16000;

        public bool hqFiltering = true;
        [Range(0, 2f)]
        public float mipMapBias = 1;

        public FilterMode filterMode = FilterMode.Point;

        public bool doubleSidedGlass = true;

        public bool transparentBling = true;

        /// <summary>
        /// 복셀을 손상시키거나 파괴할 때 입자를 추가합니다.
        /// </summary>
        public bool damageParticles = true;

        /// <summary>
        /// 사용자 정의 복셀에 대해 ComputeBuffer를 활성화합니다(Shader Model 4.5 이상이 필요함).
        /// </summary>
        public bool useComputeBuffers;

        public InstancingCullingMode instancingCullingMode = InstancingCullingMode.Aggresive;

        public float instancingCullingPadding = 50;

        [Tooltip("그리디 메싱을 사용할 때 사후 처리 효과를 사용하여 인접한 가장자리 사이의 흰색 픽셀을 감지하고 제거합니다.")]
        public bool usePostProcessing;

        /// <summary>
        /// 모바일 플랫폼을 대상으로 할 때 에디터에서 듀얼 터치 컨트롤러 UI를 사용합니다.
        /// </summary>
        public bool previewTouchUIinEditor;

        [NonSerialized]
        public Camera cameraMain;

        /// <summary>
        /// 가시 거리를 기준으로 한 최소 권장 청크 풀 크기
        /// </summary>
        public int maxChunksRecommended {
            get {
                int dxz = _visibleChunksDistance * 2 + 1;
                int dy = Mathf.Min(_visibleChunksDistance, 8) * 2 + 1;
                return Mathf.Max(3000, dxz * dxz * dy * 2);
            }
        }

        public int prewarmChunksInEditor = 5000;

        public bool enableTinting;
        public bool enableColoredShadows;

        public bool enableGlobalSpecular;
        [Range(0, 1)]
        public float globalSpecularIntensity = 0.5f;

        public bool enableFresnel;
        public float fresnelExponent = 8f;
        [Range(0, 1)]
        public float fresnelIntensity = 0.2f;
        public Color fresnelColor = new Color32(232, 230, 253, 255);

        public bool enableBevel;

        public bool enableOutline;
        public Color outlineColor = new Color(1, 1, 1, 0.5f);
        [Range(0, 1f)]
        public float outlineThreshold = 0.49f;

        public bool enableCurvature;

        public bool seeThrough;
        public GameObject seeThroughTarget;
        public float seeThroughRadius = 3f;
        [Range(0f, 1f)]
        public float seeThroughAlpha;

        [SerializeField]
        int _seeThroughHeightOffset = 1;

        public int seeThroughHeightOffset {
            get { return _seeThroughHeightOffset; }
            set {
                if (value != _seeThroughHeightOffset) {
                    _seeThroughHeightOffset = Mathf.Max(0, value);
                    NotifyCameraMove();
                    if (OnSeeThroughHeightOffsetChanged != null) {
                        OnSeeThroughHeightOffsetChanged();
                    }
                }
            }
        }

        public bool enableBrightPointLights;
        public float brightPointsMaxDistance = 10000;

        public bool enableURPNativeLights;

        [Range(1, 30)]
        [SerializeField]
        int _visibleChunksDistance = 10;

        /// <summary>
        /// 청크 단위의 최대 렌더링 거리입니다.값이 높을수록 성능에 영향을 줍니다.
        /// 일반적인 범위: 1-30.이를 변경하면 청크 재계산이 강제됩니다.범위를 더 넓히려면 원거리 청크 렌더링 옵션을 사용하세요.
        /// </summary>
        public int visibleChunksDistance {
            get { return _visibleChunksDistance; }
            set {
                if (_visibleChunksDistance != value) {
                    _visibleChunksDistance = value;
                    NotifyCameraMove();// forces check chunks in frustum
                    InitOctrees();
                }
            }
        }
        [Range(1, 8)] public int forceChunkDistance = 3;

        /// <summary>
        /// 가시 거리를 벗어나면 청크 gameObject를 비활성화합니다.
        /// </summary>
        public bool unloadFarChunks;

        /// <summary>
        /// 'Toggle visible'은 보이는 거리 매개변수에 따라 청크를 숨기거나 표시합니다.'Destroy'는 실제로 청크 메시와 해당 충돌체 및 NavMesh(있는 경우)의 메모리를 파괴하고 해제합니다.
        /// </summary>
        public UnloadChunkMode unloadFarChunksMode = UnloadChunkMode.ToggleVisibility;

        /// <summary>
        /// 가시 거리를 벗어났을 때 청크 navMesh를 재사용할 수 있습니다.
        /// </summary>
        public bool unloadFarNavMesh;

        /// <summary>
        /// 거리가 계산되는 곳입니다.일반적으로 이는 카메라(1인칭 보기) 또는 캐릭터(3인칭 보기)입니다.
        /// </summary>
        public Transform distanceAnchor;

        public bool adjustCameraFarClip = true;

        public bool usePixelLights = true;
        public bool enableShadows = true;
        public bool shadowsOnWater;
        public bool realisticWater;
        public long maxCPUTimePerFrame = 30;
        public int maxChunksPerFrame = 50;
        public int maxTreesPerFrame = 10;
        public int maxBushesPerFrame = 10;
        public bool multiThreadGeneration = true;
        public bool lowMemoryMode;
        public bool delayedInitialization;
        public bool onlyRenderInFrustum = true;

        public bool serverMode;
        public bool enableColliders = true;
        public bool enableNavMesh;
        public NavMeshResolution navMeshResolution = NavMeshResolution.Default;
        public bool hideChunksInHierarchy = true;

        public bool enableTrees = true;
        public bool denseTrees = true;
        public bool enableVegetation = true;

        public Light sun;
        [Range(0, 1)]
        public float fogAmount = 0.5f;
        public bool fogDistanceAuto = true;
        public float fogDistance = 300;
        [Range(0, 1)]
        public float fogFallOff = 0.8f;
        public Color fogTint = Color.white;
        public bool enableClouds = true;

        /// <summary>
        /// 활성화되면 마이크로 복셀 공간이 가장 가까운 마이크로 복셀에 맞춰집니다.
        /// </summary>
        [Tooltip("활성화되면 마이크로 복셀 공간이 가장 가까운 마이크로 복셀에 맞춰집니다.")]
        public bool microVoxelsSnap = true;

        /// <summary>
        /// 기본 빌드 사운드.
        /// </summary>
        public AudioClip defaultBuildSound;

        /// <summary>
        /// 기본 픽업 사운드.
        /// </summary>
        public AudioClip defaultPickupSound;

        /// <summary>
        /// 기본 픽업 사운드.
        /// </summary>
        public AudioClip defaultDestructionSound;

        /// <summary>
        /// 기본 충격/타격 소리.
        /// </summary>
        public AudioClip defaultImpactSound;

        [Tooltip("복셀 정의가 없거나 위치에 직접 색상을 배치하는 경우 복셀로 가정합니다.")]
        public VoxelDefinition defaultVoxel;

        [Tooltip("지형 생성기가 할당하지 않은 경우 가정된 물 복셀 정의")]
        public VoxelDefinition defaultWaterVoxel;

        /// <summary>
        /// 원할 때까지 초기 로딩 화면을 강제로 유지하는 데 사용할 수 있습니다.
        /// </summary>
        public static bool canHideInitialLoadingScreen = true;


        // far chunks

        public bool enableFarChunksRendering;
        public bool farChunksShadows = true;
        [Range(0, 1)]
        public float farChunksShadowIntensity = 0.8f;
        public bool farChunksWaterReflections = true;
        [Range(0, 1)]
        public float farChunksWaterReflectionsIntensity = 0.5f;
        public bool farChunksWaterColorOverride;
        public Color farChunksWaterColor = new Color(0.247f, 0.396f, 0.745f, 0.87f);
        [ColorUsage(showAlpha: false)]
        public Color farChunksShoreColor = Color.white;
        [Tooltip("수위 아래 추적")]
        public bool farChunksDeepWater = true;

        #region Public useful state fields

        bool _cameraHasMoved;
        public bool cameraHasMoved {
            get { return _cameraHasMoved || _notifyCameraMove; }
        }

        bool _notifyCameraMove;
        /// <summary>
        /// 카메라 위치가 변경되었음을 시스템에 알립니다.
        /// 보이는 청크와 환경 조명을 강제로 업데이트합니다.
        /// </summary>
        public void NotifyCameraMove () {
            _notifyCameraMove = true;
            _cameraHasMoved = true;
        }

        /// <summary>
        /// 새로운 메인 카메라 위치를 설정하고 위치 변경을 시스템에 알립니다.
        /// </summary>
        public void MoveMainCameraTo (Vector3 position) {
            if (cameraMain == null) return;
            cameraMain.transform.position = position;
            NotifyCameraMove();
        }


        [NonSerialized]
        public int chunksCreated, chunksUsed, chunksInRenderQueueCount, chunksDrawn;
        [NonSerialized]
        public int voxelsCreatedCount;
        [NonSerialized]
        public int treesInCreationQueueCount, treesCreated;
        [NonSerialized]
        public int vegetationInCreationQueueCount, vegetationCreated;

        #endregion

        #region Public Events

        /// <summary>
        /// 이벤트 전달을 허용합니다.CaptureEvents가 false로 설정되면 API에서 이벤트가 발생하지 않습니다.
        /// </summary>
        [NonSerialized]
        public bool captureEvents = true;

        /// <summary>
        /// 청크 변경 알림을 허용합니다.informChunkChanges가 false로 설정되면 API에서 청크 변경 이벤트가 발생하지 않습니다.
        /// </summary>
        public bool captureChunkChanges {
            get { return VoxelChunk.markModifiedChunks; }
            set { VoxelChunk.markModifiedChunks = value; }
        }

        /// <summary>
        /// 저장된 게임이 로드된 후 트리거됨
        /// </summary>
        public event VoxelPlayEvent OnGameLoaded;

        /// <summary>
        /// 월드가 초기화되고 모든 시스템이 생성되기 직전에 트리거됩니다.로드되기 전에 월드 시드를 설정하거나 다른 설정을 변경하는 데 유용합니다.
        /// </summary>
        public event VoxelPlayEvent OnBeforeWorldLoad;

        /// <summary>
        /// 월드 로딩이 완료되면 트리거됩니다.이는 저장 파일 게임이 로드되었고 월드가 로드되었으며 엔진이 유휴 상태임을 의미합니다.
        /// </summary>
        public event VoxelPlayEvent OnWorldLoaded;

        /// <summary>
        /// Voxel Play가 항목 로드 및 초기화를 마친 후 트리거됩니다.
        /// </summary>
        public event VoxelPlayEvent OnInitialized;

        /// <summary>
        /// Voxel Play Environment의 일부 설정이 변경되면 트리거됩니다.
        /// </summary>
        public event VoxelPlayEvent OnSettingsChanged;

        /// <summary>
        /// 복셀이 피해를 받은 후 트리거됨
        /// </summary>
        public event VoxelHitEvent OnVoxelDamaged;

        /// <summary>
        /// 복셀이 손상을 받은 후 트리거됩니다(전체 HitInfo 데이터 제공).
        /// </summary>
        public event VoxelHitInfoEvent OnVoxelDamagedHitInfo;

        /// <summary>
        /// 복셀이 피해를 받은 후 트리거됨
        /// </summary>
        public event VoxelHitAfterEvent OnVoxelAfterDamaged;

        /// <summary>
        /// 복셀이 손상을 받은 후 트리거됩니다(전체 HitInfo 데이터 제공).
        /// </summary>
        public event VoxelHitInfoAfterEvent OnVoxelAfterDamagedHitInfo;

        /// <summary>
        /// 지역 피해가 발생하기 전에 발동됩니다.이벤트 핸들러에서 수정될 수 있는 잠재적으로 영향을 받는 복셀 목록을 전달합니다.
        /// </summary>
        public event VoxelHitsEvent OnVoxelBeforeAreaDamage;

        /// <summary>
        /// 지역 피해가 발생하기 전에 발동됩니다.영향을 받는 복셀 지수 목록을 전달합니다.
        /// </summary>
        public event VoxelHitsEvent OnVoxelAfterAreaDamage;

        /// <summary>
        /// 복셀이 파괴되기 직전에 트리거됨
        /// </summary>
        public event VoxelEvent OnVoxelBeforeDestroyed;

        /// <summary>
        /// 복셀이 파괴된 후 트리거됨
        /// </summary>
        public event VoxelEvent OnVoxelDestroyed;

        /// <summary>
        /// 복셀이 배치되기 직전에 트리거됩니다.작업을 취소하려면 voxelType을 null로 설정하면 됩니다.
        /// </summary>
        public event VoxelPlaceEvent OnVoxelBeforePlace;

        /// <summary>
        /// 청크가 새로 고쳐지기 전에 복셀이 배치된 직후에 트리거됩니다.
        /// </summary>
        public event VoxelPositionEvent OnVoxelAfterPlace;

        /// <summary>
        /// 복구 가능한 복셀이 생성되기 직전에 트리거됩니다.
        /// </summary>
        public event VoxelDropItemEvent OnVoxelBeforeDropItem;

        /// <summary>
        /// 복셀을 클릭하면 트리거됩니다.
        /// </summary>
        public event VoxelClickEvent OnVoxelClick;

        /// <summary>
        /// 횃불을 배치한 후 트리거됨
        /// </summary>
        public event VoxelTorchEvent OnTorchAttached;

        /// <summary>
        /// 토치가 제거된 후 트리거됨
        /// </summary>
        public event VoxelTorchEvent OnTorchDetached;

        /// <summary>
        /// 청크의 내용이 변경된 후(예: 새 복셀 배치) 트리거됩니다.
        /// </summary>
        public event VoxelChunkEvent OnChunkChanged;

        /// <summary>
        /// 청크가 언로드될 때 트리거됩니다(작업을 거부하려면 canUnload 인수를 사용하십시오).
        /// </summary>
        public event VoxelChunkUnloadEvent OnChunkReuse;

        /// <summary>
        /// 청크가 기본 콘텐츠(지형 등)로 채워지기 직전에 트리거됩니다.
        /// 자신만의 콘텐츠로 복셀 배열을 채우려면 overrideDefaultContents를 설정하세요.
        /// </summary>
        public event VoxelChunkBeforeCreationEvent OnChunkBeforeCreate;

        /// <summary>
        /// 청크에 대한 세부 생성기를 호출하기 직전에 트리거됨
        /// </summary>
        public event VoxelChunkBeforeDetailGenerationEvent OnChunkBeforeDetailGeneration;

        /// <summary>
        /// 청크가 기본 콘텐츠(지형 등)로 채워진 직후에 트리거됩니다.
        /// </summary>
        public event VoxelChunkEvent OnChunkAfterCreate;

        /// <summary>
        /// 청크가 처음으로 렌더링된 직후에 트리거됩니다.
        /// </summary>
        public event VoxelChunkEvent OnChunkAfterFirstRender;

        /// <summary>
        /// 청크 메시가 새로 고쳐질 때 트리거됩니다(업데이트되어 GPU에 업로드됨).
        /// </summary>
        public event VoxelChunkEvent OnChunkRender;

        /// <summary>
        /// 모델 구축이 시작되면 트리거됩니다.
        /// </summary>
        public event VoxelModelBuildStartEvent OnModelBuildStart;

        /// <summary>
        /// 모델 구축이 종료되면 트리거됩니다.
        /// </summary>
        public event VoxelModelBuildEndEvent OnModelBuildEnd;

        /// <summary>
        /// seeThroughClearRoofYPos가 변경되면 트리거됩니다.
        /// </summary>
        public event VoxelPlayEvent OnSeeThroughHeightOffsetChanged;

        /// <summary>
        /// 청크가 더 이상 가시 거리 내에 있지 않을 때 트리거됩니다.이 이벤트를 사용하여 청크 게임 객체를 비활성화할 수 있습니다.
        /// </summary>
        public event VoxelChunkEvent OnChunkExitVisibleDistance;

        /// <summary>
        /// 청크가 가시 거리에 들어갈 때 트리거됩니다.이 이벤트를 사용하여 청크 게임 개체를 다시 활성화할 수 있습니다.
        /// </summary>
        public event VoxelChunkEvent OnChunkEnterVisibleDistance;

        /// <summary>
        /// 앵커가 다른 청크에 들어갈 때 발생합니다(앵커는 VoxelPlayEnvironment 인스펙터의 DistanceAnchor 속성에 의해 참조되는 개체를 나타냄).
        /// </summary>
        public event VoxelPlayEvent OnPlayerEnterChunk;

        /// <summary>
        /// 트리가 생성될 때 트리거됩니다.해당 트리 생성을 취소하려면 false를 반환합니다.
        /// </summary>
        public event TreeBeforeCreateEvent OnTreeBeforeCreate;

        /// <summary>
        /// 트리가 생성된 후 트리거됩니다.위치가 수정된 VoxelIndex 요소 목록을 수신합니다.
        /// </summary>
        public event TreeAfterCreateEvent OnTreeAfterCreate;

        /// <summary>
        /// 하나 이상의 복셀이 붕괴되어 떨어질 때 트리거됩니다.
        /// </summary>
        public event VoxelCollapseEvent OnVoxelCollapse;

        /// <summary>
        /// 원점 이동이 발생하기 직전에 트리거됩니다.이벤트 핸들러는 원점 이동 작업을 취소하기 위해 false를 반환할 수 있습니다.
        /// </summary>
        public event OriginShiftPreEvent OnOriginPreShift;

        /// <summary>
        /// 원점 이동 직후에 발생
        /// </summary>
        public event OriginShiftPostEvent OnOriginPostShift;

        #endregion



        #region Public API

        static VoxelPlayEnvironment _instance;

        /// <summary>
        /// 세부 생성기를 활성화/비활성화합니다.
        /// </summary>
        public bool enableDetailGenerators = true;

        /// <summary>
        /// 이 세션의 타임스탬프입니다.저장된 게임에서 로드되었습니다. 시작 시 0입니다.저장/로드 중 청크의 증분 변화를 감지하는 데 사용됩니다.
        /// </summary>
        public static int stage;

        /// <summary>
        /// Voxel Play API의 싱글톤 인스턴스를 반환합니다.
        /// </summary>
        /// <value>인스턴스.</value>
        public static VoxelPlayEnvironment instance {
            get {
                if (_instance == null) {
                    _instance = Misc.FindObjectOfType<VoxelPlayEnvironment>(true);
                }
                return _instance;
            }
        }

        /// <summary>
        /// 투명 복셀의 광량에 대한 기본값입니다.전역 조명이 활성화된 경우 이 값은 0(어두움)입니다.비활성화된 경우 이 값은 15입니다(복셀을 어둡게 하지 않음).
        /// </summary>
        /// <value>빛이 없는 값입니다.</value>
        public byte noLightValue {
            get {
                return effectiveGlobalIllumination ? FULL_DARK : FULL_LIGHT;
            }
        }

        /// <summary>
        /// 플레이어의 GameObject를 가져옵니다.
        /// </summary>
        /// <value>플레이어 게임 개체입니다.</value>
        public GameObject playerGameObject {
            get {
                if ((UnityEngine.Object)characterController != null) {
                    return characterController.gameObject;
                } else if (cameraMain != null) {
                    return cameraMain.gameObject;
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// 모든 것을 파괴하고 현재 할당된 세계를 다시 로드합니다.
        /// </summary>
        /// <param name="keepWorldChanges">If set to <c>진실</c> any change to chunks will be preserved.</param>
        public void ReloadWorld (bool keepWorldChanges = true) {
            if (!applicationIsPlaying && !renderInEditor)
                return;

            byte[] changes = null;
            if (cachedChunks == null) {
                keepWorldChanges = false;
            }
            if (keepWorldChanges) {
                changes = SaveGameToByteArray();
            }
            LoadWorldInt();
            if (keepWorldChanges) {
                LoadGameFromByteArray(changes, true, false);
            }
            SetInitialized();
            DoWork();
            // Refresh behaviours lighting
            VoxelPlayBehaviour[] bh = Misc.FindObjectsOfType<VoxelPlayBehaviour>();
            for (int k = 0; k < bh.Length; k++) {
                bh[k].UpdateLighting();
            }
        }


        /// <summary>
        /// 월드의 모든 청크를 지우고 모든 구조를 초기화합니다.
        /// </summary>
        public void DestroyAllVoxels () {
            LoadWorldInt();
            WarmChunks(null);
            SetInitialized();
        }


        /// <summary>
        /// 모든 청크에 다시 그리기 명령을 실행합니다.
        /// </summary>
        public void Redraw (bool reloadWorldTextures = false) {
            if (reloadWorldTextures) {
                // reset only texture providers; keep rendering materials and other stuff for speed purposes
                foreach (TextureArrayPacker tap in texturesProviders.Values) {
                    tap.Clear();
                }
                LoadWorldTextures();
            }
            UpdateMaterialProperties();
            if (cachedChunks != null) {
                foreach (KeyValuePair<int, CachedChunk> kv in cachedChunks) {
                    CachedChunk cc = kv.Value;
                    if (cc != null && cc.chunk != null) {
                        ChunkRequestRefresh(cc.chunk, true, true);
                    }
                }
            }
        }

        /// <summary>
        /// 청크 가시성을 켜거나 끕니다.
        /// </summary>
        public bool ChunksToggle () {
            if (chunksRoot != null) {
                chunksRoot.gameObject.SetActive(!chunksRoot.gameObject.activeSelf);
                return chunksRoot.gameObject.activeSelf;
            }
            return false;
        }

        /// <summary>
        /// 청크 가시성을 전환합니다.
        /// </summary>
        public void ChunksToggle (bool visible) {
            if (chunksRoot != null) {
                chunksRoot.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 렌더링된 모든 청크를 일반 게임 객체로 내보냅니다.
        /// </summary>
        public void ChunksExport () {
            ChunksExportAll();
            renderInEditor = false;
            if (cachedChunks != null) {
                cachedChunks.Clear();
            }
            keepTexturesOnDestroy = true;
            DisposeAll(removeVoxelPlayEnvironment: true);
        }

        /// <summary>
        /// 광선을 발사하고 해당 방향으로 영향을 받는 모든 복셀에 피해를 줍니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was hit, <c>거짓</c> otherwise.</returns>
        /// <param name="ray">레이.</param>
        /// <param name="damage">손상.</param>
        /// <param name="maxDistance">광선의 최대 거리.</param>
        /// <param name="addParticles">충격 입자 추가</param>
        /// <param name="damageRadius">복셀 단위의 손상 반경</param>
        /// <param name="microVoxels">전체 복셀 대신 마이크로복셀에 도달합니다.</param>
        public bool RayHit (Rayd ray, int damage, float maxDistance = 0, int damageRadius = 1, bool addParticles = true, bool playSound = true, int microVoxels = 0, float microVoxelDestroyProb = 1f) {
            return RayHit(ray.origin, ray.direction, damage, maxDistance, damageRadius, addParticles, playSound, microVoxels: microVoxels, microVoxelDestroyProb: microVoxelDestroyProb);
        }

        /// <summary>
        /// 광선을 발사하고 해당 방향으로 영향을 받는 모든 복셀에 피해를 줍니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was hit, <c>거짓</c> otherwise.</returns>
        /// <param name="ray">레이.</param>
        /// <param name="damage">손상.</param>
        /// <param name="hitInfo">추가 세부정보가 포함된 VoxelHitInfo 구조입니다.</param>
        /// <param name="maxDistance">광선의 최대 거리.</param>
        /// <param name="microVoxels">전체 복셀 대신 마이크로복셀에 도달합니다.</param>
        public bool RayHit (Rayd ray, int damage, out VoxelHitInfo hitInfo, float maxDistance = 0, int damageRadius = 1, bool addParticles = true, bool playSound = true, int microVoxels = 0, float microVoxelDestroyProb = 1f) {
            return RayHit(ray.origin, ray.direction, damage, out hitInfo, maxDistance, damageRadius, addParticles: addParticles, playSound: playSound, microVoxels: microVoxels, microVoxelDestroyProb: microVoxelDestroyProb);
        }

        /// <summary>
        /// 광선을 발사하고 해당 방향으로 영향을 받는 모든 복셀에 피해를 줍니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was hit, <c>거짓</c> otherwise.</returns>
        /// <param name="damage">손상.</param>
        /// <param name="maxDistance">광선의 최대 거리.</param>
        /// <param name="microVoxels">전체 복셀 대신 마이크로복셀에 도달합니다.</param>
        public bool RayHit (Vector3d origin, Vector3 direction, int damage, float maxDistance = 0, int damageRadius = 1, bool addParticles = true, bool playSound = true, int microVoxels = 0, float microVoxelDestroyProb = 1f) {
            bool impact = HitVoxelFast(origin, direction, damage, out _, maxDistance, damageRadius, addParticles, playSound, microVoxels: microVoxels, microVoxelDestroyProb: microVoxelDestroyProb);
            return impact;

        }

        /// <summary>
        /// 광선을 발사하고 해당 방향으로 영향을 받는 모든 복셀에 피해를 줍니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was hit, <c>거짓</c> otherwise.</returns>
        /// <param name="damage">손상.</param>
        /// <param name="hitInfo">추가 세부정보가 포함된 VoxelHitInfo 구조입니다.</param>
        /// <param name="maxDistance">광선의 최대 거리.</param>
        /// <param name="microVoxels">전체 복셀 대신 마이크로복셀에 도달합니다.</param>
        public bool RayHit (Vector3d origin, Vector3 direction, int damage, out VoxelHitInfo hitInfo, float maxDistance = 0, int damageRadius = 1, int layerMask = -1, bool addParticles = true, bool playSound = true, int microVoxels = 0, float microVoxelDestroyProb = 1f) {
            return HitVoxelFast(origin, direction, damage, out hitInfo, maxDistance, damageRadius, layerMask: layerMask, addParticles: addParticles, playSound: playSound, microVoxels: microVoxels, microVoxelDestroyProb: microVoxelDestroyProb);
        }


        /// <summary>
        /// 광선의 원점에서 광선 방향으로 광선투사합니다.
        /// </summary>
        /// <returns><c>진실</c>, if a voxel was hit, <c>거짓</c> otherwise.</returns>
        /// <param name="hitInfo">히트 정보.</param>
        /// <param name="maxDistance">최대 거리.</param>
        /// <param name="minOpaque">선택적으로 특정 불투명 요소(15 = 고체/완전 불투명, 3 = 컷아웃, 2 = 물, 0 = 잔디)가 있는 복셀로 레이히트를 제한합니다.</param>
        /// <param name="colliderTypes">선택적으로 사용할 수 있는 충돌체를 지정합니다.</param>
        /// <param name="layerMask">콜라이더 기반(비복셀) 객체를 필터링하는 선택적 레이어 마스크</param>
        /// <param name="microVoxel">마이크로 복셀 적중 검사를 수행합니다.</param>
        public bool RayCast (Rayd ray, out VoxelHitInfo hitInfo, float maxDistance = 0, int minOpaque = 0, ColliderTypes colliderTypes = ColliderTypes.AnyCollider, int layerMask = -1, bool createChunksIfNeeded = false, bool microVoxels = false, bool ignoreWater = false) {
            return RayCastFast(ray.origin, ray.direction, out hitInfo, maxDistance, createChunksIfNeeded, (byte)minOpaque, colliderTypes, layerMask, useMicroVoxels: microVoxels, ignoreWater: ignoreWater);
        }

        /// <summary>
        /// 주어진 원점과 방향에서 레이캐스트합니다.
        /// </summary>
        /// <returns><c>진실</c>, if a voxel was hit, <c>거짓</c> otherwise.</returns>
        /// <param name="ray">레이.</param>
        /// <param name="hitInfo">히트 정보.</param>
        /// <param name="maxDistance">최대 거리.</param>
        /// <param name="minOpaque">선택적으로 특정 불투명 요소(15 = 고체/완전 불투명, 3 = 컷아웃, 2 = 물, 0 = 잔디)가 있는 복셀로 레이히트를 제한합니다.</param>
        /// <param name="colliderTypes">선택적으로 사용할 수 있는 충돌체를 지정합니다.</param>
        /// <param name="layerMask">콜라이더 기반(비복셀) 객체를 필터링하는 선택적 레이어 마스크</param>
        /// <param name="microVoxel">마이크로 복셀 적중 검사를 수행합니다.</param>
        public bool RayCast (Vector3d origin, Vector3 direction, out VoxelHitInfo hitInfo, float maxDistance = 0, int minOpaque = 0, ColliderTypes colliderTypes = ColliderTypes.AnyCollider, int layerMask = -1, bool createChunksIfNeeded = false, bool microVoxels = false, bool ignoreWater = false) {
            return RayCastFast(origin, direction, out hitInfo, maxDistance, createChunksIfNeeded, (byte)minOpaque, colliderTypes, layerMask, useMicroVoxels: microVoxels, ignoreWater: ignoreWater);
        }

        /// <summary>
        /// startPosition과 endPosition 사이에 있는 모든 복셀을 반환합니다.보이지 않는 복셀도 반환됩니다.VoxelGetHidden을 사용하여 복셀이 표시되는지 여부를 확인합니다.
        /// </summary>
        /// <returns>출연진.</returns>
        /// <param name="startPosition">시작 위치.</param>
        /// <param name="endPosition">끝 위치.</param>
        /// <param name="indices">지수.</param>
        /// <param name="startIndex">인덱스 배열의 시작 인덱스입니다.</param>
        /// <param name="minOpaque">최소 복셀 불투명.일반 고체 복셀의 불투명도는 15입니다. 나무 잎의 불투명도는 3입니다. 물과 투명 복셀의 불투명도는 2입니다.</param>
        public int LineCast (Vector3d startPosition, Vector3d endPosition, VoxelIndex[] indices, int startIndex = 0, int minOpaque = 0) {
            return LineCastFastVoxel(startPosition, endPosition, indices, startIndex, (byte)minOpaque);
        }


        /// <summary>
        /// startPosition과 endPosition 사이에 있는 모든 청크를 반환합니다.
        /// </summary>
        /// <returns>출연진.</returns>
        /// <param name="startPosition">시작 위치.</param>
        /// <param name="endPosition">끝 위치.</param>
        /// <param name="chunks">청크 배열.</param>
        /// <param name="startIndex">인덱스 배열의 시작 인덱스입니다.</param>
        public int LineCast (Vector3d startPosition, Vector3d endPosition, VoxelChunk[] chunks, int startIndex = 0) {
            return LineCastFastChunk(startPosition, endPosition, chunks, startIndex);
        }


        /// <summary>
        /// 주어진 위치에서 가장 높은 기존 복셀 위치를 가져옵니다.
        /// </summary>
        public float GetHeight (Vector3d position, byte minOpaque = 0, HashSet<int> allowedVoxelDefinitions = null) {
            float maxAltitude = (object)world.terrainGenerator != null ? world.terrainGenerator.maxHeight : 255;
            float minAltitude = (object)world.terrainGenerator != null ? world.terrainGenerator.minHeight : -255;
            float maxDistance = maxAltitude - minAltitude + 1;
            position.y = maxAltitude;
            if (!RayCastFast(position, Misc.vector3down, out VoxelHitInfo hitInfo, maxDistance, false, minOpaque, ColliderTypes.OnlyVoxels, allowedVoxelDefinitions: allowedVoxelDefinitions)) {
                hitInfo.point.y = float.MinValue;
            }
            return (float)hitInfo.point.y;
        }


        /// <summary>
        /// 주어진 위치 위의 가장 높은 기존 복셀을 가져옵니다.
        /// </summary>
        public float GetTopMostHeight (Vector3d position, float maxDistance = 128) {
            if (!RayCastFast(position, Misc.vector3up, out VoxelHitInfo hitInfo, maxDistance, false, 0, ColliderTypes.OnlyVoxels)) {
                hitInfo.point.y = float.MinValue;
            }
            return (float)hitInfo.point.y;
        }


        /// <summary>
        /// 플레이어가 위치한 복셀 청크를 반환합니다.
        /// </summary>
        public VoxelChunk GetCurrentChunk () {
            if (cameraMain == null)
                return null;
            GetChunkOrCreate(currentAnchorPos, out VoxelChunk chunk);
            return chunk;
        }


        /// </summary>
        /// <returns><c>진실</c> if this instance is water at position  (only X/Z values are considered); otherwise, <c>거짓</c>.</returns>
        public bool IsWaterAtPosition (Vector3d position) {
            return IsWaterAtPosition(position.x, position.z);
        }

        /// <summary>
        /// 복셀 인덱스가 청크 가장자리의 복셀 위치에 해당하는 경우 true를 반환합니다.
        /// </summary>
        /// <param name="voxelIndex"></param>
        public bool IsVoxelAtChunkEdge (int voxelIndex) {
            int bx = voxelIndex & VOXELINDEX_X_EDGE_BITWISE;
            int bz = voxelIndex & VOXELINDEX_Z_EDGE_BITWISE;
            int by = voxelIndex & VOXELINDEX_Y_EDGE_BITWISE;
            return (bx == 0 || bx == VOXELINDEX_X_EDGE_BITWISE || by == 0 || by == VOXELINDEX_Y_EDGE_BITWISE || bz == 0 || bz == VOXELINDEX_Z_EDGE_BITWISE);
        }

        /// <summary>
        /// 주어진 위치(x/z)에서 물이 발견되면 true를 반환합니다.
        /// </summary>
        /// <returns><c>진실</c> if this instance is water at position; otherwise, <c>거짓</c>.</returns>
        public bool IsWaterAtPosition (double x, double z) {
            if (heightMapCache == null)
                return false;
            float groundLevel = GetHeightMapInfoFast(x, z).groundLevel;
            if (waterLevel > groundLevel) {
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// 주어진 위치에서 물의 깊이를 반환합니다.
        /// </summary>
        public float GetWaterDepth (Vector3d position) {
            if (heightMapCache == null)
                return 0;
            float groundLevel = GetHeightMapInfoFast(position.x, position.z).groundLevel;
            if (waterLevel > groundLevel) {
                return waterLevel - groundLevel;
            } else {
                return 0;
            }
        }


        /// <summary>
        /// 특정 위치에서 플러딩 시작
        /// </summary>
        /// <param name="position">위치.</param>
        public void AddWaterFlood (Vector3d position, VoxelDefinition waterVoxel, int lifeTime = 24) {
            if (enableWaterFlood && lifeTime > 0 && waterVoxel != null) {
                waterFloodSources.Add(ref position, waterVoxel, lifeTime);
            }
        }

        [NonSerialized]
        public bool enableWaterFlood = true;

        /// <summary>
        /// 특정 위치에 고체 블록이 있으면 true를 반환합니다.
        /// </summary>
        /// <returns><c>진실</c> if this instance is occupied by a solid voxel it returns true; otherwise, <c>거짓</c>.</returns>
        public bool IsWallAtPosition (Vector3d position) {
            VoxelChunk chunk;
            int voxelIndex;
            if (GetVoxelIndex(position, out chunk, out voxelIndex)) {
                return chunk.voxels[voxelIndex].opaque == FULL_OPAQUE;
            }
            return false;
        }


        /// <summary>
        /// 해당 위치의 지형이 렌더링되고 충돌체가 있는 경우 true를 반환합니다.
        /// </summary>
        public bool IsTerrainReadyAtPosition (Vector3d position, bool includeWater) {
            float height = GetTerrainHeight(position, includeWater);
            position.y = height;
            VoxelChunk chunk = GetChunk(position, false);
            if ((object)chunk != null) {
                return enableColliders ? chunk.hasColliderMesh : chunk.isRendered;
            }
            return false;
        }

        /// <summary>
        /// 이 위치에 복셀이 있으면 true를 반환합니다.
        /// </summary>
        public bool IsVoxelAtPosition (Vector3d position) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) {
                return chunk.voxels[voxelIndex].hasContent;
            }
            return false;
        }


        /// <summary>
        /// 위치가 비어 있으면(복셀 없음) true를 반환합니다.
        /// </summary>
        public bool IsEmptyAtPosition (Vector3d position) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) {
                return chunk.voxels[voxelIndex].isEmpty;
            }
            return false;
        }

        /// <summary>
        /// 위치가 불투명 복셀로 채워지면 true를 반환합니다.
        /// </summary>
        public bool IsSolidAtPosition (Vector3d position) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) {
                return chunk.voxels[voxelIndex].isSolid;
            }
            return false;
        }

        /// <summary>
        /// 주어진 위치의 청크를 반환합니다.
        /// </summary>
        /// <returns><c>진실</c>, if chunk was gotten, <c>거짓</c> otherwise.</returns>
        /// <param name="position">위치.</param>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="forceCreation">If set to <c>진실</c> force creation.</param>
        public bool GetChunk (Vector3d position, out VoxelChunk chunk, bool forceCreation = false) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            return GetChunkFast(chunkX, chunkY, chunkZ, out chunk, forceCreation);
        }


        /// <summary>
        /// 주어진 위치의 청크를 반환합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="forceCreation">If set to <c>진실</c> force creation.</param>
        public VoxelChunk GetChunk (Vector3d position, bool forceCreation = false) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            GetChunkFast(chunkX, chunkY, chunkZ, out VoxelChunk chunk, forceCreation);
            return chunk;
        }


        /// <summary>
        /// Returns a list of created chunks. 
        /// </summary>
        /// <param name="chunks">청크 반환을 위해 사용자가 제공한 목록입니다.</param>
        /// <param name="modifiedFilter">수정된 플래그로 반환된 청크를 필터링합니다.</param>
        public void GetChunks (List<VoxelChunk> chunks, ChunkModifiedFilter modifiedFilter = ChunkModifiedFilter.All) {
            chunks.Clear();
            for (int k = 0; k < chunksPoolLoadIndex; k++) {
                VoxelChunk chunk = chunksPool[k];
                if (!chunk.isPopulated) continue;
                if (modifiedFilter == ChunkModifiedFilter.All ||
                    (modifiedFilter == ChunkModifiedFilter.OnlyModified && chunk.modified) ||
                    (modifiedFilter == ChunkModifiedFilter.ModifiedThisSession && chunk.modified && chunk.modifiedTimestamp > stage) ||
                    (modifiedFilter == ChunkModifiedFilter.NonModified && !chunk.modified)) {
                    chunks.Add(chunk);
                }
            }
        }

        /// <summary>
        /// Returns a list of created chunks. 
        /// </summary>
        /// <param name="modifiedFilter">수정된 플래그로 반환된 청크를 필터링합니다.</param>
        public List<VoxelChunk> GetChunks (ChunkModifiedFilter modifiedFilter = ChunkModifiedFilter.All) {
            List<VoxelChunk> chunks = new List<VoxelChunk>();
            GetChunks(chunks, modifiedFilter);
            return chunks;
        }

        /// <summary>
        /// 현재 보이는 청크를 둘러싸는 경계를 반환합니다.
        /// </summary>
        public Boundsd GetChunksBounds () {
            Boundsd bounds = new Boundsd();
            Boundsd chunkBounds = new Boundsd();
            bool first = true;
            for (int k = 0; k < chunksPoolLoadIndex; k++) {
                VoxelChunk chunk = chunksPool[k];
                if (chunk.isPopulated) {
                    chunkBounds.center = chunk.position;
                    if (first) {
                        bounds = chunkBounds;
                        first = false;
                    } else {
                        bounds.Encapsulate(chunkBounds);
                    }
                }
            }
            return bounds;
        }

        /// <summary>
        /// 월드 공간에서 주어진 청크의 경계를 반환합니다.
        /// </summary>
        public Boundsd GetChunkBounds (VoxelChunk chunk) {
            Vector3d center = chunk.position;
            Vector3d min = center - CHUNK_HALF_SIZE_VECTOR;
            Vector3d max = center + CHUNK_HALF_SIZE_VECTOR;
            return new Boundsd((max + min) * 0.5, max - min);
        }


        /// <summary>
        /// 특정 볼륨에 있는 모든 기존 청크를 반환합니다.
        /// </summary>
        public void GetChunks (Boundsd bounds, List<VoxelChunk> chunks, ChunkModifiedFilter modifiedFilter = ChunkModifiedFilter.All) {
            Vector3d position;
            Vector3d min = bounds.min;
            FastMath.FloorToInt(min.x / CHUNK_SIZE, min.y / CHUNK_SIZE, min.z / CHUNK_SIZE, out int xmin, out int ymin, out int zmin);
            xmin *= CHUNK_SIZE;
            ymin *= CHUNK_SIZE;
            zmin *= CHUNK_SIZE;
            Vector3d max = bounds.max;
            FastMath.FloorToInt(max.x / CHUNK_SIZE, max.y / CHUNK_SIZE, max.z / CHUNK_SIZE, out int xmax, out int ymax, out int zmax);
            xmax *= CHUNK_SIZE;
            ymax *= CHUNK_SIZE;
            zmax *= CHUNK_SIZE;

            chunks.Clear();
            for (int y = ymax; y >= ymin; y -= CHUNK_SIZE) {
                position.y = y;
                for (int z = zmin; z <= zmax; z += CHUNK_SIZE) {
                    position.z = z;
                    for (int x = xmin; x <= xmax; x += CHUNK_SIZE) {
                        position.x = x;
                        if (GetChunk(position, out VoxelChunk chunk, false)) {
                            if (modifiedFilter == ChunkModifiedFilter.All || chunk.modified) {
                                chunks.Add(chunk);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 주어진 범위 내의 모든 청크의 위치를 ​​반환합니다.
        /// </summary>
        public void GetChunksPositions (Boundsd bounds, List<Vector3d> chunksPositions) {
            Vector3d min = bounds.min;
            FastMath.FloorToInt(min.x / CHUNK_SIZE, min.y / CHUNK_SIZE, min.z / CHUNK_SIZE, out int xmin, out int ymin, out int zmin);
            xmin = xmin * CHUNK_SIZE + CHUNK_HALF_SIZE;
            ymin = ymin * CHUNK_SIZE + CHUNK_HALF_SIZE;
            zmin = zmin * CHUNK_SIZE + CHUNK_HALF_SIZE;
            Vector3d max = bounds.max;
            FastMath.FloorToInt(max.x / CHUNK_SIZE, max.y / CHUNK_SIZE, max.z / CHUNK_SIZE, out int xmax, out int ymax, out int zmax);
            xmax = xmax * CHUNK_SIZE + CHUNK_HALF_SIZE;
            ymax = ymax * CHUNK_SIZE + CHUNK_HALF_SIZE;
            zmax = zmax * CHUNK_SIZE + CHUNK_HALF_SIZE;

            chunksPositions.Clear();
            for (int y = ymax; y >= ymin; y -= CHUNK_SIZE) {
                for (int z = zmin; z <= zmax; z += CHUNK_SIZE) {
                    for (int x = xmin; x <= xmax; x += CHUNK_SIZE) {
                        chunksPositions.Add(new Vector3d(x, y, z));
                    }
                }
            }
        }


        /// <summary>
        /// 지형 생성기를 호출하지 않고 주어진 위치의 청크를 반환합니다(청크는 비어 있어야 하지만 렌더링되기 전에 확보된 경우에만 해당).
        /// Chunk.isPopulated를 사용하여 지형이 이 청크로 렌더링되었는지 여부를 쿼리할 수 있습니다.
        /// </summary>
        /// <returns><c>진실</c>, if chunk was gotten, <c>거짓</c> otherwise.</returns>
        /// <param name="position">위치.</param>
        public VoxelChunk GetChunkUnpopulated (Vector3d position) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            STAGE = 201;
            GetChunkFast(chunkX, chunkY, chunkZ, out VoxelChunk chunk, false);
            if ((object)chunk == null) {
                STAGE = 202;
                int hash = GetChunkHash(chunkX, chunkY, chunkZ);
                chunk = CreateChunk(hash, chunkX, chunkY, chunkZ, true, false);
            }
            return chunk;
        }




        /// <summary>
        /// 지형 생성기를 호출하지 않고 주어진 위치의 청크를 반환합니다(청크는 비어 있어야 하지만 렌더링되기 전에 확보된 경우에만 해당).
        /// Chunk.isPopulated를 사용하여 지형이 이 청크로 렌더링되었는지 여부를 쿼리할 수 있습니다.
        /// </summary>
        /// <returns><c>진실</c>, if chunk was gotten, <c>거짓</c> otherwise.</returns>
        /// <param name="chunkX">청크의 X 위치 / 16. FastMath.FloorToInt(chunk.position.x/16)를 사용하세요.</param>
        /// <param name="chunkY">청크의 Y 위치 / 16.</param>
        /// <param name="chunkZ">청크의 Z 위치 / 16.</param>
        public VoxelChunk GetChunkUnpopulated (int chunkX, int chunkY, int chunkZ) {
            STAGE = 201;
            if (!GetChunkFast(chunkX, chunkY, chunkZ, out VoxelChunk chunk, false)) {
                STAGE = 202;
                int hash = GetChunkHash(chunkX, chunkY, chunkZ);
                chunk = CreateChunk(hash, chunkX, chunkY, chunkZ, true, false);
            }
            return chunk;
        }


        /// <summary>
        /// 주어진 위치를 둘러싸는 청크 위치를 반환합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        public Vector3d GetChunkPosition (Vector3d position) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int x, out int y, out int z);
            x = x * CHUNK_SIZE + CHUNK_HALF_SIZE;
            y = y * CHUNK_SIZE + CHUNK_HALF_SIZE;
            z = z * CHUNK_SIZE + CHUNK_HALF_SIZE;
            return new Vector3d(x, y, z);
        }


        /// <summary>
        /// 주어진 위치에서 복셀을 가져옵니다.복셀이 없으면 Voxel.Empty를 반환합니다.
        /// </summary>
        /// <returns>복셀.</returns>
        /// <param name="position">위치.</param>
        /// <param name="createChunkIfNotExists">If set to <c>진실</c> create chunk if not exists.</param>
        /// <param name="onlyRenderedVoxels">If set to <c>진실</c> the voxel will only be returned if it's rendered. If you're calling GetVoxel as part of a spawning logic, pass true as it will ensure the voxel returned also has the collider in place so your spawned stuff won't fall down.</param>
        public Voxel GetVoxel (Vector3d position, bool createChunkIfNotExists = true, bool onlyRenderedVoxels = false) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            VoxelChunk chunk;
            GetChunkFast(chunkX, chunkY, chunkZ, out chunk, createChunkIfNotExists);
            if (chunk != null && (!onlyRenderedVoxels || onlyRenderedVoxels && chunk.renderState == ChunkRenderState.RenderingComplete)) {
                Voxel[] voxels = chunk.voxels;
                int px = (int)(position.x - chunkX * CHUNK_SIZE);
                int py = (int)(position.y - chunkY * CHUNK_SIZE);
                int pz = (int)(position.z - chunkZ * CHUNK_SIZE);
                int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                return voxels[voxelIndex];
            }
            return Voxel.Empty;
        }

        /// <summary>
        /// boxMin 및 boxMax에 의해 정의된 볼륨 내에서 보이는 모든 복셀을 인덱스로 반환합니다.
        /// </summary>
        /// <returns>보이는 모든 복셀 인덱스의 수입니다.</returns>
        /// <param name="boxMin">둘러싸는 상자의 하단/왼쪽/뒤 ​​또는 최소 모서리.</param>
        /// <param name="boxMax">둘러싸는 상자의 상단/오른쪽/앞 또는 최대 모서리.</param>
        /// <param name="indices">쓰기 위해 사용자가 제공한 인덱스 목록입니다.</param>
        /// <param name="minOpaque">고려해야 할 복셀의 최소 불투명 값입니다.물의 불투명 요소는 2, 컷아웃 = 3, 잔디 = 0입니다.</param>
        /// <param name="hasContent">기존 복셀을 반환하는 기본값은 1입니다.복셀 없이 위치를 검색하려면 0을 전달합니다.이 필터를 무시하려면 -1을 전달합니다.</param>  
        public int GetVoxelIndices (Vector3d boxMin, Vector3d boxMax, List<VoxelIndex> indices, byte minOpaque = 0, int hasContent = 1) {
            Vector3d chunkPos, voxelPosition;
            VoxelIndex index = new VoxelIndex();
            indices.Clear();

            FastVector.Floor(ref boxMin);
            Vector3d chunkMinPos = GetChunkPosition(boxMin);
            Vector3d chunkMaxPos = GetChunkPosition(boxMax);
            FastVector.Floor(ref boxMax);
            boxMax += Vector3d.one;
            Vector3d center = (boxMax - boxMin) * 0.5;

            bool createChunkIfNotExists = hasContent != 1;

            if (hasContent == 0) {
                minOpaque = 0;
            }

            bool mustHaveContent = hasContent == 1;
            for (double y = chunkMinPos.y; y <= chunkMaxPos.y; y += CHUNK_SIZE) {
                chunkPos.y = y;
                int voxelIndexMin = 0;
                if (y == chunkMinPos.y) {
                    int optimalMin = (int)(boxMin.y - (chunkMinPos.y - CHUNK_HALF_SIZE)) * ONE_Y_ROW;
                    if (optimalMin > voxelIndexMin) {
                        voxelIndexMin = optimalMin;
                    }
                }
                int voxelIndexMax = CHUNK_VOXEL_COUNT;
                if (y == chunkMaxPos.y) {
                    int optimalMax = (int)(boxMax.y - (chunkMaxPos.y - CHUNK_HALF_SIZE)) * ONE_Y_ROW;
                    if (optimalMax < voxelIndexMax) {
                        voxelIndexMax = optimalMax;
                    }
                }
                for (double z = chunkMinPos.z; z <= chunkMaxPos.z; z += CHUNK_SIZE) {
                    chunkPos.z = z;
                    for (double x = chunkMinPos.x; x <= chunkMaxPos.x; x += CHUNK_SIZE) {
                        chunkPos.x = x;
                        VoxelChunk chunk;
                        if (GetChunk(chunkPos, out chunk, createChunkIfNotExists)) {
                            for (int v = voxelIndexMin; v < voxelIndexMax; v++) {
                                if (chunk.voxels[v].opaque >= minOpaque && (hasContent == -1 || chunk.voxels[v].hasContent == mustHaveContent)) {
                                    int py = v / ONE_Y_ROW;
                                    voxelPosition.y = chunk.position.y - CHUNK_HALF_SIZE + 0.5 + py;
                                    int pz = (v / ONE_Z_ROW) & CHUNK_SIZE_MINUS_ONE;
                                    voxelPosition.z = chunk.position.z - CHUNK_HALF_SIZE + 0.5 + pz;
                                    if (voxelPosition.z >= boxMin.z && voxelPosition.z < boxMax.z) {
                                        int px = v & CHUNK_SIZE_MINUS_ONE;
                                        voxelPosition.x = chunk.position.x - CHUNK_HALF_SIZE + 0.5 + px;
                                        if (voxelPosition.x >= boxMin.x && voxelPosition.x < boxMax.x) {
                                            index.chunk = chunk;
                                            index.voxelIndex = v;
                                            index.position = voxelPosition;
                                            index.sqrDistance = (float)FastVector.SqrDistance(ref voxelPosition, ref center);
                                            indices.Add(index);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return indices.Count;
        }


        /// <summary>
        /// boxMin 및 boxMax에 의해 정의된 볼륨 내에서 보이는 모든 복셀을 인덱스로 반환합니다.
        /// </summary>
        /// <returns>보이는 모든 복셀 인덱스의 수입니다.</returns>
        /// <param name="boxMin">둘러싸는 상자의 하단/왼쪽/뒤 ​​또는 최소 모서리.</param>
        /// <param name="boxMax">둘러싸는 상자의 상단/오른쪽/앞 또는 최대 모서리.</param>
        /// <param name="indices">쓰기 위해 사용자가 제공한 인덱스 목록입니다.</param>
        /// <param name="sdf">월드 공간 위치를 허용하고 해당 위치가 사용자 정의 볼륨 내에 포함된 경우 음수 값을 반환하는 메서드에 대한 대리자입니다.</param>
        public int GetVoxelIndices (Vector3d boxMin, Vector3d boxMax, List<VoxelIndex> indices, SDF sdf) {
            Vector3d chunkPos, voxelPosition;
            VoxelIndex index = new VoxelIndex();
            indices.Clear();

            Vector3d chunkMinPos = GetChunkPosition(boxMin);
            Vector3d chunkMaxPos = GetChunkPosition(boxMax);

            for (double y = chunkMinPos.y; y <= chunkMaxPos.y; y += CHUNK_SIZE) {
                chunkPos.y = y;
                int voxelIndexMin = 0;
                if (y == chunkMinPos.y) {
                    int optimalMin = (int)(boxMin.y - (chunkMinPos.y - CHUNK_HALF_SIZE)) * ONE_Y_ROW;
                    if (optimalMin > 0) {
                        voxelIndexMin = optimalMin;
                    }
                }
                int voxelIndexMax = CHUNK_VOXEL_COUNT;
                if (y == chunkMaxPos.y) {
                    int optimalMax = (int)(boxMax.y - (chunkMaxPos.y - CHUNK_HALF_SIZE) + 1) * ONE_Y_ROW;
                    if (optimalMax < CHUNK_VOXEL_COUNT) {
                        voxelIndexMax = optimalMax;
                    }
                }
                for (double z = chunkMinPos.z; z <= chunkMaxPos.z; z += CHUNK_SIZE) {
                    chunkPos.z = z;
                    for (double x = chunkMinPos.x; x <= chunkMaxPos.x; x += CHUNK_SIZE) {
                        chunkPos.x = x;
                        VoxelChunk chunk;
                        if (GetChunk(chunkPos, out chunk, true)) {
                            for (int v = voxelIndexMin; v < voxelIndexMax; v++) {
                                int py = v / ONE_Y_ROW;
                                voxelPosition.y = chunk.position.y - CHUNK_HALF_SIZE + 0.5 + py;
                                int pz = (v / ONE_Z_ROW) & CHUNK_SIZE_MINUS_ONE;
                                voxelPosition.z = chunk.position.z - CHUNK_HALF_SIZE + 0.5 + pz;
                                if (voxelPosition.z >= boxMin.z && voxelPosition.z < boxMax.z) {
                                    int px = v & CHUNK_SIZE_MINUS_ONE;
                                    voxelPosition.x = chunk.position.x - CHUNK_HALF_SIZE + 0.5 + px;
                                    if (voxelPosition.x >= boxMin.x && voxelPosition.x < boxMax.x) {
                                        if (sdf(voxelPosition) < 0) {
                                            index.chunk = chunk;
                                            index.voxelIndex = v;
                                            index.position = voxelPosition;
                                            indices.Add(index);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return indices.Count;
        }

        /// <summary>
        /// 인덱스 반환은 구 내부에 보이는 모든 복셀을 나열합니다.
        /// </summary>
        /// <returns>보이는 모든 복셀 인덱스의 수입니다.</returns>
        /// <param name="center">구의 중심.</param>
        /// <param name="radiusX">X축에 있는 구의 반경입니다.</param>
        /// <param name="radiusY">Y축에 있는 구의 반경입니다.</param>
        /// <param name="radiusZ">Z축에서 구의 반경입니다.</param>
        /// <param name="indices">쓰기 위해 사용자가 제공한 인덱스 목록입니다.</param>
        /// <param name="minOpaque">고려해야 할 복셀의 최소 불투명 값입니다.물은 불투명 = 2, 컷아웃 = 3, 잔디 = 0, 고체 = 15입니다.</param>
        /// <param name="mustHaveContent">기본값은 기존 복셀을 반환하는 true입니다.복셀 없이 위치를 검색하려면 false를 전달합니다.</param>
        public int GetVoxelIndices (Vector3d center, float radius, List<VoxelIndex> indices, byte minOpaque = 0, bool mustHaveContent = true) {
            return GetVoxelIndices(center, radius, radius, radius, indices, minOpaque, mustHaveContent);
        }

        /// <summary>
        /// 인덱스 반환은 구 내부에 보이는 모든 복셀을 나열합니다.
        /// </summary>
        /// <returns>보이는 모든 복셀 인덱스의 수입니다.</returns>
        /// <param name="center">구의 중심.</param>
        /// <param name="radius">구의 반경.</param>
        /// <param name="indices">쓰기 위해 사용자가 제공한 인덱스 목록입니다.</param>
        /// <param name="minOpaque">고려해야 할 복셀의 최소 불투명 값입니다.물은 불투명 = 2, 컷아웃 = 3, 잔디 = 0, 고체 = 15입니다.</param>
        /// <param name="mustHaveContent">기본값은 기존 복셀을 반환하는 true입니다.복셀 없이 위치를 검색하려면 false를 전달합니다.</param>
        public int GetVoxelIndices (Vector3d center, float radiusX, float radiusY, float radiusZ, List<VoxelIndex> indices, byte minOpaque = 0, bool mustHaveContent = true) {
            if (indices == null) return 0;

            Vector3d chunkPos, voxelPosition;
            VoxelIndex index = new VoxelIndex();
            indices.Clear();

            center.x = FastMath.FloorToInt(center.x) + 0.5;
            center.y = FastMath.FloorToInt(center.y) + 0.5;
            center.z = FastMath.FloorToInt(center.z) + 0.5;
            Vector3d boxMin = new Vector3d(
                center.x - radiusX,
                center.y - radiusY,
                center.z - radiusZ
            );

            Vector3d boxMax = new Vector3d(
                center.x + radiusX,
                center.y + radiusY,
                center.z + radiusZ
            );
            Vector3d chunkMinPos = GetChunkPosition(boxMin);
            Vector3d chunkMaxPos = GetChunkPosition(boxMax);

            if (!mustHaveContent)
                minOpaque = 0;

            for (double y = chunkMinPos.y; y <= chunkMaxPos.y; y += CHUNK_SIZE) {
                chunkPos.y = y;
                int voxelIndexMin = 0;
                if (y == chunkMinPos.y) {
                    int optimalMin = (int)(boxMin.y - (chunkMinPos.y - CHUNK_HALF_SIZE)) * ONE_Y_ROW;
                    if (optimalMin > 0) {
                        voxelIndexMin = optimalMin;
                    }
                }
                int voxelIndexMax = CHUNK_VOXEL_COUNT;
                if (y == chunkMaxPos.y) {
                    int optimalMax = (int)(boxMax.y - (chunkMaxPos.y - CHUNK_HALF_SIZE) + 1) * ONE_Y_ROW;
                    if (optimalMax < CHUNK_VOXEL_COUNT) {
                        voxelIndexMax = optimalMax;
                    }
                }

                for (double z = chunkMinPos.z; z <= chunkMaxPos.z; z += CHUNK_SIZE) {
                    chunkPos.z = z;
                    for (double x = chunkMinPos.x; x <= chunkMaxPos.x; x += CHUNK_SIZE) {
                        chunkPos.x = x;
                        VoxelChunk chunk;
                        if (GetChunk(chunkPos, out chunk, false)) {
                            for (int v = voxelIndexMin; v < voxelIndexMax; v++) {
                                if (chunk.voxels[v].hasContent == mustHaveContent && chunk.voxels[v].opaque >= minOpaque) {
                                    int py = v / ONE_Y_ROW;
                                    voxelPosition.y = chunk.position.y - CHUNK_HALF_SIZE + 0.5f + py;
                                    int pz = (v / ONE_Z_ROW) & CHUNK_SIZE_MINUS_ONE;
                                    voxelPosition.z = chunk.position.z - CHUNK_HALF_SIZE + 0.5f + pz;
                                    if (voxelPosition.z >= boxMin.z && voxelPosition.z <= boxMax.z) {
                                        int px = v & CHUNK_SIZE_MINUS_ONE;
                                        voxelPosition.x = chunk.position.x - CHUNK_HALF_SIZE + 0.5f + px;
                                        if (voxelPosition.x >= boxMin.x && voxelPosition.x <= boxMax.x) {
                                            double radiusXSqr = radiusX * radiusX;
                                            double radiusYSqr = radiusY * radiusY;
                                            double radiusZSqr = radiusZ * radiusZ;
                                            double distX = (voxelPosition.x - center.x) * (voxelPosition.x - center.x) / radiusXSqr;
                                            double distY = (voxelPosition.y - center.y) * (voxelPosition.y - center.y) / radiusYSqr;
                                            double distZ = (voxelPosition.z - center.z) * (voxelPosition.z - center.z) / radiusZSqr;
                                            double dist = distX + distY + distZ;
                                            if (dist < 1.0) {
                                                index.chunk = chunk;
                                                index.voxelIndex = v;
                                                index.position = voxelPosition;
                                                index.sqrDistance = (float)dist;
                                                indices.Add(index);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return indices.Count;
        }

        /// <summary>
        /// 이 방법은 위치를 복셀 인덱스로 변환합니다.
        /// </summary>
        public void GetVoxelIndices (List<Vector3d> positions, List<VoxelIndex> indices, bool createChunkIfNotExists = false) {
            if (indices == null) return;
            indices.Clear();
            if (positions == null) return;
            int positionsCount = positions.Count;
            VoxelIndex vindex = new VoxelIndex();
            for (int k = 0; k < positionsCount; k++) {
                Vector3d position = positions[k];
                FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
                if (GetChunkFast(chunkX, chunkY, chunkZ, out VoxelChunk chunk, createChunkIfNotExists)) {
                    int py = (int)(position.y - chunkY * CHUNK_SIZE);
                    int pz = (int)(position.z - chunkZ * CHUNK_SIZE);
                    int px = (int)(position.x - chunkX * CHUNK_SIZE);
                    vindex.chunk = chunk;
                    vindex.voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                    indices.Add(vindex);
                }
            }
        }

        /// <summary>
        /// 이 메서드는 위치를 기존 인덱스 배열로 변환합니다.
        /// </summary>
        /// <param name="indices">사전 할당되어야 함</param>
        /// <returns>인덱스 배열에 배치된 인덱스 수를 반환합니다.</returns>
        public int GetVoxelIndices (Vector3d[] positions, VoxelIndex[] indices, bool createChunkIfNotExists = false) {
            if (indices == null) return 0;
            if (positions == null) return 0;
            int positionsLength = positions.Length;
            if (indices.Length < positionsLength) positionsLength = indices.Length;

            VoxelIndex vindex = new VoxelIndex();
            for (int k = 0; k < positionsLength; k++) {
                Vector3d position = positions[k];
                FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
                if (GetChunkFast(chunkX, chunkY, chunkZ, out VoxelChunk chunk, createChunkIfNotExists)) {
                    int py = (int)(position.y - chunkY * CHUNK_SIZE);
                    int pz = (int)(position.z - chunkZ * CHUNK_SIZE);
                    int px = (int)(position.x - chunkX * CHUNK_SIZE);
                    vindex.chunk = chunk;
                    vindex.voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                    indices[k] = vindex;
                }
            }
            return positionsLength;
        }

        /// <summary>
        /// 주어진 볼륨에 있는 모든 복셀의 복사본을 반환합니다.
        /// </summary>
        /// <param name="boxMin">둘러싸는 상자의 하단/왼쪽/뒤 ​​또는 최소 모서리.</param>
        /// <param name="boxMax">둘러싸는 상자의 상단/오른쪽/앞 또는 최대 모서리.</param>
        /// <param name="voxels">사용자가 3차원 복셀 배열(y/z/x)을 제공했습니다.이 메서드를 호출하기 전에 충분한 공간을 할당해야 합니다.</param>  
        public void GetVoxels (Vector3d boxMin, Vector3d boxMax, Voxel[,,] voxels) {

            if (voxels == null) {
                Debug.LogError("You must allocate enough space for the voxels array parameter.");
                return;
            }

            Vector3d position;

            Vector3d chunkMinPos = GetChunkPosition(boxMin);
            Vector3d chunkMaxPos = GetChunkPosition(boxMax);

            int minX, minY, minZ, maxX, maxY, maxZ;
            FastMath.FloorToInt(boxMin.x, boxMin.y, boxMin.z, out minX, out minY, out minZ);
            FastMath.FloorToInt(boxMax.x, boxMax.y, boxMax.z, out maxX, out maxY, out maxZ);

            int sizeY = maxY - minY;
            int sizeZ = maxZ - minZ;
            int sizeX = maxX - minX;
            int msizeY = voxels.GetUpperBound(0);
            int msizeZ = voxels.GetUpperBound(1);
            int msizeX = voxels.GetUpperBound(2);
            if (msizeY < sizeY || msizeZ < sizeZ || msizeX < sizeX) {
                Debug.LogError($"Voxels array size does not match volume size. Expected format and size (y,z,x): [{sizeY + 1}, {sizeZ + 1}, {sizeX + 1}");
                return;
            }

            for (double y = chunkMinPos.y; y <= chunkMaxPos.y; y += CHUNK_SIZE) {
                position.y = y;
                for (double z = chunkMinPos.z; z <= chunkMaxPos.z; z += CHUNK_SIZE) {
                    position.z = z;
                    for (double x = chunkMinPos.x; x <= chunkMaxPos.x; x += CHUNK_SIZE) {
                        position.x = x;
                        VoxelChunk chunk;
                        if (GetChunk(position, out chunk, false)) {
                            int chunkMinX, chunkMinY, chunkMinZ;
                            FastMath.FloorToInt(chunk.position.x, chunk.position.y, chunk.position.z, out chunkMinX, out chunkMinY, out chunkMinZ);
                            chunkMinX -= CHUNK_HALF_SIZE;
                            chunkMinY -= CHUNK_HALF_SIZE;
                            chunkMinZ -= CHUNK_HALF_SIZE;
                            for (int vy = 0; vy < CHUNK_SIZE; vy++) {
                                int wy = chunkMinY + vy;
                                if (wy < minY || wy > maxY)
                                    continue;
                                int my = wy - minY;
                                int voxelIndexY = vy * ONE_Y_ROW;
                                for (int vz = 0; vz < CHUNK_SIZE; vz++) {
                                    int wz = chunkMinZ + vz;
                                    if (wz < minZ || wz > maxZ)
                                        continue;
                                    int mz = wz - minZ;
                                    int voxelIndex = voxelIndexY + vz * ONE_Z_ROW;
                                    for (int vx = 0; vx < CHUNK_SIZE; vx++, voxelIndex++) {
                                        int wx = chunkMinX + vx;
                                        if (wx < minX || wx > maxX)
                                            continue;
                                        int mx = wx - minX;
                                        voxels[my, mz, mx] = chunk.voxels[voxelIndex];
                                    }
                                }
                            }
                        } else {
                            int chunkMinY = FastMath.FloorToInt(y) - CHUNK_HALF_SIZE;
                            int chunkMinZ = FastMath.FloorToInt(z) - CHUNK_HALF_SIZE;
                            int chunkMinX = FastMath.FloorToInt(x) - CHUNK_HALF_SIZE;
                            int voxelIndex = 0;
                            for (int vy = 0; vy < CHUNK_SIZE; vy++) {
                                int wy = chunkMinY + vy;
                                if (wy < minY || wy > maxY)
                                    continue;
                                int my = wy - minY;
                                for (int vz = 0; vz < CHUNK_SIZE; vz++) {
                                    int wz = chunkMinZ + vz;
                                    if (wz < minZ || wz > maxZ)
                                        continue;
                                    int mz = wz - minZ;
                                    for (int vx = 0; vx < CHUNK_SIZE; vx++, voxelIndex++) {
                                        int wx = chunkMinX + vx;
                                        if (wx < minX || wx > maxX)
                                            continue;
                                        int mx = wx - minX;
                                        voxels[my, mz, mx] = Voxel.Empty;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 3D 배열로 제공되는 복셀 볼륨을 배치합니다.
        /// </summary>
        /// <param name="position">둘러싸는 상자의 하단/왼쪽/뒤 ​​또는 최소 모서리.</param>
        /// <param name="voxels">사용자가 3차원 복셀 배열(y/z/x)을 제공했습니다.</param>  
        public void SetVoxels (Vector3d position, Voxel[,,] voxels) {
            int msizeY = voxels.GetUpperBound(0);
            int msizeZ = voxels.GetUpperBound(1);
            int msizeX = voxels.GetUpperBound(2);
            Vector3d boxMax = new Vector3d(position.x + msizeX, position.y + msizeY, position.z + msizeZ);
            SetVoxels(position, boxMax, voxels);
        }


        /// <summary>
        /// 3D 배열로 제공되는 복셀 볼륨을 배치합니다.
        /// </summary>
        /// <param name="boxMin">둘러싸는 상자의 하단/왼쪽/뒤 ​​또는 최소 모서리.</param>
        /// <param name="boxMax">둘러싸는 상자의 상단/오른쪽/앞 또는 최대 모서리.</param>
        /// <param name="voxels">사용자가 3차원 복셀 배열(y/z/x)을 제공했습니다.</param>
        /// <param name="redraw">영향을 받은 청크를 다시 그리기 발행</param>
        public void SetVoxels (Vector3d boxMin, Vector3d boxMax, Voxel[,,] voxels, bool redraw = true) {

            if (voxels == null) {
                Debug.LogError("SetVoxels: voxels array is null.");
                return;
            }

            Vector3d position;

            Vector3d chunkMinPos = GetChunkPosition(boxMin);
            Vector3d chunkMaxPos = GetChunkPosition(boxMax);

            int minX, minY, minZ, maxX, maxY, maxZ;
            FastMath.FloorToInt(boxMin.x, boxMin.y, boxMin.z, out minX, out minY, out minZ);
            FastMath.FloorToInt(boxMax.x, boxMax.y, boxMax.z, out maxX, out maxY, out maxZ);

            int sizeY = maxY - minY;
            int sizeZ = maxZ - minZ;
            int sizeX = maxX - minX;
            int msizeY = voxels.GetUpperBound(0);
            int msizeZ = voxels.GetUpperBound(1);
            int msizeX = voxels.GetUpperBound(2);
            if (msizeY < sizeY || msizeZ < sizeZ || msizeX < sizeX) {
                Debug.LogError("Voxels array size does not match volume size. Expected size: [" + sizeY + ", " + sizeZ + ", " + sizeX + "]");
                return;
            }

            for (double y = chunkMinPos.y; y <= chunkMaxPos.y; y += CHUNK_SIZE) {
                position.y = y;
                for (double z = chunkMinPos.z; z <= chunkMaxPos.z; z += CHUNK_SIZE) {
                    position.z = z;
                    for (double x = chunkMinPos.x; x <= chunkMaxPos.x; x += CHUNK_SIZE) {
                        position.x = x;
                        VoxelChunk chunk = GetChunkUnpopulated(position);
                        if (chunk != null) {
                            int chunkMinX, chunkMinY, chunkMinZ;
                            FastMath.FloorToInt(chunk.position.x, chunk.position.y, chunk.position.z, out chunkMinX, out chunkMinY, out chunkMinZ);
                            chunkMinX -= CHUNK_HALF_SIZE;
                            chunkMinY -= CHUNK_HALF_SIZE;
                            chunkMinZ -= CHUNK_HALF_SIZE;
                            for (int vy = 0; vy < CHUNK_SIZE; vy++) {
                                int wy = chunkMinY + vy;
                                if (wy < minY || wy > maxY)
                                    continue;
                                int my = wy - minY;
                                int voxelIndexY = vy * ONE_Y_ROW;
                                for (int vz = 0; vz < CHUNK_SIZE; vz++) {
                                    int wz = chunkMinZ + vz;
                                    if (wz < minZ || wz > maxZ)
                                        continue;
                                    int mz = wz - minZ;
                                    int voxelIndex = voxelIndexY + vz * ONE_Z_ROW;
                                    for (int vx = 0; vx < CHUNK_SIZE; vx++, voxelIndex++) {
                                        int wx = chunkMinX + vx;
                                        if (wx < minX || wx > maxX)
                                            continue;
                                        int mx = wx - minX;
                                        chunk.voxels[voxelIndex] = voxels[my, mz, mx];
                                    }
                                }
                            }
                        }
                        ChunkRequestRefresh(chunk, true, true);
                    }
                }
            }
        }

        /// <summary>
        /// 복셀을 대체합니다.
        /// </summary>
        /// <param name="boxMin">둘러싸는 상자의 하단/왼쪽/뒤 ​​또는 최소 모서리.</param>
        /// <param name="boxMax">둘러싸는 상자의 상단/오른쪽/앞 또는 최대 모서리.</param>
        /// <param name="voxels">사용자가 3차원 복셀 배열(y/z/x)을 제공했습니다.이 메서드를 호출하기 전에 충분한 공간을 할당해야 합니다.</param>  
        /// <param name="ignoreEmptyVoxels">비어 있지 않은 배열의 복셀만 배치하려면 true로 설정하세요.</param>
        public void VoxelPlace (Vector3d boxMin, Vector3d boxMax, Voxel[,,] voxels, bool ignoreEmptyVoxels = false) {
            Vector3d position;

            Vector3d chunkMinPos = GetChunkPosition(boxMin);
            Vector3d chunkMaxPos = GetChunkPosition(boxMax);

            int minX, minY, minZ, maxX, maxY, maxZ;
            FastMath.FloorToInt(boxMin.x, boxMin.y, boxMin.z, out minX, out minY, out minZ);
            FastMath.FloorToInt(boxMax.x, boxMax.y, boxMax.z, out maxX, out maxY, out maxZ);

            int sizeY = maxY - minY;
            int sizeZ = maxZ - minZ;
            int sizeX = maxX - minX;
            int msizeY = voxels.GetUpperBound(0);
            int msizeZ = voxels.GetUpperBound(1);
            int msizeX = voxels.GetUpperBound(2);
            if (msizeY < sizeY || msizeZ < sizeZ || msizeX < sizeX) {
                Debug.LogError("Voxels array size does not match volume size. Expected size: [" + sizeY + ", " + sizeZ + ", " + sizeX + "]");
                return;
            }

            for (double y = chunkMinPos.y; y <= chunkMaxPos.y; y += CHUNK_SIZE) {
                position.y = y;
                for (double z = chunkMinPos.z; z <= chunkMaxPos.z; z += CHUNK_SIZE) {
                    position.z = z;
                    for (double x = chunkMinPos.x; x <= chunkMaxPos.x; x += CHUNK_SIZE) {
                        position.x = x;
                        VoxelChunk chunk;
                        if (GetChunk(position, out chunk, true)) {
                            int chunkMinX, chunkMinY, chunkMinZ;
                            FastMath.FloorToInt(chunk.position.x, chunk.position.y, chunk.position.z, out chunkMinX, out chunkMinY, out chunkMinZ);
                            chunkMinX -= CHUNK_HALF_SIZE;
                            chunkMinY -= CHUNK_HALF_SIZE;
                            chunkMinZ -= CHUNK_HALF_SIZE;
                            for (int vy = 0; vy < CHUNK_SIZE; vy++) {
                                int wy = chunkMinY + vy;
                                if (wy < minY || wy > maxY)
                                    continue;
                                int my = wy - minY;
                                int voxelIndexY = vy * ONE_Y_ROW;
                                for (int vz = 0; vz < CHUNK_SIZE; vz++) {
                                    int wz = chunkMinZ + vz;
                                    if (wz < minZ || wz > maxZ)
                                        continue;
                                    int mz = wz - minZ;
                                    int voxelIndex = voxelIndexY + vz * ONE_Z_ROW;
                                    for (int vx = 0; vx < CHUNK_SIZE; vx++, voxelIndex++) {
                                        int wx = chunkMinX + vx;
                                        if (wx < minX || wx > maxX)
                                            continue;
                                        int mx = wx - minX;
                                        if (voxels[my, mz, mx].hasContent || !ignoreEmptyVoxels) {
                                            chunk.voxels[voxelIndex] = voxels[my, mz, mx];
                                        }
                                    }
                                }
                            }
                            ChunkRequestRefresh(chunk, true, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 내부 사전에 새로운 복셀 정의를 추가합니다.
        /// </summary>
        /// <param name="vd">Vd.</param>
        public void AddVoxelDefinition (VoxelDefinition vd) {
            if (vd == null)
                return;
            // Check if voxelType is not added
            if (vd.index <= 0 && sessionUserVoxels != null) {
                InsertUserVoxelDefinition(vd);
                requireTextureArrayUpdate = true;
            }
        }

        /// <summary>
        /// 이미 추가된 복셀 정의의 텍스처를 업데이트합니다.
        /// </summary>
        public void UpdateVoxelDefinitionTextures (VoxelDefinition vd) {
            if (vd == null || vd.index <= 0 || vd.renderType == RenderType.Custom || vd.textureArrayPacker == null) return;
            AddVoxelTexturesNonCustom(vd);
            vd.textureArrayPacker.CreateTextureArray();
            SetRenderingMaterialsTextures(createTextureArrays: false);
            ChunkRedrawAll(refreshLightmap: false, refreshMesh: true, ignoreFrustum: true);
        }

        /// <summary>
        /// 내부 사전에 복셀 정의 목록을 추가합니다.
        /// </summary>
        /// <param name="vd">Vd.</param>
        public void AddVoxelDefinitions (List<VoxelDefinition> vd) {
            if (vd == null)
                return;
            for (int k = 0; k < vd.Count; k++) {
                AddVoxelDefinition(vd[k]);
            }
        }

        /// <summary>
        /// 모델에 포함된 복셀 정의를 내부 사전에 추가합니다.
        /// </summary>
        public void AddVoxelDefinitions (ModelDefinition model) {
            if (model == null)
                return;
            for (int k = 0; k < model.bits.Length; k++) {
                AddVoxelDefinition(model.bits[k].voxelDefinition);
            }
        }

        /// <summary>
        /// 내부 사전에 복셀 정의 목록을 추가합니다.
        /// </summary>
        /// <param name="vd">Vd.</param>
        public void AddVoxelDefinitions (params VoxelDefinition[] vd) {
            if (vd == null)
                return;
            for (int k = 0; k < vd.Length; k++) {
                AddVoxelDefinition(vd[k]);
            }
        }

        /// <summary>
        /// 이름으로 복셀 정의를 가져옵니다.
        /// </summary>
        /// <returns>복셀 정의.</returns>
        public VoxelDefinition GetVoxelDefinition (string name) {
            if (string.IsNullOrEmpty(name)) return null;
            voxelDefinitionsDict.TryGetValue(name, out VoxelDefinition vd);
            return vd;
        }

        /// <summary>
        /// 색인별로 복셀 정의를 가져옵니다.
        /// </summary>
        /// <returns>복셀 정의.</returns>
        /// <param name="index">색인.</param>
        public VoxelDefinition GetVoxelDefinition (int index) {
            if (index >= 0 && index < voxelDefinitionsCount) {
                return voxelDefinitions[index];
            }
            return null;
        }



        /// <summary>
        /// 청크 내부의 지정된 위치에 있는 복셀의 인덱스를 반환합니다.
        /// </summary>
        /// <returns>복셀 지수.</returns>
        /// <param name="position">월드 공간에서의 위치.</param>
        public int GetVoxelIndex (Vector3d position) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            chunkX *= CHUNK_SIZE;
            chunkY *= CHUNK_SIZE;
            chunkZ *= CHUNK_SIZE;
            int px = (int)(position.x - chunkX);
            int py = (int)(position.y - chunkY);
            int pz = (int)(position.z - chunkZ);
            int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
            return voxelIndex;
        }


        /// <summary>
        /// 월드 공간에서 복셀 위치를 찾는 VoxelIndex 구조체를 가져옵니다.
        /// </summary>
        /// <returns>복셀 지수.</returns>
        /// <param name="position">월드 공간에서의 위치.</param>
        /// <param name="createChunkIfNotExists">해당 위치에 청크가 존재하지 않는 경우 강제로 청크를 생성하려면 true를 전달합니다.기본값은 거짓입니다.</param>
        public bool GetVoxelIndex (Vector3d position, out VoxelIndex index, bool createChunkIfNotExists = false) {
            index = new VoxelIndex();
            return GetVoxelIndex(position, out index.chunk, out index.voxelIndex, createChunkIfNotExists);
        }

        /// <summary>
        /// 주어진 세계 위치에 해당하는 청크 위치와 voxelIndex를 가져옵니다(청크가 아직 존재하지 않을 수도 있음).
        /// </summary>
        /// <param name="position">세계 위치.</param>
        public void GetVoxelIndex (Vector3d position, out Vector3d chunkPosition, out int voxelIndex) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            chunkX *= CHUNK_SIZE;
            chunkY *= CHUNK_SIZE;
            chunkZ *= CHUNK_SIZE;
            chunkPosition.x = chunkX + CHUNK_HALF_SIZE;
            chunkPosition.y = chunkY + CHUNK_HALF_SIZE;
            chunkPosition.z = chunkZ + CHUNK_HALF_SIZE;
            int px = (int)(position.x - chunkX);
            int py = (int)(position.y - chunkY);
            int pz = (int)(position.z - chunkZ);
            voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
        }

        /// <summary>
        /// 주어진 위치에서 복셀의 청크 및 배열 인덱스를 가져옵니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">Chunk.voxels 배열의 복셀 인덱스</param>
        public bool GetVoxelIndex (Vector3d position, out VoxelChunk chunk, out int voxelIndex, bool createChunkIfNotExists = true) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            if (GetChunkFast(chunkX, chunkY, chunkZ, out chunk, createChunkIfNotExists)) {
                int py = (int)(position.y - chunkY * CHUNK_SIZE);
                int pz = (int)(position.z - chunkZ * CHUNK_SIZE);
                int px = (int)(position.x - chunkX * CHUNK_SIZE);
                voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                return true;
            }

            voxelIndex = 0;
            return false;
        }


        /// <summary>
        /// 주어진 위치에서 복셀의 청크 및 배열 인덱스를 가져옵니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="px">청크 내부에 x 위치</param>
        /// <param name="py">청크 내부에 y 위치</param>
        /// <param name="pz">청크 내부에 z 위치 지정</param>
        public bool GetVoxelIndex (Vector3d position, out VoxelChunk chunk, out int px, out int py, out int pz, bool createChunkIfNotExists = true) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            if (GetChunkFast(chunkX, chunkY, chunkZ, out chunk, createChunkIfNotExists)) {
                py = (int)(position.y - chunkY * CHUNK_SIZE);
                pz = (int)(position.z - chunkZ * CHUNK_SIZE);
                px = (int)(position.x - chunkX * CHUNK_SIZE);
                return true;
            }
            px = py = pz = 0;
            return false;
        }

        /// <summary>
        /// 다른 복셀 인덱스에 대한 특정 오프셋에 해당하는 복셀 인덱스를 가져옵니다.다른 것 위에 있는 복셀에 대한 안전한 참조를 얻는 데 유용합니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel index was gotten, <c>거짓</c> otherwise.</returns>
        /// <param name="index">복셀 지수.</param>
        /// <param name="offsetX">오프셋 x.</param>
        /// <param name="offsetY">오프셋 y.</param>
        /// <param name="offsetZ">오프셋 z.</param>
        /// <param name="otherIndex">기타 복셀 지수.</param>
        /// <param name="createChunkIfNotExists">If set to <c>진실</c> create chunk if not exists.</param>
        public bool GetVoxelIndex (ref VoxelIndex index, int offsetX, int offsetY, int offsetZ, out VoxelIndex otherIndex, bool createChunkIfNotExists = false) {
            otherIndex = new VoxelIndex();
            if ((object)index.chunk == null) return false;
            Vector3d pos = index.chunk.position;
            int py = index.voxelIndex / ONE_Y_ROW;
            int pz = (index.voxelIndex / ONE_Z_ROW) & CHUNK_SIZE_MINUS_ONE;
            int px = index.voxelIndex & CHUNK_SIZE_MINUS_ONE;

            pos.y = pos.y - CHUNK_HALF_SIZE + py + offsetY;
            pos.z = pos.z - CHUNK_HALF_SIZE + pz + offsetZ;
            pos.x = pos.x - CHUNK_HALF_SIZE + px + offsetX;

            otherIndex.position = pos;

            return GetVoxelIndex(pos, out otherIndex.chunk, out otherIndex.voxelIndex, createChunkIfNotExists);
        }




        /// <summary>
        /// 복셀 내부의 로컬 x,y,z 위치로 복셀의 인덱스를 가져옵니다.
        /// </summary>
        /// <returns>복셀 지수.</returns>
        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public int GetVoxelIndex (int px, int py, int pz) {
            return py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
        }


        /// <summary>
        /// 다른 청크/복셀 인덱스에 대한 특정 오프셋에 해당하는 청크 및 복셀 인덱스를 가져옵니다.다른 것 위에 있는 복셀에 대한 안전한 참조를 얻는 데 유용합니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel index was gotten, <c>거짓</c> otherwise.</returns>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        /// <param name="offsetX">오프셋 x.</param>
        /// <param name="offsetY">오프셋 y.</param>
        /// <param name="offsetZ">오프셋 z.</param>
        /// <param name="otherChunk">다른 덩어리.</param>
        /// <param name="otherVoxelIndex">기타 복셀 지수.</param>
        /// <param name="createChunkIfNotExists">If set to <c>진실</c> create chunk if not exists.</param>
        public bool GetVoxelIndex (VoxelChunk chunk, int voxelIndex, int offsetX, int offsetY, int offsetZ, out VoxelChunk otherChunk, out int otherVoxelIndex, bool createChunkIfNotExists = false) {
            GetVoxelChunkCoordinates(voxelIndex, out int px, out int py, out int pz);

            // inside chunk?
            int qx = px + offsetX;
            int qy = py + offsetY;
            int qz = pz + offsetZ;
            if (qx >= 0 && qy >= 0 && qz >= 0 && qx < CHUNK_SIZE && qy < CHUNK_SIZE && qz < CHUNK_SIZE) {
                otherChunk = chunk;
                otherVoxelIndex = qy * ONE_Y_ROW + qz * ONE_Z_ROW + qx;
                return true;
            }

            Vector3d position;
            position.x = chunk.position.x - CHUNK_HALF_SIZE + 0.5 + qx;
            position.y = chunk.position.y - CHUNK_HALF_SIZE + 0.5 + qy;
            position.z = chunk.position.z - CHUNK_HALF_SIZE + 0.5 + qz;
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            if (GetChunkFast(chunkX, chunkY, chunkZ, out otherChunk, createChunkIfNotExists)) {
                py = (int)(position.y - chunkY * CHUNK_SIZE);
                pz = (int)(position.z - chunkZ * CHUNK_SIZE);
                px = (int)(position.x - chunkX * CHUNK_SIZE);
                otherVoxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                return true;
            } else {
                otherVoxelIndex = 0;
                return false;
            }
        }

        /// <summary>
        /// voxelIndices 길이에 따라 위치 주변의 9, 11 또는 27개 복셀을 반환합니다.결과는 아래에서 위로, 뒤에서 앞으로, 왼쪽에서 오른쪽으로 시작하여 Y/Z/X 순서로 구성된 9, 11 또는 27개 복셀 인덱스의 사용자 제공 배열로 반환됩니다.
        /// </summary>
        /// <param name="faceOrientation">0 = 앞, 1 = 오른쪽, 2 = 뒤, 3 = 왼쪽</param>
        public void GetVoxelNeighbourhood (Vector3d position, ref VoxelIndex[] voxelIndices, int faceOrientation = 0) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) {
                GetVoxelNeighbourhood(chunk, voxelIndex, ref voxelIndices, faceOrientation);
            } else {
                voxelIndices.Fill(VoxelIndex.Null);
            }
        }

        /// <summary>
        /// voxelIndices 길이에 따라 위치 주변의 9, 11 또는 27개 복셀을 반환합니다.결과는 아래에서 위로, 뒤에서 앞으로, 왼쪽에서 오른쪽으로 시작하여 Y/Z/X 순서로 구성된 9, 11 또는 27개 복셀 인덱스의 사용자 제공 배열로 반환됩니다.
        /// </summary>
        /// <param name="faceOrientation">0 = 앞, 1 = 오른쪽, 2 = 뒤, 3 = 왼쪽</param>
        public void GetVoxelNeighbourhood (VoxelChunk chunk, int voxelIndex, ref VoxelIndex[] voxelIndices, int faceOrientation = 0) {
            int[] neighbourIndicesOrientationArray;
            switch (faceOrientation) {
                case 1: neighbourIndicesOrientationArray = neighbourIndicesOrientationArrayRight; break;
                case 2: neighbourIndicesOrientationArray = neighbourIndicesOrientationArrayBack; break;
                case 3: neighbourIndicesOrientationArray = neighbourIndicesOrientationArrayLeft; break;
                default: neighbourIndicesOrientationArray = neighbourIndicesOrientationArrayForward; break;
            }
            if (voxelIndices.Length >= 27) {
                GetVoxelNeighbourhood27(chunk, voxelIndex, ref voxelIndices, neighbourIndicesOrientationArray);
            } else if (voxelIndices.Length == 11) {
                GetVoxelNeighbourhood11(chunk, voxelIndex, ref voxelIndices, neighbourIndicesOrientationArray);
            } else {
                GetVoxelNeighbourhood9(chunk, voxelIndex, ref voxelIndices, neighbourIndicesOrientationArray);
            }
        }

        /// <summary>
        /// 월드 공간 좌표에서 복셀 위치를 가져옵니다.
        /// </summary>
        /// <returns>복셀 위치.</returns>
        public Vector3d GetVoxelPosition (VoxelIndex voxelIndex) {
            return GetVoxelPosition(voxelIndex.chunk, voxelIndex.voxelIndex);
        }

        /// <summary>
        /// 월드 공간 좌표에서 복셀 위치를 가져옵니다.
        /// </summary>
        /// <returns>복셀 위치.</returns>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        public Vector3d GetVoxelPosition (VoxelChunk chunk, int voxelIndex) {
            GetVoxelChunkCoordinates(voxelIndex, out int px, out int py, out int pz);
            Vector3d position;
            position.x = chunk.position.x - CHUNK_HALF_SIZE + 0.5 + px;
            position.y = chunk.position.y - CHUNK_HALF_SIZE + 0.5 + py;
            position.z = chunk.position.z - CHUNK_HALF_SIZE + 0.5 + pz;
            return position;
        }



        /// <summary>
        /// 월드 공간 좌표에서 해당 복셀 위치를 가져옵니다(복셀 위치는 정확히 복셀의 중심입니다).
        /// </summary>
        /// <returns>복셀 위치.</returns>
        /// <param name="position">월드 공간 좌표의 모든 위치.</param>
        public Vector3d GetVoxelPosition (Vector3d position) {
            position.x = Math.Floor(position.x) + 0.5;
            position.y = Math.Floor(position.y) + 0.5;
            position.z = Math.Floor(position.z) + 0.5;
            return position;
        }



        /// <summary>
        /// 청크 내부의 복셀 로컬 위치를 가져옵니다.
        /// </summary>
        /// <returns>복셀 위치.</returns>
        public Vector3 GetVoxelChunkPosition (int voxelIndex) {
            GetVoxelChunkCoordinates(voxelIndex, out int px, out int py, out int pz);
            Vector3 position;
            position.x = px - CHUNK_HALF_SIZE + 0.5f;
            position.y = py - CHUNK_HALF_SIZE + 0.5f;
            position.z = pz - CHUNK_HALF_SIZE + 0.5f;
            return position;
        }


        /// <summary>
        /// 월드 공간 좌표에서 복셀 위치를 가져옵니다.
        /// </summary>
        /// <returns>복셀 위치.</returns>
        /// <param name="chunkPosition">청크 위치.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        public Vector3d GetVoxelPosition (Vector3d chunkPosition, int voxelIndex) {
            GetVoxelChunkCoordinates(voxelIndex, out int px, out int py, out int pz);
            Vector3d position;
            position.x = chunkPosition.x - CHUNK_HALF_SIZE + 0.5 + px;
            position.y = chunkPosition.y - CHUNK_HALF_SIZE + 0.5 + py;
            position.z = chunkPosition.z - CHUNK_HALF_SIZE + 0.5 + pz;
            return position;
        }



        /// <summary>
        /// 월드 공간 좌표에서 복셀 위치를 가져옵니다.
        /// </summary>
        /// <returns>복셀 위치.</returns>
        /// <param name="chunkPosition">청크 위치.</param>
        /// <param name="px">청크에 있는 복셀의 x 인덱스입니다.</param>
        /// <param name="py">청크에 있는 복셀의 y 인덱스입니다.</param>
        /// <param name="pz">청크에 있는 복셀의 z 인덱스입니다.</param>
        public Vector3d GetVoxelPosition (Vector3d chunkPosition, int px, int py, int pz) {
            Vector3d position;
            position.x = chunkPosition.x - CHUNK_HALF_SIZE + 0.5 + px;
            position.y = chunkPosition.y - CHUNK_HALF_SIZE + 0.5 + py;
            position.z = chunkPosition.z - CHUNK_HALF_SIZE + 0.5 + pz;
            return position;
        }

        /// <summary>
        /// 복셀 인덱스가 주어지면 청크 내부의 x, y, z 위치를 반환합니다.
        /// </summary>
        /// <param name="voxelIndex">복셀 지수.</param>
        /// <param name="px">청크에 있는 복셀의 x 인덱스입니다.</param>
        /// <param name="py">청크에 있는 복셀의 y 인덱스입니다.</param>
        /// <param name="pz">청크에 있는 복셀의 z 인덱스입니다.</param>
        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public void GetVoxelChunkCoordinates (int voxelIndex, out int px, out int py, out int pz) {
            px = voxelIndex & CHUNK_SIZE_MINUS_ONE;
            py = voxelIndex / ONE_Y_ROW;
            pz = (voxelIndex / ONE_Z_ROW) & CHUNK_SIZE_MINUS_ONE;
        }


        /// <summary>
        /// 위치가 주어지면 청크 내부의 x, y, z 위치를 반환합니다.
        /// </summary>
        /// <param name="position">복셀 위치.</param>
        /// <param name="px">청크에 있는 복셀의 x 인덱스입니다.</param>
        /// <param name="py">청크에 있는 복셀의 y 인덱스입니다.</param>
        /// <param name="pz">청크에 있는 복셀의 z 인덱스입니다.</param>
        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public void GetVoxelChunkCoordinates (Vector3d position, out int px, out int py, out int pz) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            chunkX *= CHUNK_SIZE;
            chunkY *= CHUNK_SIZE;
            chunkZ *= CHUNK_SIZE;
            px = (int)(position.x - chunkX);
            py = (int)(position.y - chunkY);
            pz = (int)(position.z - chunkZ);
        }


        /// <summary>
        /// 지정된 복셀에 콘텐츠가 있고 주변 6개 면 중 하나에서 볼 수 있는 경우 true를 반환합니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel is visible, <c>거짓</c> otherwise.</returns>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        public bool GetVoxelVisibility (VoxelChunk chunk, int voxelIndex) {
            if ((object)chunk == null || chunk.voxels[voxelIndex].isEmpty)
                return false;

            GetVoxelChunkCoordinates(voxelIndex, out int px, out int py, out int pz);
            return GetVoxelVisibility(chunk, px, py, pz);
        }


        /// <summary>
        /// 지정된 복셀에 콘텐츠가 있고 주변 6개 면 중 하나에서 볼 수 있는 경우 true를 반환합니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel is visible, <c>거짓</c> otherwise.</returns>
        /// <param name="chunk">큰 덩어리.</param>
        bool GetVoxelVisibility (VoxelChunk chunk, int px, int py, int pz) {

            for (int o = 0; o < 6 * 3; o += 3) {
                VoxelChunk otherChunk = chunk;
                int ox = px + neighbourOffsets[o];
                int oy = py + neighbourOffsets[o + 1];
                int oz = pz + neighbourOffsets[o + 2];
                if (ox < 0) {
                    otherChunk = chunk.left;
                    ox = CHUNK_SIZE_MINUS_ONE;
                    if ((object)otherChunk == null)
                        return true;
                } else if (ox >= CHUNK_SIZE) {
                    ox = 0;
                    otherChunk = chunk.right;
                    if ((object)otherChunk == null)
                        return true;
                }
                if (oy < 0) {
                    otherChunk = chunk.bottom;
                    oy = CHUNK_SIZE_MINUS_ONE;
                    if ((object)otherChunk == null)
                        return true;
                } else if (oy >= CHUNK_SIZE) {
                    oy = 0;
                    otherChunk = chunk.top;
                    if ((object)otherChunk == null)
                        return true;
                }
                if (oz < 0) {
                    otherChunk = chunk.back;
                    oz = CHUNK_SIZE_MINUS_ONE;
                    if ((object)otherChunk == null)
                        return true;
                } else if (oz >= CHUNK_SIZE) {
                    oy = 0;
                    otherChunk = chunk.forward;
                    if ((object)otherChunk == null)
                        return true;
                }
                int otherIndex = GetVoxelIndex(ox, oy, oz);
                if (otherChunk.voxels[otherIndex].isEmpty)
                    return true;
            }

            return false;
        }


        /// <summary>
        /// 주어진 청크의 새로 고침을 요청합니다.청크 메시가 다시 생성되고 라이트맵이 다시 계산됩니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="ignoreFrustum">true인 경우 절두체 가시성이나 거리에 관계없이 청크가 렌더링됩니다.</param>
        public void ChunkRedraw (VoxelChunk chunk, bool includeNeighbours = false, bool refreshLightmap = true, bool refreshMesh = true, bool ignoreFrustum = false) {
            if (includeNeighbours) {
                RefreshNeighbourhood(chunk, refreshMesh, refreshLightmap, ignoreFrustum: ignoreFrustum);
            } else {
                ChunkRequestRefresh(chunk, refreshLightmap, refreshMesh, ignoreFrustum);
            }
        }

        /// <summary>
        /// 주어진 청크의 이웃에 대한 새로 고침을 요청합니다.
        /// </summary>
        /// <param name="ignoreFrustum">true인 경우 절두체 가시성이나 거리에 관계없이 청크가 렌더링됩니다.</param>
        public void ChunkRedrawNeighbours (VoxelChunk chunk, bool refreshLightmap = false, bool refreshMesh = true, bool ignoreFrustum = false) {
            RefreshNeighbourhood(chunk, refreshLightmap, refreshMesh, true, ignoreFrustum: ignoreFrustum);

        }

        /// <summary>
        /// 가시 거리 내에 있는 청크의 전역 새로 고침을 요청합니다.
        /// </summary>
        /// <param name="ignoreFrustum">true인 경우 절두체 가시성이나 거리에 관계없이 청크가 렌더링됩니다.</param>
        public void ChunkRedrawAll (bool refreshLightmap = true, bool refreshMesh = true, bool ignoreFrustum = false) {

            float d = _visibleChunksDistance * CHUNK_SIZE;
            Vector3d size = new Vector3d(d, d, d);
            ChunkRequestRefresh(new Boundsd(currentAnchorPos, size), refreshLightmap, refreshMesh, ignoreFrustum);
        }

        /// <summary>
        /// 주어진 위치에서 복셀을 가져옵니다.복셀이 없으면 Voxel.Empty를 반환합니다.
        /// </summary>
        /// <returns>아래 복셀.</returns>
        /// <param name="position">위치.</param>
        public Voxel GetVoxelUnder (Vector3d position, bool includeWater = false, ColliderTypes colliderTypes = ColliderTypes.AnyCollider) {
            VoxelHitInfo hitinfo;
            byte minOpaque = includeWater ? (byte)255 : (byte)0;
            if (RayCastFast(position, Misc.vector3down, out hitinfo, 0, false, minOpaque, colliderTypes)) {
                return hitinfo.chunk.voxels[hitinfo.voxelIndex];
            }
            return Voxel.Empty;
        }


        /// <summary>
        /// 주어진 위치에서 복셀을 가져옵니다.복셀이 없으면 Voxel.Empty를 반환합니다.
        /// </summary>
        /// <returns>아래 복셀.</returns>
        /// <param name="position">위치.</param>
        public VoxelIndex GetVoxelUnderIndex (Vector3d position, bool includeWater = false, ColliderTypes colliderTypes = ColliderTypes.AnyCollider) {
            VoxelIndex index = new VoxelIndex();
            VoxelHitInfo hitinfo;
            byte minOpaque = includeWater ? (byte)255 : (byte)0;
            if (RayCastFast(position, Misc.vector3down, out hitinfo, 0, false, minOpaque, colliderTypes)) {
                index.chunk = hitinfo.chunk;
                index.voxelIndex = hitinfo.voxelIndex;
                index.position = hitinfo.point;
                index.sqrDistance = hitinfo.sqrDistance;
            }
            return index;
        }

        /// <summary>
        /// 하나의 복셀의 색조 색상을 변경/설정합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="color">색상.</param>
        public void VoxelSetColor (Vector3d position, Color32 color) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex, false)) {
                VoxelSetColor(chunk, voxelIndex, color);
            }
        }

        /// <summary>
        /// 하나의 복셀의 색조 색상을 변경/설정합니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        /// <param name="color">색상.</param>
        public void VoxelSetColor (VoxelChunk chunk, int voxelIndex, Color32 color) {
            if ((object)chunk == null) return;

#if UNITY_EDITOR
            CheckEditorTintColor();
#endif
            chunk.voxels[voxelIndex].color = color;
            RegisterChunkChanges(chunk);
            ChunkRequestRefresh(chunk, false, true);
        }


        /// <summary>
        /// 하나의 복셀의 색조 색상을 가져옵니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        public Color32 GetVoxelColor (VoxelChunk chunk, int voxelIndex) {
            if ((object)chunk == null) return Misc.color32White;

#if UNITY_EDITOR
            CheckEditorTintColor();
#endif
            return chunk.voxels[voxelIndex].color;
        }


        /// <summary>
        /// 하나의 복셀의 색조 색상을 가져옵니다.
        /// </summary>
        /// <param name="position">위치.</param>
        public Color32 GetVoxelColor (Vector3d position) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex, false)) {
                return GetVoxelColor(chunk, voxelIndex);
            }
            return Misc.color32White;
        }

        /// <summary>
        /// 복셀을 숨길지 여부를 설정합니다.기본적으로 모든 복셀이 표시됩니다.복셀을 숨기는 것은 영구적이지 않습니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        public void VoxelSetHidden (VoxelChunk chunk, int voxelIndex, bool hidden, HideStyle hiddenStyle = HideStyle.DefinedByVoxelDefinition) {
            if ((object)chunk == null || voxelIndex < 0)
                return;

            VoxelSetHiddenOne(chunk, voxelIndex, hidden, hiddenStyle);
            ChunkRequestRefresh(chunk, false, true);
        }

        /// <summary>
        /// 복셀 목록을 숨길지 여부를 설정합니다.기본적으로 모든 복셀이 표시됩니다.복셀을 숨기는 것은 영구적이지 않습니다.
        /// </summary>
        /// <param name="indices">복셀 인덱스 목록입니다.</param>
        public void VoxelSetHidden (List<VoxelIndex> indices, bool hidden, HideStyle hiddenStyle = HideStyle.DefinedByVoxelDefinition) {
            if (indices == null)
                return;

            VoxelChunk lastChunk = null;
            int count = indices.Count;
            for (int k = 0; k < count; k++) {
                if (indices[k].chunk != null && indices[k].voxelIndex >= 0) {
                    VoxelSetHiddenOne(indices[k].chunk, indices[k].voxelIndex, hidden, hiddenStyle);
                    if (indices[k].chunk != lastChunk) {
                        lastChunk = indices[k].chunk;
                        ChunkRequestRefresh(indices[k].chunk, false, true);
                    }
                }
            }
        }

        /// <summary>
        /// 복셀 목록을 숨길지 여부를 설정합니다.기본적으로 모든 복셀이 표시됩니다.복셀을 숨기는 것은 영구적이지 않습니다.
        /// </summary>
        /// <param name="indices">복셀 인덱스 목록입니다.</param>
        public void VoxelSetHidden (VoxelIndex[] indices, int count, bool hidden, HideStyle hideStyle = HideStyle.DefinedByVoxelDefinition) {
            if (indices == null)
                return;

            VoxelChunk lastChunk = null;
            for (int k = 0; k < count; k++) {
                VoxelChunk chunk = indices[k].chunk;
                if (chunk != null && indices[k].voxelIndex >= 0) {
                    VoxelSetHiddenOne(chunk, indices[k].voxelIndex, hidden, hideStyle);
                    if (chunk != lastChunk) {
                        lastChunk = chunk;
                        ChunkRequestRefresh(chunk, false, true);
                    }
                }
            }
        }



        /// <summary>
        /// 복셀이 숨겨져 있으면 true를 반환합니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        public bool VoxelIsHidden (VoxelChunk chunk, int voxelIndex) {
            if ((object)chunk == null || chunk.voxelsExtraData == null)
                return false;
            VoxelHiddenData data;
            if (chunk.voxelsExtraData.TryGetValue(voxelIndex, out data)) {
                return data.hidden;
            }
            return false;
        }

        #region Voxel user properties

        /// <summary>
        /// 특정 복셀의 모든 속성을 지웁니다.
        /// </summary>
        public void VoxelClearProperties (VoxelChunk chunk, int voxelIndex) {
            if ((object)chunk != null && chunk.voxelsProperties != null) {
                chunk.voxelsProperties.Remove(voxelIndex);
            }
        }

        /// <summary>
        /// 특정 복셀의 특정 복셀 속성을 지웁니다.
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="voxelIndex"></param>
        /// <param name="propertyName"></param>
        public void VoxelClearProperty (VoxelChunk chunk, int voxelIndex, string propertyName) {
            int propertyId = propertyName.GetHashCode();
            VoxelClearProperty(chunk, voxelIndex, propertyId);
        }

        /// <summary>
        /// 특정 복셀의 특정 복셀 속성을 지웁니다.
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="voxelIndex"></param>
        /// <param name="propertyName"></param>
        public void VoxelClearProperty (VoxelChunk chunk, int voxelIndex, int propertyId) {
            if ((object)chunk == null || chunk.voxelsProperties == null) {
                return;
            }
            FastHashSet<VoxelProperty> voxelProperties;
            if (!chunk.voxelsProperties.TryGetValue(voxelIndex, out voxelProperties)) {
                return;
            }
            voxelProperties.Remove(propertyId);
        }

        /// <summary>
        /// 복셀에서 int 유형의 사용자 정의 속성을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public float GetVoxelPropertyFloat (VoxelChunk chunk, int voxelIndex, string propertyName) {
            int propertyId = propertyName.GetHashCode();
            return GetVoxelPropertyFloat(chunk, voxelIndex, propertyId);
        }


        /// <summary>
        /// 복셀에서 int 유형의 사용자 정의 속성을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public float GetVoxelPropertyFloat (VoxelChunk chunk, int voxelIndex, int propertyId) {
            if ((object)chunk == null || chunk.voxelsProperties == null) {
                return 0;
            }
            VoxelProperty property = VoxelGetProperty(chunk, voxelIndex, propertyId);
            return property.floatValue;
        }


        /// <summary>
        /// 복셀에서 문자열 유형의 사용자 정의 속성을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public string GetVoxelPropertyString (VoxelChunk chunk, int voxelIndex, string propertyName) {
            int propertyId = propertyName.GetHashCode();
            return GetVoxelPropertyString(chunk, voxelIndex, propertyId);
        }

        /// <summary>
        /// 복셀에서 문자열 유형의 사용자 정의 속성을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public string GetVoxelPropertyString (VoxelChunk chunk, int voxelIndex, int propertyId) {
            if ((object)chunk == null || chunk.voxelsProperties == null) {
                return null;
            }
            VoxelProperty property = VoxelGetProperty(chunk, voxelIndex, propertyId);
            return property.stringValue;
        }

        /// <summary>
        /// 정수 복셀 속성을 설정합니다.
        /// </summary>
        public void VoxelSetProperty (VoxelChunk chunk, int voxelIndex, string propertyName, float value) {
            int propertyId = propertyName.GetHashCode();
            VoxelSetProperty(chunk, voxelIndex, propertyId, value);
        }

        /// <summary>
        /// 정수 복셀 속성을 설정합니다.
        /// </summary>
        public void VoxelSetProperty (VoxelChunk chunk, int voxelIndex, int propertyId, float value) {
            if ((object)chunk == null) {
                return;
            }

            if (chunk.voxelsProperties == null) {
                chunk.voxelsProperties = new FastHashSet<FastHashSet<VoxelProperty>>();
            }
            FastHashSet<VoxelProperty> voxelProperties;
            if (!chunk.voxelsProperties.TryGetValue(voxelIndex, out voxelProperties)) {
                voxelProperties = new FastHashSet<VoxelProperty>();
                chunk.voxelsProperties[voxelIndex] = voxelProperties;
            }
            voxelProperties.TryGetValue(propertyId, out VoxelProperty prop);
            prop.floatValue = value;
            voxelProperties[propertyId] = prop;

        }


        /// <summary>
        /// 문자열 복셀 속성을 설정합니다.
        /// </summary>
        public void VoxelSetProperty (VoxelChunk chunk, int voxelIndex, string propertyName, string value) {
            int propertyId = propertyName.GetHashCode();
            VoxelSetProperty(chunk, voxelIndex, propertyId, value);
        }

        /// <summary>
        /// 문자열 복셀 속성을 설정합니다.
        /// </summary>
        public void VoxelSetProperty (VoxelChunk chunk, int voxelIndex, int propertyId, string value) {
            if ((object)chunk == null) {
                return;
            }

            if (chunk.voxelsProperties == null) {
                chunk.voxelsProperties = new FastHashSet<FastHashSet<VoxelProperty>>();
            }
            FastHashSet<VoxelProperty> voxelProperties;
            if (!chunk.voxelsProperties.TryGetValue(voxelIndex, out voxelProperties)) {
                voxelProperties = new FastHashSet<VoxelProperty>();
                chunk.voxelsProperties[voxelIndex] = voxelProperties;
            }
            VoxelProperty prop;
            voxelProperties.TryGetValue(propertyId, out prop);
            prop.stringValue = value;
            voxelProperties[propertyId] = prop;
        }

        #endregion


        /// <summary>
        /// 월드 공간 좌표의 지정된 위치에 새 복셀을 배치합니다.선택적으로 소리를 재생합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="voxelType">복셀.</param>
        /// <param name="playSound">If set to <c>진실</c> play sound.</param>
        public void VoxelPlace (Vector3d position, VoxelDefinition voxelType, bool playSound = false, bool refresh = true) {
            if (voxelType == null)
                return;
            VoxelPlace(position, voxelType, playSound, voxelType.tintColor, refresh: refresh);
        }

        /// <summary>
        /// 월드 공간 좌표의 지정된 위치에 새 복셀을 배치합니다.선택적으로 소리를 재생합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="voxelType">복셀.</param>
        /// <param name="tintColor">틴트 색상입니다.</param>
        /// <param name="playSound">If set to <c>진실</c> play sound.</param>
        public void VoxelPlace (Vector3d position, VoxelDefinition voxelType, Color tintColor, bool playSound = false, bool refresh = true) {
            VoxelPlace(position, voxelType, playSound, tintColor, refresh: refresh);
        }

        /// <summary>
        /// 지정된 색상을 사용하여 월드 공간 좌표의 특정 위치에 기본 복셀을 배치합니다.선택적으로 소리를 재생합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="tintColor">복셀의 색조 색상입니다.</param>
        /// <param name="playSound">If set to <c>진실</c> play sound when placing the voxel.</param>
        public void VoxelPlace (Vector3d position, Color tintColor, bool playSound = false, bool refresh = true) {
            VoxelPlace(position, defaultVoxel, playSound, tintColor, refresh: refresh);
        }


        /// <summary>
        /// 지정된 색상으로 기존 청크에 기본 복셀을 배치합니다.선택적으로 소리를 재생합니다.
        /// </summary>
        /// <param name="chunk">청크 개체입니다.</param>
        /// <param name="voxelIndex">복셀의 인덱스입니다.</param>
        /// <param name="tintColor">복셀의 색조 색상입니다.</param>
        /// <param name="playSound">If set to <c>진실</c> play sound when placing the voxel.</param>
        public void VoxelPlace (VoxelChunk chunk, int voxelIndex, Color tintColor, bool playSound = false, bool refresh = true) {
            Vector3d position = GetVoxelPosition(chunk, voxelIndex);
            VoxelPlace(position, defaultVoxel, playSound, tintColor, refresh: refresh);
        }

        /// <summary>
        /// 주어진 청크 내에 복셀 목록을 배치합니다.복셀 목록은 ModelBit 구조체 목록으로 제공됩니다.
        /// </summary>
        /// <param name="chunk">청크 개체입니다.</param>
        /// <param name="voxels">청크에 삽입할 복셀 목록입니다.</param>
        public void VoxelPlace (VoxelChunk chunk, List<ModelBit> voxels) {
            ModelPlace(chunk, voxels);
        }


        /// <summary>
        /// 주어진 위치에 복셀을 배치합니다.
        /// </summary>
        /// /// <param name="playSound">If set to <c>진실</c> play sound when placing the voxel.</param>
        public void VoxelPlace (Vector3d position, Voxel voxel, bool playSound = false, bool refresh = true) {
            VoxelPlace(position, voxelDefinitions[voxel.typeIndex], voxel.color, playSound, refresh: refresh);
        }


        /// <summary>
        /// 월드 공간 좌표의 지정된 위치에 새 복셀을 배치합니다.선택적으로 소리를 재생합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="voxelType">복셀.</param>
        /// <param name="playSound">If set to <c>진실</c> play sound when placing the voxel.</param>
        /// <param name="tintColor">복셀의 색조 색상입니다.</param>
        /// <param name="amount">특정 양의 물과 같은 복셀(0-1)을 배치하는 데에만 사용됩니다.</param>
        /// <param name="rotation">회전이 돌아갑니다.0, 1, 2 또는 3일 수 있으며 시계 방향 90도 단계 회전을 나타냅니다.</param>
        /// <param name="refresh">영향을 받은 청크를 새로 고쳐야 하는 경우</param>
        /// <param name="placeMicroVoxel">true이고 복셀 정의에 마이크로복셀이 있는 경우 마이크로복셀을 배치합니다.</param>
        public bool VoxelPlace (Vector3d position, VoxelDefinition voxelType, bool playSound, Color tintColor, float amount = 1f, int rotation = 0, bool refresh = true, bool placeMicroVoxels = true) {

            if (voxelType == null) {
                return true;
            }

#if UNITY_EDITOR
            if (!enableTinting && tintColor != Misc.colorWhite) {
                Debug.Log("Option enableTinting is disabled. To use colored voxels, please enable the option in the VoxelPlayEnvironment component inspector.");
            }
#endif

            // Check Connected Voxels rules
            if (voxelType.customVoxelDefinitionProvider != null) {
                voxelType = voxelType.customVoxelDefinitionProvider(position, voxelType, rotation);
                if (voxelType == null) return false;
            }

            // Check if voxelType is known
            if (voxelType.index <= 0) {
                AddVoxelDefinition(voxelType);
            }

            if (playSound) {
                PlayBuildSound(voxelType.buildSound, position);
            }

            return VoxelPlaceFast(position, voxelType, out _, out _, tintColor, amount, rotation, refresh, placeMicroVoxels);
        }



        /// <summary>
        /// 주어진 위치에 배치된 복셀이 충돌체와 겹치는 경우 true를 반환합니다.
        /// </summary>
        public bool VoxelOverlaps (Vector3d position, VoxelDefinition type, Quaternion rotation, int layerMask = -1) {
            // Check if the voxel will overlap any collider then 
            if (type.renderType == RenderType.Custom && type.prefabUsesCollider) {

                Bounds bounds = type.prefabColliderBounds; // .mesh.bounds;
                Vector3 extents = bounds.extents;
                FastVector.Multiply(ref extents, ref type.scale, 0.9f);

                Quaternion rot = type.GetRotation(position);

                Vector3 localPosition = bounds.center;
                localPosition = rot * localPosition;
                localPosition += type.GetOffset(position);
                localPosition = rotation * localPosition;
                position += localPosition;

                int collidersCount = Physics.OverlapBoxNonAlloc(position, extents, tempColliders, rotation * rot, layerMask, QueryTriggerInteraction.Ignore);
                return collidersCount > 0;
            }
            return Physics.OverlapBoxNonAlloc(position, new Vector3(0.45f, 0.45f, 0.45f), tempColliders, Misc.quaternionZero, layerMask, QueryTriggerInteraction.Ignore) > 0;
        }


        /// <summary>
        /// 주어진 위치에 많은 복셀을 배치합니다.이웃 청크에 알리는 작업을 처리합니다.
        /// </summary>
        /// <param name="positions">위치.</param>
        /// <param name="voxelType">복셀 유형.</param>
        /// <param name="tintColor">틴트 색상입니다.</param>
        public void VoxelPlace (List<Vector3d> positions, VoxelDefinition voxelType, Color32 tintColor, List<VoxelChunk> modifiedChunks = null) {

            VoxelChunk chunk;
            int voxelIndex;
            int count = positions.Count;

            List<VoxelChunk> updatedChunks = BufferPool<VoxelChunk>.Get();
            modificationTag++;

            if (voxelType == null) {
                for (int k = 0; k < count; k++) {
                    Vector3d position = positions[k];
                    if (GetVoxelIndex(position, out chunk, out voxelIndex, false)) {
                        ClearLightmapAtPosition(chunk, voxelIndex);
                        VoxelDestroyFastSingle(chunk, voxelIndex);
                        if (chunk.SetModified(modificationTag)) {
                            updatedChunks.Add(chunk);
                        }
                    }
                }
            } else {
                for (int k = 0; k < count; k++) {
                    Vector3d position = positions[k];
                    if (GetVoxelIndex(position, out chunk, out voxelIndex)) {
                        ClearLightmapAtPosition(chunk, voxelIndex);
                        if (captureEvents && OnVoxelBeforePlace != null) {
                            OnVoxelBeforePlace(position, chunk, voxelIndex, ref voxelType, ref tintColor);
                            if (voxelType == null)
                                continue;
                        }
                        chunk.voxels[voxelIndex].Set(voxelType, tintColor);

                        if (chunk.SetModified(modificationTag)) {
                            updatedChunks.Add(chunk);
                        }

                        if (captureEvents && OnVoxelAfterPlace != null) {
                            OnVoxelAfterPlace(position, chunk, voxelIndex);
                        }
                    }
                }
            }

            RegisterChunkChanges(updatedChunks);
            ChunkRequestRefresh(updatedChunks, true, true);

            if (modifiedChunks != null) {
                modifiedChunks.AddRange(updatedChunks);
            }

            BufferPool<VoxelChunk>.Release(updatedChunks);
        }



        /// <summary>
        /// 주어진 위치에 많은 복셀을 배치합니다.이웃 청크에 알리는 작업을 처리합니다.
        /// </summary>
        /// <param name="indices">배치를 위한 복셀 인덱스 배열입니다.</param>
        /// <param name="voxelType">복셀 유형.</param>
        /// <param name="tintColor">틴트 색상입니다.</param>
        /// <param name="modifiedChunks">선택적으로 수정된 청크 목록을 반환합니다.</param>
        public void VoxelPlace (List<VoxelIndex> indices, VoxelDefinition voxelType, Color tintColor, List<VoxelChunk> modifiedChunks = null) {
            int count = indices.Count;

            List<VoxelChunk> updatedChunks = BufferPool<VoxelChunk>.Get();
            modificationTag++;

            if (voxelType == null) {
                byte light = noLightValue;
                for (int k = 0; k < count; k++) {
                    VoxelIndex vi = indices[k];
                    vi.chunk.voxels[vi.voxelIndex].Clear(light);
                    if (vi.chunk.SetModified(modificationTag)) {
                        updatedChunks.Add(vi.chunk);
                    }
                }
            } else {
                for (int k = 0; k < count; k++) {
                    VoxelIndex vi = indices[k];
                    vi.chunk.voxels[vi.voxelIndex].Set(voxelType, tintColor);
                    ClearLightmapAtPosition(vi.chunk, vi.voxelIndex);
                    if (vi.chunk.SetModified(modificationTag)) {
                        updatedChunks.Add(vi.chunk);
                    }
                }
            }

            RegisterChunkChanges(updatedChunks);
            ChunkRequestRefresh(updatedChunks, true, true);

            if (modifiedChunks != null) {
                modifiedChunks.AddRange(updatedChunks);
            }

            BufferPool<VoxelChunk>.Release(updatedChunks);
        }


        /// <summary>
        /// 동일한 복셀 정의 및 선택적 색조 색상으로 영역을 채웁니다.
        /// </summary>
        /// <param name="boxMin">상자 최소.</param>
        /// <param name="boxMax">상자 최대.</param>
        /// <param name="voxelType">복셀 유형.</param>
        /// <param name="modifiedChunks">선택적으로 수정된 청크 목록을 반환합니다.</param>
        public void VoxelPlace (Vector3d boxMin, Vector3d boxMax, VoxelDefinition voxelType, List<VoxelChunk> modifiedChunks = null) {
            VoxelPlace(boxMin, boxMax, voxelType, Misc.colorWhite, modifiedChunks);
        }


        /// <summary>
        /// 동일한 복셀 정의 및 선택적 색조 색상으로 영역을 채웁니다.
        /// </summary>
        /// <param name="boxMin">상자 최소.</param>
        /// <param name="boxMax">상자 최대.</param>
        /// <param name="voxelType">복셀 유형.</param>
        /// <param name="tintColor">틴트 색상입니다.</param>
        /// <param name="modifiedChunks">선택적으로 수정된 청크 목록을 반환합니다.</param>
        public void VoxelPlace (Vector3d boxMin, Vector3d boxMax, VoxelDefinition voxelType, Color tintColor, List<VoxelChunk> modifiedChunks = null) {
            List<VoxelIndex> tempVoxelIndices = BufferPool<VoxelIndex>.Get();
            GetVoxelIndices(boxMin, boxMax, tempVoxelIndices, 0, -1);
            VoxelPlace(tempVoxelIndices, voxelType, tintColor, modifiedChunks);
            BufferPool<VoxelIndex>.Release(tempVoxelIndices);
        }


        /// <summary>
        /// 다양한 색상으로 최적화된 복셀 게임 개체를 생성합니다.
        /// </summary>
        /// <returns>게임 개체를 만듭니다.</returns>
        /// <param name="colors">Y/Z/X 분포의 색상입니다.</param>
        /// <param name="sizeX">크기 x.</param>
        /// <param name="sizeY">크기 y.</param>
        /// <param name="sizeZ">크기 z.</param>
        public GameObject VoxelCreateGameObject (Color32[] colors, int sizeX, int sizeY, int sizeZ) {
            return VoxelPlayConverter.GenerateVoxelObject(colors, sizeX, sizeY, sizeZ, Misc.vector3zero, Misc.vector3one);
        }

        /// <summary>
        /// 다양한 색상으로 최적화된 복셀 게임 개체를 생성합니다.
        /// </summary>
        /// <returns>게임 개체를 만듭니다.</returns>
        /// <param name="colors">Y/Z/X 분포의 색상입니다.</param>
        /// <param name="sizeX">크기 x.</param>
        /// <param name="sizeY">크기 y.</param>
        /// <param name="sizeZ">크기 z.</param>
        /// <param name="offset">메쉬 오프셋.</param>
        /// <param name="scale">메쉬 스케일.</param>
        public GameObject VoxelCreateGameObject (Color32[] colors, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale) {
            return VoxelPlayConverter.GenerateVoxelObject(colors, sizeX, sizeY, sizeZ, offset, scale);
        }


        /// <summary>
        /// 다양한 색상으로 최적화된 복셀 게임 개체를 생성합니다.
        /// </summary>
        /// <returns>게임 개체를 만듭니다.</returns>
        /// <param name="colors">Y/Z/X 분포의 색상입니다.</param>
        /// <param name="texture">모든 항목에 대한 선택적 텍스처</param>
        /// <param name="sizeX">크기 x.</param>
        /// <param name="sizeY">크기 y.</param>
        /// <param name="sizeZ">크기 z.</param>
        /// <param name="offset">메쉬 오프셋.</param>
        /// <param name="scale">메쉬 스케일.</param>
        public GameObject VoxelCreateGameObject (Color32[] colors, Texture2D texture, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale) {
            return VoxelPlayConverter.GenerateVoxelObject(colors, texture, null, sizeX, sizeY, sizeZ, offset, scale);
        }


        /// <summary>
        /// 다양한 색상으로 최적화된 복셀 게임 개체를 생성합니다.
        /// </summary>
        /// <returns>게임 개체를 만듭니다.</returns>
        /// <param name="colors">Y/Z/X 분포의 색상입니다.</param>
        /// <param name="textures">각 항목에 대한 선택적 텍스처</param>
        /// <param name="sizeX">크기 x.</param>
        /// <param name="sizeY">크기 y.</param>
        /// <param name="sizeZ">크기 z.</param>
        /// <param name="offset">메쉬 오프셋.</param>
        /// <param name="scale">메쉬 스케일.</param>
        public GameObject VoxelCreateGameObject (Color32[] colors, Texture2D[] textures, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale) {
            return VoxelPlayConverter.GenerateVoxelObject(colors, textures, null, sizeX, sizeY, sizeZ, offset, scale);
        }

        /// <summary>
        /// 다양한 색상으로 최적화된 복셀 게임 개체를 생성합니다.
        /// </summary>
        /// <returns>게임 개체를 만듭니다.</returns>
        /// <param name="colors">Y/Z/X 분포의 색상입니다.</param>
        /// <param name="textures">각 항목에 대한 선택적 텍스처</param>
        /// <param name="normalMaps">각 항목에 대한 선택적 노멀 맵</param>
        /// <param name="sizeX">크기 x.</param>
        /// <param name="sizeY">크기 y.</param>
        /// <param name="sizeZ">크기 z.</param>
        /// <param name="offset">메쉬 오프셋.</param>
        /// <param name="scale">메쉬 스케일.</param>
        public GameObject VoxelCreateGameObject (Color32[] colors, Texture2D texture, Texture2D normalMap, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale) {
            return VoxelPlayConverter.GenerateVoxelObject(colors, texture, normalMap, sizeX, sizeY, sizeZ, offset, scale);
        }


        /// <summary>
        /// 다양한 색상으로 최적화된 복셀 게임 개체를 생성합니다.
        /// </summary>
        /// <returns>게임 개체를 만듭니다.</returns>
        /// <param name="colors">Y/Z/X 분포의 색상입니다.</param>
        /// <param name="textures">각 항목에 대한 선택적 텍스처</param>
        /// <param name="normalMaps">각 항목에 대한 선택적 노멀 맵</param>
        /// <param name="sizeX">크기 x.</param>
        /// <param name="sizeY">크기 y.</param>
        /// <param name="sizeZ">크기 z.</param>
        /// <param name="offset">메쉬 오프셋.</param>
        /// <param name="scale">메쉬 스케일.</param>
        public GameObject VoxelCreateGameObject (Color32[] colors, Texture2D[] textures, Texture2D[] normalMaps, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale) {
            return VoxelPlayConverter.GenerateVoxelObject(colors, textures, normalMaps, sizeX, sizeY, sizeZ, offset, scale);
        }


        /// <summary>
        /// 모델 정의에서 최적화된 복셀 게임 개체를 생성합니다.
        /// </summary>
        /// <param name="modelDefinition"></param>
        /// <param name="offset"></param>
        /// <param name="scale"></param>
        /// <param name="useTextures"></param>
        /// <param name="useNormals"></param>
        /// <returns></returns>
        public GameObject VoxelCreateGameObject (ModelDefinition modelDefinition, bool useTextures = true, bool useNormals = true) {
            return VoxelPlayConverter.GenerateVoxelObject(modelDefinition, Misc.vector3zero, Misc.vector3one, useTextures, useNormals);
        }

        /// <summary>
        /// 모델 정의에서 최적화된 복셀 게임 개체를 생성합니다.
        /// </summary>
        /// <param name="modelDefinition"></param>
        /// <param name="offset"></param>
        /// <param name="scale"></param>
        /// <param name="useTextures"></param>
        /// <param name="useNormals"></param>
        /// <returns></returns>
        public GameObject VoxelCreateGameObject (ModelDefinition modelDefinition, Vector3 offset, Vector3 scale, bool useTextures = true, bool useNormals = true) {
            return VoxelPlayConverter.GenerateVoxelObject(modelDefinition, offset, scale, useTextures, useNormals);
        }


        /// <summary>
        /// 주어진 위치의 복셀을 기반으로 큐브 게임오브젝트를 반환합니다.
        /// </summary>
        public GameObject VoxelToCube (Vector3d position) {
            if (!GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) return null;
            return MakeCubeFromVoxel(chunk, voxelIndex);
        }

        /// <summary>
        /// 주어진 위치의 복셀을 기반으로 큐브 게임오브젝트를 반환합니다.
        /// </summary>
        public GameObject VoxelToCube (VoxelChunk chunk, int voxelIndex) {
            return MakeCubeFromVoxel(chunk, voxelIndex);
        }

        /// <summary>
        /// 복셀을 손상시킵니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was hit, <c>거짓</c> otherwise.</returns>
        /// <param name="position">위치.</param>
        public bool VoxelDamage (Vector3d position, int damage, bool playSound = false) {
            VoxelChunk chunk;
            int voxelIndex;
            if (!GetVoxelIndex(position, out chunk, out voxelIndex, false) || chunk.voxels[voxelIndex].isEmpty)
                return false;
            bool impact = HitVoxelFast(position, Misc.vector3down, damage, out _, 1, 1, false, playSound);
            return impact;
        }


        /// <summary>
        /// 특정 방향에서 복셀에 손상을 줍니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was hit, <c>거짓</c> otherwise.</returns>
        /// <param name="position">위치.</param>
        public bool VoxelDamage (Vector3d position, Vector3 hitDirection, int damage, bool addParticles = false, bool playSound = false) {
            if (!GetVoxelIndex(position, out VoxelChunk _, out _)) {
                return false;
            }

            return HitVoxelFast(position - hitDirection, hitDirection, damage, out _, addParticles: addParticles, playSound: playSound);
        }



        /// <summary>
        /// 특정 방향에서 복셀에 손상을 줍니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was hit, <c>거짓</c> otherwise.</returns>
        /// <param name="voxelPosition">복셀의 위치.</param>
        /// <param name="hitPoint">히트 위치.</param>
        /// <param name="normal">복셀 표면의 법선입니다.</param>
        public bool VoxelDamage (Vector3d voxelPosition, Vector3d hitPoint, Vector3 normal, int damage, bool addParticles = false, bool playSound = false) {
            if (!BuildVoxelHitInfo(out VoxelHitInfo hitInfo, voxelPosition, hitPoint, normal)) return false;
            return VoxelDamage(hitInfo, damage, addParticles, playSound);
        }



        /// <summary>
        /// VoxelHitInfo 구조체의 데이터를 사용하여 복셀을 손상시킵니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was hit, <c>거짓</c> otherwise.</returns>
        public bool VoxelDamage (VoxelHitInfo hitInfo, int damage, bool addParticles = false, bool playSound = false) {
            if ((object)hitInfo.chunk == null || hitInfo.voxelIndex < 0)
                return false;
            DamageVoxelFast(ref hitInfo, damage, addParticles, playSound);
            return true;
        }


        /// <summary>
        /// VoxelHitInfo 구조체의 데이터를 사용하여 복셀을 손상시킵니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was hit, <c>거짓</c> otherwise.</returns>
        public bool VoxelDamage (VoxelHitInfo hitInfo, int damage, bool addParticles = false, bool playSound = false, bool showDamageCracks = true, bool canAddRecoverableVoxel = true) {
            if ((object)hitInfo.chunk == null || hitInfo.voxelIndex < 0)
                return false;
            DamageVoxelFast(ref hitInfo, damage, addParticles, playSound, showDamageCracks, canAddRecoverableVoxel);
            return true;
        }


        /// <summary>
        /// 반경 내의 모든 복셀에 손상을 입히는 특정 위치에서의 폭발을 시뮬레이션합니다.
        /// </summary>
        /// <returns><c>정수</c>, 손상된 복셀 수<c>거짓</c> otherwise.</returns>
        /// <param name="origin">폭발 기원.</param>
        /// <param name="damage">최대 손상.</param>
        /// <param name="radius">반지름.</param>
        /// <param name="attenuateDamageWithDistance">If set to <c>진실</c> damage will be reduced with distance.</param>
        /// <param name="addParticles">If set to <c>진실</c> damage particles will be added.</param>
        /// <param name="canAddRecoverableVoxel">true인 경우 복셀이 파괴될 때 부동 복구 가능한 복셀을 삭제할 수 있습니다.</param>
        public int VoxelDamage (Vector3d origin, int damage, int radius, bool attenuateDamageWithDistance, bool addParticles, bool playSound = false, bool showDamageCracks = false, bool canAddRecoverableVoxel = true) {
            return DamageAreaFast(origin, damage, radius, attenuateDamageWithDistance, addParticles, null, playSound, showDamageCracks, canAddRecoverableVoxel);
        }

        /// <summary>
        /// 반경 내의 모든 복셀에 손상을 입히는 특정 위치에서의 폭발을 시뮬레이션합니다.
        /// </summary>
        /// <returns><c>정수</c>, 손상된 복셀 수<c>거짓</c> otherwise.</returns>
        /// <param name="origin">폭발 기원.</param>
        /// <param name="damage">최대 손상.</param>
        /// <param name="radius">반지름.</param>
        /// <param name="attenuateDamageWithDistance">If set to <c>진실</c> damage will be reduced with distance.</param>
        /// <param name="addParticles">If set to <c>진실</c> damage particles will be added.</param>
        /// <param name="damagedVoxels">이미 초기화된 목록을 전달하여 손상된 복셀을 반환합니다.</param>
        /// <param name="canAddRecoverableVoxel">true인 경우 복셀이 파괴될 때 부동 복구 가능한 복셀을 삭제할 수 있습니다.</param>
        public int VoxelDamage (Vector3d origin, int damage, int radius, bool attenuateDamageWithDistance, bool addParticles, List<VoxelIndex> damagedVoxels, bool playSound = false, bool canAddRecoverableVoxel = true) {
            return DamageAreaFast(origin, damage, radius, attenuateDamageWithDistance, addParticles, damagedVoxels, playSound, canAddRecoverableVoxel);
        }


        /// <summary>
        /// 복셀을 지웁니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was destroyed, <c>거짓</c> otherwise.</returns>
        public bool VoxelDestroy (VoxelChunk chunk, int voxelIndex) {
            if ((object)chunk == null)
                return false;
            if (chunk.voxels[voxelIndex].hasContent) {
                VoxelDestroyFast(chunk, voxelIndex);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 복셀을 지웁니다.
        /// </summary>
        /// <returns><c>진실</c>, if voxel was destroyed, <c>거짓</c> otherwise.</returns>
        /// <param name="position">위치.</param>
        public bool VoxelDestroy (Vector3d position) {
            if (!GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex, false)) {
                return false;
            }
            if (chunk.voxels[voxelIndex].hasContent) {
                VoxelDestroyFast(chunk, voxelIndex);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 주어진 범위 내의 모든 복셀을 파괴합니다.
        /// </summary>
        public bool VoxelDestroy (Bounds bounds) {
            List<VoxelIndex> voxelIndices = BufferPool<VoxelIndex>.Get();
            Vector3d min = bounds.min + Misc.vector3one * 0.1f;
            Vector3d max = bounds.max - Misc.vector3one * 0.1f;
            GetVoxelIndices(min, max, voxelIndices);
            foreach (var vi in voxelIndices) {
                VoxelDestroyFast(vi.chunk, vi.voxelIndex);
            }
            BufferPool<VoxelIndex>.Release(voxelIndices);
            return true;
        }


        /// <summary>
        /// 특정 위치 위에 "willCollapse" 플래그가 있는 모든 복셀이 붕괴되어 떨어지도록 만듭니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="amount">축소할 최대 복셀 수입니다.</param>
        /// <param name="voxelIndices">voxelIndices가 제공되면 축소되는 복셀로 채워집니다.</param>
        /// <param name="consolidateDelay">ConsolidDelay가 0보다 큰 경우 축소된 복셀은 '지속 시간'(초) 후에 파괴되거나 일반 복셀로 다시 변환됩니다.</param>
        public void VoxelCollapse (Vector3d position, int amount, List<VoxelIndex> voxelIndices = null, float consolidateDelay = 0) {
            List<VoxelIndex> tempVoxelIndices = BufferPool<VoxelIndex>.Get();
            int count = GetCrumblyVoxelIndices(position, amount, tempVoxelIndices);

            if (voxelIndices != null) {
                voxelIndices.Clear();
                voxelIndices.AddRange(tempVoxelIndices);
            }
            if (count > 0) {
                VoxelGetDynamic(tempVoxelIndices, true, consolidateDelay);
            }
            if (captureEvents && OnVoxelCollapse != null) {
                OnVoxelCollapse(tempVoxelIndices);
            }
            BufferPool<VoxelIndex>.Release(tempVoxelIndices);
        }

        /// <summary>
        /// 주어진 복셀이 동적이면 true를 반환합니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        public bool VoxelIsDynamic (VoxelChunk chunk, int voxelIndex) {
            VoxelPlaceholder placeHolder = GetVoxelPlaceholder(chunk, voxelIndex, true);
            if (placeHolder == null)
                return false;

            if (placeHolder.modelMeshFilter == null)
                return false;

            return true;
        }


        /// <summary>
        /// 동적 복셀을 다시 일반 복셀로 변환합니다.이 작업을 수행하면 현재 복셀 위치가 이미 다른 복셀에 의해 점유된 경우 복셀이 파괴됩니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        /// <param name="immediate">true인 경우 복셀이 즉시 업데이트됩니다.false인 경우 복셀은 카메라 절두체에 있지 않고 플레이어로부터 특정 거리에 있을 때 업데이트됩니다.</param>
        public bool VoxelCancelDynamic (VoxelChunk chunk, int voxelIndex, bool immediate = true) {

            // If no voxel there cancel
            if ((object)chunk == null || chunk.voxels[voxelIndex].isEmpty)
                return false;

            // If it a dynamic voxel?
            VoxelPlaceholder placeholder = GetVoxelPlaceholder(chunk, voxelIndex, false);
            if (placeholder == null)
                return false;

            if (placeholder.modelMeshFilter == null)
                return false;

            if (immediate) {
                placeholder.CancelDynamicNow();
            } else {
                placeholder.CancelDynamic();
            }
            return true;
        }


        /// <summary>
        /// 동적 복셀을 다시 일반 복셀로 변환합니다.이 작업을 수행하면 현재 복셀 위치가 이미 다른 복셀에 의해 점유된 경우 복셀이 파괴됩니다.
        /// </summary>
        /// <param name="placeholder">자리 표시자.</param>
        public bool VoxelCancelDynamic (VoxelPlaceholder placeholder) {

            // No model instance? Return
            if (placeholder == null || placeholder.modelInstance == null)
                return false;

            // Check if voxel is of dynamic type
            VoxelChunk chunk = placeholder.chunk;
            if ((object)chunk == null)
                return false;

            int voxelIndex = placeholder.voxelIndex;
            if (voxelIndex < 0 || chunk.voxels[voxelIndex].isEmpty)
                return false;

            VoxelDefinition voxelType = chunk.voxels[voxelIndex].type.staticDefinition;
            if (voxelType != null) {
                Color voxelColor = chunk.voxels[voxelIndex].color;
                Vector3d targetPosition = placeholder.transform.position;

                // Places the voxel if destination target is empty (it could have moved by physics or gravity)
                if (GetVoxelIndex(targetPosition, out VoxelChunk targetChunk, out int targetVoxelIndex, false)) {
                    if (targetVoxelIndex != voxelIndex || targetChunk != chunk) {

                        // Removes old voxel from chunk (this call also removed placeholder gameobject)
                        VoxelDestroyFastSingle(chunk, voxelIndex);

                        // Place only if it's empty at the new position
                        if (targetChunk.voxels[targetVoxelIndex].opaque < 3) {
                            targetChunk.voxels[targetVoxelIndex].Set(voxelType, voxelColor);
                            targetChunk.SetNeedsColliderRebuild();
                            RegisterChunkChanges(targetChunk);

                            // Clear lighting
                            ClearLightmapAtPosition(targetChunk, targetVoxelIndex);
                        }
                    } else {
                        // If it has not moved from original position, just replace with original type
                        targetChunk.voxels[targetVoxelIndex].Set(voxelType, voxelColor);
                        targetChunk.SetNeedsColliderRebuild();
                        RegisterChunkChanges(targetChunk);

                        // Clear lighting
                        ClearLightmapAtPosition(targetChunk, targetVoxelIndex);

                        // Remove placeholder with small delay to prevent flickering between old voxel being removed and the chunk getting refreshed
                        VoxelPlaceholderDestroy(placeholder.chunk, placeholder.voxelIndex, 0.3f);
                    }
                } else {
                    // There's no chunk where the voxel has moved, just remove the voxel
                    VoxelPlaceholderDestroy(placeholder.chunk, placeholder.voxelIndex, 0.3f);
                }
            }

            ChunkRequestRefresh(chunk, clearLightmap: false, refreshMesh: true);

            return true;
        }



        /// <summary>
        /// 복셀 목록을 동적 게임 개체로 변환합니다.
        /// </summary>
        /// <param name="voxelIndices">복셀 지수.</param>
        /// <param name="addRigidbody">If set to <c>진실</c> add rigidbody.</param>
        /// <param name="duration">지속 시간이 0보다 크면 복셀은 'duration'초 후에 일반 복셀로 다시 변환됩니다.</param>
        public void VoxelGetDynamic (List<VoxelIndex> voxelIndices, bool addRigidbody = false, float duration = 0) {

            List<VoxelChunk> tempChunks = BufferPool<VoxelChunk>.Get();
            modificationTag++;

            int count = voxelIndices.Count;
            for (int k = 0; k < count; k++) {
                VoxelIndex vi = voxelIndices[k];
                VoxelChunk chunk = vi.chunk;
                GameObject obj = VoxelSetDynamic(chunk, vi.voxelIndex, addRigidbody, duration);
                if (obj == null)
                    continue;
                if (chunk.SetModified(modificationTag)) {
                    tempChunks.Add(chunk);
                }
                SpreadLightmapAroundPosition(chunk, vi.voxelIndex);
            }

            ChunkRequestRefresh(tempChunks, false, true);
            RegisterChunkChanges(tempChunks);

            BufferPool<VoxelChunk>.Release(tempChunks);
        }


        /// <summary>
        /// 복셀을 동적 게임 개체로 변환합니다.복셀이 이미 변환된 경우 게임 개체에 대한 참조만 반환합니다.
        /// </summary>
        /// <returns>역동적이게 됩니다.</returns>
        /// <param name="position">월드 공간의 복셀 위치.</param>
        /// <param name="addRigidbody">If set to <c>진실</c> add rigidbody.</param>
        /// <param name="duration">지속 시간이 0보다 크면 복셀은 'duration'초 후에 파괴되거나 일반 복셀로 다시 변환됩니다.</param>
        public GameObject VoxelGetDynamic (Vector3d position, bool addRigidbody = false, float duration = 0) {
            VoxelChunk chunk;
            int voxelIndex;
            if (!GetVoxelIndex(position, out chunk, out voxelIndex))
                return null;
            return VoxelGetDynamic(chunk, voxelIndex, addRigidbody, duration);
        }


        /// <summary>
        /// 복셀을 동적 게임 개체로 변환합니다.복셀이 이미 변환된 경우 게임 개체에 대한 참조만 반환합니다.
        /// </summary>
        /// <returns>역동적이게 됩니다.</returns>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        /// <param name="addRigidbody">If set to <c>진실</c> add rigidbody.</param>
        /// <param name="duration">지속 시간이 0보다 크면 복셀은 'duration'초 후에 파괴되거나 일반 복셀로 다시 변환됩니다.</param>
        public GameObject VoxelGetDynamic (VoxelChunk chunk, int voxelIndex, bool addRigidbody = false, float duration = 0) {

            if ((object)chunk == null || chunk.voxels[voxelIndex].isEmpty)
                return null;

            VoxelDefinition vd = voxelDefinitions[chunk.voxels[voxelIndex].typeIndex];
            if (!vd.renderType.supportsDynamic()) {
#if UNITY_EDITOR
                Debug.LogError("Only opaque, transparent, opaque-no-AO and cutout voxel types can be converted to dynamic voxels.");
#endif
                return null;
            }

            GameObject obj = VoxelSetDynamic(chunk, voxelIndex, addRigidbody, duration);
            if (obj == null)
                return null;

            // If voxel is already custom-type, then just returns the placeholder gameobject
            if (vd.renderType == RenderType.Custom)
                return obj;

            // Notify changes
            RegisterChunkChanges(chunk);

            // Refresh chunk
            ChunkRequestRefresh(chunk, true, true);

            return obj;
        }


        /// <summary>
        /// 복구 가능한 복셀을 생성하여 지정된 위치, 방향 및 강도로 던집니다.
        /// </summary>
        /// <param name="position">월드 공간에서의 위치.</param>
        /// <param name="direction">방향.</param>
        /// <param name="voxelType">복셀 정의.</param>
        public GameObject VoxelThrow (Vector3d position, Vector3 direction, float velocity, VoxelDefinition voxelType, Color32 color) {
            GameObject voxelGO = CreateRecoverableVoxel(position, voxelType, color);
            if (voxelGO == null)
                return null;
            if (!voxelGO.TryGetComponent(out Rigidbody rb)) {
                return null;
            }
            rb.velocity = direction * velocity;
            return voxelGO;
        }

        /// <summary>
        /// 복셀을 회전합니다.
        /// </summary>
        /// <param name="position">월드 공간의 복셀 위치.</param>
        /// <param name="angleX">각도 x(도)</param>
        /// <param name="angleY">각도 y(도).</param>
        /// <param name="angleZ">각도 z(도)입니다.</param>
        public void VoxelRotate (Vector3d position, float angleX, float angleY, float angleZ) {
            if (!GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex, false)) {
                return;
            }
            VoxelRotate(chunk, voxelIndex, angleX, angleY, angleZ);
        }

        /// <summary>
        /// 복셀을 회전합니다.
        /// </summary>
        /// <param name="chunk">복셀의 덩어리.</param>
        /// <param name="voxelIndex">청크의 복셀 인덱스입니다.</param>
        /// <param name="angleX">각도 x(도)</param>
        /// <param name="angleY">각도 y(도).</param>
        /// <param name="angleZ">각도 z(도)입니다.</param>
        public void VoxelRotate (VoxelChunk chunk, int voxelIndex, float angleX, float angleY, float angleZ) {
            GameObject obj = VoxelGetDynamic(chunk, voxelIndex);
            if (obj != null) {
                obj.transform.Rotate(angleX, angleY, angleZ);
                RegisterChunkChanges(chunk);
            }
        }

        /// <summary>
        /// 복셀의 회전을 설정합니다.필요한 경우 복셀이 먼저 동적으로 변환됩니다.
        /// </summary>
        /// <param name="chunk">복셀의 덩어리.</param>
        /// <param name="voxelIndex">청크의 복셀 인덱스입니다.</param>
        /// <param name="angleX">각도 x(도)</param>
        /// <param name="angleY">각도 y(도).</param>
        /// <param name="angleZ">각도 z(도)입니다.</param>
        public void VoxelSetRotation (VoxelChunk chunk, int voxelIndex, float angleX, float angleY, float angleZ) {
            GameObject obj = VoxelGetDynamic(chunk, voxelIndex);
            if (obj != null) {
                obj.transform.localRotation = Quaternion.Euler(angleX, angleY, angleZ);
                RegisterChunkChanges(chunk);
            }
        }

        /// <summary>
        /// 복셀의 회전을 설정합니다.필요한 경우 복셀이 먼저 동적으로 변환됩니다.
        /// </summary>
        /// <param name="chunk">복셀의 덩어리.</param>
        /// <param name="voxelIndex">청크의 복셀 인덱스입니다.</param>
        public void VoxelSetRotation (VoxelChunk chunk, int voxelIndex, Quaternion rotation) {
            GameObject obj = VoxelGetDynamic(chunk, voxelIndex);
            if (obj != null) {
                obj.transform.localRotation = rotation;
                RegisterChunkChanges(chunk);
            }
        }


        /// <summary>
        /// 복셀의 회전을 설정합니다.필요한 경우 복셀이 먼저 동적으로 변환됩니다.
        /// </summary>
        /// <param name="position">복셀의 위치.</param>
        /// <param name="angleX">각도 x(도)</param>
        /// <param name="angleY">각도 y(도).</param>
        /// <param name="angleZ">각도(도)입니다.</param>
        public void VoxelSetRotation (Vector3d position, float angleX, float angleY, float angleZ) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) {
                VoxelSetRotation(chunk, voxelIndex, angleX, angleY, angleZ);
            }
        }

        /// <summary>
        /// 복셀의 회전을 설정합니다.복셀이 먼저 동적으로 변환됩니다.
        /// </summary>
        /// <param name="position">복셀의 위치.</param>
        public void VoxelSetRotation (Vector3d position, Quaternion rotation) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) {
                VoxelSetRotation(chunk, voxelIndex, rotation);
            }
        }

        /// <summary>
        /// 사용자 정의 또는 동적 복셀의 회전을 반환합니다.
        /// </summary>
        /// <param name="chunk">복셀의 덩어리.</param>
        /// <param name="voxelIndex">청크의 복셀 인덱스입니다.</param>
        public Quaternion GetVoxelRotation (VoxelChunk chunk, int voxelIndex) {
            VoxelPlaceholder placeHolder = GetVoxelPlaceholder(chunk, voxelIndex, false);
            if (placeHolder != null) {
                return placeHolder.transform.localRotation;
            }
            return Misc.quaternionZero;
        }

        /// <summary>
        /// 사용자 정의 또는 동적 복셀의 회전을 반환합니다.
        /// </summary>
        /// <param name="chunk">복셀의 덩어리.</param>
        /// <param name="voxelIndex">청크의 복셀 인덱스입니다.</param>
        public Quaternion GetVoxelRotation (Vector3d position) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex, false)) {
                return GetVoxelRotation(chunk, voxelIndex);
            }
            return Misc.quaternionZero;
        }

        /// <summary>
        /// 복셀 측면 텍스처의 회전을 설정합니다.
        /// </summary>
        /// <param name="position">월드 공간의 복셀 위치.</param>
        /// <param name="rotation">회전이 돌아갑니다.0, 1, 2 또는 3일 수 있으며 시계 방향 90도 단계 회전을 나타냅니다.</param>
        public bool VoxelSetTexturesRotation (Vector3d position, int rotation) {
            VoxelChunk chunk;
            int voxelIndex;
            if (!GetVoxelIndex(position, out chunk, out voxelIndex, false)) {
                return false;
            }
            return VoxelSetTexturesRotation(chunk, voxelIndex, rotation);
        }


        /// <summary>
        /// 복셀 측면 텍스처의 회전을 설정합니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        /// <param name="rotation">회전이 돌아갑니다.0, 1, 2 또는 3일 수 있으며 시계 방향 90도 단계 회전을 나타냅니다.</param>
        public bool VoxelSetTexturesRotation (VoxelChunk chunk, int voxelIndex, int rotation) {
            if ((object)chunk == null || voxelIndex < 0)
                return false;

            VoxelDefinition vd = voxelDefinitions[chunk.voxels[voxelIndex].typeIndex];
            if (vd.allowsTextureRotation && vd.renderType.supportsTextureRotation()) {
                int currentRotation = chunk.voxels[voxelIndex].GetTextureRotation();
                if (currentRotation != rotation) {
                    chunk.voxels[voxelIndex].SetTextureRotation(rotation);
                    RegisterChunkChanges(chunk);
                    ChunkRequestRefresh(chunk, false, true);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 복셀의 측면 텍스처에 대한 현재 회전을 반환합니다.
        /// </summary>
        /// <returns>텍스처 회전 가져오기입니다.</returns>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        public int GetVoxelTexturesRotation (VoxelChunk chunk, int voxelIndex) {
            if ((object)chunk != null && voxelIndex >= 0) {
                return chunk.voxels[voxelIndex].GetTextureRotation();
            }
            return 0;
        }


        /// <summary>
        /// 복셀의 측면 텍스처에 대한 현재 회전을 반환합니다.
        /// </summary>
        /// <returns>텍스처 회전 가져오기입니다.</returns>
        public int GetVoxelTexturesRotation (Vector3d position) {
            if (GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) {
                return GetVoxelTexturesRotation(chunk, voxelIndex);
            } else {
                return 0;
            }
        }



        /// <summary>
        /// 장면에 배치된 사용자 정의 복셀과 연관된 게임 개체를 반환합니다(복셀 정의에 "Create GameObject" 옵션이 활성화된 경우)
        /// </summary>
        public GameObject GetVoxelGameObject (Vector3d position) {
            if (!GetVoxelIndex(position, out VoxelChunk chunk, out int voxelIndex)) return null;
            return GetVoxelGameObject(chunk, voxelIndex);
        }

        /// <summary>
        /// 장면에 배치된 사용자 정의 복셀과 연관된 게임 개체를 반환합니다(복셀 정의에 "Create GameObject" 옵션이 활성화된 경우)
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        public GameObject GetVoxelGameObject (VoxelChunk chunk, int voxelIndex) {
            VoxelPlaceholder placeholder = GetVoxelPlaceholder(chunk, voxelIndex, false);
            if (placeholder != null) {
                return placeholder.modelInstance;
            }
            return null;
        }

        /// <summary>
        /// 복셀의 현재 저항 포인트를 반환합니다.
        /// </summary>
        public int GetVoxelResistancePoints (Vector3d position) {
            VoxelChunk chunk;
            int voxelIndex;
            if (GetVoxelIndex(position, out chunk, out voxelIndex)) {
                return GetVoxelResistancePoints(chunk, voxelIndex);
            }
            return 0;
        }


        /// <summary>
        /// 복셀의 현재 저항 포인트를 반환합니다.
        /// </summary>
        public int GetVoxelResistancePoints (VoxelChunk chunk, int voxelIndex) {
            if ((object)chunk == null || voxelIndex < 0) return 0;
            VoxelPlaceholder placeholder = GetVoxelPlaceholder(chunk, voxelIndex, false);
            if (placeholder != null) {
                return placeholder.resistancePointsLeft;
            } else {
                return chunk.voxels[voxelIndex].hasContent ? chunk.voxels[voxelIndex].type.resistancePoints : 0;
            }
        }


        /// <summary>
        /// 복셀의 측면 텍스처를 회전합니다.
        /// </summary>
        /// <param name="position">월드 공간의 복셀 위치.</param>
        /// <param name="rotation">회전합니다(0, 1, 2 또는 3).각 회전은 90도 회전을 나타냅니다.</param>
        public bool VoxelRotateTextures (Vector3d position, int rotation) {
            VoxelChunk chunk;
            int voxelIndex;
            if (!GetVoxelIndex(position, out chunk, out voxelIndex, false)) {
                return false;
            }
            return VoxelRotateTextures(chunk, voxelIndex, rotation);
        }

        /// <summary>
        /// 복셀의 측면 텍스처를 회전합니다.
        /// </summary>
        /// <param name="chunk">복셀의 덩어리.</param>
        /// <param name="voxelIndex">청크의 복셀 인덱스입니다.</param>
        /// <param name="rotation">회전합니다(0, 1, 2 또는 3).각 회전은 90도 회전을 나타냅니다.긍정적일 수도 있고 부정적일 수도 있습니다.</param>
        public bool VoxelRotateTextures (VoxelChunk chunk, int voxelIndex, int rotation) {
            if ((object)chunk == null || voxelIndex < 0 || !voxelDefinitions[chunk.voxels[voxelIndex].typeIndex].renderType.supportsTextureRotation())
                return false;

            int currentRotation = chunk.voxels[voxelIndex].GetTextureRotation();
            currentRotation = (currentRotation + rotation + 128000) % 4; // avoids negative values by adding a fairly large number
            chunk.voxels[voxelIndex].SetTextureRotation(currentRotation);
            RegisterChunkChanges(chunk);
            ChunkRequestRefresh(chunk, false, true);
            return true;
        }



        /// <summary>
        /// 월드 공간 위치가 주어진 청크를 지웁니다.
        /// </summary>
        /// <param name="position">위치.</param>
        public bool ChunkDestroy (Vector3d position) {
            GetChunk(position, out VoxelChunk chunk, false);
            return ChunkDestroy(chunk);
        }


        /// <summary>
        /// 기존 청크를 모두 지웁니다.
        /// </summary>
        public void ChunkDestroyAll () {
            tempChunks.Clear();
            GetChunks(tempChunks, ChunkModifiedFilter.All);
            foreach (var chunk in tempChunks) {
                ChunkDestroy(chunk);
            }
        }


        /// <summary>
        /// 월드 공간 위치가 주어진 청크를 지웁니다.
        /// </summary>
        public bool ChunkDestroy (VoxelChunk chunk) {
            if ((object)chunk == null)
                return false;
            ChunkClearFast(chunk);
            RegisterChunkChanges(chunk);

            // Refresh rendering
            UpdateChunkRR(chunk);

            return true;
        }

        /// <summary>
        /// 청크 게임오브젝트를 파괴하고 풀에 릴리스합니다.
        /// </summary>
        public void ChunkRelease (VoxelChunk chunk) {
            if ((object)chunk == null) return;
            chunk.PrepareForReuse(effectiveGlobalIllumination ? FULL_DARK : FULL_LIGHT);
            ReleaseChunkNavMesh(chunk);
            if (chunk.mc != null && chunk.mc.sharedMesh != null) DestroyImmediate(chunk.mc.sharedMesh);
            if (chunk.mf.sharedMesh != null) DestroyImmediate(chunk.mf.sharedMesh);
            // Remove cached chunk at old position
            SetChunkOctreeIsDirty(chunk.position, true);
        }


        /// <summary>
        /// 청크에서 사용자 콘텐츠 수정 사항을 모두 지우고 수정된 것으로 표시를 해제한 후 지형 생성기를 호출하여 해당 콘텐츠를 일반 청크로 채웁니다.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public bool ChunkReset (VoxelChunk chunk) {
            if ((object)chunk == null)
                return false;
            ChunkDestroy(chunk);

            Vector3d position = chunk.position;
            position.x -= CHUNK_HALF_SIZE;
            position.z -= CHUNK_HALF_SIZE;
            ResetHeightMapCache(position.x, position.z);
            world.terrainGenerator.PaintChunk(chunk);
            ChunkRequestRefresh(chunk, true, true);
            chunk.modified = false;
            return true;
        }

        /// <summary>
        /// 밑줄이 있는 NavME인 경우 true를 반환합니다.
        /// </summary>
        /// <returns><c>진실</c>, if has nav mesh ready was chunked, <c>거짓</c> otherwise.</returns>
        /// <param name="chunk">큰 덩어리.</param>
        public bool ChunkHasNavMeshReady (VoxelChunk chunk) {
            if ((object)chunk == null) return false;
            return chunk.navMeshSourceIndex >= 0 && chunk.navMeshSourceIndex >= 0 && chunk.navMeshUpdateRequestTime <= navMeshLastBakeTime;
        }

        /// <summary>
        /// 청크에 충돌체가 있는 경우 true를 반환합니다.
        /// </summary>
        public bool ChunkHasCollider (VoxelChunk chunk) {
            if ((object)chunk == null) return false;
            return chunk.hasColliderMesh;
        }

        /// <summary>
        /// 청크가 카메라 절두체 내에 있으면 true를 반환합니다.
        /// </summary>
        public bool ChunkIsInFrustum (VoxelChunk chunk) {
            Vector3d boundsMin;
            boundsMin.x = chunk.position.x - CHUNK_HALF_SIZE;
            boundsMin.y = chunk.position.y - CHUNK_HALF_SIZE;
            boundsMin.z = chunk.position.z - CHUNK_HALF_SIZE;
            Vector3d boundsMax;
            boundsMax.x = chunk.position.x + CHUNK_HALF_SIZE;
            boundsMax.y = chunk.position.y + CHUNK_HALF_SIZE;
            boundsMax.z = chunk.position.z + CHUNK_HALF_SIZE;
            chunk.visibleInFrustum = GeometryUtilityNonAlloc.TestPlanesAABB(frustumPlanesNormals, frustumPlanesDistances, ref boundsMin, ref boundsMax);
            return chunk.visibleInFrustum;
        }


        /// <summary>
        /// 주어진 범위 내의 청크가 생성되도록 보장합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="chunkExtents">청크 단위의 거리(각 청크는 16개의 월드 단위입니다)</param>
        /// <param name="renderChunks">If set to <c>진실</c> enable chunk rendering.</param>
        public void ChunkCheckArea (Vector3d position, Vector3 chunkExtents, bool renderChunks = false) {

            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);
            int xmin = chunkX - (int)chunkExtents.x;
            int ymin = chunkY - (int)chunkExtents.y;
            int zmin = chunkZ - (int)chunkExtents.z;
            int xmax = chunkX + (int)chunkExtents.x;
            int ymax = chunkY + (int)chunkExtents.y;
            int zmax = chunkZ + (int)chunkExtents.z;

            for (int x = xmin; x <= xmax; x++) {
                int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (x + WORLD_SIZE_WIDTH);
                for (int y = ymin; y <= ymax; y++) {
                    int y00 = WORLD_SIZE_DEPTH * (y + WORLD_SIZE_HEIGHT);
                    int h00 = x00 + y00;
                    for (int z = zmin; z <= zmax; z++) {
                        int hash = h00 + z;
                        if (cachedChunks.TryGetValue(hash, out CachedChunk cachedChunk)) {
                            VoxelChunk chunk = cachedChunk.chunk;
                            if ((object)chunk == null)
                                continue;
                            if (chunk.isPopulated) {
                                if (renderChunks && (chunk.renderState != ChunkRenderState.RenderingComplete || !chunk.mr.enabled)) {
                                    ChunkRequestRefresh(chunk, false, true, true);
                                }
                                continue;
                            }
                        }
                        VoxelChunk newChunk = CreateChunk(hash, x, y, z, false);
                        if (renderChunks) {
                            ChunkRequestRefresh(newChunk, false, true, true);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 모든 보류 중인 작업을 실행하도록 Voxel Play를 강제합니다.
        /// </summary>
        public void CompleteWork () {
            executePendingTasks = true;
        }

        int biomeStartLoop;

        /// <summary>
        /// 고도와 습도가 주어진 생물 군계를 반환합니다.
        /// </summary>
        /// <returns>생물 군계.</returns>
        /// <param name="altitude">세계 공간 단위의 고도입니다.</param>
        /// <param name="moisture">0-1 범위의 수분.</param>
        public BiomeDefinition GetBiome (float altitude, float moisture) {
            int biomesCount = world.biomes.Length;
            int biomeEndLoop = biomesCount + biomeStartLoop;
            for (int k = biomeStartLoop; k < biomeEndLoop; k++) {
                BiomeDefinition biome = world.biomes[k % biomesCount];
                int zonesCount = biome.zones.Length;
                for (int j = 0; j < zonesCount; j++) {
                    if (altitude >= biome.zones[j].altitudeMin && altitude <= biome.zones[j].altitudeMax &&
                         moisture >= biome.zones[j].moistureMin && moisture <= biome.zones[j].moistureMax) {
                        biomeStartLoop = k;
                        return biome;
                    }
                }
            }
            return world.defaultBiome;
        }

        /// <summary>
        /// 주어진 위치의 생물 군계를 반환합니다.
        /// </summary>
        public BiomeDefinition GetBiome (Vector3d position) {
            HeightMapInfo h = GetTerrainInfo(position);
            return h.biome;
        }


        /// <summary>
        /// 선택적으로 물을 포함하여 주어진 위치에서 지형 높이를 가져옵니다.
        /// </summary>
        public float GetTerrainHeight (double x, double z, bool includeWater = false) {

            if (heightMapCache == null)
                return 0;
            float groundLevel = GetHeightMapInfoFast(x, z).groundLevel;
            if (includeWater && waterLevel > groundLevel) {
                return waterLevel + 0.9f;
            } else {
                return groundLevel + 1f;
            }
        }

        /// <summary>
        /// 선택적으로 물을 포함하여 주어진 위치에서 지형 높이를 가져옵니다.
        /// </summary>
        public float GetTerrainHeight (Vector3d position, bool includeWater = false) {

            if (heightMapCache == null)
                return 0;
            float groundLevel = GetHeightMapInfoFast(position.x, position.z).groundLevel;
            if (includeWater && waterLevel > groundLevel) {
                return waterLevel + 0.9f;
            } else {
                return groundLevel + 1f;
            }
        }

        /// <summary>
        /// 특정 위치의 지형에 대한 정보를 가져옵니다.
        /// </summary>
        public HeightMapInfo GetTerrainInfo (Vector3d position) {
            return GetTerrainInfo(position.x, position.z);
        }

        /// <summary>
        /// 특정 위치의 지형에 대한 정보를 가져옵니다.
        /// </summary>
        public HeightMapInfo GetTerrainInfo (double x, double z) {
            if (heightMapCache == null) {
                InitHeightMap();
            }
            return GetHeightMapInfoFast(x, z);
        }


        /// <summary>
        /// 0..1 범위의 주어진 위치에서 계산된 빛의 양을 가져옵니다.
        /// </summary>
        /// <returns>빛의 강도.</returns>
        public float GetVoxelLight (Vector3d position) {
            return GetVoxelLight(position, out VoxelChunk chunk, out int voxelIndex);
        }


        /// <summary>
        /// 0..1 범위의 주어진 위치에서 계산된 빛의 양을 가져옵니다.
        /// </summary>
        /// <returns>복셀 위치의 광도..</returns>
        public float GetVoxelLight (Vector3d position, out VoxelChunk chunk, out int voxelIndex) {
            chunk = null;
            voxelIndex = 0;
            if (!effectiveGlobalIllumination) {
                return 1f;
            }

            if (GetVoxelIndex(position, out chunk, out voxelIndex, false) && !chunk.needsLightmapRebuild) {
                if (chunk.voxels[voxelIndex].lightOrTorch != 0 || chunk.voxels[voxelIndex].opaque < FULL_OPAQUE) {
                    return chunk.voxels[voxelIndex].lightOrTorch / 15f;
                }
                // voxel has contents try to retrieve light information from nearby voxels
                int nearby = voxelIndex + ONE_Y_ROW;
                if (nearby < chunk.voxels.Length && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].lightOrTorch / 15f;
                }
                nearby = voxelIndex - ONE_Z_ROW;
                if (nearby >= 0 && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].lightOrTorch / 15f;
                }
                nearby = voxelIndex - 1;
                if (nearby >= 0 && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].lightOrTorch / 15f;
                }
                nearby = voxelIndex + ONE_Z_ROW;
                if (nearby < chunk.voxels.Length && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].lightOrTorch / 15f;
                }
                nearby = voxelIndex + 1;
                if (nearby < chunk.voxels.Length && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].lightOrTorch / 15f;
                }
                return chunk.voxels[voxelIndex].lightOrTorch / 15f;
            }

            // Estimate light by height
            float height = GetTerrainHeight(position, false);
            if (height >= position.y) { // is below surface, we assume a lightIntensity of 0
                return 0;
            }
            return 1f;
        }

        /// <summary>
        /// 압축된 형식으로 주어진 위치에서 조명량을 가져옵니다(토치 + 태양광 기여 포함).
        /// </summary>
        /// <returns>빛의 강도.</returns>
        public int GetVoxelLightPacked (Vector3d position) {
            return GetVoxelLightPacked(position, out _, out _);
        }


        /// <summary>
        /// 압축된 형식으로 주어진 위치에서 조명량을 가져옵니다(토치 + 태양광 기여 포함).
        /// </summary>
        /// <returns>복셀 위치의 광도..</returns>
        public int GetVoxelLightPacked (Vector3d position, out VoxelChunk chunk, out int voxelIndex) {
            chunk = null;
            voxelIndex = 0;
            if (!effectiveGlobalIllumination) {
                return FULL_LIGHT;
            }

            if (GetVoxelIndex(position, out chunk, out voxelIndex, false) && !chunk.needsLightmapRebuild) {
                if (chunk.voxels[voxelIndex].lightOrTorch != 0 || chunk.voxels[voxelIndex].opaque < FULL_OPAQUE) {
                    return chunk.voxels[voxelIndex].packedLight;
                }
                // voxel has contents try to retrieve light information from nearby voxels
                int nearby = voxelIndex + ONE_Y_ROW;
                if (nearby < chunk.voxels.Length && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].packedLight;
                }
                nearby = voxelIndex - ONE_Z_ROW;
                if (nearby >= 0 && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].packedLight;
                }
                nearby = voxelIndex - 1;
                if (nearby >= 0 && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].packedLight;
                }
                nearby = voxelIndex + ONE_Z_ROW;
                if (nearby < chunk.voxels.Length && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].packedLight;
                }
                nearby = voxelIndex + 1;
                if (nearby < chunk.voxels.Length && chunk.voxels[nearby].opaque < FULL_OPAQUE) {
                    return chunk.voxels[nearby].packedLight;
                }
                return chunk.voxels[voxelIndex].packedLight;
            }

            // Estimate light by height
            float height = GetTerrainHeight(position, false);
            if (height >= position.y) { // is below surface, we assume a lightIntensity of 0
                return 0;
            }
            return FULL_LIGHT;
        }



        /// <summary>
        /// 지정된 복셀에 대한 자리 표시자를 만듭니다.
        /// </summary>
        /// <returns>복셀 자리 표시자입니다.</returns>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        /// <param name="createIfNotExists">If set to <c>진실</c> create if not exists.</param>
        public VoxelPlaceholder GetVoxelPlaceholder (VoxelChunk chunk, int voxelIndex, bool createIfNotExists = true) {
            if (voxelIndex < 0 || (object)chunk == null)
                return null;

            VoxelDefinition type = voxelDefinitions[chunk.voxels[voxelIndex].typeIndex];
            return GetVoxelPlaceholder(chunk, voxelIndex, type, createIfNotExists);
        }


        /// <summary>
        /// 지정된 복셀에 대한 자리 표시자를 만듭니다.
        /// </summary>
        /// <returns>복셀 자리 표시자입니다.</returns>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        /// <param name="createIfNotExists">If set to <c>진실</c> create if not exists.</param>
        public VoxelPlaceholder GetVoxelPlaceholder (VoxelChunk chunk, int voxelIndex, VoxelDefinition voxelDefinition, bool createIfNotExists = true) {

            if (voxelIndex < 0 || (object)chunk == null)
                return null;

            bool phArrayCreated = chunk.placeholders != null;

            if (phArrayCreated) {
                if (chunk.placeholders.TryGetValue(voxelIndex, out VoxelPlaceholder ph)) {
                    // has rotation changes? Update if needed
                    float rotationDegrees = chunk.voxels[voxelIndex].GetTextureRotationDegrees();
                    if (ph.currentRotationDegrees != rotationDegrees) {
                        ph.currentRotationDegrees = rotationDegrees;
                        UpdatePlaceholderTransform(ph.transform, chunk, voxelIndex, voxelDefinition, rotationDegrees);
                    }
                    return ph;
                }
            }

            // Create placeholder
            if (createIfNotExists) {
                if (!phArrayCreated) {
                    chunk.placeholders = new FastHashSet<VoxelPlaceholder>();
                }
                GameObject placeholderGO = Instantiate(voxelPlaceholderPrefab, chunk.transform, false);
                if (!placeholderGO.TryGetComponent(out VoxelPlaceholder placeholder)) return null;
                Transform phTransform = placeholderGO.transform;
                phTransform.localPosition = GetVoxelChunkPosition(voxelIndex);
                placeholder.chunk = chunk;
                placeholder.voxelIndex = voxelIndex;
                placeholder.bounds = Misc.bounds1;
                if (chunk.voxels[voxelIndex].hasContent) {
                    placeholder.resistancePointsLeft = voxelDefinitions[chunk.voxels[voxelIndex].typeIndex].resistancePoints;

                    // Bounds for highlighting
                    Mesh mesh = voxelDefinition.mesh;
                    if ((object)mesh != null) {
                        Bounds bounds = mesh.bounds;
                        bounds.size = new Vector3(bounds.size.x * voxelDefinition.scale.x, bounds.size.y * voxelDefinition.scale.y, bounds.size.z * voxelDefinition.scale.z);
                        placeholder.bounds = bounds;
                    }

                    placeholder.currentRotationDegrees = chunk.voxels[voxelIndex].GetTextureRotationDegrees();
                    UpdatePlaceholderTransform(phTransform, chunk, voxelIndex, voxelDefinition, placeholder.currentRotationDegrees);
                }
                chunk.placeholders.Add(voxelIndex, placeholder);
                return placeholder;
            }

            return null;
        }

        void UpdatePlaceholderTransform (Transform phTransform, VoxelChunk chunk, int voxelIndex, VoxelDefinition voxelDefinition, float rotationDegrees) {
            // Custom rotation
            Vector3d position = GetVoxelPosition(chunk.position, voxelIndex);
            phTransform.localRotation = voxelDefinition.GetRotation(position);

            if (!voxelDefinition.isDynamic) {

                Vector3 localPosition = GetVoxelChunkPosition(voxelIndex); // needs to be in local coordinates because chunk might still be at -10000

                // User rotation in word space
                Quaternion placementRotation;
                if (rotationDegrees != 0) {
                    placementRotation = Quaternion.Euler(0, rotationDegrees, 0);
                    phTransform.localRotation = placementRotation * phTransform.localRotation;
                    phTransform.localPosition = localPosition + placementRotation * voxelDefinition.GetOffset(position);
                } else {
                    phTransform.localPosition = localPosition + voxelDefinition.GetOffset(position);
                }

            }
        }

        /// <summary>
        /// 복셀 자리 표시자를 제거합니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        /// <param name="voxelIndex">복셀 지수.</param>
        /// <param name="placeholderObjectRemovalDelay">실제 플레이스홀더 게임오브젝트 파괴를 지연시키는 데 사용됩니다.</param>
        public void VoxelPlaceholderDestroy (VoxelChunk chunk, int voxelIndex, float placeholderObjectRemovalDelay = 0f) {

            if ((object)chunk == null)
                return;

            bool phArrayCreated = (object)chunk.placeholders != null;

            if (phArrayCreated) {
                VoxelPlaceholder ph;
                if (chunk.placeholders.TryGetValue(voxelIndex, out ph) && ph != null) {
                    if (ph.gameObject != null) {
                        Misc.DestroySafely(ph.gameObject, placeholderObjectRemovalDelay);
                    }
                    chunk.placeholders.Remove(voxelIndex);
                }
            }
        }


        /// <summary>
        /// 현재 세계에서 사용 가능한 모든 항목 정의가 포함된 배열
        /// </summary>
        [NonSerialized]
        public List<InventoryItem> allItems;


        /// <summary>
        /// 주어진 위치에 세계의 색상 매트릭스로 정의된 모델을 배치합니다.알파가 0인 색상은 건너뜁니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="colors">3차원 색상 배열(y/z/x).</param>
        public void ModelPlace (Vector3d position, Color[,,] colors, ModelPlacementAlignment alignment = ModelPlacementAlignment.Centered, bool previewMode = false) {
#if UNITY_EDITOR
            CheckEditorTintColor();
#endif

            List<VoxelChunk> updatedChunks = BufferPool<VoxelChunk>.Get();
            modificationTag++;

            Vector3d pos;
            int maxY = colors.GetUpperBound(0) + 1;
            int maxZ = colors.GetUpperBound(1) + 1;
            int maxX = colors.GetUpperBound(2) + 1;
            int halfZ, halfX;
            if (alignment == ModelPlacementAlignment.Centered) {
                halfZ = maxZ / 2;
                halfX = maxX / 2;
            } else {
                halfZ = halfX = 0;
            }
            VoxelDefinition vd = defaultVoxel;
            for (int y = 0; y < maxY; y++) {
                pos.y = position.y + y;
                for (int z = 0; z < maxZ; z++) {
                    pos.z = position.z + z - halfZ;
                    for (int x = 0; x < maxX; x++) {
                        Color32 color = colors[y, z, x];
                        if (color.a == 0)
                            continue;
                        pos.x = position.x + x - halfX;
                        VoxelChunk chunk;
                        int voxelIndex;
                        if (GetVoxelIndex(pos, out chunk, out voxelIndex)) {
                            if (!previewMode) {
                                chunk.voxels[voxelIndex].Set(vd, color);
                            }
                            if (chunk.SetModified(modificationTag)) {
                                updatedChunks.Add(chunk);
                            }
                        }
                    }
                }
            }

            if (!previewMode) {
                RegisterChunkChanges(updatedChunks);
                Boundsd bounds = new Boundsd(new Vector3d(position.x, position.y + maxY / 2, position.z), new Vector3(maxX + 2, maxY + 2, maxZ + 2));
                ChunkRequestRefresh(bounds, true, true);
            }

            BufferPool<VoxelChunk>.Release(updatedChunks);

        }


        /// <summary>
        /// 주어진 위치에 세계의 색상 매트릭스로 정의된 모델을 배치합니다.알파가 0인 색상은 건너뜁니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="voxels">복셀 정의의 3차원 배열(y/z/x).</param>
        /// <param name="colors">3차원 색상 배열(y/z/x).</param>
        public void ModelPlace (Vector3d position, VoxelDefinition[,,] voxels, Color[,,] colors = null, bool fitTerrain = false, ModelPlacementAlignment alignment = ModelPlacementAlignment.Centered) {
            Vector3d pos;
            int maxY = voxels.GetUpperBound(0) + 1;
            int maxZ = voxels.GetUpperBound(1) + 1;
            int maxX = voxels.GetUpperBound(2) + 1;
            int halfZ, halfX;
            if (alignment == ModelPlacementAlignment.Centered) {
                halfZ = maxZ / 2;
                halfX = maxX / 2;
            } else {
                halfZ = halfX = 0;
            }
            bool hasColors = colors != null;
            if (hasColors) {
#if UNITY_EDITOR
                CheckEditorTintColor();
#endif
                if (colors.GetUpperBound(0) != voxels.GetUpperBound(0) || colors.GetUpperBound(1) != voxels.GetUpperBound(1) || colors.GetUpperBound(2) != voxels.GetUpperBound(2)) {
                    Debug.LogError("Colors array dimensions must match those of voxels array.");
                    return;
                }
            }

            List<VoxelChunk> updatedChunks = BufferPool<VoxelChunk>.Get();
            modificationTag++;

            for (int y = 0; y < maxY; y++) {
                pos.y = position.y + y;
                for (int z = 0; z < maxZ; z++) {
                    pos.z = position.z + z - halfZ;
                    for (int x = 0; x < maxX; x++) {
                        VoxelDefinition vd = voxels[y, z, x];
                        if ((object)vd != null) {
                            pos.x = position.x + x - halfX;
                            if (GetVoxelIndex(pos, out VoxelChunk chunk, out int voxelIndex)) {
                                if (hasColors) {
                                    chunk.voxels[voxelIndex].Set(vd, colors[y, z, x]);
                                } else {
                                    chunk.voxels[voxelIndex].Set(vd);
                                }
                                if (chunk.SetModified(modificationTag)) {
                                    updatedChunks.Add(chunk);
                                }
                                if (fitTerrain) {
                                    // Fill beneath row 1
                                    if (y == 0) {
                                        Vector3d under = pos;
                                        under.y -= 1;
                                        float terrainAltitude = GetTerrainHeight(under);
                                        for (int k = 0; k < 100; k++, under.y--) {
                                            GetVoxelIndex(under, out VoxelChunk lowChunk, out int vindex);
                                            if (under.y < terrainAltitude || (object)lowChunk == null || lowChunk.voxels[vindex].opaque == FULL_OPAQUE) break;
                                            if (hasColors) {
                                                lowChunk.voxels[vindex].Set(vd, colors[y, z, x]);
                                            } else {
                                                lowChunk.voxels[vindex].Set(vd);
                                            }

                                            if (lowChunk.SetModified(modificationTag)) {
                                                updatedChunks.Add(lowChunk);
                                            }

                                            if (!lowChunk.inqueue) {
                                                ChunkRequestRefresh(lowChunk, true, true);
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }

            RegisterChunkChanges(updatedChunks);
            BufferPool<VoxelChunk>.Release(updatedChunks);

            Boundsd bounds = new Boundsd(new Vector3d(position.x, position.y + maxY / 2, position.z), new Vector3(maxX + 2, maxY + 2, maxZ + 2));
            ChunkRequestRefresh(bounds, true, true);
        }


        /// <summary>
        /// 주어진 위치에 세계의 복셀 배열로 정의된 모델을 배치합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="voxels">복셀의 3차원 배열(y/z/x).</param>
        /// <param name="useUnpopulatedChunks">지형 생성기가 모델을 채우기 전에 채워지지 않은 덩어리에 모델을 3개 배치합니다.</param>
        public void ModelPlace (Vector3d position, Voxel[,,] voxels, bool useUnpopulatedChunks, ModelPlacementAlignment alignment = ModelPlacementAlignment.Centered) {
            Vector3d pos;
            int maxY = voxels.GetUpperBound(0) + 1;
            int maxZ = voxels.GetUpperBound(1) + 1;
            int maxX = voxels.GetUpperBound(2) + 1;
            int halfZ, halfX;
            if (alignment == ModelPlacementAlignment.Centered) {
                halfZ = maxZ / 2;
                halfX = maxX / 2;
            } else {
                halfZ = halfX = 0;
            }

            List<VoxelChunk> updatedChunks = BufferPool<VoxelChunk>.Get();
            modificationTag++;

            for (int y = 0; y < maxY; y++) {
                pos.y = position.y + y;
                for (int z = 0; z < maxZ; z++) {
                    pos.z = position.z + z - halfZ;
                    for (int x = 0; x < maxX; x++) {
                        if (voxels[y, z, x].isEmpty) continue;
                        pos.x = position.x + x - halfX;
                        if (useUnpopulatedChunks) {
                            GetChunkUnpopulated(pos);
                        }
                        if (GetVoxelIndex(pos, out VoxelChunk chunk, out int voxelIndex)) {
                            chunk.voxels[voxelIndex] = voxels[y, z, x];

                            if (chunk.SetModified(modificationTag)) {
                                updatedChunks.Add(chunk);
                            }
                        }
                    }
                }
            }

            RegisterChunkChanges(updatedChunks);
            BufferPool<VoxelChunk>.Release(updatedChunks);

            Boundsd bounds = new Boundsd(new Vector3d(position.x, position.y + maxY / 2, position.z), new Vector3(maxX + 2, maxY + 2, maxZ + 2));
            ChunkRequestRefresh(bounds, true, true);
        }

        /// <summary>
        /// 주어진 청크 내에 ModelBits 목록에 의해 제공된 복셀 목록을 배치합니다.
        /// </summary>
        /// <param name="bits">modelbit 구조체 목록으로 설명되는 복셀 목록</param>
        public void ModelPlace (VoxelChunk chunk, List<ModelBit> bits) {
            int count = bits.Count;
            for (int b = 0; b < count; b++) {
                int voxelIndex = bits[b].voxelIndex;
                if (bits[b].isEmpty) {
                    chunk.voxels[voxelIndex] = Voxel.Empty;
                    continue;
                }
                VoxelDefinition vd = bits[b].voxelDefinition;
                if (vd == null) {
                    vd = defaultVoxel;
                }
                chunk.voxels[voxelIndex].Set(vd, bits[b].finalColor);
                float rotation = bits[b].rotation;
                if (rotation != 0) {
                    chunk.voxels[voxelIndex].SetTextureRotation(Voxel.GetTextureRotationFromDegrees(rotation));
                }
                MicroVoxels mv = bits[b].microVoxels;
                if (mv != null) {
                    chunk.SetMicroVoxels(voxelIndex, mv);
                    byte newOpaque = mv.GetOpaqueProportional();
                    if (chunk.voxels[voxelIndex].opaque != newOpaque) {
                        chunk.voxels[voxelIndex].opaque = newOpaque;
                        chunk.voxelSignature = -1;
                    }
                }
            }
            RegisterChunkChanges(chunk);
            RefreshNeighbourhood(chunk);
        }

        /// <summary>
        /// 반복적으로 주어진 위치에 모델을 세계에 배치합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="model">모델 정의.</param>
        /// <param name="rotationDegrees">0, 90, 180 또는 270도 회전.값 360은 임의 회전을 의미합니다.</param>
        /// <param name="colorBrightness">선택적으로 색상 밝기 값을 전달합니다.이 값에 복셀 색상을 곱합니다.</param>
        /// <param name="fitTerrain">true로 설정하면 초목과 나무가 방지되고 모델 주변의 일부 공간이 평평해집니다.</param>
        /// <param name="callback">모델이 장면에서 구축을 완료하면 호출되는 사용자 정의 함수입니다.</param>
        public void ModelPlace (Vector3d position, ModelDefinition model, float buildDuration, int rotationDegrees = 0, float colorBrightness = 1f, bool fitTerrain = false, VoxelModelBuildEndEvent callback = null, ModelPlacementAlignment alignment = ModelPlacementAlignment.Centered, bool previewMode = false) {
            if (buildDuration <= 0) {
                ModelPlace(position, model, rotationDegrees, colorBrightness, fitTerrain, alignment: alignment, previewMode: previewMode);
            } else {
                int rotation = Voxel.GetTextureRotationFromDegrees(rotationDegrees);
                StartCoroutine(ModelPlaceWithDuration(position, model, buildDuration, rotation, colorBrightness, fitTerrain, callback, alignment, previewMode));
            }
        }

        /// <summary>
        /// 월드의 주어진 위치에 모델을 배치합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="model">모델 정의.</param>
        /// <param name="rotationDegrees">0, 90, 180 또는 270도 회전.값 360은 임의 회전을 의미합니다.</param>
        /// <param name="colorBrightness">선택적으로 색상 밝기 값을 전달합니다.이 값에 복셀 색상을 곱합니다.</param>
        /// <param name="fitTerrain">true로 설정하면 초목과 나무가 방지되고 모델 주변의 일부 공간이 평평해집니다.</param>
        /// <param name="indices">선택적 사용자 제공 목록입니다.제공되는 경우 모델에 표시되는 모든 복셀의 색인과 위치가 포함됩니다.</param>
        public void ModelPlace (Vector3d position, ModelDefinition model, int rotationDegrees = 0, float colorBrightness = 1f, bool fitTerrain = false, List<VoxelIndex> indices = null, ModelPlacementAlignment alignment = ModelPlacementAlignment.Centered, bool previewMode = false) {
            ModelPlace(position, model, out _, rotationDegrees, colorBrightness, fitTerrain, indices, alignment, previewMode);
        }

        /// <summary>
        /// 월드의 주어진 위치에 모델을 배치합니다.
        /// </summary>
        /// <param name="position">위치.</param>
        /// <param name="model">모델 정의.</param>
        /// <param name="bounds">배치된 모델의 경계입니다.</param>
        /// <param name="rotationDegrees">0, 90, 180 또는 270도 회전.값 360은 임의 회전을 의미합니다.</param>
        /// <param name="colorBrightness">선택적으로 색상 밝기 값을 전달합니다.이 값에 복셀 색상을 곱합니다.</param>
        /// <param name="fitTerrain">true로 설정하면 초목과 나무가 방지되고 모델 주변의 일부 공간이 평평해집니다.</param>
        /// <param name="indices">선택적 사용자 제공 목록입니다.제공되는 경우 모델에 표시되는 모든 복셀의 색인과 위치가 포함됩니다.</param>
        public void ModelPlace (Vector3d position, ModelDefinition model, out Boundsd bounds, int rotationDegrees = 0, float colorBrightness = 1f, bool fitTerrain = false, List<VoxelIndex> indices = null, ModelPlacementAlignment alignment = ModelPlacementAlignment.Centered, bool previewMode = false) {
            if (!previewMode && OnModelBuildStart != null) {
                OnModelBuildStart(model, position, out bool cancel);
                if (cancel) {
                    bounds = Boundsd.empty;
                    return;
                }
            }

            Boundsd dummyBounds = Boundsd.empty;
            int rotation;
            if (rotationDegrees == 360) {
                rotation = WorldRand.Range(0, 4);
            } else {
                rotation = Voxel.GetTextureRotationFromDegrees(rotationDegrees);
            }
            ModelPlace(position, model, ref dummyBounds, rotation, colorBrightness, fitTerrain, indices, -1, -1, alignment: alignment, previewMode: previewMode);
            if (!previewMode) {
                ModelPlaceTorches(position, model, rotation, alignment);
            }
            bounds = dummyBounds;
        }


        /// <summary>
        /// 모델 내부의 빈 복셀을 빈 블록으로 채워 모델을 배치할 때 기존의 다른 복셀을 모두 지웁니다.
        /// </summary>
        /// <param name="model">모델.</param>
        public void ModelFillInside (ModelDefinition model) {

            if (model == null)
                return;
            int sx = model.sizeX;
            int sy = model.sizeY;
            int sz = model.sizeZ;
            Voxel[] voxels = new Voxel[sy * sz * sx];

            List<ModelBit> newBits = new List<ModelBit>();

            // Load model inside the temporary voxel array
            int bitsLength = model.bits.Length;
            for (int k = 0; k < bitsLength; k++) {
                int voxelIndex = model.bits[k].voxelIndex;
                if (!model.bits[k].isEmpty) {
                    voxels[voxelIndex].isEmpty = false;
                    newBits.Add(model.bits[k]);
                }
            }

            // Fill inside
            ModelBit empty = new ModelBit();
            empty.isEmpty = true;
            int yrow = sz * sx;
            for (int z = 0; z < sz; z++) {
                for (int x = 0; x < sx; x++) {
                    int bindex = -1;
                    int voxelIndex = z * sx + x;
                    for (int y = 0; y < sy; y++, voxelIndex += yrow) {
                        if (voxels[voxelIndex].isEmpty) {
                            if (bindex < 0) {
                                bindex = voxelIndex;
                            }
                        } else if (bindex >= 0) {
                            while (bindex < voxelIndex) {
                                if (voxels[bindex].isEmpty) {
                                    empty.voxelIndex = bindex;
                                    newBits.Add(empty);
                                }
                                bindex += yrow;
                            }
                            bindex = -1;
                        }
                    }
                }
            }
            model.bits = newBits.ToArray();
        }


        /// <summary>
        /// 모델 정의를 일반 게임 객체로 변환합니다.
        /// </summary>
        /// <param name="updateGameObject">새로운 게임오브젝트를 생성하기 위해 업데이트하거나 null을 전달할 게임오브젝트</param>
        public static GameObject ModelCreateGameObject (ModelDefinition modelDefinition, GameObject updateGameObject = null) {
            return VoxelPlayConverter.GenerateVoxelObject(modelDefinition, Misc.vector3zero, Misc.vector3one, updateGameObject: updateGameObject);
        }


        /// <summary>
        /// 모델 정의를 일반 게임 객체로 변환합니다.
        /// </summary>
        /// <param name="updateGameObject">새로운 게임오브젝트를 생성하기 위해 업데이트하거나 null을 전달할 게임오브젝트</param>
        public static GameObject ModelCreateGameObject (ModelDefinition modelDefinition, Vector3 offset, Vector3 scale, GameObject updateGameObject = null) {
            return VoxelPlayConverter.GenerateVoxelObject(modelDefinition, offset, scale, updateGameObject: updateGameObject);
        }

        /// <summary>
        /// 모델 정의를 일반 게임 객체로 변환합니다.
        /// </summary>
        /// <param name="updateGameObject">새로운 게임오브젝트를 생성하기 위해 업데이트하거나 null을 전달할 게임오브젝트</param>
        public static GameObject ModelCreateGameObject (ModelDefinition modelDefinition, Vector3 offset, Vector3 scale, bool useTextures, bool useNormalMaps = true, GameObject updateGameObject = null) {
            return VoxelPlayConverter.GenerateVoxelObject(modelDefinition, offset, scale, useTextures: useTextures, useNormalMaps: useNormalMaps, updateGameObject: updateGameObject);
        }


        /// <summary>
        /// 세계의 일부를 캡처하고 해당 콘텐츠로 모델 정의를 만듭니다.
        /// </summary>
        /// <returns>주어진 경계의 세계 콘텐츠를 포함하는 모델 정의</returns>
        public ModelDefinition ModelWorldCapture (Bounds bounds) {

            if (bounds.size.x < 1 || bounds.size.y < 1 || bounds.size.z < 1) return null;

            ModelDefinition md = ScriptableObject.CreateInstance<ModelDefinition>();
            int sizeX = (int)bounds.size.x;
            int sizeY = (int)bounds.size.y;
            int sizeZ = (int)bounds.size.z;
            md.sizeX = sizeX;
            md.sizeY = sizeY;
            md.sizeZ = sizeZ;
            ModelBit bit = new ModelBit();
            Vector3 min = bounds.min;
            List<ModelBit> bits = BufferPool<ModelBit>.Get();
            for (int y = 0; y < sizeY; y++) {
                for (int z = 0; z < sizeZ; z++) {
                    for (int x = 0; x < sizeX; x++) {
                        Vector3 pos = new Vector3(min.x + x, min.y + y, min.z + z);
                        if (!GetVoxelIndex(pos, out VoxelChunk chunk, out int voxelIndex, false)) continue;
                        if (chunk.voxels[voxelIndex].hasContent) {
                            VoxelDefinition vd = chunk.voxels[voxelIndex].type;
                            if (!vd.isDynamic) {
                                bit.voxelIndex = y * sizeZ * sizeX + z * sizeX + x;
                                bit.voxelDefinition = vd;
                                bit.color = chunk.voxels[voxelIndex].color;
                                bit.rotation = chunk.voxels[voxelIndex].GetTextureRotationDegrees();
                                bit.microVoxels = null;
                                if (chunk.usesMicroVoxels && chunk.microVoxels.TryGetValue(voxelIndex, out MicroVoxels microVoxels)) {
                                    bit.microVoxels = microVoxels.Clone();
                                }
                                bits.Add(bit);
                            }
                        }
                    }
                }
            }
            md.SetBits(bits.ToArray());
            BufferPool<ModelBit>.Release(bits);
            return md;
        }


        /// <summary>
        /// 현재 복셀 하이라이트 표시/숨기기
        /// </summary>
        /// <returns><c>진실</c>, if highlight was voxeled, <c>거짓</c> otherwise.</returns>
        /// <param name="visible">If set to <c>진실</c> visible.</param>
        public void VoxelHighlight (bool visible) {
            foreach (var vh in highlightedVoxels) {
                if (vh != null) {
                    vh.SetActive(visible);
                }
            }
            if (!visible) {
                RefreshVoxelHighlight();
            }
        }

        /// <summary>
        /// 주어진 위치에서 모델 정의의 홀로그램을 표시합니다.
        /// </summary>
        /// <returns>하이라이트.</returns>
        /// <param name="modelDefinition">모델 정의.</param>
        /// <param name="position">위치.</param>
        public GameObject ModelHighlight (ModelDefinition modelDefinition, Vector3d position, float rotationDegrees = 0) {
            if (modelDefinition == null) return null;

            if (modelDefinition.modelGameObject == null) {
                modelDefinition.modelGameObject = ModelCreateGameObject(modelDefinition);
            }
            GameObject modelGO = modelDefinition.modelGameObject;
            if (modelGO == null) {
                return null;
            }

            if (modelGO.TryGetComponent(out MeshRenderer renderer)) {
                Material mat = renderer.sharedMaterial;
                modelHighlightMat.SetTexture(ShaderParams.MainTex, mat.GetTexture(ShaderParams.MainTex));
                renderer.sharedMaterial = modelHighlightMat;
            }

            int halfSizeX = modelDefinition.sizeX / 2;
            int halfSizeZ = modelDefinition.sizeZ / 2;

            Vector3d corner1 = Quaternion.Euler(0, rotationDegrees, 0) * new Vector3(-halfSizeX, 0, -halfSizeZ);
            Vector3d corner2 = corner1 + Quaternion.Euler(0, rotationDegrees, 0) * new Vector3(modelDefinition.sizeX - 1, 0, modelDefinition.sizeZ - 1);
            Vector3d previewPos = new Vector3d((corner1.x + corner2.x) * 0.5f + position.x, position.y - 0.5f, (corner1.z + corner2.z) * 0.5f + position.z);

            modelGO.transform.SetParent(worldRoot, false);
            modelGO.transform.localPosition = previewPos + modelDefinition.offset;
            modelGO.transform.localRotation = Quaternion.Euler(0, rotationDegrees, 0);

            modelGO.SetActive(true);

            return modelGO;
        }


        /// <summary>
        /// 모든 월드 텍스처를 다시 로드합니다.
        /// </summary>
        public void ReloadTextures () {
            // reset texture providers
            DisposeTextures();
            InitRenderingMaterials();
            LoadWorldTextures();
        }


        /// <summary>
        /// 빌드 모드 설정 또는 취소
        /// </summary>
        /// <param name="buildMode">If set to <c>진실</c> build mode.</param>
        public void SetBuildMode (bool buildMode) {
            if (!enableBuildMode)
                return;

            // Get current selected item
            InventoryItem currentItem = VoxelPlayPlayer.instance.GetSelectedItem();

            this.buildMode = buildMode;

            // refresh inventory
            VoxelPlayUI ui = VoxelPlayUI.instance;
            if (ui != null) {
                ui.RefreshInventoryContents();
            }

            // Reselect item
            if (!VoxelPlayPlayer.instance.SetSelectedItem(currentItem)) {
                VoxelPlayPlayer.instance.SetSelectedItem(0);
            }

        }

        /// <summary>
        /// 콘솔에 오류 메시지를 표시합니다.
        /// </summary>
        /// <param name="errorMessage">오류 메시지.</param>
        public void ShowError (string errorMessage) {
            if (applicationIsPlaying) {
                ShowMessage(errorMessage, 4, false);
            } else {
                Debug.LogError(errorMessage);
            }
        }

        /// <summary>
        /// 상태 텍스트에 사용자 정의 메시지를 표시합니다.
        /// </summary>
        /// <param name="txt">텍스트.</param>
        public void ShowMessage (string txt, float displayDuration = 4, bool flashEffect = false, bool openConsole = false, bool allowDuplicatedMessage = false, string colorName = null) {
            if (!allowDuplicatedMessage && lastMessage == txt)
                return;
            lastMessage = txt;

            if (VoxelPlayUI.instance != null) {
                ExecuteInMainThread(delegate () {
                    if (!string.IsNullOrEmpty(colorName)) {
                        txt = "<color=" + colorName + ">" + txt + "</color>";
                    }
                    VoxelPlayUI.instance.AddMessage(txt, displayDuration, flashEffect, openConsole);
                });
            }
        }

        /// <summary>
        /// 디버그 수준이 상세로 설정된 경우 메시지를 기록하고 추가합니다.
        /// </summary>
        /// <param name="txt"></param>
        public void LogMessage (string txt, bool bold = false) {
            if (debugLevel == LogLevel.Verbose && Application.isPlaying) {
                if (bold) {
                    txt = "<b>" + txt + "</b>";
                }
                Debug.Log(string.Format("<color=green>Voxel Play {0:yyyy-MM-dd HH:mm:ss}: {1}</color>", DateTime.Now, txt));
            }
        }


        /// <summary>
        /// 주어진 카테고리와 복셀 유형의 allItems 배열에서 항목을 가져옵니다.
        /// </summary>
        /// <returns>요청된 카테고리 및 유형의 항목입니다.</returns>
        /// <param name="category">범주.</param>
        /// <param name="voxelType">복셀 유형.</param>
        public ItemDefinition GetItemDefinition (ItemCategory category, VoxelDefinition voxelType = null) {
            if (allItems == null)
                return null;
            int allItemsCount = allItems.Count;
            for (int k = 0; k < allItemsCount; k++) {
                if (allItems[k].item.category == category && (allItems[k].item.voxelType == voxelType || voxelType == null)) {
                    return allItems[k].item;
                }
            }
            return null;
        }

        /// <summary>
        /// 주어진 카테고리와 복셀 유형의 allItems 배열에서 항목을 가져옵니다.
        /// </summary>
        /// <returns>요청된 카테고리 및 유형의 항목입니다.</returns>
        /// <param name="category">범주.</param>
        /// <param name="objName">복셀 유형 이름 또는 항목 이름입니다.</param>
        public ItemDefinition GetItemDefinition (ItemCategory category, string objName) {
            if (category == ItemCategory.Voxel) {
                VoxelDefinition voxelDefinition = GetVoxelDefinition(objName);
                return GetItemDefinition(ItemCategory.Voxel, voxelDefinition);
            }

            return GetItemDefinition(objName);
        }


        /// <summary>
        /// 이름으로 항목 정의를 반환합니다.
        /// </summary>
        public ItemDefinition GetItemDefinition (string name) {
            if (string.IsNullOrEmpty(name)) return null;
            itemDefinitionsDict.TryGetValue(name, out ItemDefinition id);
            return id;
        }

        /// <summary>
        /// 회복 가능한 아이템을 생성하여 지정된 위치, 방향, 강도로 던집니다.
        /// </summary>
        /// <param name="position">월드 공간에서의 위치.</param>
        /// <param name="direction">방향.</param>
        /// <param name="itemDefinition">품목 정의.</param>
        public GameObject ItemThrow (Vector3d position, Vector3 direction, float velocity, ItemDefinition itemDefinition) {
            GameObject itemGO = CreateRecoverableItem(position, itemDefinition);
            if (itemGO == null)
                return null;
            if (itemGO.TryGetComponent(out Rigidbody rb)) {
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.velocity = direction * velocity;
            }
            return itemGO;
        }


        /// <summary>
        /// 이름으로 영구 항목을 생성합니다.
        /// </summary>
        /// <returns><c>진실</c>, if item was spawned, <c>거짓</c> otherwise.</returns>
        public GameObject ItemSpawn (string itemDefinitionName, Vector3d position, float quantity = 1) {
            ItemDefinition id = GetItemDefinition(itemDefinitionName);
            return CreateRecoverableItem(position, id, quantity);
        }

        /// <summary>
        /// 횃불을 추가합니다.
        /// </summary>
        /// <param name="hitInfo">토치를 부착해야 하는 적중 위치에 대한 정보입니다.</param>
        /// <param name="torchDefinition">원하는 토치아이템</param>
        public GameObject TorchAttach (VoxelHitInfo hitInfo, ItemDefinition torchDefinition = null, bool refreshChunks = true) {
            if ((object)torchDefinition != null && torchDefinition.category != ItemCategory.Torch) {
                //Not a torch item
                return null;
            }

            return TorchAttachInt(hitInfo, torchDefinition, refreshChunks);
        }

        /// <summary>
        /// 법선으로 정의된 면의 월드 위치에 따라 주어진 복셀에 토치를 연결합니다.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public GameObject TorchAttach (Vector3d worldPos, Vector3 normal, ItemDefinition torchDefinition = null, bool refreshChunks = true) {
            if ((object)torchDefinition != null && torchDefinition.category != ItemCategory.Torch) {
                //Not a torch item
                return null;
            }

            VoxelChunk chunk;
            int voxelIndex;
            if (GetVoxelIndex(worldPos, out chunk, out voxelIndex, false)) {
                VoxelHitInfo hitInfo = new VoxelHitInfo();
                Vector3d voxelCenter = GetVoxelPosition(chunk, voxelIndex);
                hitInfo.voxelCenter = voxelCenter;
                hitInfo.normal = normal;
                hitInfo.chunk = chunk;
                hitInfo.voxelIndex = voxelIndex;
                return TorchAttachInt(hitInfo, torchDefinition, refreshChunks);
            } else {
                return null;
            }

        }

        /// <summary>
        /// 기존 토치를 제거합니다.
        /// </summary>
        /// <param name="chunk">토치가 현재 부착된 청크입니다.</param>
        /// <param name="gameObject">횃불 자체의 게임 개체입니다.</param>
        public void TorchDetach (VoxelChunk chunk, GameObject gameObject) {
            TorchDetachInt(chunk, gameObject);
        }


        /// <summary>
        /// 복셀 플레이 입력 관리자 참조.
        /// </summary>
        [NonSerialized]
        public VoxelPlayInputController input;


        /// <summary>
        /// 현재 시간을 24시간 숫자 형식으로 설정합니다.
        /// </summary>
        /// <param name="time">0-23.9999 범위의 시간</param>
        /// <param name="azimuth">선택적 태양 방위각.값이 제공되지 않으면 세계 정의에 지정된 방위각 값이 사용됩니다.</param>
        public void SetTimeOfDay (float time, float azimuth = -1) {
            Vector3 r;
            r.x = 360 * (time / 24f) + 270;
            r.y = azimuth < 0 ? world.azimuth : azimuth;
            r.z = 0;
            Transform t = sun.transform;
            t.rotation = Quaternion.Euler(r);
            sunStartRotation = t.rotation;
            sunStartDirectionTimestamp = Time.time;
        }

        /// <summary>
        /// HH:MM 문자열 형식으로 현재 시간을 설정합니다.
        /// </summary>
        /// <param name="time">HH:MM 문자열(HH=24시간 형식, MM=분)</param>
        /// <param name="azimuth"></param>
        public void SetTimeOfDay (string time, float azimuth = -1) {
            if (string.IsNullOrEmpty(time)) return;
            string[] t = time.Trim().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (t.Length == 2) {
                if (float.TryParse(t[0], out float hour) && float.TryParse(t[1], out float minute))
                    SetTimeOfDay(hour + minute / 6000f, azimuth);
            }
        }

        /// <summary>
        /// 태양 방향을 설정합니다.
        /// </summary>
        public void SetSunRotation (Quaternion rotation) {
            if (sun == null) return;
            sunStartRotation = rotation;
            sunStartDirectionTimestamp = Time.time;
            sun.transform.rotation = sunStartRotation;
        }


        /// <summary>
        /// 복셀 클릭 이벤트를 트리거합니다.이 함수는 복셀 클릭이 발생했음을 알리기 위해 외부 클래스에서 호출될 수 있습니다(예: 플레이어가 복셀에 닿음).
        /// </summary>
        public void TriggerVoxelClickEvent (VoxelChunk chunk, int voxelIndex, int mouseButtonIndex) {
            if (captureEvents && OnVoxelClick != null) {
                OnVoxelClick(chunk, voxelIndex, mouseButtonIndex);
            }
        }
        #endregion

    }

}
