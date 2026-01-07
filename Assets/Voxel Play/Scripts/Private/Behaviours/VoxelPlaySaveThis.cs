using System.Collections;
using UnityEngine;

namespace VoxelPlay
{

    public class VoxelPlaySaveThis : MonoBehaviour {

        /// <summary>
        /// 프리팹의 경로입니다(예: "세계/지구/모델/사슴").
        /// </summary>
        [Tooltip("리소스 폴더의 프리팹 경로")]
        public string prefabResourcesPath;

        VoxelPlayEnvironment env;
        Rigidbody rb;

        void Start() {
            if (!TryGetComponent(out rb) || rb.isKinematic) return;

            // If chunk is not rendered and have a rigidbody, wait until ready
            env = VoxelPlayEnvironment.instance;
            if (env == null) return;

            if (!env.GetChunk(transform.position, out VoxelChunk chunk, false) || !chunk.isRendered) {
                rb.isKinematic = true;
                StartCoroutine(WaitForChunk(chunk));
            }
        }

        IEnumerator WaitForChunk(VoxelChunk chunk) {
            while (chunk != null && !chunk.isRendered) {
                if (gameObject == null) yield break;
                yield return null;
            }
            if (rb != null) {
                rb.isKinematic = false;
            }
        }
    }
}
