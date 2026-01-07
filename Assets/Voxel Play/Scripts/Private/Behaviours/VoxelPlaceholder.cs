using System;
using System.Runtime.CompilerServices;
using System.Collections;
using UnityEngine;

namespace VoxelPlay {

    public class VoxelPlaceholder : MonoBehaviour {

        [NonSerialized]
        public int resistancePointsLeft;

        [NonSerialized]
        public Renderer damageIndicator;

        [NonSerialized]
        public VoxelChunk chunk;

        [NonSerialized]
        public int voxelIndex;

        [NonSerialized]
        public GameObject modelTemplate;

        [NonSerialized]
        public GameObject modelInstance;

        /// <summary>
        /// 자리 표시자가 생성될 때의 기본 경계
        /// </summary>
        [NonSerialized]
        public Bounds bounds;

        /// <summary>
        /// 자리 표시자가 생성되었을 때 회전을 저장합니다.복셀이 회전되었는지 감지하고 그에 따라 자리 표시자를 업데이트하는 데 사용됩니다.
        /// </summary>
        [NonSerialized]
        public float currentRotationDegrees;


        /// <summary>
        /// 렌더러가 일부 스크립트에 의해 위치나 크기를 변경했을 수 있다는 점을 고려한 월드 공간의 현재 경계
        /// </summary>
        public Bounds GetWorldSpaceBounds() {
            Vector3 center, size;
            if (modelMeshRenderers != null && modelMeshRenderers.Length > 0 && modelMeshRenderers[0] != null) {
                center = modelMeshRenderers[0].bounds.center;
                size = modelMeshRenderers[0].bounds.size;
            } else {
                center = transform.position + bounds.center;
                size = bounds.size;
            }
            return new Bounds(center, size);
        }

        public struct ModelParts {
            public MeshFilter meshFilter;
            /// <summary>
            /// 부드러운 조명으로 인해 색상을 조정하기 위해 인스턴스화되는 경우 사용자 정의 복셀 프리팹의 원본 메시를 참조합니다.
            /// </summary>
            public Mesh originalMesh;
            /// <summary>
            /// 이 위치에서 Miv를 렌더링할 때 마지막으로 계산된 색조 색상
            /// </summary>
            public Color32 lastMivTintColor; 
        }

        [NonSerialized]
        public ModelParts[] parts;


        public void ResetLastMivTintColors() {
            if (parts == null) return;
            int partsCount = parts.Length;
            for (int k=0;k<partsCount;k++) {
                parts[k].lastMivTintColor = Misc.color32White;
            }
        }

        /// <summary>
        /// 이 인스턴스의 첫 번째 메시 필터에 대한 참조를 반환합니다.
        /// </summary>
        public MeshFilter modelMeshFilter {
            get {
                if (parts == null) return null;
                return parts[0].meshFilter;
            }
        }

        /// <summary>
        /// 이 인스턴스의 첫 번째 메시 렌더러에 대한 참조를 반환합니다.
        /// </summary>
        public MeshRenderer modelMeshRenderer {
            get {
                if (modelMeshRenderers == null) return null;
                return modelMeshRenderers[0];
            }
        }

        [NonSerialized]
        public MeshRenderer[] modelMeshRenderers;

        [NonSerialized]
        public Rigidbody rb;

        public Material damageIndicatorMaterial {
            get {
                if (_damageIndicatorMaterial == null && damageIndicator != null) {
                    _damageIndicatorMaterial = Instantiate(damageIndicator.sharedMaterial);
                    damageIndicator.sharedMaterial = _damageIndicatorMaterial;
                }
                return _damageIndicatorMaterial;
            }
        }


        float recoveryTime;
        Material _damageIndicatorMaterial;
        bool[] prevEnableState;

        void OnDestroy() {
            CancelInvoke(nameof(Recover));
            StopAllCoroutines();
        }

        public void StartHealthRecovery(float damageDuration) {
            recoveryTime = Time.time + damageDuration;
            CancelInvoke(nameof(Recover));
            Invoke(nameof(Recover), damageDuration + 0.1f);
        }

        void Recover() {
            float time = Time.time;
            if (time >= recoveryTime) {
                if (chunk != null && chunk.voxels[voxelIndex].typeIndex != 0) {
                    resistancePointsLeft = chunk.voxels[voxelIndex].type.resistancePoints;
                }
                if (damageIndicator != null) {
                    damageIndicator.enabled = false;
                }
            }
        }


        public void SetAutoCancelDynamic(float delay) {
            Invoke(nameof(CancelDynamic), delay + UnityEngine.Random.value);
        }

        public void CancelDynamic() {
            if (this != null && isActiveAndEnabled) {
                StartCoroutine(Consolidate());
            }
        }

        public void CancelDynamicNow() {
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null || gameObject.Equals(null))
                return;
            env.VoxelCancelDynamic(this);
        }

        IEnumerator Consolidate() {
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null || gameObject.Equals(null))
                yield break;
            if (env.GetChunk(transform.position, out VoxelChunk targetChunk, false)) {
                const float maxDist = 100 * 100;
                if (env == null || gameObject.Equals(null) || env.cameraMain == null)
                    yield break;
                while (env.cameraMain != null && FastVector.SqrDistanceByValue((Vector3)targetChunk.position, env.cameraMain.transform.position) < maxDist && env.ChunkIsInFrustum(targetChunk)) {
                    yield return Misc.waitForOneSecond;
                }
                env.VoxelCancelDynamic(this);
            }
        }

        /// <summary>
        /// 다른 시스템에서 설정한 가시성을 유지하면서 렌더러 가시성을 토글합니다.
        /// </summary>
        /// <param name="enabled"></param>
        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public void ToggleRenderers(bool enabled) {
            if (modelMeshRenderers == null) return;

            int renderersCount = modelMeshRenderers.Length;
            if (prevEnableState == null || prevEnableState.Length < renderersCount) {
                prevEnableState = new bool[renderersCount];
            }
            for (int j = 0; j < renderersCount; j++) {
                if (modelMeshRenderers[j] != null) {
                    if (enabled) {
                        modelMeshRenderers[j].enabled = prevEnableState[j];
                    } else {
                        prevEnableState[j] = modelMeshRenderers[j].enabled;
                        modelMeshRenderers[j].enabled = false;
                    }
                }
            }
        }


    }
}