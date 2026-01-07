using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;

namespace VoxelPlay {

    public class WorldEditorToolBuild : WorldEditorTool {

        public override Texture2D icon => Resources.Load<Texture2D>("VoxelPlay/WorldEditorIcons/toolBuild");
        public override string instructions => "Add/grow voxels. Hold shift to remove.";
        public override int priority => 50;
        public override int minOpaque => 0;
        public override bool supportsMicroVoxels => true;
        public override WorldEditorToolCategory category => WorldEditorToolCategory.SculptTool;
        public override bool canIgnoreWater => true;

        static class ShaderParams {
            public static int MicroVoxelsSize = Shader.PropertyToID("_MicroVoxelsSize");
        }


        Editor meshPreviewEditor;
        GameObject voxelPreview;

        public override void Dispose () {
            if (meshPreviewEditor != null) {
                UnityEngine.Object.DestroyImmediate(meshPreviewEditor);
            }
            DestroyVoxelPreview();
        }

        public override void SwitchTool () {
            DestroyVoxelPreview();
        }

        void DestroyVoxelPreview () {
            if (voxelPreview != null) {
                UnityEngine.Object.DestroyImmediate(voxelPreview);
            }
        }


        public override void DrawInspector () {
            env.sceneEditorBuildAutoSelectVoxel = EditorGUILayout.Toggle("Repeat Existing Voxel", env.sceneEditorBuildAutoSelectVoxel);
            if (!env.sceneEditorBuildAutoSelectVoxel) {
                DestroyVoxelPreview();
                EditorGUI.BeginChangeCheck();
                env.sceneEditorBuildVoxel = (VoxelDefinition)EditorGUILayout.ObjectField("Voxel Definition", env.sceneEditorBuildVoxel, typeof(VoxelDefinition), false);
                if (EditorGUI.EndChangeCheck()) {
                    if (env.sceneEditorBuildVoxel != null && env.sceneEditorBuildVoxel.usesMicroVoxels) {
                        env.sceneEditorBrushMicroVoxelSize = 0;
                    }
                }
                if (env.sceneEditorBrushMicroVoxelSize == 0 && env.sceneEditorBuildVoxel != null) {
                    if (env.sceneEditorBuildVoxel.usesMicroVoxels) {
                        Mesh mesh = env.sceneEditorBuildVoxel.GetMicroVoxelsPreviewMesh();
                        if (mesh != null) {
                            if (meshPreviewEditor == null) {
                                meshPreviewEditor = Editor.CreateEditor(mesh);
                            }
                            meshPreviewEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(64, 64), new GUIStyle());
                        }
                    }
                }
            }
            env.sceneEditorBrushMicroVoxelSize = EditorGUILayout.IntSlider("MicroVoxel Size", env.sceneEditorBrushMicroVoxelSize, 0, MicroVoxels.COUNT_PER_AXIS);
            if (env.sceneEditorBrushMicroVoxelSize == 0) {
                DestroyVoxelPreview();
                env.sceneEditorBrushSize = EditorGUILayout.IntSlider("Brush Size", env.sceneEditorBrushSize, 1, 32);
                EditorGUILayout.BeginHorizontal();
                env.sceneEditorPlacementRotation = EditorGUILayout.IntSlider("Rotation", env.sceneEditorPlacementRotation, 0, 3);
                GUILayout.Label(env.sceneEditorPlacementRotation * 90 + "°");
                EditorGUILayout.EndHorizontal();
                env.sceneEditorBuildIgnoreWater = EditorGUILayout.Toggle("Ignore Water", env.sceneEditorBuildIgnoreWater);
            } else {
            }
            env.sceneEditorBrushContinuousMode = EditorGUILayout.Toggle(new GUIContent("Continuous Mode", "Hold left button mouse to operate"), env.sceneEditorBrushContinuousMode);
            if (env.sceneEditorBrushContinuousMode) {
                EditorGUI.indentLevel++;
                env.sceneEditorBrushSpeed = EditorGUILayout.Slider("Speed", env.sceneEditorBrushSpeed, 0, 1);
                env.sceneEditorBuildMaxLength = EditorGUILayout.IntSlider("Max Length", env.sceneEditorBuildMaxLength, 0, 20);
                EditorGUI.indentLevel--;
            }
        }

        public override void Update () {
            if (keyR) {
                env.sceneEditorPlacementRotation = (env.sceneEditorPlacementRotation + 1) % 4;
            }
        }

        public override void DrawGizmos (VoxelHitInfo hitInfo, List<VoxelIndex> voxelIndices) {
            if (!isMouseDown) {
                if (shift) {
                    if (supportsMicroVoxels && env.sceneEditorBrushMicroVoxelSize > 0 && hitInfo.voxel.type.supportsMicroVoxels) {
                        Boundsd bounds = env.GetHighlightBounds(ref hitInfo, env.sceneEditorBrushMicroVoxelSize);
                        float size = (float)bounds.size.x;
                        DrawArrow(bounds.center + hitInfo.normal * size * 1.5f, -hitInfo.normal, size);
                    } else if (hitInfo.voxel.isSolid) {
                        DrawArrow(hitInfo.center + hitInfo.normal * 0.8f, -hitInfo.normal, 0.3f);
                    }
                } else {
                    if (supportsMicroVoxels && env.sceneEditorBrushMicroVoxelSize > 0 && hitInfo.voxel.type.supportsMicroVoxels) {
                        Boundsd bounds = env.GetHighlightBounds(ref hitInfo, env.sceneEditorBrushMicroVoxelSize);
                        float size = (float)bounds.size.x;
                        DrawArrow(bounds.center + hitInfo.normal * size * 0.5f, hitInfo.normal, size);
                    } else if (hitInfo.voxel.isSolid) {
                        DrawArrow(hitInfo.center + hitInfo.normal * 0.5f, hitInfo.normal, 0.3f);
                    }
                }
            }

            if (env.sceneEditorBrushMicroVoxelSize > 0) {
                // draw cube around voxel containing the micro voxels
                var originalZTest = Handles.zTest;
                Handles.zTest = CompareFunction.LessEqual;
                Handles.color = new Color(1, 1, 1, 0.35f);
                Vector3d center = hitInfo.point;
                if (!env.IsMicroVoxelAtPosition(center)) {
                    center -= hitInfo.normal * 0.5f;
                }
                FastVector.Middling(ref center);
                float size = 1.01f;
                Handles.DrawWireCube(center, new Vector3(size, size, size));
                Handles.zTest = originalZTest;
            } else if (env.sceneEditorBuildVoxel != null) {
                // Show voxel preview with microvoxels
                if (env.sceneEditorBuildVoxel.usesMicroVoxels) {
                    if (hitInfo.voxel.isSolid) {
                        voxelPreview = MicroVoxelsHighlight(env.sceneEditorBuildVoxel, hitInfo.voxelCenter + hitInfo.normal);
                    } else {
                        voxelPreview = MicroVoxelsHighlight(env.sceneEditorBuildVoxel, hitInfo.voxelCenter);
                    }
                    voxelPreview.transform.localRotation = Quaternion.Euler(0, env.sceneEditorPlacementRotation * 90, 0);
                }
            }
        }




        /// <summary>
        /// 주어진 위치에서 마이크로복셀을 사용하여 복셀 정의의 임시 홀로그램을 표시합니다.월드 에디터 도구에서 사용됩니다.
        /// </summary>
        /// <returns>하이라이트.</returns>
        /// <param name="vd">복셀 정의.</param>
        /// <param name="position">위치.</param>
        GameObject MicroVoxelsHighlight (VoxelDefinition vd, Vector3d position) {
            if (vd == null) return null;

            if (vd.microVoxelsPreviewGO == null) {
                vd.microVoxelsPreviewGO = Resources.Load<GameObject>("VoxelPlay/Prefabs/MicroVoxelsPreview");
                if (vd.microVoxelsPreviewGO == null) return null;
                vd.microVoxelsPreviewGO = UnityEngine.Object.Instantiate(vd.microVoxelsPreviewGO);
                vd.microVoxelsPreviewGO.name = "MicroVoxelsPreview";
            }
            GameObject previewGO = vd.microVoxelsPreviewGO;
            if (previewGO.TryGetComponent(out MeshFilter meshFilter)) {
                meshFilter.mesh = vd.GetMicroVoxelsPreviewMesh();
            }
            if (previewGO.TryGetComponent(out MeshRenderer meshRenderer)) {
                meshRenderer.sharedMaterial.SetFloat(ShaderParams.MicroVoxelsSize, MicroVoxels.COUNT_PER_AXIS);
            }
            previewGO.transform.SetParent(env.worldRoot, false);
            previewGO.transform.localPosition = position.vector3;
            previewGO.SetActive(true);

            return previewGO;
        }


        public override bool RayCast (Ray ray, out VoxelHitInfo hitInfo) {
            bool res = base.RayCast(ray, out hitInfo);
            // in continous mode, make it eaiser to build rows of voxels over the same surface by comparing the new normal with the start hit normal
            if (env.sceneEditorBrushContinuousMode && isMouseDown && executionCount > 0 && hitInfo.normal != startHitInfo.normal && !hitInfo.voxel.type.isVegetation) {
                hitInfo = startHitInfo;
                return false;
            }
            return res;
        }

        bool IsWithinDistance (Vector3d pos, float maxDistance) {
            Vector3d startCenter = startHitInfo.voxelCenter;
            Vector3d diff = pos - startCenter;
            diff.x *= startHitInfo.normal.x;
            diff.y *= startHitInfo.normal.y;
            diff.z *= startHitInfo.normal.z;
            double dist = Math.Abs(diff.x) + Math.Abs(diff.y) + Math.Abs(diff.z);
            return dist < maxDistance;
        }

        public override void SelectVoxels (ref VoxelHitInfo hitInfo, int brushSize, List<VoxelIndex> voxelIndices) {

            voxelIndices.Clear();

            if (env.sceneEditorBrushMicroVoxelSize > 0) return;

            Vector3d center = hitInfo.voxelCenter;

            List<VoxelIndex> tempVoxels = BufferPool<VoxelIndex>.Get();
            Vector3d boxMin = center - new Vector3d(brushSize / 2, brushSize / 2, brushSize / 2);
            Vector3d boxMax = center + new Vector3d((brushSize - 1) / 2, (brushSize - 1) / 2, (brushSize - 1) / 2);
            env.GetVoxelIndices(boxMin, boxMax, tempVoxels);
            int count = tempVoxels.Count;
            for (int k = 0; k < count; k++) {
                VoxelIndex vi = tempVoxels[k];
                Vector3d pos = env.GetVoxelPosition(vi.chunk, vi.voxelIndex);

                if (isMouseDown) {
                    if (!IsWithinDistance(pos, env.sceneEditorBuildMaxLength)) {
                        continue;
                    }
                }

                pos += hitInfo.normal;
                if (!env.IsSolidAtPosition(pos)) {
                    voxelIndices.Add(vi);
                }
            }
            BufferPool<VoxelIndex>.Release(tempVoxels);
        }

        public override void HighlightVoxels (ref VoxelHitInfo hitInfo, List<VoxelIndex> voxelIndices, Color color, float edgeWidth, float fadeAmplitude) {
            VoxelHitInfo highlightHitInfo = hitInfo;
            if (env.sceneEditorBrushContinuousMode && isMouseDown) {
                highlightHitInfo = startHitInfo;
            }
            if (env.sceneEditorBrushMicroVoxelSize == 0 || voxelIndices.Count > 1) {
                base.HighlightVoxels(ref highlightHitInfo, voxelIndices, color, edgeWidth, fadeAmplitude);
                return;
            }
            env.VoxelHighlight(ref highlightHitInfo, color, edgeWidth, env.sceneEditorBrushMicroVoxelSize, fadeAmplitude);
        }

        protected override bool Execute (ref VoxelHitInfo hitInfo, int brushSize, float brushStrength, List<VoxelIndex> indices) {

            List<VoxelChunk> modifiedChunks = BufferPool<VoxelChunk>.Get();
            int modifiedCount;

            if (hitInfo.voxel.type.isVegetation) {
                undoManager.SaveChunk(hitInfo.chunk);
                hitInfo.chunk.ClearVoxel(hitInfo.voxelIndex, VoxelPlayEnvironment.FULL_LIGHT);
                modifiedChunks.Add(hitInfo.chunk);
                modifiedCount = modifiedChunks.Count;
                VoxelPlayEnvironmentEditor.currentEditingEnv.RefreshSelection();
            } else {

                if (env.sceneEditorBrushMicroVoxelSize > 0) {
                    if (shift) {
                        undoManager.SaveChunk(hitInfo.chunk);
                        if (!hitInfo.voxel.type.supportsMicroVoxels) {
                            hitInfo.chunk.ClearVoxel(hitInfo.voxelIndex, VoxelPlayEnvironment.FULL_LIGHT);
                            modifiedChunks.Add(hitInfo.chunk);
                        } else {
                            if (env.MicroVoxelDestroy(ref hitInfo, env.sceneEditorBrushMicroVoxelSize)) {
                                modifiedChunks.Add(hitInfo.chunk);
                            }
                        }
                    } else {
                        undoManager.SaveChunk(hitInfo.chunk);
                        VoxelDefinition vd = env.sceneEditorBuildAutoSelectVoxel || env.sceneEditorBuildVoxel == null ? hitInfo.voxel.type : env.sceneEditorBuildVoxel;
                        if (env.MicroVoxelPlace(ref hitInfo, env.sceneEditorBrushMicroVoxelSize, vd, probability: 1f, hitInfo.voxel.color, hitInfo.voxel.GetTextureRotation())) {
                            modifiedChunks.Add(hitInfo.chunk);
                        }
                    }
                    modifiedCount = modifiedChunks.Count;
                    float size = MicroVoxels.SIZE;
                    if (env.microVoxelsSnap) {
                        size *= env.sceneEditorBrushMicroVoxelSize;
                    }
                    if (modifiedCount > 0 && IsWithinDistance(hitInfo.voxelCenter, (env.sceneEditorBuildMaxLength - 1) * size)) {
                        if (shift) {
                            hitInfo.voxelCenter -= startHitInfo.normal * size;
                        } else {
                            hitInfo.voxelCenter += startHitInfo.normal * size;
                        }
                    }

                } else {
                    int count = indices.Count;
                    VoxelDefinition vdPlaced = null;
                    for (int k = 0; k < count; k++) {
                        VoxelIndex vi = indices[k];
                        Vector3d pos = env.GetVoxelPosition(vi.chunk, vi.voxelIndex);
                        if (shift) {
                            undoManager.SaveChunk(vi.chunk);
                            vi.chunk.ClearVoxel(vi.voxelIndex, VoxelPlayEnvironment.FULL_LIGHT);
                            modifiedChunks.Add(vi.chunk);
                        } else {
                            pos += startHitInfo.normal;
                            if (env.GetVoxelIndex(pos, out VoxelChunk otherChunk, out int otherIndex, createChunkIfNotExists: true)) {
                                VoxelDefinition vd = env.sceneEditorBuildAutoSelectVoxel || env.sceneEditorBuildVoxel == null ? vi.chunk.voxels[vi.voxelIndex].type : env.sceneEditorBuildVoxel;
                                if (vd != null) {
                                    if (vd.index <= 0) {
                                        env.AddVoxelDefinition(vd);
                                        env.world.AddVoxelDefinition(vd);
                                        EditorUtility.SetDirty(env.world);
                                    }
                                    undoManager.SaveChunk(otherChunk);

                                    vdPlaced = vd;
                                    otherChunk.SetVoxel(otherIndex, vd);

                                    otherChunk.voxels[otherIndex].SetTextureRotation(env.sceneEditorPlacementRotation);

                                    // Set microvoxels
                                    if (vd.usesMicroVoxels) {
                                        otherChunk.SetMicroVoxels(otherIndex, vd.microVoxels.Clone());
                                        byte newOpaque = vd.microVoxels.GetOpaqueProportional();
                                        if (otherChunk.voxels[otherIndex].opaque != newOpaque) {
                                            otherChunk.voxels[otherIndex].opaque = newOpaque;
                                            otherChunk.voxelSignature = -1;
                                        }
                                    }
                                    modifiedChunks.Add(otherChunk);
                                }
                            }
                        }
                    }
                    modifiedCount = modifiedChunks.Count;
                    if (modifiedCount > 0 && (vdPlaced == null || !vdPlaced.usesMicroVoxels)) {
                        if (shift) {
                            hitInfo.voxelCenter -= startHitInfo.normal;
                        } else {
                            hitInfo.voxelCenter += startHitInfo.normal;
                        }
                    }
                }
            }

            RefreshModifiedChunks(modifiedChunks);

            BufferPool<VoxelChunk>.Release(modifiedChunks);

            return modifiedCount > 0;

        }

    }

}