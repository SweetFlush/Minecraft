using System;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;


namespace VoxelPlay {

    public enum ChunkRenderState : byte {
        Pending,
        RenderingRequested,
        RenderingComplete
    }

    public enum ChunkVisibleDistanceStatus : byte {
        Unknown = 0,
        WithinVisibleDistance = 1,
        OutOfVisibleDistance = 2
    }

    public partial class VoxelChunk : MonoBehaviour {

#if UNITY_EDITOR
        [Tooltip("SceneView에서 청크를 표시하고 디버깅 목적으로 이 값을 사용할 수 있습니다.")]
        public bool debug;
        [Tooltip("SceneView에서 청크를 선택할 때 유용합니다.디버깅 목적으로 이 값을 사용하십시오.")]
        public int debugVoxelIndex;
#endif

        /// <summary>
        /// 풀에 있는 이 청크의 인덱스
        /// </summary>
        [NonSerialized]
        public int poolIndex;

        /// <summary>
        /// 복셀 정의
        /// </summary>
        [NonSerialized]
        public Voxel[] voxels;

        /// <summary>
        /// 메시 또는 사용자 정의 유형에 기여하는 이 청크의 복셀 수
        /// </summary>
        [NonSerialized]
        public int totalVisibleVoxelsCount;

        /// <summary>
        /// 로컬 공간의 청크 중심 위치.16x16x16 청크는 위치 8에서 시작하여 위치 +8에서 끝납니다.
        /// </summary>
        [NonSerialized] public Vector3d position;

        /// <summary>
        /// 청크가 절두체에 보이는 경우.이 값은 내부 최적화 목적으로 저장되며 현재 상태를 반영할 수 없습니다. 청크가 카메라 절두체 내에 있는지 알고 싶다면 대신 ChunkIsInFrustum()을 호출하세요.
        /// </summary>
        [NonSerialized] public bool visibleInFrustum;

        [NonSerialized] public int frustumCheckIteration;

        [NonSerialized] public int voxelSignature;

        [NonSerialized] public MeshFilter mf;

        [NonSerialized] public MeshRenderer mr;

        [NonSerialized] public MeshCollider mc;

        [NonSerialized] public bool allowTrees = true;

        [NonSerialized] public int navMeshSourceIndex = -1;

        /// <summary>
        /// 청크 navmesh 업데이트를 요청한 경우
        /// </summary>
        [NonSerialized] public float navMeshUpdateRequestTime;

        [NonSerialized] public Mesh navMesh;

        /// <summary>
        /// 이 청크가 현재 가시 거리 내에 있는지 여부
        /// </summary>
        [NonSerialized] public ChunkVisibleDistanceStatus visibleDistanceStatus;

        /// <summary>
        /// 이 청크가 위에서 햇빛에 닿는지 여부를 지정하는 플래그
        /// </summary>
		[NonSerialized] public bool isAboveSurface = true;

        /// <summary>
        /// 청크 메시가 새로 고쳐질 때 다시 빌드되어야 함을 지정하는 플래그
        /// </summary>
        [NonSerialized] public bool needsLightmapRebuild;

        /// <summary>
        /// 청크 메시가 새로 고쳐질 때 다시 빌드되어야 함을 지정하는 플래그
        /// </summary>
        [NonSerialized] public bool needsMeshRebuild;

        /// <summary>
        /// 렌더링할 청크가 절두체를 무시하도록 지정하는 플래그(즉, 멀리 있는 AI가 요구하는 청크일 수 있음)
        /// </summary>
        [NonSerialized] public bool ignoreFrustum;

        /// <summary>
        /// 청크가 복셀로 채워지거나 채워진 경우.아직 렌더링되지 않았을 수 있습니다.
        /// </summary>
        [NonSerialized] public bool isPopulated;

        /// <summary>
        /// 청크가 렌더링 보류 중입니다(대기열에 있음).
        /// </summary>
        [NonSerialized] public bool inqueue;

        /// <summary>
        /// 이 청크를 재사용할 수 있거나 그대로 유지해야 하는 특수 청크인 경우
        /// </summary>
        /// <value><c>진실</c> if can be reused; otherwise, <c>거짓</c>.</value>
        [NonSerialized] public bool cannotBeReused;

        /// <summary>
        /// 이 청크가 클라우드 렌더링에 사용되는 경우.
        /// </summary>
        [NonSerialized] public bool isCloud;

        /// <summary>
        /// 루프에서 동일한 청크를 여러 번 수정하는 경우 수정된 청크 목록에 이 청크를 두 번 이상 추가하지 않는 카운터입니다.
        /// </summary>
        [NonSerialized]
        public int modifiedTag;

        /// <summary>
        /// 청크가 게임에서 수정되었습니다.
        /// </summary>
        public bool modified;

        /// <summary>
        /// 세션 단계와 비교할 수정의 타임스탬프
        /// </summary>
        public int modifiedTimestamp;

        /// <summary>
        /// 청크 수정 사항을 표시해야 하는지 여부를 제어하는 ​​세션 플래그입니다.비활성화하면 Chunk.modified가 설정되지 않으므로 게임을 저장할 때 해당 수정 사항이 고려되지 않습니다.
        /// 이는 게임을 로드하는 동안 API를 사용하여 청크를 수정하거나 코드 자체에서 수정이 이루어지고 세션 간에 재현이 가능한 세계를 절차적으로 생성할 때 유용합니다.
        /// </summary>
        public static bool markModifiedChunks = true;

        /// <summary>
        /// 이 청크가 알림 대기열에 추가된 프레임 수(OnChunkChanged 알림을 프레임당 한 번만 보내는 것을 방지하는 데 도움이 됨)
        /// </summary>
        public int modifiedFrameCount;

        /// <summary>
        /// ModifiedCount 값이 지정된 값보다 작은 경우 true를 반환한 다음 Modified 및 mofieidCount 값을 업데이트합니다.
        /// </summary>
        /// <returns></returns>
        public bool SetModified (int tag) {
            if (markModifiedChunks) {
                modified = true;
                modifiedTimestamp = VoxelPlayEnvironment.stage;
            }
            if (modifiedTag != tag) {
                modifiedTag = tag;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 청크가 한 번 이상 렌더링된 경우 true를 반환합니다(표시되는 내용이 없을 수 있음).
        /// </summary>
        [NonSerialized] public ChunkRenderState renderState = ChunkRenderState.Pending;

        /// <summary>
        /// 청크가 렌더링되어 GPU에 업로드되었나요?
        /// </summary>
        public bool isRendered { get { return renderState == ChunkRenderState.RenderingComplete; } }


        /// <summary>
        /// 청크에 충돌체(비어 있지 않음)가 있으면 true를 반환합니다.
        /// </summary>
        public bool hasColliderMesh { get { return (object)mc != null && mc.sharedMesh != null; } }


        /// <summary>
        /// 더티 청크는 알 수 없는 라이트맵 상태를 가질 수 있고 지형 생성기가 이를 사용하기 전에 다시 지워야 하는 ChunkGetUnpopulated로 얻은 청크입니다.
        /// </summary>
        [NonSerialized]
        public bool isDirty;

        /// <summary>
        /// 이 청크가 렌더링되는 프레임 번호입니다.최적화에 사용됩니다.
        /// </summary>
        [NonSerialized]
        public int renderingFrame;

        /// <summary>
        /// 이 청크의 광원(예: 횃불)
        /// </summary>
        [NonSerialized]
        public List<LightSource> lightSources;

        /// <summary>
        /// 이 청크의 복셀 자리 표시자입니다.자리 표시자는 특정 복셀에 추가 시각적 또는 상호 작용을 제공하는 데 사용됩니다(예: 손상 균열, 물리학 등).
        /// </summary>
        [NonSerialized]
        public FastHashSet<VoxelPlaceholder> placeholders;

        /// <summary>
        /// 이 청크에 아이템이 스폰됩니다.
        /// </summary>
        [NonSerialized]
        public FastList<Item> items;

        /// <summary>
        /// 일부 복셀에 대한 추가 선택 데이터입니다.내부적으로 사용됩니다.
        /// </summary>
        [NonSerialized]
        public FastHashSet<VoxelHiddenData> voxelsExtraData;

        /// <summary>
        /// 추가 사용자 정의 복셀 속성.
        /// </summary>
        [NonSerialized]
        public FastHashSet<FastHashSet<VoxelProperty>> voxelsProperties;

        /// <summary>
        /// 이웃 청크에 대한 링크
        /// </summary>
        [NonSerialized]
        public VoxelChunk top, bottom, left, right, forward, back;

        /// <summary>
        /// 특정 알고리즘을 가속화하는 데 사용됩니다.
        /// </summary>
        [NonSerialized]
        public int tempFlag;


        [NonSerialized]
        public bool lightmapIsClear;


        /// <summary>
        /// voxelSignature 필드를 재설정하여 충돌체를 강제로 다시 빌드합니다.이는 충돌체(즉, SetDynamic)에만 영향을 미치는 복셀 유형을 변경할 때 사용됩니다.
        /// </summary>
        public void SetNeedsColliderRebuild () {
            voxelSignature = -1;
        }

        /// <summary>
		/// 이 청크의 라이트맵을 지우거나 값으로 초기화합니다.
		/// </summary>
		public void ClearLightmap (byte value = 0) {
            if (lightmapIsClear && voxels[VoxelPlayEnvironment.CHUNK_VOXEL_COUNT - 1].light == value)
                return;
            for (int k = 0; k < voxels.Length; k++) {
                voxels[k].light = value;
                voxels[k].torchLight = 0;
            }
            lightmapIsClear = true;
            needsLightmapRebuild = true;
        }

        /// <summary>
        /// 이 청크에 있는 기존 복셀을 모두 제거합니다.
        /// </summary>
        public void ClearVoxels (byte light) {
            if (lightSources != null) {
                int lightSourcesCount = lightSources.Count;
                for (int k = lightSourcesCount - 1; k >= 0; k--) {
                    LightSource ls = lightSources[k];
                    if (ls.gameObject != null) {
                        Misc.DestroySafely(ls.gameObject);
                    }
                }
                lightSources.Clear();
            }
            if (placeholders != null) {
                int phCount = placeholders.Count;
                for (int k = 0; k < phCount; k++) {
                    if (placeholders.entries[k].key >= 0) {
                        VoxelPlaceholder ph = placeholders.entries[k].value;
                        if (ph != null) {
                            Misc.DestroySafely(ph.gameObject);
                        }
                    }
                }
                placeholders.Clear();
            }
            if (voxelsExtraData != null) {
                voxelsExtraData.Clear();
            }
            if (voxelsProperties != null) {
                voxelsProperties.Clear();
            }
            if (microVoxels != null) {
                microVoxels.Clear();
            }
            usesMicroVoxels = false;

            Voxel.Clear(voxels, light);
            lightmapIsClear = true;
        }

        /// <summary>
        /// 단일 복셀을 지웁니다.
        /// </summary>
        /// <param name="voxelIndex">청크의 복셀 인덱스</param>
        /// <param name="light">빈 위치에 남은 광량</param>
        public void ClearVoxel (int voxelIndex, byte light) {
            if (lightSources != null) {
                int lightSourcesCount = lightSources.Count;
                for (int k = lightSourcesCount - 1; k >= 0; k--) {
                    LightSource ls = lightSources[k];
                    if (ls.voxelIndex == voxelIndex) {
                        if (ls.gameObject != null) {
                            Misc.DestroySafely(ls.gameObject);
                        }
                        lightSources.RemoveAt(k);
                    }
                }
            }
            if (placeholders != null) {
                if (placeholders.TryGetValue(voxelIndex, out VoxelPlaceholder ph)) {
                    if (ph != null && ph.voxelIndex == voxelIndex) {
                        Misc.DestroySafely(ph.gameObject);
                    }
                    placeholders.Remove(voxelIndex);
                }
            }
            if (voxelsExtraData != null) {
                voxelsExtraData.Remove(voxelIndex);
            }
            if (voxelsProperties != null) {
                voxelsProperties.Remove(voxelIndex);
            }
            ClearMicroVoxels(voxelIndex);
            voxels[voxelIndex].Clear(light);
        }

        /// <summary>
        /// 이웃에 대한 링크를 설정합니다.
        /// </summary>
        public void ComputeNeighbours () {
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;

            Vector3d topPosition = position;
            topPosition.y += VoxelPlayEnvironment.CHUNK_SIZE;
            if (env.GetChunk(topPosition, out top, false)) {
                top.bottom = this;
            }

            Vector3d bottomPosition = position;
            bottomPosition.y -= VoxelPlayEnvironment.CHUNK_SIZE;
            if (env.GetChunk(bottomPosition, out bottom, false)) {
                bottom.top = this;
            }

            Vector3d leftPosition = position;
            leftPosition.x -= VoxelPlayEnvironment.CHUNK_SIZE;
            if (env.GetChunk(leftPosition, out left, false)) {
                left.right = this;
            }

            Vector3d rightPosition = position;
            rightPosition.x += VoxelPlayEnvironment.CHUNK_SIZE;
            if (env.GetChunk(rightPosition, out right, false)) {
                right.left = this;
            }

            Vector3d forwardPosition = position;
            forwardPosition.z += VoxelPlayEnvironment.CHUNK_SIZE;
            if (env.GetChunk(forwardPosition, out forward, false)) {
                forward.back = this;
            }

            Vector3d backPosition = position;
            backPosition.z -= VoxelPlayEnvironment.CHUNK_SIZE;
            if (env.GetChunk(backPosition, out back, false)) {
                back.forward = this;
            }
        }


        /// <summary>
        /// 이 청크가 월드 공간에서 주어진 위치를 포함하는 경우 true를 반환합니다.
        /// </summary>
        public bool Contains (Vector3d position) {
            double xDiff = position.x - this.position.x;
            double yDiff = position.y - this.position.y;
            double zDiff = position.z - this.position.z;
            return (xDiff <= (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1) && xDiff >= -VoxelPlayEnvironment.CHUNK_HALF_SIZE && yDiff <= (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1) && yDiff >= -VoxelPlayEnvironment.CHUNK_HALF_SIZE && zDiff <= (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1) && zDiff >= -VoxelPlayEnvironment.CHUNK_HALF_SIZE);
        }


        /// <summary>
        /// 풀로 반환하기 전에 청크 상태를 지웁니다.이 청크가 재사용될 때 이 메서드가 호출됩니다.
        /// </summary>
        public void PrepareForReuse (byte light) {
            isAboveSurface = true;
            needsMeshRebuild = false;
            isPopulated = false;
            inqueue = false;
            modified = false;
            modifiedTimestamp = 0;
            renderState = ChunkRenderState.Pending;
            allowTrees = true;
            frustumCheckIteration = 0;
            isDirty = false;
            renderingFrame = -1;
            visibleDistanceStatus = ChunkVisibleDistanceStatus.Unknown;
            needsLightmapRebuild = false;
            usesMicroVoxels = false;

            if (items != null) {
                for (int k = 0; k < items.count; k++) {
                    Item item = items.values[k];
                    if (item != null && item.gameObject != null) {
                        Misc.DestroySafely(item.gameObject);
                    }
                }
                items.Clear();
            }

            if (microVoxels != null) {
                microVoxels.Clear();
            }

            if ((object)left != null) {
                left.right = null;
                left = null;
            }
            if ((object)right != null) {
                right.left = null;
                right = null;
            }
            if ((object)forward != null) {
                forward.back = null;
                forward = null;
            }
            if ((object)back != null) {
                back.forward = null;
                back = null;
            }
            if ((object)top != null) {
                top.bottom = null;
                top = null;
            }
            if ((object)bottom != null) {
                bottom.top = null;
                bottom = null;
            }
            ClearVoxels(light);
            mr.enabled = false;
            if (mc != null) mc.enabled = false;
            gameObject.SetActive(true);
        }

        public void RemoveItem (Item item) {
            if (items != null) {
                if (items.Remove(item)) {
                    VoxelPlayEnvironment.instance.RegisterChunkChanges(this);
                }
            }
        }

        public void AddItem (Item item) {
            if (items == null) {
                items = new FastList<Item>();
            }
            items.Add(item);
            VoxelPlayEnvironment.instance.RegisterChunkChanges(this);
        }

        public override string ToString () {
            return string.Format("[VoxelChunk: x={0}, y={1}, zm={2}]", position.x, position.y, position.z);
        }

        public LightSource GetLightSource (int voxelIndex) {
            if (lightSources == null) return null;
            int lsCount = lightSources.Count;
            for (int k = 0; k < lsCount; k++) {
                LightSource ls = lightSources[k];
                if (ls.voxelIndex == voxelIndex) {
                    return ls;
                }
            }
            return null;
        }

        public void AddLightSource (LightSource ls) {
            if (lightSources == null) {
                lightSources = new List<LightSource>();
            }
            lightSources.Add(ls);
        }

        public void RemoveLightSource (int voxelIndex) {
            int count = lightSources.Count;
            for (int k = count - 1; k >= 0; k--) {
                if (lightSources[k].voxelIndex == voxelIndex) {
                    lightSources.RemoveAt(k);
                }
            }
        }

        public void AddLightSource (int voxelIndex, byte lightIntensity) {
            // Check if current light intensity is lower
            if (lightSources != null) {
                int count = lightSources.Count;
                for (int k = 0; k < count; k++) {
                    LightSource l = lightSources[k];
                    if (l.voxelIndex == voxelIndex) {
                        if (l.lightIntensity < lightIntensity) {
                            l.lightIntensity = lightIntensity;
                        }
                        return;
                    }
                }
            }
            LightSource ls = new LightSource();
            ls.voxelIndex = voxelIndex;
            ls.lightIntensity = lightIntensity;
            AddLightSource(ls);
        }

        /// <summary>
        /// 주어진 voxelDefinition을 사용하여 이 청크에 복셀을 설정합니다.이 메서드는 복셀이 빛을 방출하는 경우 이에 따라 라이트맵을 업데이트합니다.
        /// </summary>
        public void SetVoxel (int voxelIndex, VoxelDefinition voxelDefinition) {
            SetVoxel(voxelIndex, voxelDefinition, Misc.color32White);
        }

        /// <summary>
        /// 주어진 voxelDefinition 및 색조 색상을 사용하여 이 청크에 복셀을 설정합니다.이 메서드는 복셀이 빛을 방출하는 경우 이에 따라 라이트맵을 업데이트합니다.
        /// </summary>
        public void SetVoxel (int voxelIndex, VoxelDefinition voxelDefinition, Color32 tintColor) {
            voxels[voxelIndex].Set(voxelDefinition, tintColor);
            if (voxelDefinition.lightIntensity > 0) {
                AddLightSource(voxelIndex, voxelDefinition.lightIntensity);
                VoxelPlayEnvironment.instance.SetTorchLightmap(this, voxelIndex, voxelDefinition.lightIntensity);
            }
            if (usesMicroVoxels) {
                microVoxels.Remove(voxelIndex);
                if (microVoxels.Count == 0) {
                    usesMicroVoxels = false;
                }
            }
        }

        /// <summary>
        /// 이 청크의 모든 복셀을 주어진 복셀 배열로 대체합니다.
        /// </summary>
        /// <param name="voxels">설정할 복셀 배열</param>
        public void SetVoxels (Voxel[] voxels) {
            Array.Copy(voxels, this.voxels, this.voxels.Length);
        }

    }


}