using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxelPlay {

    public enum WorldEditorToolCategory {
        TerrainTool,
        SculptTool
    }

    public abstract class WorldEditorTool {

        public abstract WorldEditorToolCategory category { get; }
        public abstract Texture2D icon { get; }
        public abstract string instructions { get; }
        public abstract int priority { get; }
        public virtual int minOpaque => VoxelPlayEnvironment.FULL_OPAQUE;
        public virtual bool supportsContinuousMode => true;
        public virtual bool supportsMicroVoxels => false;
        public virtual bool canIgnoreWater => false;
        public abstract void SelectVoxels (ref VoxelHitInfo hitInfo, int radius, List<VoxelIndex> voxelIndices);
        protected abstract bool Execute (ref VoxelHitInfo hitInfo, int brushSize, float brushStrength, List<VoxelIndex> indices);

        protected VoxelPlayEnvironment env;
        protected Color32[] mask;
        protected int maskWidth, maskHeight;
        protected VoxelHitInfo startHitInfo;
        protected float startTime;
        protected HashSet<int> terrainVoxelDefinitions;
        protected float lastExecutionTime;
        protected int executionCount;
        protected bool shift, control, keyR, isMouseDown;
        protected UndoManager undoManager;

        Texture2D currentMask;
        protected const float ROUNDNESS = 0.2f;

        public WorldEditorTool () {
            env = VoxelPlayEnvironment.instance;
        }

        public virtual void DrawInspector () {
            env.sceneEditorBrushSize = EditorGUILayout.IntSlider("Brush Size", env.sceneEditorBrushSize, 1, 32);
            EditorGUI.BeginChangeCheck();
            env.sceneEditorBrushShape = (Texture2D)EditorGUILayout.ObjectField("Brush Shape", env.sceneEditorBrushShape, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck()) {
                TextureTools.EnsureTextureReadable(env.sceneEditorBrushShape);
            }
            env.sceneEditorBrushStrength = EditorGUILayout.Slider("Shape Blend Strength", env.sceneEditorBrushStrength, 0.001f, 1f);
            if (supportsContinuousMode) {
                env.sceneEditorBrushContinuousMode = EditorGUILayout.Toggle(new GUIContent("Continuous Mode", "Hold left button mouse to operate"), env.sceneEditorBrushContinuousMode);
                if (env.sceneEditorBrushContinuousMode) {
                    EditorGUI.indentLevel++;
                    env.sceneEditorBrushSpeed = EditorGUILayout.Slider("Speed", env.sceneEditorBrushSpeed, 0, 1);
                    EditorGUI.indentLevel--;
                }
            }
        }        

        public void SetTerrainVoxelDefinitions (HashSet<int> terrainVoxelDefinitions) {
            this.terrainVoxelDefinitions = terrainVoxelDefinitions;
        }

        public void SetUndoManager (UndoManager undoManager) {
            this.undoManager = undoManager;
        }

        public void SetControlKeys (bool shift, bool control, bool keyR, bool isMouseDown) {
            this.shift = shift;
            this.control = control;
            this.keyR = keyR;
            this.isMouseDown = isMouseDown;
        }

        public virtual void DrawGizmos (VoxelHitInfo hitInfo, List<VoxelIndex> voxelIndices) {
        }

        public virtual void Update () {
        }

        protected void DrawArrow (Vector3 start, Vector3 direction, float arrowLength) {
            if (direction.sqrMagnitude == 0) return;
            Vector3 end = start + direction * arrowLength;
            Handles.DrawLine(start, end);
            Handles.ConeHandleCap(0, end, Quaternion.LookRotation(direction), arrowLength * 0.3f, EventType.Repaint);
        }

        public void SetMask (Texture2D maskTexture) {
            if (currentMask == maskTexture) return;
            currentMask = maskTexture;

            if (maskTexture == null) {
                maskWidth = maskHeight = 0;
                return;
            }
            mask = maskTexture.GetPixels32();
            maskWidth = maskTexture.width;
            maskHeight = maskTexture.height;
        }

        public virtual bool RayCast (Ray ray, out VoxelHitInfo hitInfo) {
            if (supportsMicroVoxels && env.sceneEditorBrushMicroVoxelSize > 0) {
                return env.RayCast(ray, out hitInfo, microVoxels: true, ignoreWater: canIgnoreWater);
            }
            return env.RayCast(ray, out hitInfo, minOpaque: minOpaque, ignoreWater: env.sceneEditorBuildIgnoreWater && canIgnoreWater);
        }

        public virtual void HighlightVoxels (ref VoxelHitInfo hitInfo, List<VoxelIndex> voxelIndices, Color color, float edgeWidth, float fadeAmplitude) {
            env.VoxelHighlight(voxelIndices, color, edgeWidth, fadeAmplitude);
        }

        /// <summary>
        /// 사용자가 다른 도구를 선택할 때 트리거됨
        /// </summary>
        public virtual void SwitchTool () { }

        /// <summary>
        /// 인스펙터가 완전히 파괴되면 트리거됩니다.
        /// </summary>
        public virtual void Dispose () { }

        public virtual void StartExecution (VoxelHitInfo hitInfo) {
            startHitInfo = hitInfo;
            startTime = Time.time;
            lastExecutionTime = 0;
            executionCount = 0;
        }

        public virtual void EndExecution () {
        }


        public bool ExecuteTool (ref VoxelHitInfo hitInfo, int brushSize, float brushStrength, List<VoxelIndex> indices) {

            float now = Time.time;
            if (supportsContinuousMode && env.sceneEditorBrushContinuousMode) {
                if (1f - env.sceneEditorBrushSpeed > (now - lastExecutionTime)) return false;
                lastExecutionTime = now;
                if (executionCount == 0) {
                    lastExecutionTime += 0.1f; // small delay in first click
                }
            } else {
                if (executionCount > 0) return false;
            }
            executionCount++;

            bool enableDetailGenerators = env.enableDetailGenerators; // disable detail generators during brush execution
            env.enableDetailGenerators = false;
            bool result = Execute(ref hitInfo, brushSize, brushStrength, indices);
            env.enableDetailGenerators = enableDetailGenerators;

            return result;
        }

        protected float ComputeMaskFactor (Vector3d center, Vector3d pos, float brushSize) {
            if (maskWidth == 0) return 1f;

            if (brushSize < 1) brushSize = 1f;

            float dx = (float)(pos.x - center.x) / brushSize;
            float dz = (float)(pos.z - center.z) / brushSize;
            dx = dx * 0.5f + 0.5f;
            dz = dz * 0.5f + 0.5f;

            int tw = (int)(dx * maskWidth);
            int th = (int)(dz * maskHeight);

            if (tw < 0) tw = 0; else if (tw >= maskWidth) tw = maskWidth - 1;
            if (th < 0) th = 0; else if (th >= maskHeight) th = maskHeight - 1;

            return mask[th * maskWidth + tw].a / 255f;
        }

        /// <summary>
        /// 주어진 크기의 스탬프 내부 좌표에서 마스크 계수를 계산합니다.
        /// </summary>
        protected float ComputeMaskFactor (int pz, int px, int size) {
            if (maskWidth == 0) return 1f;
            pz = (int)((pz + 0.5f) * maskHeight / size);
            px = (int)((px + 0.5f) * maskWidth / size);
            return mask[pz * maskWidth + px].a / 255f;
        }

        protected void UpdateChunkElevation (VoxelChunk chunk) {
            if (chunk.terrainInfo != null) return;
            env.GetHeightMapInfoFast(chunk.position.x, chunk.position.z, out chunk.terrainInfo, out _);
        }


        protected void RefreshModifiedChunks (List<VoxelChunk> modifiedChunks) {
            VoxelChunk lastChunk = null;
            foreach (var chunk in modifiedChunks) {
                if (lastChunk == chunk) continue;
                env.ChunkRedraw(chunk, includeNeighbours: true, refreshLightmap: true, refreshMesh: true);
                lastChunk = chunk;
            }
        }
    }
}