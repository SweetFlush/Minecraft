#define USES_URP
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEditorInternal;
using NUnit.Framework;

#if USES_URP
using UnityEngine.Rendering.Universal;
#endif

namespace VoxelPlay {

    [CustomEditor(typeof(VoxelPlayEnvironment))]
    public partial class VoxelPlayEnvironmentEditor : Editor {

        SerializedProperty debugLevel, enableGeneration;
        SerializedProperty world, enableBuildMode, buildMode, welcomeMessage, welcomeMessageDuration, renderInEditor, renderInEditorLowPriority, renderInEditorDetail, generateAroundCamera, renderInEditorAreaCenter, renderInEditorAreaSize;
        SerializedProperty enableConsole, showConsole, enableInventory, enableStatusBar, enableLoadingPanel, loadingText, initialWaitTime, initialWaitText, loadSavedGame, saveFilename, enableDebugWindow, showFPS;
        SerializedProperty globalIllumination, ambientLight, daylightShadowAtten, enableSmoothLighting, enableFogSkyBlending, textureSize, enableShadows, obscuranceMode, obscuranceIntensity;
        SerializedProperty shadowsOnWater, realisticWater;
        SerializedProperty enableTinting, enableColoredShadows, enableBevel, enableOutline, outlineColor, outlineThreshold, enableCurvature;
        SerializedProperty seeThrough, seeThroughTarget, seeThroughRadius, seeThroughHeightOffset, seeThroughAlpha;
        SerializedProperty useOriginShift, originShiftDistanceThreshold;
        SerializedProperty enableReliefMapping, reliefStrength, reliefMaxDistance, reliefIterations, reliefIterationsBinarySearch;
        SerializedProperty enableBrightPointLights, enableURPNativeLights, brightPointsMaxDistance;
        SerializedProperty enableNormalMap, usePixelLights, enableFresnel, fresnelExponent, fresnelIntensity, fresnelColor;
        SerializedProperty enableGlobalSpecular, globalSpecularIntensity;
        SerializedProperty hqFiltering, mipMapBias, filterMode, doubleSidedGlass, transparentBling, damageParticles, useComputeBuffers, usePostProcessing;
        SerializedProperty maxChunks, prewarmChunksInEditor, visibleChunksDistance, distanceAnchor, unloadFarChunks, unloadFarChunksMode, unloadFarNavMesh;
        SerializedProperty adjustCameraFarClip, forceChunkDistance, maxCPUTimePerFrame, maxChunksPerFrame, maxTreesPerFrame, maxBushesPerFrame, lowMemoryMode, delayedInitialization, onlyRenderInFrustum;
        SerializedProperty microVoxelsSnap;
#if !UNITY_WEBGL
        SerializedProperty multiThreadGeneration;
#endif
        SerializedProperty serverMode, enableColliders, enableTrees, denseTrees, enableVegetation, enableDetailGenerators, enableNavMesh, navMeshResolution, hideChunksInHierarchy;
        SerializedProperty sun, fogAmount, fogDistance, fogDistanceAuto, fogFallOff, fogTint, enableClouds;
        SerializedProperty uiCanvasPrefab, inputControllerPC, inputControllerMobile, crosshairPrefab, crosshairTexture, consoleBackgroundColor, statusBarBackgroundColor;
        SerializedProperty defaultBuildSound, defaultPickupSound, defaultImpactSound, defaultDestructionSound, defaultVoxel, defaultWaterVoxel;
        SerializedProperty layerParticles, layerVoxels, layerClouds, particlePoolSize;
        SerializedProperty previewTouchUIinEditor;
        SerializedProperty instancingCullingMode, instancingCullingPadding;
        SerializedProperty enableFarChunksRendering, farChunksShadows, farChunksShadowIntensity, farChunksWaterColorOverride, farChunksWaterColor, farChunksShoreColor, farChunksWaterReflections, farChunksWaterReflectionsIntensity, farChunksDeepWater;
        SerializedProperty sceneEditorAutomaticBackup;

        const string VP_SECTION_WORLD_SETTINGS = "VoxelPlayWorldSection";
        const string VP_SECTION_TERRAIN_SETTINGS = "VoxelPlayTerrainGeneratorSection";
        const string VP_SECTION_SCENE_EDITOR = "VoxelPlaySceneEditorSection";
        const string VP_SECTION_QUALITY = "VoxelPlayExpandQualitySection";
        const string VP_SECTION_RENDERING = "VoxelPlayExpandRenderingSection";
        const string VP_SECTION_STATS = "VoxelPlayVoxelStatsSection";
        const string VP_SECTION_GENERATION = "VoxelPlayVoxelGenerationSection";
        const string VP_SECTION_SKY = "VoxelPlaySkySection";
        const string VP_SECTION_GAME_FEATURES = "VoxelPlayInGameSection";
        const string VP_SECTION_DEFAULTS = "VoxelPlayDefaultsSection";
        const string VP_SECTION_ADVANCED = "VoxelPlayAdvancedSection";

        const string WORLD_EDITOR_ENABLED_LABEL = " World Editor (ON)";
        const string WORLD_EDITOR_ENABLED_LABEL_UNSAVED = " World Editor (ON) (Unsaved Changes)";
        const string WORLD_EDITOR_LABEL = " World Editor";

        VoxelPlayEnvironment env;
        WorldDefinition cachedWorld;
        VoxelPlayTerrainGenerator cachedTerrainGenerator;
        Editor cachedWorldEditor, cachedTerrainGeneratorEditor;
        static GUIStyle titleLabelStyle, boxStyle;
        static int cookieIndex = -1;
        Color titleColor;
        static GUIStyle sectionHeaderStyle;
        static bool expandWorldSettings, expandTerrainGeneratorSettings, expandSceneEditor;
        static bool expandQualitySection, expandRenderingSection, expandStatsSection, expandVoxelGenerationSection, expandSkySection, expandInGameSection, expandDefaultsSection, expandAdvancedSection;
        bool enableCurvatureFromShader;
        string[] chunkSizeOptions;
        int[] chunkSizeValues;
        int chunkNewSize;
        string[] microVoxelsSizeOptions;
        int[] microVoxelsSizeValues;
        int microVoxelsNewSize;
        string curvatureAmount;
        bool voxelPadding;
        int maxMaterialsPerChunk;

        [MenuItem("Assets/Create/Voxel Play/Online Documentation", false, 2001)]
        public static void ShowDocs () {
            Application.OpenURL("https://kronnect.freshdesk.com/support/home");
        }

        [MenuItem("Assets/Create/Voxel Play/Tutorials", false, 2002)]
        public static void ShowTutorials () {
            Application.OpenURL("https://youtube.com/playlist?list=PLqzCcLYG3btgt5wsMqTf7ANjrwacdjfr8");
        }

        [MenuItem("Assets/Create/Voxel Play/Support Forum", false, 2003)]
        public static void ShowSupport () {
            Application.OpenURL("https://kronnect.com/support");
        }


        void OnEnable () {
            titleColor = EditorGUIUtility.isProSkin ? new Color(0.52f, 0.66f, 0.9f) : new Color(0.12f, 0.16f, 0.4f);
            debugLevel = serializedObject.FindProperty("debugLevel");
            world = serializedObject.FindProperty("world");
            enableGeneration = serializedObject.FindProperty("enableGeneration");
            enableBuildMode = serializedObject.FindProperty("enableBuildMode");
            buildMode = serializedObject.FindProperty("buildMode");
            welcomeMessage = serializedObject.FindProperty("welcomeMessage");
            welcomeMessageDuration = serializedObject.FindProperty("welcomeMessageDuration");
            renderInEditor = serializedObject.FindProperty("renderInEditor");
            renderInEditorLowPriority = serializedObject.FindProperty("renderInEditorLowPriority");
            generateAroundCamera = serializedObject.FindProperty("generateAroundCamera");
            renderInEditorDetail = serializedObject.FindProperty("renderInEditorDetail");
            renderInEditorAreaCenter = serializedObject.FindProperty("renderInEditorAreaCenter");
            renderInEditorAreaSize = serializedObject.FindProperty("renderInEditorAreaSize");
            enableConsole = serializedObject.FindProperty("enableConsole");
            consoleBackgroundColor = serializedObject.FindProperty("consoleBackgroundColor");
            showConsole = serializedObject.FindProperty("showConsole");
            enableInventory = serializedObject.FindProperty("enableInventory");
            prewarmChunksInEditor = serializedObject.FindProperty("prewarmChunksInEditor");
            enableLoadingPanel = serializedObject.FindProperty("enableLoadingPanel");
            loadingText = serializedObject.FindProperty("loadingText");
            initialWaitTime = serializedObject.FindProperty("initialWaitTime");
            initialWaitText = serializedObject.FindProperty("initialWaitText");
            loadSavedGame = serializedObject.FindProperty("loadSavedGame");
            saveFilename = serializedObject.FindProperty("saveFilename");
            enableDebugWindow = serializedObject.FindProperty("enableDebugWindow");
            showFPS = serializedObject.FindProperty("showFPS");

            globalIllumination = serializedObject.FindProperty("globalIllumination");
            ambientLight = serializedObject.FindProperty("ambientLight");
            daylightShadowAtten = serializedObject.FindProperty("daylightShadowAtten");
            enableSmoothLighting = serializedObject.FindProperty("enableSmoothLighting");
            obscuranceMode = serializedObject.FindProperty("obscuranceMode");
            obscuranceIntensity = serializedObject.FindProperty("obscuranceIntensity");

            enableReliefMapping = serializedObject.FindProperty("enableReliefMapping");
            reliefStrength = serializedObject.FindProperty("reliefStrength");
            reliefMaxDistance = serializedObject.FindProperty("reliefMaxDistance");
            reliefIterations = serializedObject.FindProperty("reliefIterations");
            reliefIterationsBinarySearch = serializedObject.FindProperty("reliefIterationsBinarySearch");

            enableNormalMap = serializedObject.FindProperty("enableNormalMap");
            usePixelLights = serializedObject.FindProperty("usePixelLights");
            enableBevel = serializedObject.FindProperty("enableBevel");

            enableFresnel = serializedObject.FindProperty("enableFresnel");
            fresnelExponent = serializedObject.FindProperty("fresnelExponent");
            fresnelIntensity = serializedObject.FindProperty("fresnelIntensity");
            fresnelColor = serializedObject.FindProperty("fresnelColor");

            enableGlobalSpecular = serializedObject.FindProperty("enableGlobalSpecular");
            globalSpecularIntensity = serializedObject.FindProperty("globalSpecularIntensity");

            enableBrightPointLights = serializedObject.FindProperty("enableBrightPointLights");
            enableURPNativeLights = serializedObject.FindProperty("enableURPNativeLights");
            brightPointsMaxDistance = serializedObject.FindProperty("brightPointsMaxDistance");

            enableFogSkyBlending = serializedObject.FindProperty("enableFogSkyBlending");
            textureSize = serializedObject.FindProperty("textureSize");
            realisticWater = serializedObject.FindProperty("realisticWater");
            shadowsOnWater = serializedObject.FindProperty("shadowsOnWater");
            enableShadows = serializedObject.FindProperty("enableShadows");
            enableTinting = serializedObject.FindProperty("enableTinting");
            enableColoredShadows = serializedObject.FindProperty("enableColoredShadows");
            enableCurvature = serializedObject.FindProperty("enableCurvature");
            enableOutline = serializedObject.FindProperty("enableOutline");
            outlineColor = serializedObject.FindProperty("outlineColor");
            outlineThreshold = serializedObject.FindProperty("outlineThreshold");
            doubleSidedGlass = serializedObject.FindProperty("doubleSidedGlass");
            transparentBling = serializedObject.FindProperty("transparentBling");
            damageParticles = serializedObject.FindProperty("damageParticles");
            hqFiltering = serializedObject.FindProperty("hqFiltering");
            mipMapBias = serializedObject.FindProperty("mipMapBias");
            filterMode = serializedObject.FindProperty("filterMode");
            useComputeBuffers = serializedObject.FindProperty("useComputeBuffers");
            usePostProcessing = serializedObject.FindProperty("usePostProcessing");

            seeThrough = serializedObject.FindProperty("seeThrough");
            seeThroughTarget = serializedObject.FindProperty("seeThroughTarget");
            seeThroughRadius = serializedObject.FindProperty("seeThroughRadius");
            seeThroughHeightOffset = serializedObject.FindProperty("_seeThroughHeightOffset");
            seeThroughAlpha = serializedObject.FindProperty("seeThroughAlpha");

            useOriginShift = serializedObject.FindProperty("useOriginShift");
            originShiftDistanceThreshold = serializedObject.FindProperty("originShiftDistanceThreshold");

            maxChunks = serializedObject.FindProperty("maxChunks");
            visibleChunksDistance = serializedObject.FindProperty("_visibleChunksDistance");
            distanceAnchor = serializedObject.FindProperty("distanceAnchor");
            unloadFarChunks = serializedObject.FindProperty("unloadFarChunks");
            unloadFarChunksMode = serializedObject.FindProperty("unloadFarChunksMode");
            unloadFarNavMesh = serializedObject.FindProperty("unloadFarNavMesh");
            adjustCameraFarClip = serializedObject.FindProperty("adjustCameraFarClip");

            forceChunkDistance = serializedObject.FindProperty("forceChunkDistance");
            maxCPUTimePerFrame = serializedObject.FindProperty("maxCPUTimePerFrame");
            maxChunksPerFrame = serializedObject.FindProperty("maxChunksPerFrame");
            maxTreesPerFrame = serializedObject.FindProperty("maxTreesPerFrame");
            maxBushesPerFrame = serializedObject.FindProperty("maxBushesPerFrame");
#if !UNITY_WEBGL
            multiThreadGeneration = serializedObject.FindProperty("multiThreadGeneration");
#endif
            lowMemoryMode = serializedObject.FindProperty("lowMemoryMode");
            delayedInitialization = serializedObject.FindProperty("delayedInitialization");
            onlyRenderInFrustum = serializedObject.FindProperty("onlyRenderInFrustum");
            serverMode = serializedObject.FindProperty("serverMode");
            enableColliders = serializedObject.FindProperty("enableColliders");
            enableNavMesh = serializedObject.FindProperty("enableNavMesh");
            navMeshResolution = serializedObject.FindProperty("navMeshResolution");
            hideChunksInHierarchy = serializedObject.FindProperty("hideChunksInHierarchy");
            enableTrees = serializedObject.FindProperty("enableTrees");
            denseTrees = serializedObject.FindProperty("denseTrees");
            enableVegetation = serializedObject.FindProperty("enableVegetation");
            enableDetailGenerators = serializedObject.FindProperty("enableDetailGenerators");

            microVoxelsSnap = serializedObject.FindProperty("microVoxelsSnap");

            sun = serializedObject.FindProperty("sun");
            fogAmount = serializedObject.FindProperty("fogAmount");
            fogDistance = serializedObject.FindProperty("fogDistance");
            fogDistanceAuto = serializedObject.FindProperty("fogDistanceAuto");
            fogFallOff = serializedObject.FindProperty("fogFallOff");
            fogTint = serializedObject.FindProperty("fogTint");

            enableClouds = serializedObject.FindProperty("enableClouds");

            uiCanvasPrefab = serializedObject.FindProperty("UICanvasPrefab");
            inputControllerPC = serializedObject.FindProperty("inputControllerPCPrefab");
            inputControllerMobile = serializedObject.FindProperty("inputControllerMobilePrefab");
            crosshairPrefab = serializedObject.FindProperty("crosshairPrefab");
            crosshairTexture = serializedObject.FindProperty("crosshairTexture");

            enableStatusBar = serializedObject.FindProperty("enableStatusBar");
            statusBarBackgroundColor = serializedObject.FindProperty("statusBarBackgroundColor");

            layerParticles = serializedObject.FindProperty("layerParticles");
            particlePoolSize = serializedObject.FindProperty("particlePoolSize");
            layerVoxels = serializedObject.FindProperty("layerVoxels");
            layerClouds = serializedObject.FindProperty("layerClouds");

            defaultBuildSound = serializedObject.FindProperty("defaultBuildSound");
            defaultPickupSound = serializedObject.FindProperty("defaultPickupSound");
            defaultImpactSound = serializedObject.FindProperty("defaultImpactSound");
            defaultDestructionSound = serializedObject.FindProperty("defaultDestructionSound");
            defaultVoxel = serializedObject.FindProperty("defaultVoxel");
            defaultWaterVoxel = serializedObject.FindProperty("defaultWaterVoxel");

            enableFarChunksRendering = serializedObject.FindProperty("enableFarChunksRendering");
            farChunksShadows = serializedObject.FindProperty("farChunksShadows");
            farChunksShadowIntensity = serializedObject.FindProperty("farChunksShadowIntensity");
            farChunksWaterReflections = serializedObject.FindProperty("farChunksWaterReflections");
            farChunksWaterReflectionsIntensity = serializedObject.FindProperty("farChunksWaterReflectionsIntensity");
            farChunksWaterColorOverride = serializedObject.FindProperty("farChunksWaterColorOverride");
            farChunksWaterColor = serializedObject.FindProperty("farChunksWaterColor");
            farChunksShoreColor = serializedObject.FindProperty("farChunksShoreColor");
            farChunksDeepWater = serializedObject.FindProperty("farChunksDeepWater");

            sceneEditorAutomaticBackup = serializedObject.FindProperty("sceneEditorAutomaticBackup");

            env = (VoxelPlayEnvironment)target;
            if (!Application.isPlaying) {
                if (!env.initialized && env.gameObject.activeInHierarchy) {
                    env.InitAndLoadSaveGame();
                }
                env.WantRepaintInspector += Repaint;
            }

            expandWorldSettings = EditorPrefs.GetBool(VP_SECTION_WORLD_SETTINGS, expandWorldSettings);
            expandTerrainGeneratorSettings = EditorPrefs.GetBool(VP_SECTION_TERRAIN_SETTINGS, expandTerrainGeneratorSettings);
            expandSceneEditor = EditorPrefs.GetBool(VP_SECTION_SCENE_EDITOR, expandSceneEditor);
            expandQualitySection = EditorPrefs.GetBool(VP_SECTION_QUALITY, false);
            expandRenderingSection = EditorPrefs.GetBool(VP_SECTION_RENDERING, false);
            expandStatsSection = EditorPrefs.GetBool(VP_SECTION_STATS, false);
            expandVoxelGenerationSection = EditorPrefs.GetBool(VP_SECTION_GENERATION, false);
            expandSkySection = EditorPrefs.GetBool(VP_SECTION_SKY, false);
            expandInGameSection = EditorPrefs.GetBool(VP_SECTION_GAME_FEATURES, false);
            expandDefaultsSection = EditorPrefs.GetBool(VP_SECTION_DEFAULTS, false);
            expandAdvancedSection = EditorPrefs.GetBool(VP_SECTION_ADVANCED, true);

            enableCurvatureFromShader = "1".Equals(GetShaderOptionValue("VOXELPLAY_CURVATURE", "VPCommonVertexModifier.cginc"));
            curvatureAmount = GetShaderOptionValue("VOXELPLAY_CURVATURE_AMOUNT", "VPCommonVertexModifier.cginc");

            previewTouchUIinEditor = serializedObject.FindProperty("previewTouchUIinEditor");

            instancingCullingMode = serializedObject.FindProperty("instancingCullingMode");
            instancingCullingPadding = serializedObject.FindProperty("instancingCullingPadding");

            chunkSizeOptions = new string[] { "16", "32" };
            chunkSizeValues = new int[] { 16, 32 };
            chunkNewSize = VoxelPlayEnvironment.CHUNK_SIZE;

            microVoxelsSizeOptions = new string[] { "2", "4", "8", "16" };
            microVoxelsSizeValues = new int[] { 2, 4, 8, 16 };
            microVoxelsNewSize = MicroVoxels.COUNT_PER_AXIS;

            maxMaterialsPerChunk = VoxelPlayEnvironment.MAX_MATERIALS_PER_CHUNK;
            voxelPadding = VoxelPlayGreedyCommon.PADDING != 0;

            WorldEditorInit();
        }

        void OnDisable () {
            if (env != null) {
                env.VoxelHighlight(false);
                env.WantRepaintInspector -= this.Repaint;
            }

            WorldEditorDispose();

            EditorPrefs.SetBool(VP_SECTION_WORLD_SETTINGS, expandWorldSettings);
            EditorPrefs.SetBool(VP_SECTION_TERRAIN_SETTINGS, expandTerrainGeneratorSettings);
            EditorPrefs.SetBool(VP_SECTION_SCENE_EDITOR, expandSceneEditor);

            EditorPrefs.SetBool(VP_SECTION_QUALITY, expandQualitySection);
            EditorPrefs.SetBool(VP_SECTION_RENDERING, expandRenderingSection);
            EditorPrefs.SetBool(VP_SECTION_STATS, expandStatsSection);
            EditorPrefs.SetBool(VP_SECTION_GENERATION, expandVoxelGenerationSection);
            EditorPrefs.SetBool(VP_SECTION_SKY, expandSkySection);
            EditorPrefs.SetBool(VP_SECTION_GAME_FEATURES, expandInGameSection);
            EditorPrefs.SetBool(VP_SECTION_DEFAULTS, expandDefaultsSection);
            EditorPrefs.SetBool(VP_SECTION_ADVANCED, expandAdvancedSection);
        }

        void CollapseAllSections () {
            expandWorldSettings = false;
            expandTerrainGeneratorSettings = false;
            expandSceneEditor = false;
            expandQualitySection = false;
            expandRenderingSection = false;
            expandStatsSection = false;
            expandVoxelGenerationSection = false;
            expandSkySection = false;
            expandInGameSection = false;
            expandDefaultsSection = false;
        }

        void ToggleSection (ref bool section) {
            var state = !section;
            CollapseAllSections();
            section = state;
        }

        public override void OnInspectorGUI () {
            serializedObject.UpdateIfRequiredOrScript();
            if (boxStyle == null) {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(15, 10, 5, 5);
            }
            if (titleLabelStyle == null) {
                titleLabelStyle = new GUIStyle(EditorStyles.label);
            }
            titleLabelStyle.normal.textColor = titleColor;
            titleLabelStyle.fontStyle = FontStyle.Bold;
            if (sectionHeaderStyle == null) {
                sectionHeaderStyle = new GUIStyle(EditorStyles.foldout);
            }
            sectionHeaderStyle.SetFoldoutColor();

            if (cookieIndex >= 0) {
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Help & Tutorials", titleLabelStyle);
                EditorGUILayout.HelpBox("인스펙터의 속성에 대해 더 알아보려면 레이블 위에 마우스를 올려 간단한 설명(툴팁)을 확인하세요.", MessageType.Info);
                if (GUILayout.Button("Online Documentation")) {
                    Application.OpenURL("https://kronnect.freshdesk.com/support/home");
                }
                if (GUILayout.Button("Tutorials")) {
                    Application.OpenURL("https://youtube.com/playlist?list=PLqzCcLYG3btgt5wsMqTf7ANjrwacdjfr8");
                }
                if (GUILayout.Button("Support Forum")) {
                    Application.OpenURL("https://kronnect.com/support");

                }
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Random Tip", titleLabelStyle);
                EditorGUILayout.HelpBox(VoxelPlayCookie.GetCookie(cookieIndex), MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("  ");
                ShowHelpButtons(true);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("General Settings", titleLabelStyle);
            if (cookieIndex < 0)
                ShowHelpButtons(false);
            EditorGUILayout.EndHorizontal();

            bool rebuildWorld = false;
            bool refreshChunks = false;
            bool reloadWorldTextures = false;
            bool updateSpecialFeaturesMacro = false;
            bool updateCurvatureMacro = false;
            bool prevBool = false;

            // General settings
            bool isURPActive = GraphicsSettings.currentRenderPipeline != null;
            if (isURPActive != VoxelPlayEnvironment.supportsURP) {
                refreshChunks = true;
                updateSpecialFeaturesMacro = true;
            }
#if USES_URP
            UniversalRenderPipelineAsset pipe = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipe != null) {
                if (!pipe.supportsCameraDepthTexture) {
                    EditorGUILayout.HelpBox("URP 자산에서 Depth Texture 옵션이 필요합니다!", MessageType.Error);
                    if (GUILayout.Button("Go to Universal Rendering Pipeline Asset")) {
                        Selection.activeObject = pipe;
                    }
                    EditorGUILayout.Separator();
                    GUI.enabled = false;
                }
                if (realisticWater.boolValue && pipe.msaaSampleCount > 1) {
                    EditorGUILayout.HelpBox("리얼리스틱 워터 옵션을 사용할 때는 MSAA를 꺼야 합니다. URP 자산에서 MSAA를 비활성화하세요.", MessageType.Warning);
                    if (GUILayout.Button("Go to Universal Rendering Pipeline Asset")) {
                        Selection.activeObject = pipe;
                    }
                    EditorGUILayout.Separator();
                }
            }

            CheckDepthPrimingMode();
#endif

            EditorGUILayout.BeginHorizontal();
            WorldDefinition wd = (WorldDefinition)world.objectReferenceValue;
            EditorGUILayout.PropertyField(world, new GUIContent("World", "월드 정의 에셋입니다. 이 에셋에는 바이옴, 복셀, 아이템 등 월드별 옵션 정의가 포함됩니다."));
            if (wd != world.objectReferenceValue)
                rebuildWorld = true;
            if (GUILayout.Button("Create", GUILayout.Width(50))) {
                CreateWorldDefinition();
            }
            if (GUILayout.Button("Locate", GUILayout.Width(50))) {
                Selection.activeObject = world.objectReferenceValue;
            }
            EditorGUILayout.EndHorizontal();
            if (world.objectReferenceValue == null) {
                EditorGUILayout.HelpBox("World Definition 자산을 만들거나 할당하세요.", MessageType.Warning);
            }

            GUIStyle leftAlignStyle = new GUIStyle(GUI.skin.button);
            leftAlignStyle.alignment = TextAnchor.MiddleLeft;
            leftAlignStyle.fixedHeight = leftAlignStyle.lineHeight + 6;

            if (world.objectReferenceValue != null) {
                if (GUILayout.Button(new GUIContent(" World Settings", Resources.Load("VoxelPlay/Icons/worldIcon") as Texture2D), leftAlignStyle)) {
                    ToggleSection(ref expandWorldSettings);
                }
                if (expandWorldSettings) {
                    if (cachedWorld != world.objectReferenceValue) {
                        cachedWorldEditor = null;
                    }
                    if (cachedWorldEditor == null) {
                        cachedWorld = (WorldDefinition)world.objectReferenceValue;
                        cachedWorldEditor = Editor.CreateEditor(world.objectReferenceValue);
                    }

                    // Drawing the world editor
                    EditorGUILayout.BeginVertical(boxStyle);
                    EditorGUI.BeginChangeCheck();
                    cachedWorldEditor.OnInspectorGUI();
                    if (EditorGUI.EndChangeCheck()) {
                        env.UpdateMaterialProperties();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Separator();
                }

                VoxelPlayTerrainGenerator terrainGenerator = (VoxelPlayTerrainGenerator)((WorldDefinition)world.objectReferenceValue).terrainGenerator;
                if (terrainGenerator != null) {
                    if (GUILayout.Button(new GUIContent(" Terrain Settings", Resources.Load("VoxelPlay/Icons/mountainsIcon") as Texture2D), leftAlignStyle)) {
                        expandTerrainGeneratorSettings = !expandTerrainGeneratorSettings;
                    }
                    if (expandTerrainGeneratorSettings) {
                        if (terrainGenerator != cachedTerrainGenerator) {
                            cachedTerrainGeneratorEditor = null;
                        }
                        if (cachedTerrainGeneratorEditor == null) {
                            cachedTerrainGenerator = terrainGenerator;
                            cachedTerrainGeneratorEditor = Editor.CreateEditor(terrainGenerator);
                        }

                        // Drawing the world editor
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.BeginVertical(boxStyle);
                        cachedTerrainGeneratorEditor.OnInspectorGUI();
                        EditorGUILayout.EndVertical();
                        if (EditorGUI.EndChangeCheck()) {
                            env.NotifyTerrainGeneratorConfigurationChanged();
                            VoxelPlayBiomeExplorer.requestRefresh = true;
                            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                        }
                        EditorGUILayout.Separator();
                    }
                }

                string worldEditorHeaderLabel = WORLD_EDITOR_LABEL;
                if (renderInEditor.boolValue) {
                    if (pendingChanges) {
                        worldEditorHeaderLabel = WORLD_EDITOR_ENABLED_LABEL_UNSAVED;
                    } else {
                        worldEditorHeaderLabel = WORLD_EDITOR_ENABLED_LABEL;
                    }
                }
                if (GUILayout.Button(new GUIContent(worldEditorHeaderLabel, Resources.Load("VoxelPlay/Inspector/sceneViewEditor") as Texture2D), leftAlignStyle)) {
                    ToggleSection(ref expandSceneEditor);
                }
                if (expandSceneEditor) {

                    EditorGUILayout.BeginVertical(boxStyle);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(renderInEditor, new GUIContent("Render In Editor", "에디터에서 월드 렌더링을 활성화합니다. 비활성화하면 플레이 모드에서만 보입니다."));
                    if (EditorGUI.EndChangeCheck()) {
                        env.NotifyCameraMove();
                        if (!renderInEditor.boolValue && !Application.isPlaying) {
                            if (EditorUtility.DisplayDialog("Render In Editor", "Remove voxels from scene?", "Yes", "No")) {
                                env.DisposeAll();
                            }
                        }
                    }

                    if (!renderInEditor.boolValue) GUI.enabled = false;

                    EditorGUILayout.PropertyField(generateAroundCamera, new GUIContent("   Automatic", "SceneView 카메라 위치를 따라 주변 월드를 렌더링합니다."));

                    EditorGUILayout.PropertyField(renderInEditorLowPriority, new GUIContent("   Low Priority", "활성화하면 씬 카메라가 정지해 있을 때만 에디터 렌더링을 수행합니다."));
                    if (wd != world.objectReferenceValue) {
                        rebuildWorld = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(renderInEditorDetail, new GUIContent("   Render Detail", "에디터에서 렌더링할 디테일 수준을 선택합니다."));
                    if (EditorGUI.EndChangeCheck()) {
                        rebuildWorld = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    GUIContent[] options = new GUIContent[] { new GUIContent("File", "불러오기/저장/내보내기 작업입니다."), new GUIContent("Terrain", "지형을 편집/커스터마이즈하는 도구입니다."), new GUIContent("Sculpt", "월드에서 복셀을 배치/제거/편집하는 도구입니다."), new GUIContent("Other", "기타 월드 관리 도구입니다.") };
                    GUIStyle style = new GUIStyle(GUI.skin.button);
                    style.fontStyle = FontStyle.Bold;
                    env.worldManagementSelectedTool = GUILayout.SelectionGrid(env.worldManagementSelectedTool, options, 4, style);
                    if (EditorGUI.EndChangeCheck()) {
                        selectedToolType = null;
                    }
                    GUI.enabled = true;

                    if (renderInEditor.boolValue) {

                        switch (env.worldManagementSelectedTool) {
                            case 0:
                                float half = EditorGUIUtility.currentViewWidth * 0.42f;
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("Toggle Chunks", GUILayout.Width(half))) {
                                    env.ChunksToggle();
                                    SceneView.RepaintAll();
                                }
                                if (GUILayout.Button("Delete Chunks", GUILayout.Width(half))) {
                                    renderInEditor.boolValue = false;
                                    env.DisposeAll();
                                }
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("Reset World", GUILayout.Width(half))) {
                                    if (EditorUtility.DisplayDialog("Reset World", "This option will discard any modified chunk and create it again from the terrain generator. Continue?", "Yes", "No")) {
                                        renderInEditor.boolValue = true;
                                        serializedObject.ApplyModifiedProperties();
                                        env.ReloadWorld(keepWorldChanges: false);
                                        GUIUtility.ExitGUI();
                                        return;
                                    }
                                }
                                GUI.enabled = env.chunksCreated > 0;
                                if (GUILayout.Button("Export Chunks", GUILayout.Width(half))) {
                                    if (EditorUtility.DisplayDialog("Export Chunks", "Do you want to make chunks permanent in the scene and remove Voxel Play Environment manager?", "Ok", "Cancel")) {
                                        env.ChunksExport();
                                        EditorUtility.DisplayDialog("Export Chunks", "Chunks now available under 'Exported Chunks' node in hierarchy as regular gameobjects. Materials, textures and meshes are now part of the scene.\n\nThe 'ExportGlobalSettings' behaviour has been attached to 'Exported Chunks' root gameobject to keep global shader values.\nVoxel Play Environment has been removed.", "Ok");
                                        GUIUtility.ExitGUI();
                                        return;
                                    }
                                }
                                GUI.enabled = true;
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("Load", GUILayout.Width(half))) {
                                    if (EditorUtility.DisplayDialog("Load World", "Discard any change and reload the entire world?", "Yes", "No")) {
                                        renderInEditor.boolValue = true;
                                        serializedObject.ApplyModifiedProperties();
                                        env.LoadGameBinary(true);
                                        GUIUtility.ExitGUI();
                                        return;
                                    }
                                }
                                GUIStyle saveButtonStyle = new GUIStyle(GUI.skin.button);
                                if (pendingChanges) {
                                    saveButtonStyle.normal.textColor = Color.yellow;
                                    saveButtonStyle.fontStyle = FontStyle.Bold;
                                }
                                if (GUILayout.Button("Save", saveButtonStyle, GUILayout.Width(half))) {
                                    if (EditorUtility.DisplayDialog("Save World?", "Do you want to save the current world to " + env.saveFilename + "?", "Yes", "No")) {
                                        SaveWorldInEditor();
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.PropertyField(saveFilename, new GUIContent("Current File", "현재 저장된 월드 파일 이름입니다."));
                                EditorGUILayout.PropertyField(sceneEditorAutomaticBackup, new GUIContent("Automatic Backup", "활성화하면 저장 게임 파일을 업데이트하기 전에 백업을 생성합니다."));
                                break;

                            case 1:
                                DrawTools(WorldEditorToolCategory.TerrainTool);
                                break;

                            case 2:
                                DrawTools(WorldEditorToolCategory.SculptTool);
                                break;

                            case 3:
                                if (env.cameraMain != null) {
                                    EditorGUILayout.LabelField("Main Cam Position", env.cameraMain.transform.position.ToString());
                                    EditorGUILayout.BeginHorizontal();
                                    env.sceneEditorCameraMainPosition = EditorGUILayout.Vector3Field("   New Position", env.sceneEditorCameraMainPosition);
                                    if (GUILayout.Button("Set", GUILayout.Width(40))) {
                                        env.MoveMainCameraTo(env.sceneEditorCameraMainPosition);
                                        EditorUtility.SetDirty(env.cameraMain);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                Camera sceneCam = null;
                                if (SceneView.lastActiveSceneView != null) {
                                    sceneCam = SceneView.lastActiveSceneView.camera;
                                }
                                if (sceneCam != null) {
                                    EditorGUILayout.LabelField("Scene Cam Pos", sceneCam.transform.position.ToString());
                                    EditorGUILayout.BeginHorizontal();
                                    env.sceneEditorCameraSceneViewPosition = EditorGUILayout.Vector3Field("   New Position", env.sceneEditorCameraSceneViewPosition);
                                    if (GUILayout.Button("Set", GUILayout.Width(40))) {
                                        SceneView.lastActiveSceneView.LookAt(env.sceneEditorCameraSceneViewPosition + new Vector3(50, 50, 50));
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                EditorGUILayout.BeginHorizontal();
                                if (sceneCam != null) {
                                    if (GUILayout.Button("Scene Cam To Surface")) {
                                        Vector3 pos = env.sceneEditorCameraSceneViewPosition;
                                        pos.y = env.GetTerrainHeight(Vector3.zero, true);
                                        SceneView.lastActiveSceneView.LookAt(pos + new Vector3(50, 50, 50));
                                    }
                                }
                                if (env.cameraMain != null) {
                                    if (GUILayout.Button("Find Main Cam")) {
                                        Vector3 pos = env.cameraMain.transform.position + new Vector3(50, 50, 50);
                                        Vector3 fwd = (env.cameraMain.transform.position - pos).normalized;
                                        SceneView.lastActiveSceneView.LookAt(pos, Quaternion.LookRotation(fwd));
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.LabelField("Generate Area");
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(renderInEditorAreaCenter, new GUIContent("Center"));
                                EditorGUILayout.PropertyField(renderInEditorAreaSize, new GUIContent("Size"));
                                if (GUILayout.Button("Generate Chunks In Area")) {
                                    generateAroundCamera.boolValue = false;
                                    serializedObject.ApplyModifiedProperties();
                                    GenerateEditorArea();
                                    GUIUtility.ExitGUI();
                                }
                                EditorGUI.indentLevel--;
                                break;

                        }
                    }

                    EditorGUILayout.EndVertical();

                }
            }

            // Voxel Generation
            if (GUILayout.Button(new GUIContent(" Voxel Generation", Resources.Load("VoxelPlay/Inspector/voxelGeneration") as Texture2D), leftAlignStyle)) {
                ToggleSection(ref expandVoxelGenerationSection);
            }
            if (expandVoxelGenerationSection) {
                EditorGUILayout.PropertyField(enableGeneration, new GUIContent("Enable Generation", "월드/복셀 생성 업데이트를 활성/비활성합니다."));
                EditorGUILayout.PropertyField(maxChunks, new GUIContent("Chunks Pool Size", "메모리에 허용되는 총 청크 수입니다."));
                EditorGUILayout.LabelField("   Recommended >=", env.maxChunksRecommended.ToString());
                EditorGUILayout.IntSlider(prewarmChunksInEditor, 1000, maxChunks.intValue, new GUIContent("   Prewarm In Editor", "유니티 에디터에서 게임 시작 전에 예약할 청크 수입니다. 최종 빌드에서는 게임 시작 전에 모든 청크가 예약되어 원활한 플레이 경험을 제공합니다."));
                EditorGUILayout.BeginHorizontal();
                chunkNewSize = EditorGUILayout.IntPopup("Chunk Size", chunkNewSize, chunkSizeOptions, chunkSizeValues);
                GUI.enabled = chunkNewSize != VoxelPlayEnvironment.CHUNK_SIZE;
                if (GUILayout.Button("Change", GUILayout.Width(80))) {
                    ChangeChunkSize();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                microVoxelsNewSize = EditorGUILayout.IntPopup("MicroVoxels Size", microVoxelsNewSize, microVoxelsSizeOptions, microVoxelsSizeValues);
                GUI.enabled = microVoxelsNewSize != MicroVoxels.COUNT_PER_AXIS;
                if (GUILayout.Button("Change", GUILayout.Width(80))) {
                    ChangeMicroVoxelsSize();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(microVoxelsSnap, new GUIContent("MicroVoxels Snap", "활성화하면 마이크로복셀 공간이 가장 가까운 마이크로복셀 위치로 스냅됩니다."));


                EditorGUILayout.PropertyField(onlyRenderInFrustum, new GUIContent("Only Render In Frustum", "활성화하면 카메라 프러스텀 안의 청크만 렌더링됩니다."));
#if UNITY_WEBGL
				GUI.enabled = false;
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Multi Thread Generation", GUILayout.Width (EditorGUIUtility.labelWidth));
				EditorGUILayout.LabelField ("(Unsupported platform)");
				EditorGUILayout.EndHorizontal ();
				GUI.enabled = true;
#else
                EditorGUILayout.PropertyField(multiThreadGeneration, new GUIContent("Multi Thread Generation", "활성화하면 청크 생성을 전용 백그라운드 스레드로 처리합니다(빌드에서만, 에디터 실행 중에는 비활성)."));
#endif
                EditorGUILayout.PropertyField(visibleChunksDistance, new GUIContent("Visible Chunk Distance", "청크 수 기준으로 측정합니다."));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(adjustCameraFarClip, new GUIContent("Adjust Cam Far Clip", "카메라 원거리 클리핑 거리를 표시 청크 거리로 자동 조정합니다."));
                EditorGUILayout.PropertyField(distanceAnchor, new GUIContent("Distance Anchor", "거리를 계산할 기준 위치입니다. 보통 1인칭은 카메라, 3인칭은 캐릭터입니다."));
                EditorGUILayout.PropertyField(unloadFarChunks, new GUIContent("Unload Far Chunks", "가시 거리 밖으로 나가면 청크 게임오브젝트를 비활성화하거나 파괴합니다. 다시 들어오면 활성화/생성됩니다."));
                if (unloadFarChunks.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(unloadFarChunksMode, new GUIContent("Mode", "청크 언로드 시 동작을 선택합니다. 'Toggle visibility'는 가시 거리 기준으로 숨김/표시만 합니다. 'Destroy'는 청크 메쉬와 콜라이더, 네브메시(있다면)를 파괴하고 메모리를 해제합니다."));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(unloadFarNavMesh, new GUIContent("Unload Far NavMesh", "가시 거리 밖일 때 청크 네브메시를 재사용할 수 있습니다. 참고: 네브메시는 청크에 연결되어 있어 풀 고갈 시 새 청크 요청에 자동 재사용됩니다. 이 옵션은 풀 고갈을 기다리지 않고 가시 거리 밖일 때 네브메시를 먼저 해제합니다."));
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(forceChunkDistance, new GUIContent("Force Chunk Distance", "게임 시작 전에 완전히 렌더링할 청크 거리(청크 수 기준)입니다."));
                EditorGUILayout.PropertyField(maxCPUTimePerFrame, new GUIContent("Max CPU Time Per Frame", "월드 생성에 프레임당 CPU가 사용할 수 있는 최대 밀리초입니다."));
                EditorGUILayout.PropertyField(maxChunksPerFrame, new GUIContent("Max Chunks Per Frame", "한 프레임에서 생성할 수 있는 최대 청크 수입니다(0=무제한, maxCPUTimePerFrame 값에만 제한됨)."));
                EditorGUILayout.PropertyField(maxTreesPerFrame, new GUIContent("Max Trees Per Frame", "한 프레임에서 생성할 수 있는 최대 나무 수입니다(0=무제한, maxCPUTimePerFrame 값에만 제한됨)."));
                EditorGUILayout.PropertyField(maxBushesPerFrame, new GUIContent("Max Bushes Per Frame", "한 프레임에서 생성할 수 있는 최대 덤불 수입니다(0=무제한, maxCPUTimePerFrame 값에만 제한됨)."));
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableColliders, new GUIContent("Colliders", "불투명 복셀의 콜라이더 생성을 활성/비활성합니다."));
                EditorGUILayout.PropertyField(enableNavMesh, new GUIContent("NavMesh", "네브메시 생성을 활성/비활성합니다."));
                if (enableNavMesh.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(navMeshResolution, new GUIContent("Resolution", "생성되는 네브메시의 디테일입니다. 단일 복셀에도 네브메시가 필요하면 높은 해상도를 사용하세요. 기본은 두 복셀마다입니다."));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(hideChunksInHierarchy, new GUIContent("Hide Chunks In Hierarchy", "계층에서 청크를 표시하지 않습니다(빌드에서는 영향 없음)."));
                EditorGUILayout.PropertyField(enableTrees, new GUIContent("Trees", "나무 생성을 활성/비활성합니다."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }
                if (enableTrees.boolValue) {
                    prevBool = denseTrees.boolValue;
                    EditorGUILayout.PropertyField(denseTrees, new GUIContent("   Dense Trees", "활성화하면 인접 복셀 오클루전을 끄고 나뭇잎 컷아웃을 더 촘촘하게 만듭니다."));
                    if (denseTrees.boolValue != prevBool)
                        refreshChunks = true;
                }
                prevBool = enableVegetation.boolValue;
                EditorGUILayout.PropertyField(enableVegetation, new GUIContent("Vegetation", "덤불 생성을 활성/비활성합니다."));
                if (enableVegetation.boolValue != prevBool)
                    rebuildWorld = true;
                EditorGUILayout.PropertyField(enableDetailGenerators, new GUIContent("Detail Generators", "월드 디테일 생성기를 활성/비활성합니다."));
                EditorGUILayout.PropertyField(particlePoolSize, new GUIContent("Particle Pool Size", "회수 가능한 복셀을 포함한 활성 파티클의 최대 수입니다."));
                layerParticles.intValue = EditorGUILayout.LayerField(new GUIContent("Particles Layer", "파티클에 사용할 레이어입니다. 물리 최적화 및 파티클 간 충돌 방지에 사용됩니다."), layerParticles.intValue);
                layerVoxels.intValue = EditorGUILayout.LayerField(new GUIContent("Voxels Layer", "복셀에 사용할 레이어입니다. 물리 최적화 및 복셀 간 충돌 방지에 사용됩니다."), layerVoxels.intValue);
                layerClouds.intValue = EditorGUILayout.LayerField(new GUIContent("Clouds Layer", "구름 복셀에 사용할 레이어입니다. 탑다운 카메라에서 구름 청크를 무시하는 등 용도로 사용할 수 있습니다."), layerClouds.intValue);
            }

            // Quality and effects
            if (GUILayout.Button(new GUIContent(" Shader Features", Resources.Load("VoxelPlay/Inspector/qualityAndEffects") as Texture2D), leftAlignStyle)) {
                ToggleSection(ref expandQualitySection);
            }
            if (expandQualitySection) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Preset", GUILayout.Width(120));
                if (GUILayout.Button(new GUIContent("All Features", "현재 플랫폼에서 사용할 수 있는 모든 시각적 기능을 활성화합니다."))) {
                    globalIllumination.boolValue = true;
                    enableShadows.boolValue = true;
                    shadowsOnWater.boolValue = true;
                    enableSmoothLighting.boolValue = true;
                    enableFogSkyBlending.boolValue = true;
                    denseTrees.boolValue = true;
                    hqFiltering.boolValue = true;
                    usePixelLights.boolValue = true;
                    enableBevel.boolValue = true;
                    enableFresnel.boolValue = true;
                    enableBrightPointLights.boolValue = true;
                    doubleSidedGlass.boolValue = true;
                    transparentBling.boolValue = true;
                    if (VoxelPlayFirstPersonController.instance != null) {
                        VoxelPlayFirstPersonController.instance.autoInvertColors = true;
                    }
                    rebuildWorld = true;
                }
                if (GUILayout.Button(new GUIContent("Medium", "성능을 위해 그림자를 끄되 전역 조명은 유지합니다."))) {
                    globalIllumination.boolValue = true;
                    enableShadows.boolValue = false;
                    shadowsOnWater.boolValue = false;
                    enableSmoothLighting.boolValue = true;
                    enableFogSkyBlending.boolValue = true;
                    enableReliefMapping.boolValue = false;
                    usePixelLights.boolValue = true;
                    enableBevel.boolValue = false;
                    enableFresnel.boolValue = false;
                    enableBrightPointLights.boolValue = false;
                    denseTrees.boolValue = true;
                    hqFiltering.boolValue = true;
                    doubleSidedGlass.boolValue = true;
                    transparentBling.boolValue = true;
                    if (VoxelPlayFirstPersonController.instance != null) {
                        VoxelPlayFirstPersonController.instance.autoInvertColors = true;
                    }
                    rebuildWorld = true;
                }
                if (GUILayout.Button(new GUIContent("Fastest", "성능을 위해 모든 효과를 비활성화합니다."))) {
                    globalIllumination.boolValue = false;
                    enableShadows.boolValue = false;
                    shadowsOnWater.boolValue = false;
                    enableSmoothLighting.boolValue = false;
                    obscuranceMode.intValue = (int)ObscuranceMode.Faster;
                    enableFogSkyBlending.boolValue = false;
                    enableReliefMapping.boolValue = false;
                    enableNormalMap.boolValue = false;
                    usePixelLights.boolValue = false;
                    enableFresnel.boolValue = false;
                    enableBevel.boolValue = false;
                    enableBrightPointLights.boolValue = false;
                    denseTrees.boolValue = false;
                    hqFiltering.boolValue = false;
                    doubleSidedGlass.boolValue = false;
                    onlyRenderInFrustum.boolValue = true;
                    transparentBling.boolValue = false;
                    if (VoxelPlayFirstPersonController.instance != null) {
                        VoxelPlayFirstPersonController.instance.autoInvertColors = false;
                    }
                    if (visibleChunksDistance.intValue > 6) {
                        visibleChunksDistance.intValue = 6;
                    }
                    if (forceChunkDistance.intValue > 2) {
                        forceChunkDistance.intValue = 2;
                    }
                    if (maxChunks.intValue > 5000) {
                        maxChunks.intValue = 5000;
                    }
                    rebuildWorld = true;
                }
                EditorGUILayout.EndHorizontal();

                prevBool = globalIllumination.boolValue;
                EditorGUILayout.PropertyField(globalIllumination, new GUIContent("Global Illumination", "Voxel Play 자체 라이트맵 계산을 활성화합니다. Unity 그림자 시스템과 함께 부드러운 음영 및 조명을 추가합니다."));
                if (globalIllumination.boolValue != prevBool)
                    refreshChunks = true;

                prevBool = enableSmoothLighting.boolValue;
                EditorGUILayout.PropertyField(enableSmoothLighting, new GUIContent("Smooth Lighting", "복셀 정점 간 조명을 보간합니다. 앰비언트 오클루전도 포함됩니다."));
                if (enableSmoothLighting.boolValue != prevBool)
                    refreshChunks = true;

                GUI.enabled = enableSmoothLighting.boolValue;
                int prevInt = obscuranceMode.intValue;
                EditorGUILayout.PropertyField(obscuranceMode, new GUIContent("Obscurance Mode", "셰이더의 오브스큐런스 함수를 변경합니다. 부드러운 조명이 필요합니다."));
                if (obscuranceMode.intValue != prevInt) {
                    updateSpecialFeaturesMacro = true;
                }
                if (obscuranceMode.intValue == (int)ObscuranceMode.Custom) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(obscuranceIntensity, new GUIContent("Intensity", "AO 강도입니다."));
                    EditorGUI.indentLevel--;
                }
                GUI.enabled = true;

                EditorGUILayout.PropertyField(ambientLight, new GUIContent("Ambient Light", "복셀에 영향을 주는 씬의 최소 광량입니다."));

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableShadows, new GUIContent("Shadows", "복셀의 그림자 투영/수신을 켜거나 끕니다."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }
                if (!enableShadows.boolValue) {
                    CheckMainLightShadows();
                }
                EditorGUI.BeginChangeCheck();
                if (!VoxelPlayEnvironment.supportsURP) {
                    EditorGUILayout.PropertyField(shadowsOnWater, new GUIContent("Shadows On Water", "물 표면의 그림자 수신을 활성화합니다."));
                } else if (shadowsOnWater.boolValue) {
                    shadowsOnWater.boolValue = false;
                }
                EditorGUILayout.PropertyField(realisticWater, new GUIContent("Realistic Water", "리얼리스틱 워터 셰이더를 사용합니다."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }

                EditorGUILayout.PropertyField(daylightShadowAtten, new GUIContent("Daylight Shadow Atten", "태양이 높을 때의 그림자 감쇠 계수입니다. 이 값을 0으로 하면 기본 그림자 강도를 유지합니다. 1이면 태양이 머리 위에 있을 때 그림자가 사라집니다. 중간 값은 태양이 낮을 때 그림자를 더 진하게, 높을 때는 더 옅게 만듭니다."));

                prevBool = enableNormalMap.boolValue;
                EditorGUILayout.PropertyField(enableNormalMap, new GUIContent("Normal Mapping", "노멀 맵 사용을 활성화합니다."));
                if (prevBool != enableNormalMap.boolValue) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                }
                prevBool = enableReliefMapping.boolValue;
                EditorGUILayout.PropertyField(enableReliefMapping, new GUIContent("Relief Mapping", "시차 오클루전/릴리프 매핑을 활성화합니다."));
                if (prevBool != enableReliefMapping.boolValue) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                }
                if (enableReliefMapping.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(reliefStrength, new GUIContent("Strength", "시차 효과의 강도입니다."));
                    EditorGUILayout.PropertyField(reliefMaxDistance, new GUIContent("Max Distance", "시차 효과가 보이는 최대 거리입니다."));
                    EditorGUILayout.PropertyField(reliefIterations, new GUIContent("Iterations", "레이 마칭 최대 단계 수입니다."));
                    EditorGUILayout.PropertyField(reliefIterationsBinarySearch, new GUIContent("Binary Search Iterations", "교차점을 정밀하게 찾기 위한 이진 탐색 최대 반복 수입니다."));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(textureSize, new GUIContent("Texture Size", "텍스처 크기는 2의 배수여야 합니다(예: 16, 32, 64, 128)."));


                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableTinting, new GUIContent("Enable Tinting", "개별 복셀 틴트 색상을 활성화합니다."));
                EditorGUILayout.PropertyField(enableColoredShadows, new GUIContent("Colored Shadows", "활성화하면 월드 정의에서 그림자 틴트 색상을 커스터마이즈합니다."));
                if (EditorGUI.EndChangeCheck()) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                EditorGUILayout.PropertyField(enableOutline, new GUIContent("Outline", "고체 복셀에 윤곽선 효과를 활성화합니다."));
                if (enableOutline.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(outlineColor, new GUIContent("Color", "윤곽선 색상과 알파입니다."));
                    EditorGUILayout.PropertyField(outlineThreshold, new GUIContent("Threshold", "윤곽선 두께를 조절합니다."));
                    EditorGUI.indentLevel--;
                }

                GUI.enabled = usePixelLights.boolValue;
                prevBool = enableBevel.boolValue;
                EditorGUILayout.PropertyField(enableBevel, new GUIContent("Bevel", "상단 면 노멀을 조정하여 베벨 효과를 활성화합니다. 3인칭 시점에서 더 보기 좋습니다."));
                if (prevBool != enableBevel.boolValue && !Application.isPlaying) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }
                GUI.enabled = true;

                GUI.enabled = usePixelLights.boolValue;
                prevBool = enableFresnel.boolValue;
                EditorGUILayout.PropertyField(enableFresnel, new GUIContent("Fresnel", "프레넬 효과를 활성화합니다."));
                if (enableFresnel.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(fresnelExponent, new GUIContent("Exponent"));
                    EditorGUILayout.PropertyField(fresnelIntensity, new GUIContent("Intensity"));
                    EditorGUILayout.PropertyField(fresnelColor, new GUIContent("Color"));
                    EditorGUI.indentLevel--;
                }
                if (prevBool != enableFresnel.boolValue && !Application.isPlaying) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                prevBool = enableGlobalSpecular.boolValue;
                EditorGUILayout.PropertyField(enableGlobalSpecular, new GUIContent("Global Specular", "일반 불투명 복셀의 스페큘러를 활성화합니다. 방향광을 향할 때 더 반짝이게 합니다."));
                if (enableGlobalSpecular.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(globalSpecularIntensity, new GUIContent("Intensity"));
                    EditorGUI.indentLevel--;
                }
                if (prevBool != enableGlobalSpecular.boolValue && !Application.isPlaying) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                GUI.enabled = true;


                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(doubleSidedGlass, new GUIContent("Double Sided Glass", "투명 복셀의 양면을 렌더링합니다."));
                EditorGUILayout.PropertyField(transparentBling, new GUIContent("Transparent Bling", "투명 복셀의 반짝임 효과를 활성화합니다."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }

                EditorGUILayout.PropertyField(damageParticles);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableBrightPointLights, new GUIContent("Bright Point Lights", "포인트 라이트의 외관을 개선합니다."));
                if (enableBrightPointLights.boolValue) {
                    EditorGUI.indentLevel++;
                    if (VoxelPlayEnvironment.supportsURP) {
                        EditorGUILayout.PropertyField(enableURPNativeLights, new GUIContent("Enable URP Native Lights", "그림자를 포함한 URP 네이티브 포인트/스폿 라이트 지원을 추가합니다. Project Settings/Quality 또는 Project Settings/Graphics에서 사용하는 URP 에셋에서 Additional Lights와 Shadows가 활성화되어 있는지 확인하세요."));
                    }
                }

                if (EditorGUI.EndChangeCheck()) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                if (enableBrightPointLights.boolValue) {
                    EditorGUILayout.PropertyField(brightPointsMaxDistance, new GUIContent("Max Distance", "밝은 포인트 라이트를 렌더링할 최대 거리입니다."));
                    EditorGUI.indentLevel--;
                }

                GUI.enabled = !Application.isPlaying;
                EditorGUI.BeginChangeCheck();
                enableCurvatureFromShader = EditorGUILayout.Toggle(new GUIContent("Curvature", "VoxelPlay 셰이더에서 곡률 버텍스 수정자를 활성화합니다."), enableCurvatureFromShader);
                enableCurvature.boolValue = enableCurvatureFromShader;
                if (EditorGUI.EndChangeCheck()) {
                    updateCurvatureMacro = true;
                    rebuildWorld = true;
                }
                if (enableCurvatureFromShader) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.indentLevel++;
                    curvatureAmount = EditorGUILayout.TextField(new GUIContent("Amount", "버텍스 이동량 배율입니다."), curvatureAmount);
                    if (GUILayout.Button("Update", GUILayout.Width(65))) {
                        updateCurvatureMacro = true;
                        rebuildWorld = true;
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndHorizontal();
                }
                GUI.enabled = true;

                prevBool = seeThrough.boolValue;
                EditorGUILayout.PropertyField(seeThrough, new GUIContent("See Through", "카메라와 대상 사이의 복셀을 숨깁니다. 이 옵션은 3인칭 시점을 위한 것입니다."));
                if (prevBool != seeThrough.boolValue) {
                    updateSpecialFeaturesMacro = true;
                }
                if (seeThrough.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(seeThroughTarget, new GUIContent("Target", "대상 게임오브젝트입니다. 보통 캐릭터 컨트롤러나 플레이어 게임오브젝트입니다."));
                    EditorGUILayout.PropertyField(seeThroughRadius, new GUIContent("Radius", "효과 반경입니다. 대상에서 이 거리 내의 복셀은 보이지 않습니다."));
                    EditorGUILayout.PropertyField(seeThroughHeightOffset, new GUIContent("Height Offset", "대상 아래 + 이 높이 오프셋 이하의 복셀은 숨기지 않습니다. 이 옵션은 지면이 숨겨지는 것을 방지합니다."));
                    EditorGUILayout.PropertyField(seeThroughAlpha, new GUIContent("Alpha", "시스루 모드가 투명일 때 가려진 복셀에 사용하는 알파 값입니다."));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(useOriginShift, new GUIContent("Origin Shift", "플레이어 위치가 임계값을 넘으면 원점으로 이동시킵니다. 이를 오리진 시프트라고 하며 부동소수점 문제를 피하는 데 필요합니다."));
                if (useOriginShift.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(originShiftDistanceThreshold, new GUIContent("Distance Threshold", "오리진 시프트가 발생하는 거리입니다."));
                    EditorGUI.indentLevel--;
                }
            }

            // Rendering
            if (GUILayout.Button(new GUIContent(" Rendering Options", Resources.Load("VoxelPlay/Inspector/renderingOptions") as Texture2D), leftAlignStyle)) {
                ToggleSection(ref expandRenderingSection);
            }
            if (expandRenderingSection) {

                EditorGUI.BeginChangeCheck();
                GUI.enabled = SystemInfo.supportsComputeShaders;
                GUIContent computeGUIContent = new GUIContent("Compute Buffers", "커스텀 복셀에 컴퓨트 버퍼를 활성화합니다. 이 옵션은 Shader Model 4.5를 지원하는 GPU가 필요하므로 실행 가능한 모바일 기기가 제한됩니다. 커스텀 복셀에서 일반 GPU 인스턴싱 대비 성능 이점은 플랫폼, 복셀 수 등에 따라 달라질 수 있습니다. 사용 전에 벤치마크를 수행하세요.");
                if (!GUI.enabled) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(computeGUIContent, GUILayout.Width(EditorGUIUtility.labelWidth));
                    EditorGUILayout.LabelField("(Unsupported platform or graphics API)");
                    EditorGUILayout.EndHorizontal();
                } else {
                    EditorGUILayout.PropertyField(useComputeBuffers, computeGUIContent);
                }
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }
                GUI.enabled = true;

                EditorGUILayout.PropertyField(instancingCullingMode, new GUIContent("Instancing Culling Mode", "Aggresive가 기본값으로 보이지 않는 복셀을 컬링합니다. Gentle은 보이지 않는 복셀의 그림자를 유지하기 위해 약간의 패딩을 허용합니다. Disabled는 카메라 위치와 상관없이 모든 복셀을 렌더링합니다."));
                if ((InstancingCullingMode)instancingCullingMode.intValue == InstancingCullingMode.Gentle) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(instancingCullingPadding);
                    EditorGUI.indentLevel--;
                }

                GUI.enabled = !Application.isPlaying;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(filterMode, new GUIContent("Texture Sampling", "텍스처 샘플링 필터 모드를 선택합니다."));
                if (EditorGUI.EndChangeCheck()) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                    updateSpecialFeaturesMacro = true;
                }

                GUI.enabled = true;
                if (filterMode.intValue == (int)FilterMode.Point) {
                    GUI.enabled = !enableReliefMapping.boolValue;
                    EditorGUILayout.PropertyField(hqFiltering, new GUIContent("HQ Point Filter", "밉맵과 통합된 텍셀 안티앨리어싱을 활성화합니다."));
                    if (prevBool != hqFiltering.boolValue) {
                        refreshChunks = true;
                        reloadWorldTextures = true;
                    }
                    if (hqFiltering.boolValue) {
                        EditorGUI.indentLevel++;
                        float prevFloat = mipMapBias.floatValue;
                        EditorGUILayout.PropertyField(mipMapBias, new GUIContent("MipMap Bias", "값을 높이면 텍스처 블러를 줄입니다."));
                        if (mipMapBias.floatValue != prevFloat) {
                            refreshChunks = true;
                            reloadWorldTextures = true;
                        }
                        EditorGUI.indentLevel--;
                    }
                    GUI.enabled = true;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(usePixelLights, new GUIContent("Per-Pixel Lighting", "비활성화하면 조명을 정점 단위로 계산합니다."));
                if (EditorGUI.EndChangeCheck()) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                    updateSpecialFeaturesMacro = true;
                }

                EditorGUILayout.LabelField("Gaps/White Pixels Removal Options");
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                voxelPadding = EditorGUILayout.Toggle(new GUIContent("Voxel Padding", "그리디 메시로 인해 인접 가장자리에서 생기는 틈(흰 픽셀)을 줄이기 위해 복셀을 약간 확대합니다."), voxelPadding);
                if (EditorGUI.EndChangeCheck()) {
                    ChangeVoxelPadding();
                }
                EditorGUILayout.PropertyField(usePostProcessing, new GUIContent("Post Processing", "화이트 픽셀을 감지/제거하기 위한 커스텀 포스트 프로세싱 효과를 사용합니다."));
                if (usePostProcessing.boolValue && !VoxelPlayPostProcessing.isActive) {
                    EditorGUILayout.HelpBox("추가 단계가 필요합니다:\nBuilt-in 파이프라인에서는 카메라에 Voxel Play Post Processing 스크립트를 추가해야 합니다.\nURP에서는 URP Universal Renderer에 Voxel Play Post Processing 렌더 피처를 추가하세요.", MessageType.Warning);
                }
                if (voxelPadding && usePostProcessing.boolValue) {
                    EditorGUILayout.HelpBox("화이트 픽셀을 제거하는 방법은 하나만 사용해야 합니다.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableFarChunksRendering, new GUIContent("Far Chunks Rendering", "원거리 청크 렌더링을 활성화합니다."));
                if (EditorGUI.EndChangeCheck()) {
                    serializedObject.ApplyModifiedProperties();
                    VoxelPlayFarChunksRenderer.Dispose();
                    if (enableFarChunksRendering.boolValue) {
                        VoxelPlayFarChunksRenderer.Init(env);
                    } else {
                        env.UpdateMaterialProperties();
                    }
                    GUIUtility.ExitGUI();
                    return;
                }
                if (enableFarChunksRendering.boolValue) {
                    EditorGUI.indentLevel++;
                    if (!Application.isPlaying) {
                        EditorGUILayout.HelpBox("원거리 청크 렌더링은 플레이 모드에서만 작동합니다.", MessageType.Info);
                    }
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(farChunksShadows, new GUIContent("Shadows", "원거리 청크 그림자를 활성화합니다."));
                    if (farChunksShadows.boolValue) {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(farChunksShadowIntensity, new GUIContent("Intensity", "원거리 청크 그림자 강도입니다."));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.PropertyField(farChunksWaterReflections, new GUIContent("Water Reflections", "원거리 청크의 물 반사를 활성화합니다."));
                    if (farChunksWaterReflections.boolValue) {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(farChunksWaterReflectionsIntensity, new GUIContent("Intensity", "원거리 청크의 물 반사 강도입니다."));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.PropertyField(farChunksWaterColorOverride, new GUIContent("Water Color Override", "원거리 청크의 물 색상을 덮어쓰도록 활성화합니다."));
                    if (farChunksWaterColorOverride.boolValue) {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(farChunksWaterColor, new GUIContent("Color", "원거리 청크 물 색상입니다."));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.PropertyField(farChunksShoreColor, new GUIContent("Shore Color", "원거리 청크 해안 색상입니다."));
                    if (EditorGUI.EndChangeCheck()) {
                        serializedObject.ApplyModifiedProperties();
                        VoxelPlayFarChunksRenderer.requireUpdateMaterialProperties = true;
                    }
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(farChunksDeepWater, new GUIContent("Deep Water", "원거리 청크에서 수면 아래를 추적합니다."));
                    if (env.world != null && env.world.terrainGenerator != null && env.world.terrainGenerator.maxHeight > 255) {
                        EditorGUILayout.HelpBox("지형 생성기의 최대 높이가 255보다 크게 설정되어 있습니다. 높이맵 캡처의 정밀도가 8비트에서 16비트로 증가하여 이 기능에 필요한 메모리가 두 배로 늘어나고 렌더링 성능에도 영향을 줄 수 있습니다.", MessageType.Info);
                    }
                    if (EditorGUI.EndChangeCheck()) {
                        serializedObject.ApplyModifiedProperties();
                        VoxelPlayFarChunksRenderer.Dispose();
                        VoxelPlayFarChunksRenderer.Init(env);
                        GUIUtility.ExitGUI();
                        return;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            // Sky Options
            if (GUILayout.Button(new GUIContent(" Sky Options", Resources.Load("VoxelPlay/Inspector/skySettings") as Texture2D), leftAlignStyle)) {
                ToggleSection(ref expandSkySection);
            }
            if (expandSkySection) {
                EditorGUILayout.PropertyField(sun, new GUIContent("Sun", "태양으로 사용할 방향성 라이트를 지정합니다."));
                EditorGUILayout.PropertyField(enableFogSkyBlending, new GUIContent("Enable Fog", "안개/하늘 블렌딩을 활성화합니다."));
                if (enableFogSkyBlending.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(fogAmount, new GUIContent("Fog Height", "안개의 양입니다."));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(fogDistanceAuto, new GUIContent("Auto Distance", "안개 거리를 카메라의 원거리 클리핑 또는 표시 청크 거리(더 낮은 값)에 맞춰 조정합니다."));
                    if (env.cameraMain != null) {
                        EditorGUILayout.LabelField("(Currently: " + env.GetFogAutoDistance() + ")");
                    }
                    EditorGUILayout.EndHorizontal();
                    if (!fogDistanceAuto.boolValue) {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(fogDistance, new GUIContent("Fog Distance", "안개 거리 계수입니다."));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.PropertyField(fogFallOff, new GUIContent("Fog Fall Off", "안개 감쇠 계수입니다."));
                    EditorGUILayout.PropertyField(fogTint, new GUIContent("Fog Tint", "안개의 틴트 색상입니다."));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(enableClouds, new GUIContent("Enable Clouds", "구름 생성 on/off"));
            }

            if (GUILayout.Button(new GUIContent(" Optional Game Features", Resources.Load("VoxelPlay/Inspector/optionalGameFeatures") as Texture2D), leftAlignStyle)) {
                ToggleSection(ref expandInGameSection);
            }
            if (expandInGameSection) {
                EditorGUILayout.PropertyField(loadSavedGame, new GUIContent("Load Saved Game At Start", "시작 시 저장된 게임을 불러올지 여부입니다. 'Save Filename' 필드에 저장 파일 이름을 지정하세요."));
                if (loadSavedGame.boolValue) {
                    EditorGUILayout.PropertyField(saveFilename, new GUIContent("   Filename", "현재 저장 게임 파일 이름입니다. 런타임에서 F3(불러오기), F4(저장)를 눌렀을 때 사용됩니다. 여러 저장 슬롯을 지원하려면 런타임에서 다른 파일명을 설정할 수 있습니다."));
                }
                EditorGUILayout.PropertyField(enableLoadingPanel, new GUIContent("Loading Screen", "시작 시 청크가 예약되는 동안 로딩 패널을 표시합니다."));
                if (enableLoadingPanel.boolValue) {
                    EditorGUILayout.PropertyField(loadingText, new GUIContent("   Text", "엔진 초기화 중 표시할 텍스트입니다."));
                }
                EditorGUILayout.PropertyField(initialWaitTime, new GUIContent("Initial Wait Time", "로딩 화면을 제거하기 전에 추가로 대기할 초입니다."));
                if (initialWaitTime.floatValue > 0) {
                    EditorGUILayout.PropertyField(initialWaitText, new GUIContent("   Text", "추가 대기 시간 동안 표시할 텍스트입니다."));
                }
                GUI.enabled = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS;
                EditorGUILayout.PropertyField(previewTouchUIinEditor, new GUIContent("Preview Mobile UI in Editor", "모바일 플랫폼을 타겟할 때 에디터에서 모바일 UI를 표시합니다."));
                GUI.enabled = true;
                EditorGUILayout.PropertyField(enableBuildMode, new GUIContent("Enable Build Mode", "B 키를 눌러 빌드 모드에 들어갈 수 있게 합니다. 빌드 모드에서는 모든 월드 아이템이 인벤토리에 무제한으로 제공되며, 무엇이든 한 번에 파괴할 수 있습니다. 플레이어는 또한 무적이 됩니다."));
                if (enableBuildMode.boolValue) {
                    EditorGUILayout.PropertyField(buildMode, new GUIContent("   Build Mode ON", "빌드 모드를 활성화합니다."));
                }
                EditorGUILayout.PropertyField(enableConsole, new GUIContent("Enable Console", "콘솔 시스템을 활성화합니다. F1을 누르면 표시됩니다."));
                if (enableConsole.boolValue) {
                    EditorGUILayout.PropertyField(showConsole, new GUIContent("   Visible", "콘솔 표시/숨김을 전환합니다. 콘솔은 디버깅에 유용한 데이터를 표시합니다."));
                    EditorGUILayout.PropertyField(consoleBackgroundColor, new GUIContent("   Background Color"));
                }
                EditorGUILayout.PropertyField(enableStatusBar, new GUIContent("Enable Status Bar"));
                if (enableStatusBar.boolValue) {
                    EditorGUILayout.PropertyField(statusBarBackgroundColor, new GUIContent("   Status Bar Color"));
                }
                EditorGUILayout.PropertyField(enableInventory, new GUIContent("Enable Inventory", "Tab 키를 누르면 인벤토리 UI를 표시합니다. 자체 인터페이스를 사용하려면 비활성화하세요."));
                EditorGUILayout.PropertyField(enableDebugWindow, new GUIContent("Enable Debug Window", "F2로 디버그 창 토글을 활성화합니다."));
                EditorGUILayout.PropertyField(showFPS, new GUIContent("Show FPS", "화면 오른쪽 상단에 FPS를 표시합니다."));
            }

            if (GUILayout.Button(new GUIContent(" Default Assets", Resources.Load("VoxelPlay/Inspector/defaultAssets") as Texture2D), leftAlignStyle)) {
                ToggleSection(ref expandDefaultsSection);
            }
            if (expandDefaultsSection) {
                EditorGUILayout.PropertyField(defaultBuildSound, new GUIContent("Build Sound", "아이템이나 복셀이 씬에 배치될 때 재생되는 기본 사운드입니다."));
                EditorGUILayout.PropertyField(defaultPickupSound, new GUIContent("Pick Up Sound", "아이템을 수집할 때 재생되는 기본 사운드입니다."));
                EditorGUILayout.PropertyField(defaultImpactSound, new GUIContent("Impact Sound", "복셀을 때릴 때 재생되는 기본 사운드입니다."));
                EditorGUILayout.PropertyField(defaultDestructionSound, new GUIContent("Destruction Sound", "복셀이 파괴될 때 재생되는 기본 사운드입니다."));
                EditorGUILayout.PropertyField(defaultVoxel, new GUIContent("Default Voxel", "복셀 정의가 없거나 위치에 직접 색상을 배치할 때 가정하는 기본 복셀입니다."));
                EditorGUILayout.PropertyField(defaultWaterVoxel, new GUIContent("Default Water Voxel", "지형 생성기가 지정하지 않을 때 사용할 기본 물 복셀입니다."));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(inputControllerPC, new GUIContent("Input Prefab (PC)", "PC 입력 컨트롤러 스크립트가 포함된 프리팹입니다."));
                if (GUILayout.Button("Load Default", GUILayout.Width(120))) {
                    inputControllerPC.objectReferenceValue = Resources.Load<GameObject>("VoxelPlay/InputControllers/PC/Voxel Play PC Input Controller");
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(inputControllerMobile, new GUIContent("Input Prefab (Mobile)", "모바일 입력 컨트롤러 스크립트가 포함된 프리팹입니다."));
                if (GUILayout.Button("Load Default", GUILayout.Width(120))) {
                    inputControllerMobile.objectReferenceValue = Resources.Load<GameObject>("VoxelPlay/InputControllers/Mobile/Voxel Play Mobile Input Controller");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(uiCanvasPrefab, new GUIContent("UI Prefab", "게임 메인 인터페이스에 사용하는 캔버스 프리팹입니다. 인벤토리, 선택 아이템, 크로스헤어 등 정보가 포함됩니다."));
                if (GUILayout.Button("Load Default", GUILayout.Width(120))) {
                    uiCanvasPrefab.objectReferenceValue = Resources.Load<GameObject>("VoxelPlay/UI/Voxel Play UI Canvas");
                }
                EditorGUILayout.EndHorizontal();

                if (uiCanvasPrefab.objectReferenceValue != null) {
                    EditorGUILayout.PropertyField(welcomeMessage, new GUIContent("Welcome Text", "게임 시작 시 표시되는 선택 메시지입니다."));
                    EditorGUILayout.PropertyField(welcomeMessageDuration, new GUIContent("Welcome Duration", "환영 텍스트 표시 시간입니다."));
                }

                EditorGUILayout.PropertyField(crosshairPrefab, new GUIContent("Crosshair Prefab", "크로스헤어에 사용하는 프리팹입니다."));
                EditorGUILayout.PropertyField(crosshairTexture, new GUIContent("Crosshair Texture", "크로스헤어에 사용하는 텍스처입니다."));
            }

            // Advanced section
            if (GUILayout.Button(new GUIContent(" Advanced", Resources.Load("VoxelPlay/Inspector/advancedSettings") as Texture2D), leftAlignStyle)) {
                ToggleSection(ref expandAdvancedSection);
            }
            if (expandAdvancedSection) {
                EditorGUILayout.PropertyField(debugLevel);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serverMode, new GUIContent("Server Mode", "서버 모드에서는 복셀을 렌더링하지 않아 메모리 사용을 줄이고 무인 서버에서의 성능을 개선합니다."));
                if (EditorGUI.EndChangeCheck() && serverMode.boolValue) {
                    lowMemoryMode.boolValue = true;
                }
                EditorGUILayout.PropertyField(lowMemoryMode, new GUIContent("Low Memory Mode", "활성화하면 시작 시 내부 렌더링 버퍼를 미리 할당하지 않습니다. 필요할 때만 메모리를 할당합니다. 모바일 기기나 저메모리 전용 서버에서 메모리 압박 경고를 줄이려면 이 옵션을 사용하세요. 버퍼 크기 조정 시 메모리 할당 스파이크가 발생할 수 있습니다."));
                EditorGUILayout.PropertyField(delayedInitialization, new GUIContent("Delayed Initialization", "활성화하면 Init() 메서드를 호출할 때까지 Voxel Play가 초기화되지 않습니다."));
                EditorGUILayout.BeginHorizontal();
                maxMaterialsPerChunk = EditorGUILayout.IntField(new GUIContent("Max Materials Per Chunk", "한 청크에서 사용할 수 있는 서로 다른 머티리얼 수입니다. 메모리 사용 및 성능을 개선하려면 이 값을 낮게 유지하세요."), maxMaterialsPerChunk);
                GUI.enabled = maxMaterialsPerChunk != VoxelPlayEnvironment.MAX_MATERIALS_PER_CHUNK;
                if (GUILayout.Button("Change", GUILayout.Width(80))) {
                    ChangeMaxMaterialsPerChunk();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            // Stats
            if (GUILayout.Button(new GUIContent(" Stats", Resources.Load("VoxelPlay/Inspector/Stats") as Texture2D), leftAlignStyle)) {
                ToggleSection(ref expandStatsSection);
            }
            if (expandStatsSection) {
                ShowProgressBar("Chunk Rendering: Pending (" + env.chunksInRenderQueueCount + ") / Drawn (" + env.chunksDrawn + ")", (env.chunksDrawn + 1f) / (env.chunksDrawn + env.chunksInRenderQueueCount + 1f));
                if (env.enableTrees) {
                    ShowProgressBar("Tree Creation: Pending (" + env.treesInCreationQueueCount + ") / Created (" + env.treesCreated + ")", (env.treesCreated + 1f) / (env.treesCreated + env.treesInCreationQueueCount + 1f));
                } else {
                    ShowProgressBar("Tree Creation: ---", 1f);
                }
                if (env.enableVegetation) {
                    ShowProgressBar("Bush Creation: Pending (" + env.vegetationInCreationQueueCount + ") / Created (" + env.vegetationCreated + ")", (env.vegetationCreated + 1f) / (env.vegetationCreated + env.vegetationInCreationQueueCount + 1f));
                } else {
                    ShowProgressBar("Bush Creation: ---", 1f);
                }
                EditorGUILayout.LabelField(new GUIContent("Total Chunks Created", "청크 내용이 생성될 때 증가합니다. 이는 청크가 처음 생성되거나 재사용되면서 내용이 교체될 때 발생합니다."), new GUIContent(env.chunksCreated.ToString()));
                EditorGUILayout.LabelField("Chunks Pool Usage", env.chunksUsed + " of " + maxChunks.intValue + " (" + (env.chunksUsed * 100f / env.maxChunks).ToString("F1") + "%)");
                EditorGUILayout.LabelField(new GUIContent("Total Voxels Created", "메쉬 생성에 기여하는 복셀 수입니다. 완전히 둘러싸인 복셀은 숨겨져 포함되지 않습니다."), new GUIContent(env.voxelsCreatedCount.ToString()));
            }

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Biome Map Explorer")) {
                VoxelPlayBiomeExplorer.ShowWindow();
            }
            if (GUILayout.Button("Import Models...")) {
                VoxelPlayImportTools.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            bool undoPerformed = Event.current != null && "UndoRedoPerformed".Equals(Event.current.commandName);
            if (serializedObject.ApplyModifiedProperties() || rebuildWorld || undoPerformed) {
                if (undoPerformed) {
                    VoxelPlayFarChunksRenderer.requireUpdateMaterialProperties = true;
                }
                if (updateSpecialFeaturesMacro) {
                    Debug.Log("Optimization: modifying scripts/shaders macros to reflect special feature change...");
                    env.UpdateSpecialFeaturesCodeMacro();
                    GUIUtility.ExitGUI();
                    return;
                }
                if (updateCurvatureMacro) {
                    UpdateCurvatureMacro();
                    GUIUtility.ExitGUI();
                    return;
                }
                if (env.gameObject.activeInHierarchy) {
                    if (Application.isPlaying || env.renderInEditor) {
                        if (rebuildWorld) {
                            rebuildWorld = false;
                            env.ReloadWorld();

                            // Check if scene camera is under terrain
                            if (!Application.isPlaying && env.renderInEditor && SceneView.lastActiveSceneView != null) {
                                Camera cam = SceneView.lastActiveSceneView.camera;
                                if (cam != null) {
                                    Vector3 camPos = SceneView.lastActiveSceneView.pivot;
                                    float h = env.GetTerrainHeight(camPos, true);
                                    if (camPos.y < h + 2) {
                                        camPos.y = h + 2;
                                    } else if (camPos.y > h + 100) {
                                        camPos.y = h + 50f;
                                    }
                                    SceneView.lastActiveSceneView.LookAt(camPos);
                                }
                            }
                        } else if (refreshChunks) {
                            refreshChunks = false;
                            env.Redraw(reloadWorldTextures);
                        }
                        env.UpdateMaterialProperties();
                    }
                }

                EditorApplication.update -= env.UpdateInEditor;
                if (renderInEditor.boolValue) {
                    EditorApplication.update += env.UpdateInEditor;
                }
            }
        }

        void ShowHelpButtons (bool showHideButton) {
            if (showHideButton) {
                if (GUILayout.Button("New Tip", GUILayout.Width(90))) {
                    cookieIndex++;
                }
                if (GUILayout.Button("Hide Help Section", GUILayout.Width(130))) {
                    cookieIndex = -1;
                    GUIUtility.ExitGUI();
                }
            } else if (GUILayout.Button("Help & Tutorials", GUILayout.Width(130))) {
                cookieIndex++;
            }
        }

        void FocusSceneView () {
            if (SceneView.sceneViews != null && SceneView.sceneViews.Count > 0) {
                SceneView sv = SceneView.sceneViews[0] as SceneView;
                if (sv != null) {
                    sv.Focus();
                }
            }
        }

        void ShowProgressBar (string text, float progress) {
            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.ProgressBar(r, progress, text);
            GUILayout.Space(18);
            EditorGUILayout.EndVertical();
        }


        void CreateWorldDefinition () {
            WorldDefinition wd = ScriptableObject.CreateInstance<WorldDefinition>();
            wd.name = "New World Definition";
            AssetDatabase.CreateAsset(wd, "Assets/" + wd.name + ".asset");
            AssetDatabase.SaveAssets();
            world.objectReferenceValue = wd;
            EditorGUIUtility.PingObject(wd);
        }


        string GetShaderOptionValue (string option, string file) {
            string[] res = Directory.GetFiles(Application.dataPath, file, SearchOption.AllDirectories);
            string path = null;
            for (int k = 0; k < res.Length; k++) {
                if (res[k].Contains("Voxel Play")) {
                    path = res[k];
                    break;
                }
            }
            if (path == null) {
                Debug.LogError(file + " could not be found!");
                return "";
            }

            string[] code = File.ReadAllLines(path, System.Text.Encoding.UTF8);
            string searchToken = "#define " + option;
            for (int k = 0; k < code.Length; k++) {
                if (code[k].Contains(searchToken)) {
                    string[] values = code[k].Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length == 3) {
                        return values[2];
                    }
                    break;
                }
            }
            return "";
        }

        void SetShaderOptionValue (string option, string file, string value) {
            string[] res = Directory.GetFiles(Application.dataPath, file, SearchOption.AllDirectories);
            string path = null;
            for (int k = 0; k < res.Length; k++) {
                if (res[k].Contains("Voxel Play")) {
                    path = res[k];
                    break;
                }
            }
            if (path == null) {
                Debug.LogError(file + " could not be found!");
                return;
            }

            string[] code = File.ReadAllLines(path, System.Text.Encoding.UTF8);
            string searchToken = "#define " + option;
            for (int k = 0; k < code.Length; k++) {
                if (code[k].Contains(searchToken)) {
                    code[k] = "#define " + option + " " + value;
                    File.WriteAllLines(path, code, System.Text.Encoding.UTF8);
                    break;
                }
            }
        }

        public void UpdateCurvatureMacro () {
            env.SetShaderOptionValue("VOXELPLAY_CURVATURE", "VPCommonVertexModifier.cginc", enableCurvature.boolValue ? "1" : "0");
            env.SetShaderOptionValue("VOXELPLAY_CURVATURE_AMOUNT", "VPCommonVertexModifier.cginc", curvatureAmount);
            Debug.Log("Voxel Play shaders updated.");
            AssetDatabase.Refresh();
        }

        void CheckMainLightShadows () {
            Light[] lights = Misc.FindObjectsOfType<Light>();
            for (int k = 0; k < lights.Length; k++) {
                if (lights[k].isActiveAndEnabled && lights[k].shadows != LightShadows.None) {
                    EditorGUILayout.HelpBox("현재 광원 '" + lights[k].name + "'이(가) 그림자를 투사하도록 설정되어 있습니다. 성능 향상을 위해 해당 광원의 그림자도 비활성화하는 것을 고려하세요.", MessageType.Info);
                }
            }
        }

        void ChangeChunkSize () {
            if (!EditorUtility.DisplayDialog("Change Chunk Size", "Please note that saved games with different chunk sizes cannot be loaded.\nThe view distance and chunk pool size will be adjusted to reflect the new chunk size.\n\nDo you want to change the chunk size? (it won't modify any saved game).", "Yes", "No")) {
                return;
            }
            int newVisibleDistance = visibleChunksDistance.intValue * VoxelPlayEnvironment.CHUNK_SIZE / chunkNewSize;
            newVisibleDistance = Mathf.Clamp(newVisibleDistance, 1, 25);
            visibleChunksDistance.intValue = newVisibleDistance;
            maxChunks.intValue = env.maxChunksRecommended;
            serializedObject.ApplyModifiedProperties();
            env.UpdateChunkSizeInCode(chunkNewSize);
            Debug.Log("New chunk size updated.");
            AssetDatabase.Refresh();
            GUIUtility.ExitGUI();
        }


        void ChangeMicroVoxelsSize () {
            if (!EditorUtility.DisplayDialog("Change Micro Voxels Size", "Please note that saved games with different microvoxels sizes cannot be loaded.\nDo you want to change the microvoxels size?", "Yes", "No")) {
                return;
            }
            env.UpdateMicroVoxelsSizeInCode(microVoxelsNewSize);
            Debug.Log("New microvoxels size updated.");
            AssetDatabase.Refresh();
            GUIUtility.ExitGUI();
        }


        void ChangeMaxMaterialsPerChunk () {
            if (maxMaterialsPerChunk < 16) {
                EditorUtility.DisplayDialog("Change Max Materials Per Chunk", "Minimum material count is 16.", "Ok");
                return;
            }
            if (!EditorUtility.DisplayDialog("Change Max Materials Per Chunk", "Please note that increasing the number of materials per chunk increases memory usage and can affect performance..\n\nDo you want to change the maximum material count per chunk?", "Yes", "No")) {
                return;
            }
            env.UpdateMaxMaterialsPerChunk(maxMaterialsPerChunk);
            Debug.Log("Max Materials Per Chunk updated.");
            AssetDatabase.Refresh();
            GUIUtility.ExitGUI();
        }



        void ChangeVoxelPadding () {
            env.UpdateVoxelPadding(voxelPadding);
            Debug.Log("Voxel Padding updated.");
            AssetDatabase.Refresh();
            GUIUtility.ExitGUI();
        }


        void GenerateEditorArea () {
            if (!EditorUtility.DisplayDialog("Generate Chunks In Area", "Warning: chunks in area of size " + renderInEditorAreaSize.vector3Value + " with center at " + renderInEditorAreaCenter.vector3Value + " will be generated now.\n\nConfirm?", "Yes", "Cancel")) return;

            Vector3 sizeInChunks = renderInEditorAreaSize.vector3Value / VoxelPlayEnvironment.CHUNK_SIZE;
            int totalChunks = (int)(sizeInChunks.x * sizeInChunks.y * sizeInChunks.z);
            if (totalChunks > env.maxChunks) {
                EditorUtility.DisplayDialog("Max Chunks Exceeded!", "Total chunks to be generated (" + totalChunks + ") exceeds current pool size. Increase pool size or reduce size of area to be generated.", "Ok");
                return;
            }

            env.ReloadWorld(keepWorldChanges: false);
            env.ChunkCheckArea(renderInEditorAreaCenter.vector3Value, sizeInChunks / 2f, renderChunks: true);
            env.CompleteWork();
        }

#if USES_URP

        #region SRP utils

        void CheckDepthPrimingMode () {
            RenderPipelineAsset pipe = GraphicsSettings.currentRenderPipeline;
            if (pipe == null) return;
            // Check depth priming mode
            FieldInfo renderers = pipe.GetType().GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (renderers == null) return;
            foreach (var renderer in (object[])renderers.GetValue(pipe)) {
                if (renderer == null) continue;
                FieldInfo depthPrimingModeField = renderer.GetType().GetField("m_DepthPrimingMode", BindingFlags.NonPublic | BindingFlags.Instance);
                int depthPrimingMode = -1;
                if (depthPrimingModeField != null) {
                    depthPrimingMode = (int)depthPrimingModeField.GetValue(renderer);
                }

                FieldInfo renderingModeField = renderer.GetType().GetField("m_RenderingMode", BindingFlags.NonPublic | BindingFlags.Instance);
                int renderingMode = -1;
                if (renderingModeField != null) {
                    renderingMode = (int)renderingModeField.GetValue(renderer);
                }

                if (depthPrimingMode > 0 && renderingMode != 1) {
                    EditorGUILayout.HelpBox("URP 자산의 Depth Priming Mode를 비활성화해야 합니다.", MessageType.Warning);
                    if (GUILayout.Button("Show Pipeline Asset")) {
                        Selection.activeObject = (UnityEngine.Object)renderer;
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.Separator();
                }
            }
        }
        #endregion

#endif


    }

}
