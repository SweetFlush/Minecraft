using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;


namespace VoxelPlay {

    public partial class VoxelPlayEnvironmentEditor : Editor {

        readonly List<VoxelIndex> voxelIndices = new List<VoxelIndex>();
        VoxelHitInfo lastHighlightInfo;
        static bool isMouseDown;
        int sceneEditorHighlightedLastFrame, sceneEditorExecutionLastFrame;
        readonly List<Type> toolTypes = new List<Type>();
        static Type selectedToolType, lastToolType;
        Vector2 lastMousePosition;

        readonly Dictionary<Type, WorldEditorTool> toolInstances = new Dictionary<Type, WorldEditorTool>();
        readonly HashSet<int> terrainVoxels = new HashSet<int>();

        VoxelPlayTerrainGenerator currentTerrainGenerator;

        UndoManager undoManager;

        public static VoxelPlayEnvironmentEditor currentEditingEnv;
        bool pendingChanges;

        void WorldEditorInit () {

            if (undoManager == null) {
                undoManager = CreateInstance<UndoManager>();
            }
            undoManager.env = env;
            Undo.undoRedoPerformed += PerformUndo;

            isMouseDown = false;
            LoadTools();
            if (env.sceneEditorBrushShape == null) {
                env.sceneEditorBrushShape = Resources.Load("VoxelPlay/Brushes/Brush2") as Texture2D;
            }

            // figure out which voxel definitions can be considered as part of terrain
            if (env.initialized) {
                LoadTerrainVoxelDefinitions();
            } else {
                env.OnInitialized += LoadTerrainVoxelDefinitions;
            }

            if (!Application.isPlaying) {
                FocusSceneView();
            }

            currentEditingEnv = this;
            SceneView.duringSceneGui += OnScene;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        void WorldEditorDispose () {
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            SceneView.duringSceneGui -= OnScene;
            currentEditingEnv = null;

            foreach (var tool in toolInstances.Values) {
                if (tool != null) {
                    tool.Dispose();
                }
            }
            if (undoManager != null) {
                Undo.undoRedoPerformed -= PerformUndo;
                DestroyImmediate(undoManager);
            }
        }

        void OnSceneSaving (Scene scene, string path) {
            if (env == null || !env.runInEditMode) return;
            if (pendingChanges) {
                SaveWorldInEditor();
            }
        }

        void PerformUndo () {
            if (undoManager != null) {
                undoManager.PerformUndo();
            }
            lastMousePosition = Vector2.zero;
            pendingChanges = true;
        }

        void LoadTerrainVoxelDefinitions () {
            if (env.world == null) return;
            currentTerrainGenerator = env.world.terrainGenerator;
            if (env.world.terrainGenerator == null) return;

            List<VoxelDefinition> vds = new List<VoxelDefinition>();
            env.world.terrainGenerator.GetTerrainVoxelDefinitions(vds);
            foreach (VoxelDefinition vd in vds) {
                if (vd != null && !terrainVoxels.Contains(vd.index)) terrainVoxels.Add(vd.index);
            }
        }

        void LoadTools () {
            toolInstances.Clear();

            var types = TypeCache.GetTypesDerivedFrom<WorldEditorTool>();
            foreach (var type in types) {
                if (!type.IsAbstract) {
                    var instance = Activator.CreateInstance(type) as WorldEditorTool;
                    toolInstances[type] = instance;
                }
            }

            // Crear una lista temporal de las instancias
            var sortedInstances = new List<WorldEditorTool>(toolInstances.Values);

            // Ordenar las instancias por prioridad
            sortedInstances.Sort((t1, t2) => t1.priority.CompareTo(t2.priority));

            // Limpiar y rellenar toolTypes con el orden correcto
            toolTypes.Clear();
            foreach (var instance in sortedInstances) {
                toolTypes.Add(instance.GetType());
            }

        }

        void DrawTools (WorldEditorToolCategory category) {

            const float iconSize = 40;
            GUIStyle activeStyle = new GUIStyle(GUI.skin.button);
            GUIStyle disabledStyle = new GUIStyle(GUI.skin.button);
            disabledStyle.normal.background = Texture2D.blackTexture;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();

            var active = selectedToolType == null;
            if (GUILayout.Toggle(active, "X", active ? activeStyle : disabledStyle, GUILayout.Width(iconSize), GUILayout.Height(iconSize))) {
                selectedToolType = null;
            }

            int toolsPerRow = (int)((EditorGUIUtility.currentViewWidth - 24) / (iconSize + 4));

            int count = toolTypes.Count;
            int drawn = 1;
            for (int k = 0; k < count; k++) {
                if (drawn % toolsPerRow == 0) {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                var type = toolTypes[k];
                var tool = toolInstances[type];
                if (tool.category != category) continue;
                var content = new GUIContent(tool.icon, tool.instructions);
                active = selectedToolType == type;
                if (GUILayout.Toggle(active, content, active ? activeStyle : disabledStyle, GUILayout.Width(iconSize), GUILayout.Height(iconSize))) {
                    selectedToolType = type;
                }
                drawn++;
            }
            EditorGUILayout.EndHorizontal();

            if (selectedToolType != null) {
                var tool = toolInstances[selectedToolType];
                GUILayout.Label(toolInstances[selectedToolType].instructions, EditorStyles.boldLabel);
                tool.DrawInspector();
            }

            if (selectedToolType != lastToolType && lastToolType != null) {
                toolInstances[lastToolType].SwitchTool();
            }
            lastToolType = selectedToolType;

            if (EditorGUI.EndChangeCheck()) {
                EditorWindow.FocusWindowIfItsOpen(typeof(SceneView));
            }
        }

        public static void OnScene (SceneView sceneView) {
            currentEditingEnv?.OnSceneEditor();
        }

        void OnSceneEditor () {

            if (!expandSceneEditor || !renderInEditor.boolValue) return;
            if (env == null || !env.initialized || env.world == null) return;
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            bool shift = (e.modifiers & EventModifiers.Shift) != 0;
            bool control = (e.modifiers & EventModifiers.Control) != 0;
            bool keyR = e.type == EventType.KeyDown && e.keyCode == KeyCode.R;
            if (keyR) {
                e.Use();
            }

            if (selectedToolType == null) return;
            var tool = toolInstances[selectedToolType];
            tool.SetControlKeys(shift, control, keyR, isMouseDown);

            if (EditorWindow.mouseOverWindow != SceneView.currentDrawingSceneView) {
                if (isMouseDown) {
                    tool.EndExecution();
                    undoManager.EndChangeGroup();
                    isMouseDown = false;
                    env.VoxelHighlight(false);
                }
                return;
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive)); // prevents default selection

            if (terrainVoxels.Count == 0 || env.world.terrainGenerator != currentTerrainGenerator) {
                LoadTerrainVoxelDefinitions();
            }
            tool.SetTerrainVoxelDefinitions(terrainVoxels);
            tool.SetUndoManager(undoManager);

            if (e.isMouse && e.button == 0) {
                if (e.type == EventType.MouseDown) {
                    undoManager.StartChangeGroup();
                    tool.StartExecution(lastHighlightInfo);
                    isMouseDown = true;
                } else if (e.type == EventType.MouseDrag && isMouseDown) {

                } else if (e.type == EventType.MouseUp) {
                    tool.EndExecution();
                    undoManager.EndChangeGroup();
                    isMouseDown = false;
                }
            }

            tool.Update();

            float labelOffset = tool.supportsMicroVoxels && env.sceneEditorBrushMicroVoxelSize > 0 ? 0.1f : 0.5f;
            Handles.Label(lastHighlightInfo.point + new Vector3(labelOffset, labelOffset, labelOffset), lastHighlightInfo.point.ToString("F2") + " Rot: " + lastHighlightInfo.voxel.GetTextureRotationDegrees() + " " + lastHighlightInfo.voxel.type.name);
            tool.DrawGizmos(lastHighlightInfo, voxelIndices);

            // Highlight voxels
            int thisFrame = Time.frameCount;
            if (thisFrame != sceneEditorHighlightedLastFrame) {
                sceneEditorHighlightedLastFrame = thisFrame;

                if (e.mousePosition != lastMousePosition) {
                    lastMousePosition = e.mousePosition;
                    Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

                    if (!tool.RayCast(ray, out VoxelHitInfo hitInfo)) {
                        env.VoxelHighlight(false);
                        return;
                    }

                    if (hitInfo.point == lastHighlightInfo.point) return;
                    lastHighlightInfo = hitInfo;
                }

                tool.SetMask(env.sceneEditorBrushShape);
                tool.SelectVoxels(ref lastHighlightInfo, env.sceneEditorBrushSize, voxelIndices);

                Color highlightColor = Color.cyan;
                highlightColor.a = 0.45f;
                tool.HighlightVoxels(ref lastHighlightInfo, voxelIndices, highlightColor, edgeWidth: 2f, fadeAmplitude: 0);
            }

            // Execute tool
            if (thisFrame == sceneEditorExecutionLastFrame) return;
            sceneEditorExecutionLastFrame = thisFrame;

            if (isMouseDown) {
                if (tool.ExecuteTool(ref lastHighlightInfo, env.sceneEditorBrushSize, env.sceneEditorBrushStrength, voxelIndices)) {
                    pendingChanges = true;
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    lastHighlightInfo.point = Vector3d.zero;
                }
            }
        }

        public static void UnselectWorldEditorTool () {
            selectedToolType = null;
            isMouseDown = false;
        }

        public static void SelectWorldEditorTool (Type toolType) {
            selectedToolType = toolType;
            isMouseDown = false;
        }

        public void RefreshSelection () {
            lastHighlightInfo.point.y = lastHighlightInfo.voxelCenter.y = lastMousePosition.y = 9999999;
        }

        void SaveWorldInEditor () {
            if (env.SaveGameBinary(makeBackup: env.sceneEditorAutomaticBackup)) {
                Debug.Log("World saved to " + env.saveFilename);
                AssetDatabase.Refresh();
                pendingChanges = false;
            }
        }

    }

}
