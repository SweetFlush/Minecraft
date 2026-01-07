using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public enum TerrainStepType {
        SampleHeightMapTexture = 0,
        SampleRidgeNoiseFromTexture = 1,
        SampleHeightMapFractal = 2,
        SampleHeightMapUnityTerrain = 3,
        Constant = 100,
        Copy = 101,
        Random = 102,
        Invert = 103,
        Shift = 104,
        BeachMask = 105,
        AddAndMultiply = 200,
        MultiplyAndAdd = 201,
        Exponential = 202,
        Threshold = 203,
        FlattenOrRaise = 204,
        Island = 205,
        BlendAdditive = 300,
        BlendMultiply = 301,
        Clamp = 302,
        Select = 303,
        Fill = 304,
        Test = 305
    }

    [Serializable]
    public struct StepData {
        public bool enabled;
        public TerrainStepType operation;
        public string description;
        public Texture2D noiseTexture;
        public TerrainData terrainData;
        [Range(0.001f, 2f)]
        public float frecuency;
        public Vector2 offset;
        [Range(0, 1f)]
        public float noiseRangeMin;
        [Range(0, 1f)]
        public float noiseRangeMax;

        [Range(1, 8)]
        public int octaves;
        public float persistence;
        public float lacunarity;

        public int inputIndex0;
        public int inputIndex1;

        public float threshold, thresholdShift, thresholdParam;

        public float param, param2, param3;
        public float weight0, weight1;

        public float min, max;

        [HideInInspector, NonSerialized]
        public float[] noiseValues;
        [HideInInspector, NonSerialized]
        public int noiseTextureSize;
        [HideInInspector, NonSerialized]
        public float value;
        [HideInInspector, NonSerialized]
        public Texture2D lastTextureLoaded;
        [HideInInspector, NonSerialized]
        public TerrainData lastTerrainDataLoaded;
    }

    public partial class BiomeDefinition {
        [NonSerialized]
        public int biomeGeneration;
    }

    public partial interface ITerrainDefaultGenerator {
        StepData[] Steps { get; set; }
    }


    [CreateAssetMenu(menuName = "Voxel Play/Terrain Generators/Multi-Step Terrain Generator", fileName = "MultiStepTerrainGenerator", order = 101)]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001906-terrain-generators")]
    public partial class TerrainDefaultGenerator : VoxelPlayTerrainGenerator, ITerrainDefaultGenerator {

        [SerializeField]
        StepData[] steps;

        [TextArea]
        public string hint = "The final value returned by the steps chain should be in 0..1 range. This value will be multiplied by the Max Height param to determine the terrain altitude.";

        public StepData[] Steps {
            get { return steps; }
            set { steps = value; }
        }

        [Range(0, 1f)]
        public float seaDepthMultiplier = 0.4f;
        [Range(0, 0.02f)]
        public float beachWidth = 0.001f;
        public VoxelDefinition waterVoxel;
        public VoxelDefinition shoreVoxel;
        [Tooltip("지형 생성기에서 최소 높이의 청크에 하드 제한을 설정하는 데 사용됩니다.")]
        public VoxelDefinition bedrockVoxel;

        [Header("Underground")]
        public bool addOre;

        [Header("Moisture Parameters")]
        public Texture2D moisture;
        [Range(0, 1f)]
        public float moistureScale = 0.2f;

        // Internal fields
        protected float[] moistureValues;
        protected int noiseMoistureTextureSize;
        protected float seaLevelAlignedWithInt, beachLevelAlignedWithInt;
        protected bool paintShore;
        protected HeightMapInfo[] heightChunkData;
        protected Texture2D lastMoistureTextureLoaded;
        protected int generation;

        public override void GetTerrainVoxelDefinitions (List<VoxelDefinition> vds) {
            if (shoreVoxel != null) vds.Add(shoreVoxel);
            if (bedrockVoxel != null) vds.Add(bedrockVoxel);
            if (env == null || env.world == null || env.world.biomes == null) return;
            foreach (var biome in world.biomes) {
                if (biome.voxelTop != null) vds.Add(biome.voxelTop);
                if (biome.voxelTopAdditional != null) {
                    foreach (var voxel in biome.voxelTopAdditional) {
                        if (voxel.voxelDefinition != null) {
                            vds.Add(voxel.voxelDefinition);
                        }
                    }
                }
                if (biome.voxelDirt != null) vds.Add(biome.voxelDirt);
                if (biome.voxelDirtAdditional != null) {
                    foreach (var voxel in biome.voxelDirtAdditional) {
                        if (voxel.voxelDefinition != null) {
                            vds.Add(voxel.voxelDefinition);
                        }
                    }
                }
                if (biome.voxelLakeBed != null) vds.Add(biome.voxelLakeBed);
            }
        }

        protected override void Init () {

            if (env.world != null && env.world.biomes != null) {
                for (int k = 0; k < env.world.biomes.Length; k++) {
                    BiomeDefinition biome = env.world.biomes[k];
                    if (biome != null) {
                        biome.Init();
                    }
                }
                if (env.world.defaultBiome != null) {
                    env.world.defaultBiome.Init();
                }
            }

            seaLevelAlignedWithInt = waterLevel / maxHeight;
            beachLevelAlignedWithInt = (waterLevel + 1f) / maxHeight;
            if (steps != null) {
                for (int k = 0; k < steps.Length; k++) {
                    if (steps[k].noiseTexture != null) {
                        bool repeated = false;
                        for (int j = 0; j < k - 1; j++) {
                            if (steps[k].noiseTexture == steps[j].noiseTexture) {
                                steps[k].noiseValues = steps[j].noiseValues;
                                steps[k].noiseTextureSize = steps[j].noiseTextureSize;
                                repeated = true;
                                break;
                            }
                        }
                        if (!repeated && (steps[k].noiseTextureSize == 0 || steps[k].noiseValues == null || steps[k].lastTextureLoaded == null || steps[k].noiseTexture != steps[k].lastTextureLoaded)) {
                            steps[k].lastTextureLoaded = steps[k].noiseTexture;
                            steps[k].noiseValues = NoiseTools.LoadNoiseTexture(steps[k].noiseTexture, out steps[k].noiseTextureSize);
                        }
                    } else if (steps[k].terrainData != null) {
                        bool repeated = false;
                        for (int j = 0; j < k - 1; j++) {
                            if (steps[k].terrainData == steps[j].terrainData) {
                                steps[k].noiseValues = steps[j].noiseValues;
                                steps[k].noiseTextureSize = steps[j].noiseTextureSize;
                                repeated = true;
                                break;
                            }
                        }
                        if (!repeated && (steps[k].noiseTextureSize == 0 || steps[k].noiseValues == null || steps[k].lastTerrainDataLoaded == null || steps[k].terrainData != steps[k].lastTerrainDataLoaded)) {
                            steps[k].lastTerrainDataLoaded = steps[k].terrainData;
                            steps[k].noiseValues = NoiseTools.LoadHeightmapFromTerrainData(steps[k].terrainData, out steps[k].noiseTextureSize);
                        }
                    }

                    // Validate references
                    if (steps[k].inputIndex0 < 0 || steps[k].inputIndex0 >= steps.Length) {
                        steps[k].inputIndex0 = 0;
                    }
                    if (steps[k].inputIndex1 < 0 || steps[k].inputIndex1 >= steps.Length) {
                        steps[k].inputIndex1 = 0;
                    }
                }
            }
            if (moisture != null && (noiseMoistureTextureSize == 0 || moistureValues == null || lastMoistureTextureLoaded == null || lastMoistureTextureLoaded != moisture)) {
                lastMoistureTextureLoaded = moisture;
                moistureValues = NoiseTools.LoadNoiseTexture(moisture, out noiseMoistureTextureSize);
            }
            if (waterVoxel == null) {
                waterVoxel = env.defaultWaterVoxel;
            }
            env.currentWaterVoxelDefinition = waterVoxel;
            if (waterVoxel.height == 0) {
                env.ShowError("Water voxel definition height is 0. It should be greater than 0. Pleae check the water voxel definition.");
            }

            paintShore = shoreVoxel != null;

            // Ensure voxels are available
            env.AddVoxelDefinitions(shoreVoxel, waterVoxel, bedrockVoxel);
        }

        /// <summary>
        /// 고도와 습도를 가져옵니다(0-1 범위).
        /// </summary>
        /// <param name="x">x 좌표입니다.</param>
        /// <param name="z">z 좌표입니다.</param>
        /// <param name="altitude">0-1 범위의 고도입니다.</param>
        /// <param name="moisture">0-1 범위의 수분.</param>
        public override void GetHeightAndMoisture (double x, double z, out float altitude, out float moisture) {

            if (!isInitialized) {
                Initialize();
            }

            bool allowBeach = true;
            if (steps != null && steps.Length > 0) {
                float value = 0;
                int stepsLength = steps.Length;
                for (int k = 0; k < stepsLength; k++) {
                    if (steps[k].enabled) {
                        switch (steps[k].operation) {
                            case TerrainStepType.SampleHeightMapTexture:
                            case TerrainStepType.SampleHeightMapUnityTerrain:
                                value = NoiseTools.GetNoiseValueBilinear(steps[k].noiseValues, steps[k].noiseTextureSize, x * steps[k].frecuency + steps[k].offset.x, z * steps[k].frecuency + steps[k].offset.y);
                                value = value * (steps[k].noiseRangeMax - steps[k].noiseRangeMin) + steps[k].noiseRangeMin;
                                break;
                            case TerrainStepType.SampleRidgeNoiseFromTexture:
                                value = NoiseTools.GetNoiseValueBilinear(steps[k].noiseValues, steps[k].noiseTextureSize, x * steps[k].frecuency + steps[k].offset.x, z * steps[k].frecuency + steps[k].offset.y, true);
                                value = value * (steps[k].noiseRangeMax - steps[k].noiseRangeMin) + steps[k].noiseRangeMin;
                                break;
                            case TerrainStepType.SampleHeightMapFractal:
                                value = 0;
                                float currentFrequency = steps[k].frecuency;
                                float currentAmplitude = 1f;
                                float amplitudeTotal = 0;
                                for (int j = 0; j < steps[k].octaves; j++) {
                                    float octaveValue = NoiseTools.GetNoiseValueBilinear(steps[k].noiseValues, steps[k].noiseTextureSize, x * currentFrequency, z * currentFrequency);
                                    value += octaveValue * currentAmplitude;
                                    amplitudeTotal += currentAmplitude;
                                    currentFrequency *= steps[k].lacunarity;
                                    currentAmplitude *= steps[k].persistence;
                                }
                                value = (value / amplitudeTotal) * (steps[k].noiseRangeMax - steps[k].noiseRangeMin) + steps[k].noiseRangeMin;
                                break;
                            case TerrainStepType.Shift:
                                value += steps[k].param;
                                break;
                            case TerrainStepType.BeachMask: {
                                    int i1 = steps[k].inputIndex0;
                                    if (steps[i1].value > steps[k].threshold) {
                                        allowBeach = false;
                                    }
                                }
                                break;
                            case TerrainStepType.AddAndMultiply:
                                value = (value + steps[k].param) * steps[k].param2;
                                break;
                            case TerrainStepType.MultiplyAndAdd:
                                value = (value * steps[k].param) + steps[k].param2;
                                break;
                            case TerrainStepType.Exponential:
                                if (value < 0) {
                                    value = 0;
                                }
                                value = (float)Math.Pow(value, steps[k].param);
                                break;
                            case TerrainStepType.Constant:
                                value = steps[k].param;
                                break;
                            case TerrainStepType.Invert:
                                value = 1f - value;
                                break;
                            case TerrainStepType.Copy: {
                                    int i1 = steps[k].inputIndex0;
                                    value = steps[i1].value;
                                }
                                break;
                            case TerrainStepType.Random:
                                value = WorldRand.GetValue(x, z);
                                break;
                            case TerrainStepType.BlendAdditive: {
                                    int i1 = steps[k].inputIndex0;
                                    int i2 = steps[k].inputIndex1;
                                    value = steps[i1].value * steps[k].weight0 + steps[i2].value * steps[k].weight1;
                                }
                                break;
                            case TerrainStepType.BlendMultiply: {
                                    int i1 = steps[k].inputIndex0;
                                    int i2 = steps[k].inputIndex1;
                                    value = steps[i1].value * steps[i2].value;
                                }
                                break;
                            case TerrainStepType.Threshold: {
                                    int i1 = steps[k].inputIndex0;
                                    if (steps[i1].value >= steps[k].threshold) {
                                        value = steps[i1].value + steps[k].thresholdShift;
                                    } else {
                                        value = steps[k].thresholdParam;
                                    }
                                }
                                break;
                            case TerrainStepType.Island:
                                float d = (float)Math.Sqrt(x * x + z * z);
                                d -= steps[k].param;
                                if (d > 0) {
                                    value -= d * steps[k].param2 * (1f / maxHeight);
                                }
                                break;
                            case TerrainStepType.FlattenOrRaise:
                                if (value >= steps[k].threshold) {
                                    value = (value - steps[k].threshold) * steps[k].thresholdParam + steps[k].threshold;
                                }
                                break;
                            case TerrainStepType.Clamp:
                                if (value < steps[k].min)
                                    value = steps[k].min;
                                else if (value > steps[k].max)
                                    value = steps[k].max;
                                break;
                            case TerrainStepType.Select: {
                                    int i1 = steps[k].inputIndex0;
                                    if (steps[i1].value < steps[k].min) {
                                        value = steps[k].thresholdParam;
                                    } else if (steps[i1].value > steps[k].max) {
                                        value = steps[k].thresholdParam;
                                    } else {
                                        value = steps[i1].value;
                                    }
                                }
                                break;
                            case TerrainStepType.Fill: {
                                    int i1 = steps[k].inputIndex0;
                                    if (steps[i1].value >= steps[k].min && steps[i1].value <= steps[k].max) {
                                        value = steps[k].thresholdParam;
                                    }
                                }
                                break;
                            case TerrainStepType.Test: {
                                    int i1 = steps[k].inputIndex0;
                                    if (steps[i1].value >= steps[k].min && steps[i1].value <= steps[k].max) {
                                        value = 1f;
                                    } else {
                                        value = 0f;
                                    }
                                }
                                break;
                        }
                    }
                    steps[k].value = value;
                }
                altitude = value;
            } else {
                altitude = -9999; // no terrain so make altitude very low so every chunk be considered above terrain for GI purposes
            }

            // Moisture
            moisture = NoiseTools.GetNoiseValueBilinear(moistureValues, noiseMoistureTextureSize, x * moistureScale, z * moistureScale);

            // Remove any potential beach
            if (altitude < beachLevelAlignedWithInt && altitude >= seaLevelAlignedWithInt) {
                float depth = beachLevelAlignedWithInt - altitude;
                if (depth > beachWidth || !allowBeach) {
                    altitude = seaLevelAlignedWithInt - 0.0001f;
                }
            }

            // Adjusts sea depth
            if (altitude < seaLevelAlignedWithInt) {
                float depth = seaLevelAlignedWithInt - altitude;
                altitude = seaLevelAlignedWithInt - 0.0001f - depth * seaDepthMultiplier;
            }

        }

        /// <summary>
        /// 중앙 "위치"에 의해 정의된 청크 내부의 지형을 그립니다.
        /// </summary>
        /// <returns><c>진실</c>, if terrain was painted, <c>거짓</c> otherwise.</returns>
		public override bool PaintChunk (VoxelChunk chunk) {

            Vector3d position = chunk.position;
            if (position.y + VoxelPlayEnvironment.CHUNK_HALF_SIZE < minHeight) {
                chunk.isAboveSurface = false;
                return false;
            }
            int bedrockRow = -1;
            bool usesBedrockVoxel = bedrockVoxel != null;
            if (position.y < minHeight + VoxelPlayEnvironment.CHUNK_HALF_SIZE) {
                bedrockRow = (int)(minHeight - (position.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE) + 1) * ONE_Y_ROW - 1;
            }
            position.x -= VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            position.y -= VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            position.z -= VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            Vector3d pos;

            int waterLevel = env.waterLevel;
            Voxel[] voxels = chunk.voxels;

            bool hasContent = false;
            bool isAboveSurface = false;
            generation++;
            env.GetHeightMapInfoFast(position.x, position.z, out heightChunkData, out _);
            int shiftAmount = (int)Mathf.Log(VoxelPlayEnvironment.CHUNK_SIZE, 2);

            // iterate 256 slice of chunk (z/x plane = 16*16 positions)
            for (int arrayIndex = 0; arrayIndex < VoxelPlayEnvironment.ONE_Y_ROW; arrayIndex++) {
                float groundLevel = heightChunkData[arrayIndex].groundLevel;
                float surfaceLevel = waterLevel > groundLevel ? waterLevel : groundLevel;
                if (surfaceLevel < position.y) {
                    // position is above terrain or water
                    isAboveSurface = true;
                    continue;
                }
                BiomeDefinition biome = heightChunkData[arrayIndex].biome;
                if ((object)biome == null) {
                    biome = world.defaultBiome;
                    if ((object)biome == null)
                        continue;
                }

                int y = (int)(surfaceLevel - position.y);
                if (y >= VoxelPlayEnvironment.CHUNK_SIZE) {
                    y = VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE;
                }
                pos.y = position.y + y;
                pos.x = position.x + (arrayIndex & VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE);
                pos.z = position.z + (arrayIndex >> shiftAmount);

                // Place voxels
                bool hasWater = false;
                int voxelIndex = y * ONE_Y_ROW + arrayIndex;

                if (pos.y > groundLevel) {

                    // water above terrain
                    if (pos.y == surfaceLevel) {
                        isAboveSurface = true;
                    }
                    while (pos.y > groundLevel && voxelIndex >= 0) {
                        voxels[voxelIndex].SetFastWater(waterVoxel);
                        voxelIndex -= ONE_Y_ROW;
                        pos.y--;
                        hasWater = true;
                    }

                    // Underwater vegetation
                    if (env.enableVegetation && biome.underwaterVegetationDensity > 0 && biome.underwaterVegetation.Length > 0 && pos.y == groundLevel) {
                        float rn = WorldRand.GetValue(pos);
                        if (rn < biome.underwaterVegetationDensity) {
                            if (voxelIndex >= VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW) {
                                // request one vegetation voxel one position above which means the chunk above this one
                                Vector3d abovePos = pos;
                                abovePos.y++;
                                env.RequestVegetationCreation(abovePos, env.GetVegetation(biome.underwaterVegetation, rn / biome.underwaterVegetationDensity));
                            } else if (voxels[voxelIndex + ONE_Y_ROW].opaque < 15) {
                                // directly place a vegetation voxel above this voxel
                                voxels[voxelIndex + ONE_Y_ROW].Set(env.GetVegetation(biome.underwaterVegetation, rn / biome.underwaterVegetationDensity));
                                env.vegetationCreated++;
                            }
                        }
                    }

                } else if (pos.y == groundLevel) {
                    isAboveSurface = true;
                    if (voxels[voxelIndex].typeIndex == Voxel.EmptyTypeIndex) {
                        if (paintShore && pos.y == waterLevel) {
                            // this is on the shore, place a shoreVoxel
                            voxels[voxelIndex].Set(shoreVoxel);
                        } else {
                            // we're at the surface of the biome => draw the voxel top of the biome and also check for random vegetation and trees
                            VoxelDefinition topVoxel = biome.GetVoxelTop(pos);
                            voxels[voxelIndex].Set(topVoxel);
                            // Check tree probability
                            if (pos.y > waterLevel) {
                                float rn = WorldRand.GetValue(pos);
                                if (biome.treeDensity > 0 && rn < biome.treeDensity && biome.trees.Length > 0) {
                                    // request one tree at this position
                                    env.RequestTreeCreation(chunk, pos, env.GetTree(biome.trees, rn / biome.treeDensity));
                                } else if (biome.vegetationDensity > 0 && rn < biome.vegetationDensity && biome.vegetation.Length > 0) {
                                    if (voxelIndex >= VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW) {
                                        // request one vegetation voxel one position above which means the chunk above this one
                                        Vector3d abovePos = pos;
                                        abovePos.y++;
                                        env.RequestVegetationCreation(abovePos, env.GetVegetation(biome.vegetation, rn / biome.vegetationDensity));
                                    } else {
                                        // directly place a vegetation voxel above this voxel
                                        if (env.enableVegetation) {
                                            voxels[voxelIndex + ONE_Y_ROW].Set(env.GetVegetation(biome.vegetation, rn / biome.vegetationDensity));
                                            env.vegetationCreated++;
                                        }
                                    }
                                }
                            }
                        }
                        voxelIndex -= ONE_Y_ROW;
                        pos.y--;
                    }
                }

                biome.biomeGeneration = generation;

                // fill hole with water
                int lastHoleIndex = -1;
                int firstHoleIndex = -1;
                while (voxelIndex > bedrockRow && voxels[voxelIndex].typeIndex == Voxel.HoleTypeIndex && pos.y <= waterLevel) {
                    if (hasWater) {
                        voxels[voxelIndex].SetFastWater(waterVoxel);
                    }
                    lastHoleIndex = voxelIndex;
                    if (voxelIndex > firstHoleIndex) firstHoleIndex = voxelIndex;
                    voxelIndex -= ONE_Y_ROW;
                    pos.y--;
                }

                // Place lake/ocean bed
                if (voxelIndex > bedrockRow && voxels[voxelIndex].typeIndex == Voxel.EmptyTypeIndex && voxelIndex + ONE_Y_ROW < VoxelPlayEnvironment.CHUNK_VOXEL_COUNT && voxels[voxelIndex + ONE_Y_ROW].hasWater) {
                    voxels[voxelIndex].SetFastOpaque(biome.voxelLakeBed);
                    voxelIndex -= ONE_Y_ROW;
                    pos.y--;
                }

                // Continue filling down
                for (; voxelIndex > bedrockRow; voxelIndex -= ONE_Y_ROW, pos.y--) {
                    if (voxels[voxelIndex].typeIndex == Voxel.EmptyTypeIndex) { // avoid holes
                        VoxelDefinition dirtVoxel = biome.GetVoxelDirt(pos);
                        voxels[voxelIndex].SetFastOpaque(dirtVoxel);
                    } else if (voxels[voxelIndex].typeIndex == Voxel.HoleTypeIndex) { // hole under water level -> fill with water
                        lastHoleIndex = voxelIndex;
                        if (voxelIndex > firstHoleIndex) firstHoleIndex = voxelIndex;
                        if (hasWater && pos.y <= waterLevel) { // hole under water level -> fill with water
                            voxels[voxelIndex].SetFastWater(waterVoxel);
                        }
                    }
                }

                // Place bedrock
                if (voxelIndex >= 0 && bedrockRow >= 0 && usesBedrockVoxel) {
                    voxels[voxelIndex].SetFastOpaque(bedrockVoxel);
                }

                // Detail/vegetation in the ceiling of tunnels/caves: if there was a solid voxel on top, place vegetation
                if (biome.undergroundCeilingVegDensity > 0 && firstHoleIndex + ONE_Y_ROW < VoxelPlayEnvironment.CHUNK_VOXEL_COUNT && voxels[firstHoleIndex + ONE_Y_ROW].opaque == VoxelPlayEnvironment.FULL_OPAQUE) {
                    Vector3d placePos = pos;
                    placePos.y = position.y + firstHoleIndex / ONE_Y_ROW;
                    float rn = WorldRand.GetValue(placePos);
                    if (rn < biome.undergroundCeilingVegDensity && biome.undergroundCeilingVegetation.Length > 0) {
                        // request one vegetation voxel one position above which means the chunk above this one
                        env.RequestVegetationCreation(placePos, env.GetVegetation(biome.undergroundCeilingVegetation, rn / biome.undergroundCeilingVegDensity));
                    }
                }

                // Vegetation at base of tunnels/caves: if there was a hole on top, place vegetation
                if (lastHoleIndex >= ONE_Y_ROW && biome.undergroundVegDensity > 0 && voxels[lastHoleIndex - ONE_Y_ROW].opaque == VoxelPlayEnvironment.FULL_OPAQUE) {
                    Vector3d placePos = pos;
                    placePos.y = position.y + lastHoleIndex / ONE_Y_ROW;
                    float rn = WorldRand.GetValue(placePos);
                    if (rn < biome.undergroundVegDensity && biome.undergroundVegetation.Length > 0) {
                        // request one vegetation voxel one position above which means the chunk above this one
                        env.RequestVegetationCreation(placePos, env.GetVegetation(biome.undergroundVegetation, rn / biome.undergroundVegDensity));
                    }
                }

                hasContent = true;
            }

            // Spawn random ore
            if (addOre) {
                // Check if there's any ore in this chunk (randomly)
                float noiseValue = WorldRand.GetValue(chunk.position);
                for (int b = 0; b < world.biomes.Length; b++) {
                    BiomeDefinition biome = world.biomes[b];
                    if (biome.biomeGeneration != generation)
                        continue;
                    for (int o = 0; o < biome.ores.Length; o++) {
                        if (biome.ores[o].ore == null)
                            continue;
                        if (biome.ores[o].probabilityMin <= noiseValue && biome.ores[o].probabilityMax >= noiseValue) {
                            // ore picked; determine the number of veins in this chunk
                            int veinsCount = biome.ores[o].veinsCountMin + (int)(WorldRand.GetValue() * (biome.ores[o].veinsCountMax - biome.ores[o].veinsCountMin + 1f));
                            for (int vein = 0; vein < veinsCount; vein++) {
                                Vector3d veinPos = chunk.position;
                                veinPos.x += vein;
                                // Determine random vein position in the chunk
                                Vector3 v = WorldRand.GetVector3(veinPos, VoxelPlayEnvironment.CHUNK_SIZE);
                                int px = (int)v.x;
                                int py = (int)v.y;
                                int pz = (int)v.z;
                                veinPos = env.GetVoxelPosition(veinPos, px, py, pz);
                                int oreIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                                int veinSize = biome.ores[o].veinMinSize + (oreIndex % (biome.ores[o].veinMaxSize - biome.ores[o].veinMinSize + 1));
                                // span ore vein
                                SpawnOre(chunk, biome.ores[o].ore, veinPos, px, py, pz, veinSize, biome.ores[o].depthMin, biome.ores[o].depthMax);
                            }
                            break;
                        }
                    }
                }
            }

            // Finish, return
            chunk.isAboveSurface = isAboveSurface;
            return hasContent;
        }


        void SpawnOre (VoxelChunk chunk, VoxelDefinition oreDefinition, Vector3d veinPos, int px, int py, int pz, int veinSize, int minDepth, int maxDepth) {
            int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
            while (veinSize-- > 0 && voxelIndex >= 0 && voxelIndex < chunk.voxels.Length) {
                // Get height at position
                float groundLevel = heightChunkData[pz * VoxelPlayEnvironment.CHUNK_SIZE + px].groundLevel;
                int depth = (int)(groundLevel - veinPos.y);
                if (depth < minDepth || depth > maxDepth)
                    return;

                // Replace solid voxels with ore
                if (chunk.voxels[voxelIndex].opaque >= VoxelPlayEnvironment.FULL_OPAQUE) {
                    chunk.voxels[voxelIndex].SetFastOpaque(oreDefinition);
                }

                // Check if spawn continues
                Vector3d prevPos = veinPos;
                float v = WorldRand.GetValue(veinPos);
                int dir = (int)(v * 5);
                switch (dir) {
                    case 0: // down
                        veinPos.y--;
                        voxelIndex -= ONE_Y_ROW;
                        break;
                    case 1: // right
                        veinPos.x++;
                        voxelIndex++;
                        break;
                    case 2: // back
                        veinPos.z--;
                        voxelIndex -= ONE_Z_ROW;
                        break;
                    case 3: // left
                        veinPos.x--;
                        voxelIndex--;
                        break;
                    case 4: // forward
                        veinPos.z++;
                        voxelIndex += ONE_Z_ROW;
                        break;
                }
                if (veinPos.x == prevPos.x && veinPos.y == prevPos.y && veinPos.z == prevPos.z) {
                    veinPos.y--;
                    voxelIndex -= ONE_Y_ROW;
                }
            }
        }




    }

}