using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public enum VoxelPlaySkybox {
        UserDefined = 0,
        Earth = 1,
        Space = 2,
        EarthSimplified = 3,
        EarthNightCubemap = 4,
        EarthDayNightCubemap = 5
    }


    [CreateAssetMenu(menuName = "Voxel Play/World Definition", fileName = "WorldDefinition", order = 103)]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001884-world-definition-fields")]
    public partial class WorldDefinition : ScriptableObject {
        public int seed;

        public VoxelPlayTerrainGenerator terrainGenerator;
        [Tooltip("무한한 세계를 생성합니다.")]
        public bool infinite = true;
        [Tooltip("중심이 0,0,0이라고 가정하는 월드 범위(크기의 절반)입니다.범위는 청크 크기(16)의 배수여야 합니다.예를 들어 범위 X = 1024인 경우 세계는 -1024에서 1024 사이에 생성됩니다.")]
        public Vector3 extents = new Vector3(1024, 1024, 1024);
        public VoxelPlayDetailGenerator[] detailGenerators;

        public BiomeDefinition[] biomes;
        [Tooltip("주어진 위치의 고도/수분과 일치하는 생물 군계가 없는 경우 기본 생물 군계가 사용됩니다.선택 과목.")]
        public BiomeDefinition defaultBiome;
        [Tooltip("전체 풍속(잔디에만 해당)")]
        [Range(0, 16)]
        public float grassWindSpeed = 1;
        [Tooltip("전체 풍속(나무에만 해당)")]
        [Range(0, 16)]
        public float treeWindSpeed = 1;

        [Header("Sky & Lighting")]
        public VoxelPlaySkybox skyboxDesktop = VoxelPlaySkybox.Earth;
        public VoxelPlaySkybox skyboxMobile = VoxelPlaySkybox.EarthSimplified;
        public Texture skyboxDayCubemap, skyboxNightCubemap;

        [Range(-10, 10)]
        public float dayCycleSpeed = 1f;

        public bool setTimeAndAzimuth;

        [Range(0, 24)]
        public float timeOfDay;

        [Range(0, 360)]
        public float azimuth = 15f;

        [Range(0, 2f)]
        public float exposure = 1f;

        [Tooltip("구름을 만드는 데 사용됩니다.")]
        public VoxelDefinition cloudVoxel;

        [Range(0, 255)]
        public int cloudCoverage = 110;

        [Range(0, 1024)]
        public int cloudAltitude = 150;

        public Color skyTint = new Color(0.52f, 0.5f, 1f);

        public Color groundColor = new Color(0.369f, 0.349f, 0.341f);

        [Tooltip("포인트 라이트의 범위 승수")]
        public float lightScattering = 0.01f;
        [Tooltip("포인트 라이트의 강도 승수")]
        public float lightIntensityMultiplier = 2f;
        [Tooltip("태양빛이 지하에서 감쇠되는 속도")]
        [Range(1, 5)] public int lightSunAttenuation = 1;
        [Tooltip("토치 라이트가 거리에 따라 감쇠되는 속도")]
        [Range(1, 5)] public int lightTorchAttenuation = 1;

        [ColorUsage(showAlpha: false)]
        [Tooltip("Voxel Play 환경에서 Colored Shadows 옵션을 활성화해야 합니다.")]
        public Color shadowTintColor;

        [Header("Water Properties")]
        public Color underWaterFogColor = new Color(0.118f, 0.247f, 0.455f, 0.235f);
        [Range(0, 3)]
        public float waveAmplitude = 1f;
        public float specularIntensity = 2f;
        public float specularPower = 64;
        [Tooltip("빌드 모드에서 물이 중력의 영향을 받거나 퍼지는 경우")]
        public bool waterSpreadsInBuildMode;

        [Header("Realistic Water")]
        public Color waterColor = new Color(0.231f, 0.455f, 0.82f, 0.31f); // (0.26f, 0.46f, 0.76f);
        public Color foamColor = Color.white;
        [Range(0f, 1f)]
        public float waveScale = 0.1f;
        public float waveSpeed = 0.4f;
        public float refractionDistortion = 0.08f;
        [Range(0f, 1f)]
        public float fresnel = 0.9f;
        public float normalStrength = 2f;
        [Range(0.49f, 0.555f)]
        public float oceanWaveThreshold = 0.512f;
        [Range(0, 100)]
        public float oceanWaveIntensity = 12f;

        [Header("FX")]
        [Tooltip("특정 재질의 방출 애니메이션 기간")]
        public float emissionAnimationSpeed = 0.5f;
        public float emissionMinIntensity = 0.5f;
        public float emissionMaxIntensity = 1.2f;

        [Tooltip("복셀 손상 균열의 지속 시간")]
        public float damageDuration = 3f;
        public Texture2D[] voxelDamageTextures;
        public GameObject damageParticle;
        public float gravity = -9.8f;

        [Tooltip("true로 설정하면 'Trigger Collapse'가 포함된 복셀 유형이 'Will Collapse' 플래그로 표시된 인근 복셀을 따라 이동합니다.")]
        public bool collapseOnDestroy = true;

        [Tooltip("동시에 낙하할 수 있는 최대 복셀 수")]
        public int collapseAmount = 50;

        [Tooltip("축소된 복셀을 일반 복셀로 통합하기 위한 지연입니다.값이 0이면 장면의 동적 복셀이 유지됩니다.시각적 결함을 피하기 위해 청크가 절두체에 있지 않을 때 통합이 발생한다는 점에 유의하세요.")]
        public int consolidateDelay = 5;

        [Tooltip("삭제된 복구 가능한 작은 복셀에 사용된 원본 복셀의 축소된 텍스처에 사용되는 해상도입니다.동일한 복셀 텍스처를 사용하려면 이 값을 0으로 설정합니다.")]
        public int dropVoxelTextureResolution = 64;

        [Header("Additional Objects")]
        public VoxelDefinition[] moreVoxels;
        public ItemDefinition[] items;

        [HideInInspector]
        public string resourceLocation;


        void OnEnable () {
            if (biomes == null) {
                biomes = new BiomeDefinition[0];
            }

#if UNITY_EDITOR
            try {
                resourceLocation = System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(this));
                int i = resourceLocation.IndexOf("Resources/");
                if (i > 0) {
                    resourceLocation = resourceLocation.Substring(i + 10);
                } else {
                    resourceLocation = null;
                }
            }
            catch {
            }
#endif
        }



        void OnValidate () {
            lightScattering = Mathf.Max(lightScattering, 0);
            lightIntensityMultiplier = Mathf.Max(lightIntensityMultiplier, 0);
            specularIntensity = Mathf.Max(specularIntensity, 0);
            specularPower = Mathf.Max(specularPower, 0);
            normalStrength = Mathf.Max(normalStrength, 0);
            emissionAnimationSpeed = Mathf.Max(emissionAnimationSpeed, 0);
            emissionMinIntensity = Mathf.Max(emissionMinIntensity, 0);
            consolidateDelay = Mathf.Max(consolidateDelay, 0);
            dropVoxelTextureResolution = Mathf.Max(dropVoxelTextureResolution, 1);

            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env != null && this == env.world) {
                if (setTimeAndAzimuth) {
                    env.SetTimeOfDay(timeOfDay, azimuth);
                }
                env.UpdateMaterialProperties();
                if (terrainGenerator != null && !terrainGenerator.isInitialized) {
                    terrainGenerator.Initialize();
                }
            }
        }

        public VoxelPlayDetailGenerator GetGenerator<T> () {
            if (detailGenerators == null) return default;
            for (int k = 0; k < detailGenerators.Length; k++) {
                if (detailGenerators[k] is T) {
                    return detailGenerators[k];
                }
            }
            return default;
        }

        /// <summary>
        /// 세계 정의에 새로운 복셀 정의를 추가합니다.
        /// </summary>
        public void AddVoxelDefinition (VoxelDefinition vd) {
            if (moreVoxels == null) {
                moreVoxels = new VoxelDefinition[1];
                moreVoxels[0] = vd;
                return;
            }
            List<VoxelDefinition> list = new List<VoxelDefinition>(moreVoxels);
            if (!list.Contains(vd)) {
                list.Add(vd);
                moreVoxels = list.ToArray();
            }
        }



    }

}