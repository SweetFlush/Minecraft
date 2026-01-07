using System;
using UnityEngine;

namespace VoxelPlay {


    [CreateAssetMenu(menuName = "Voxel Play/Biome Definition", fileName = "BiomeDefinition", order = 100)]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001913-biomes")]
    public partial class BiomeDefinition : ScriptableObject {

        [Header("Biome Settings")]
        public BiomeZone[] zones;

        // Used by biome map explorer
        [NonSerialized]
        public int biomeMapOccurrences;

        /// <summary>
        /// 이 생물 군계가 생물 군계 탐색기에 표시되는 경우
        /// </summary>
        public bool showInBiomeMap = true;

        public Color biomeMapColor;

        [NonSerialized]
        public Color biomeMapColorTemp;

        [Header("Terrain Voxels")]
        [Tooltip("이 생물 군계의 표면에 있는 복셀에 사용되는 복셀 정의입니다.")]
        public VoxelDefinition voxelTop;
        [Note("Add any number of additional voxels for the SURFACE of this biome. The sum of all probabilities must be 1. If the sum is less than 1, the remaining probability will be used by the main voxelTop. For example, if the sum of probabilities for additional voxels is 0.6, the main voxel top will be used in 40% (0.4) of surface voxels in this biome.")]
        public InspectorNote noteTopAdditional;
        public BiomeSurfaceVoxel[] voxelTopAdditional;

        [Tooltip("이 생물 군계의 지하 복셀에 사용되는 복셀 정의입니다.")]
        public VoxelDefinition voxelDirt;
        [Note("Add any number of additional voxels for the UNDERGROUND of this biome. The sum of all probabilities must be 1. If the sum is less than 1, the remaining probability will be used by the main voxelDirt. For example, if the sum of probabilities for additional voxels is 0.6, the main voxel dirt will be used in 40% (0.4) of underground voxels in this biome.", margin = 8)]
        public InspectorNote noteDirtAdditional;
        public BiomeUndergroundVoxel[] voxelDirtAdditional;

        [Tooltip("바다/호수 바닥에 사용되는 선택적 복셀입니다.할당되지 않은 경우 Voxel Dirt가 대신 사용됩니다.")]
        public VoxelDefinition voxelLakeBed;


        public BiomeOre[] ores;

        [Header("Trees")]
        [Range(0, 0.05f)]
        public float treeDensity = 0.02f;
        public BiomeTree[] trees;

        [Header("Vegetation")]
        [Range(0, 1)]
        public float vegetationDensity = 0.05f;
        public BiomeVegetation[] vegetation;

        [Header("Underwater Vegetation")]
        [Range(0, 1)]
        public float underwaterVegetationDensity = 0.05f;
        public BiomeVegetation[] underwaterVegetation;

        [Header("Underground Vegetation")]
        [Range(0, 1)]
        public float undergroundVegDensity = 0.05f;
        public BiomeVegetation[] undergroundVegetation;

        [Header("Underground Ceiling Vegetation")]
        [Range(0, 1)]
        public float undergroundCeilingVegDensity = 0.05f;
        public BiomeVegetation[] undergroundCeilingVegetation;


        const int minimumAltitude = -500;
        const int maximumAltitude = 500;



        VoxelDefinition[] allTopVoxels;
        VoxelDefinition[][] allDirtVoxels;

        private void Awake () {
            ValidateSettings();
        }

        /// <summary>
        /// 지형 생성기에 의해 호출되는 초기화 함수
        /// </summary>
        public void Init () {
            if (voxelLakeBed == null) {
                voxelLakeBed = voxelDirt;
            }
            if (undergroundVegetation == null) {
                undergroundVegetation = new BiomeVegetation[0];
            }
            if (undergroundCeilingVegetation == null) {
                undergroundCeilingVegetation = new BiomeVegetation[0];
            }
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env != null) {
                for (int v = 0; v < undergroundVegetation.Length; v++) {
                    if (undergroundVegetation[v].vegetation == null) {
                        undergroundVegetation[v].vegetation = env.defaultVoxel;
                    } else {
                        // ensure voxel definition is registered
                        env.AddVoxelDefinition(undergroundVegetation[v].vegetation);
                    }
                }
                for (int v = 0; v < undergroundCeilingVegetation.Length; v++) {
                    if (undergroundCeilingVegetation[v].vegetation == null) {
                        undergroundCeilingVegetation[v].vegetation = env.defaultVoxel;
                    } else {
                        // ensure voxel definition is registered
                        env.AddVoxelDefinition(undergroundCeilingVegetation[v].vegetation);
                    }
                }
            }
            if (trees != null) {
                int treeCount = trees.Length;
                for (int t = 0; t < treeCount; t++) {
                    BiomeTree tree = trees[t];
                    ModelDefinition treeModel = tree.tree;
                    if (treeModel != null) {
                        int bitCount = treeModel.bits.Length;
                        for (int v = 0; v < bitCount; v++) {
                            VoxelDefinition vd = treeModel.bits[v].voxelDefinition;
                            if (vd != null) {
                                vd.isTree = true;
                            }
                        }
                    }
                }
            }

            // Consolidate all top/dirt voxel definitions in a single array
            DistributeSurfaceVoxels(voxelTop, voxelTopAdditional, ref allTopVoxels);
            DistributeUndergroundVoxels(voxelDirt, voxelDirtAdditional, ref allDirtVoxels);
        }

        public void ValidateSettings () {

            if (ores == null) {
                ores = new BiomeOre[0];
            }
            if (trees == null) {
                trees = new BiomeTree[0];
            }
            if (vegetation == null) {
                vegetation = new BiomeVegetation[0];
            }
            if (underwaterVegetation == null) {
                underwaterVegetation = new BiomeVegetation[0];
            }

            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null) return;

            if (zones != null) {
                for (int z = 0; z < zones.Length; z++) {
                    BiomeZone zone = zones[z];
                    zone.biome = this;
                    if (zone.altitudeMin == 0 && zone.altitudeMax == 0) {
                        Debug.LogWarning("Biome " + name + " has no minimum/maximum altitude defined. Assigning a default value of 255 to maximum altitude.", this);
                        zone.altitudeMax = 255;
                    }
                    if (zone.moistureMin == 0 && zone.moistureMax == 0) {
                        Debug.LogWarning("Biome " + name + " has no minimum/maximum moisture defined. Assigning a default value of 1 to maximum moisture.", this);
                        zone.moistureMax = 1;
                    }
                    zones[z] = zone;
                }
            }
            for (int v = 0; v < vegetation.Length; v++) {
                if (vegetation[v].vegetation == null) {
                    vegetation[v].vegetation = env.defaultVoxel;
                }
            }

            for (int v = 0; v < underwaterVegetation.Length; v++) {
                if (underwaterVegetation[v].vegetation == null) {
                    underwaterVegetation[v].vegetation = env.defaultVoxel;
                }
            }
        }


        /// <summary>
        /// 최적화 목적을 위해 이 함수는 임의의 값을 미리 계산하고 배열에 복셀을 배포합니다.
        /// </summary>
        void DistributeSurfaceVoxels (VoxelDefinition mainVoxel, BiomeSurfaceVoxel[] additionalVoxels, ref VoxelDefinition[] voxelsArray) {

            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env != null) {
                if (mainVoxel == null) {
                    mainVoxel = env.defaultVoxel;
                }
                // Ensure textures are added to the engine
                if (additionalVoxels != null) {
                    for (int k = 0; k < additionalVoxels.Length; k++) {
                        env.AddVoxelDefinition(additionalVoxels[k].voxelDefinition);
                    }
                }
            }

            voxelsArray = new VoxelDefinition[100];
            float acumProb = 0;
            int currentIndex = 0;
            if (additionalVoxels != null) {
                for (int k = 0; k < additionalVoxels.Length; k++) {
                    if (additionalVoxels[k].voxelDefinition == null)
                        continue;
                    acumProb += additionalVoxels[k].probability;
                    acumProb = Mathf.Clamp01(acumProb);
                    int nextProb = (int)(acumProb * 100);
                    if (currentIndex < nextProb) {
                        VoxelDefinition vd = additionalVoxels[k].voxelDefinition;
                        do {
                            voxelsArray[currentIndex++] = vd;
                        } while (currentIndex < nextProb);
                    }
                }
            }
            while (currentIndex < 100) {
                voxelsArray[currentIndex++] = mainVoxel;
            }
        }

        /// <summary>
        /// 최적화 목적을 위해 이 함수는 임의의 값을 미리 계산하고 배열에 복셀을 배포합니다.
        /// </summary>
        void DistributeUndergroundVoxels (VoxelDefinition mainVoxel, BiomeUndergroundVoxel[] additionalVoxels, ref VoxelDefinition[][] voxelsArray) {

            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env != null) {
                if (mainVoxel == null) {
                    mainVoxel = env.defaultVoxel;
                }
                // Ensure textures are added to the engine
                if (additionalVoxels != null) {
                    for (int k = 0; k < additionalVoxels.Length; k++) {
                        env.AddVoxelDefinition(additionalVoxels[k].voxelDefinition);
                    }
                }
            }

            voxelsArray = new VoxelDefinition[1000][];
            for (int altitude = minimumAltitude; altitude < maximumAltitude; altitude++) {
                VoxelDefinition[] voxelsThisAltitude = new VoxelDefinition[100];
                int altitudeIndex = altitude - minimumAltitude;
                voxelsArray[altitudeIndex] = voxelsThisAltitude;
                float acumProb = 0;
                int currentIndex = 0;
                if (additionalVoxels != null) {
                    for (int k = 0; k < additionalVoxels.Length; k++) {
                        if (additionalVoxels[k].voxelDefinition == null)
                            continue;
                        if (additionalVoxels[k].altitudeMin != 0 && additionalVoxels[k].altitudeMin > altitude)
                            continue;
                        if (additionalVoxels[k].altitudeMax != 0 && additionalVoxels[k].altitudeMax < altitude)
                            continue;

                        acumProb += additionalVoxels[k].probability;
                        acumProb = Mathf.Clamp01(acumProb);
                        int nextProb = (int)(acumProb * 100);
                        if (currentIndex < nextProb) {
                            VoxelDefinition vd = additionalVoxels[k].voxelDefinition;
                            do {
                                voxelsThisAltitude[currentIndex++] = vd;
                            } while (currentIndex < nextProb);
                        }
                    }
                }
                while (currentIndex < 100) {
                    voxelsThisAltitude[currentIndex++] = mainVoxel;
                }
            }
        }

        /// <summary>
        /// 주어진 위치의 표면에 대한 임의의 복셀을 반환합니다.
        /// </summary>
        public VoxelDefinition GetVoxelTop (Vector3 position) {
            float rand = WorldRand.GetValue(position);
            int index = (int)(rand * 100);
            return allTopVoxels[index];
        }

        /// <summary>
        /// 지정된 위치의 지하에 대한 임의의 복셀을 반환합니다.
        /// </summary>
        public VoxelDefinition GetVoxelDirt (Vector3 position) {
            position.y -= minimumAltitude;
            float rand = WorldRand.GetValue(position);
            int altitude;
            if (position.y < 0) {
                altitude = 0;
            } else if (position.y > 999) {
                altitude = 999;
            } else {
                altitude = (int)position.y;
            }
            int index = (int)(rand * 100);
            VoxelDefinition vd = allDirtVoxels[altitude][index];
            return vd;
        }

    }


    [Serializable]
    public struct BiomeZone {
        public float altitudeMin;
        public float altitudeMax;

        [Range(0, 1f)]
        public float moistureMin;
        [Range(0, 1f)]
        public float moistureMax;

        [NonSerialized]
        public BiomeDefinition biome;
    }

    [Serializable]
    public partial class BiomeSurfaceVoxel {
        public VoxelDefinition voxelDefinition;
        [Range(0, 1)]
        public float probability;
    }

    [Serializable]
    public partial class BiomeUndergroundVoxel {
        public VoxelDefinition voxelDefinition;
        [Range(0, 1)]
        public float probability;
        public int altitudeMin, altitudeMax;
    }


    [Serializable]
    public struct BiomeTree {
        public ModelDefinition tree;
        public float probability;
    }

    [Serializable]
    public struct BiomeVegetation {
        public VoxelDefinition vegetation;
        public float probability;
    }

    [Serializable]
    public struct BiomeOre {
        public VoxelDefinition ore;
        [Range(0, 1)]
        [Tooltip("청크당 최소 확률입니다.이 최소 확률은 이전 광석의 최대 값에서 시작해야 모든 확률이 누적됩니다.")]
        public float probabilityMin;
        [Range(0, 1)]
        [Tooltip("청크당 최대 확률")]
        public float probabilityMax;
        [Tooltip("표면으로부터의 최소 깊이")]
        public int depthMin;
        [Tooltip("표면으로부터의 최대 깊이.필수의.")]
        public int depthMax;
        [Tooltip("정맥의 최소 크기")]
        public int veinMinSize;
        [Tooltip("정맥의 최대 크기")]
        public int veinMaxSize;
        [Tooltip("청크당 최소 정맥 수")]
        public int veinsCountMin;
        [Tooltip("청크당 최대 정맥 수")]
        public int veinsCountMax;
    }

}