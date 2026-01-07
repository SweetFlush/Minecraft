// Voxel Play 
// Created by Ramiro Oliva (Kronnect)
// Voxel Play Behaviour - attach this script to any moving object that should receive voxel global illumination

using UnityEngine;
using System.Collections.Generic;

namespace VoxelPlay {

    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001858-voxel-play-behaviour")]
    public class VoxelPlayBehaviour : MonoBehaviour {

        [Tooltip("복셀 라이트 정보로 업데이트된 이 객체의 재질 색상이나 강도를 유지합니다.")]
        public bool enableVoxelLight = true;

        [Tooltip("표준 재료를 Voxel Play 최적화 재료로 자동 교체합니다.")]
        public bool useVoxelPlayMaterials;

        [Tooltip("개체가 복셀에 떨어지거나 교차하여 다른 복셀에 들어가지 않도록 합니다.")]
        public bool forceUnstuck = true;

        [Tooltip("풀려날 때 이 게임오브젝트에 적용되는 지면 위의 수직 이동")]
        public float unstuckOffsetY;

        [Tooltip("근처에 청크가 있는지 확인하고 존재하지 않는 경우 생성해야 합니다.이 옵션은 물체가 떨어지는 것을 방지하기 위해 필요합니다.")]
        public bool checkNearChunks = true;

        [Tooltip("확인할 개체 주변의 범위")]
        public Vector3 chunkExtents;

        [Tooltip("이 개체 주위에 생성된 청크도 렌더링해야 하는 경우")]
        public bool renderChunks = true;

        [Tooltip("원점 이동이 발생할 때 이 게임오브젝트가 이동되는 경우")]
        public bool useOriginShift = true;

        VoxelPlayEnvironment env;
        int lastX, lastY, lastZ;
        int lastChunkX, lastChunkY, lastChunkZ;
        Vector3d lastPosition;
        bool requireUpdateLighting;

        static readonly List<Renderer> rr = new List<Renderer>();

        struct MaterialData {
            public Material mat;
            public Color normalMatColor;
            public bool useMaterialColor;
        }

        struct RendererData {
            public MaterialData[] materials;
        }

        RendererData[] rd;
        Rigidbody rb;
        static readonly Dictionary<Material, Material> upgradedMaterials = new Dictionary<Material, Material>();

        static class ShaderParams {
            public static int VoxelLight = Shader.PropertyToID("_VoxelLight");
            public static int Color = Shader.PropertyToID("_Color");
            public static int BumpMap = Shader.PropertyToID("_BumpMap");
        }

        void Start() {
            env = VoxelPlayEnvironment.instance;
            if (env == null) {
                DestroyImmediate(this);
                return;
            }
            env.OnChunkRender += ChunkRender;
            lastPosition = transform.position;
            lastX = int.MaxValue;
            rb = GetComponent<Rigidbody>();

            if (useVoxelPlayMaterials) {
                Shader vpShaderOpaque = Shader.Find("Voxel Play/Models/Texture/Opaque");
                Shader vpShaderTransp = Shader.Find("Voxel Play/Models/Texture/Alpha");
                Shader vpShaderOpaqueBumpMap = Shader.Find("Voxel Play/Models/Texture/Opaque BumpMap");
                if (vpShaderOpaque == null || vpShaderTransp == null) {
                    Debug.LogError("Could not find Voxel Play/Models/Texture/Opaque shader.");
                } else {
                    Renderer[] rr = GetComponentsInChildren<Renderer>();
                    for (int k = 0; k < rr.Length; k++) {
                        Material[] mats = rr[k].sharedMaterials;
                        for (int m = 0; m < mats.Length; m++) {
                            Material mat = mats[m];
                            if (mat != null && !mat.shader.name.Contains("Voxel Play")) {
                                if (!upgradedMaterials.TryGetValue(mat, out Material upgradedMaterial)) {
                                    upgradedMaterial = Instantiate(mat);
                                    if (mat.renderQueue >= 3000 && mat.HasProperty(ShaderParams.Color) && mat.color.a < 1f) {
                                        upgradedMaterial.shader = vpShaderTransp;
                                    } else if (mat.HasProperty(ShaderParams.BumpMap)) {
                                        upgradedMaterial.shader = vpShaderOpaqueBumpMap;
                                    } else {
                                        upgradedMaterial.shader = vpShaderOpaque;
                                    }
                                    upgradedMaterials[mat] = upgradedMaterial;
                                }
                                mats[m] = upgradedMaterial;
                            }
                        }
                        rr[k].sharedMaterials = mats;
                    }
                }
            }

            if (enableVoxelLight) {
                FetchMaterials();
            }

            if (useOriginShift) {
                env.RegisterOriginShiftTransform(transform.root);
            }

            if (forceUnstuck) {
                CheckStuck();
            }

            if (checkNearChunks) {
                CheckNearChunks(lastPosition);
            }

        }

        void FetchMaterials() {
            GetComponentsInChildren(true, rr);
            int count = rr.Count;
            rd = new RendererData[count];
            for (int k = 0; k < count; k++) {
                Renderer mr = rr[k];
                if (mr.sharedMaterials == null) continue;
                Material[] mats = mr.sharedMaterials;
                int matsLength = mats.Length;
                rd[k].materials = new MaterialData[matsLength];
                for (int j = 0; j < matsLength; j++) {
                    Material mat = mats[j];
                    if (mat == null) continue;
                    rd[k].materials[j].useMaterialColor = !mat.shader.name.Contains("Voxel Play/Models");
                    mat = Instantiate(mat);
                    mr.sharedMaterial = mat;
                    rd[k].materials[j].normalMatColor = mat.HasProperty(ShaderParams.Color) ? mat.color : Misc.colorWhite;
                    mat.DisableKeyword(VoxelPlayEnvironment.SKW_VOXELPLAY_GPU_INSTANCING);
                    rd[k].materials[j].mat = mat;
                }
            }
            requireUpdateLighting = true;
        }

        void OnDestroy() {
            if (env == null) return;

            env.OnChunkRender -= ChunkRender;

        }

        void ChunkRender(VoxelChunk chunk) {
            if (FastVector.SqrMinDistanceXZ((Vector3)chunk.position, transform.position) < 32 * 32) {
                requireUpdateLighting = true;
            }
        }

        public void Refresh() {
            lastX = int.MaxValue;
            lastChunkX = int.MaxValue;
        }

        void LateUpdate() {

            if (!env.initialized)
                return;

            // Check if position has changed since previous
            Vector3d position = transform.position;
            FastMath.FloorToInt(position.x, position.y, position.z, out int x, out int y, out int z);

            if (lastX != x || lastY != y || lastZ != z) {
                requireUpdateLighting = true;

                lastPosition = position;
                lastX = x;
                lastY = y;
                lastZ = z;

                if (forceUnstuck) {
                    CheckStuck();
                }

                if (checkNearChunks) {
                    CheckNearChunks(position);
                }
            }
            if (requireUpdateLighting) {
                requireUpdateLighting = false;
                UpdateLightingNow();
            }
        }


        void CheckStuck() {
            Vector3 pos = transform.position;
            pos.y += unstuckOffsetY + 0.1f;
            if (env.CheckCollision(pos)) {
                float deltaY = FastMath.FloorToInt(pos.y) + 1.01f - pos.y;
                pos.y += deltaY;
                if (rb != null) {
                    rb.position = pos;
		    if (!rb.isKinematic) {
	                    rb.velocity = Misc.vector3zero;
			}
                } else {
                    transform.position = pos;
                }
                lastX--;
            }
        }

        void CheckNearChunks(Vector3d position) {
            int chunkX, chunkY, chunkZ;
            FastMath.FloorToInt(position.x / VoxelPlayEnvironment.CHUNK_SIZE, position.y / VoxelPlayEnvironment.CHUNK_SIZE, position.z / VoxelPlayEnvironment.CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);
            if (lastChunkX != chunkX || lastChunkY != chunkY || lastChunkZ != chunkZ) {
                lastChunkX = chunkX;
                lastChunkY = chunkY;
                lastChunkZ = chunkZ;
                // Ensure area is rendered
                env.ChunkCheckArea(position, chunkExtents, renderChunks);
            }
        }


        public void UpdateLighting() {
            requireUpdateLighting = true;
        }

        void UpdateLightingNow() {
            if (!enableVoxelLight) return;
            if (rd == null || rd.Length == 0) {
                FetchMaterials();
            }
            Vector3d pos = lastPosition;
            // center of voxel
            pos.x += 0.5f;
            pos.y += 0.5f;
            pos.z += 0.5f;
            float light = -1;
            int packedLight = -1;

            int rdLength = rd.Length;
            for (int k = 0; k < rdLength; k++) {
                int matsLength = rd[k].materials.Length;
                for (int j = 0; j < matsLength; j++) {
                    Material mat = rd[k].materials[j].mat;
                    if (mat == null) continue;

                    if (rd[k].materials[j].useMaterialColor) {
                        if (light < 0) {
                            light = env.GetVoxelLight(pos);
                        }
                        Color normalMatColor = rd[k].materials[j].normalMatColor;
                        Color newColor = new Color(normalMatColor.r * light, normalMatColor.g * light, normalMatColor.b * light, normalMatColor.a);
                        mat.color = newColor;
                    } else {
                        if (packedLight < 0) {
                            packedLight = env.GetVoxelLightPacked(pos);
                        }
                        mat.SetInt(ShaderParams.VoxelLight, packedLight);
                    }
                }
            }
        }

    }
}